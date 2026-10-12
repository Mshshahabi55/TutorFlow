using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;

namespace TutorFlow.Modules.Availability.Features.ReplaceWeeklyRules;

public static class ReplaceWeeklyRules
{
    public sealed record RuleInput(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

    public sealed record Command(IReadOnlyList<RuleInput> Rules);

    public sealed record RuleResponse(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

    public sealed record Response(IReadOnlyList<RuleResponse> Rules);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleForEach(x => x.Rules).ChildRules(rule =>
            {
                rule.RuleFor(r => r.StartTime)
                    .LessThan(r => r.EndTime)
                    .WithMessage("A weekly rule's start time must be before its end time.");
            });
        }
    }

    internal sealed class Handler(AvailabilityDbContext db, AvailabilitySignalPublisher signalPublisher)
    {
        public async Task<Result<Response>> HandleAsync(Guid tutorUserId, Command command, CancellationToken ct)
        {
            List<WeeklyAvailabilityRule> newRules = [];
            foreach (var input in command.Rules)
            {
                var createResult = WeeklyAvailabilityRule.Create(tutorUserId, input.DayOfWeek, input.StartTime, input.EndTime);
                if (createResult.IsFailure)
                {
                    return Result.Failure<Response>([.. createResult.Errors]);
                }

                newRules.Add(createResult.Value);
            }

            var overlapResult = WeeklyAvailabilityRule.ValidateNoOverlaps(newRules);
            if (overlapResult.IsFailure)
            {
                return Result.Failure<Response>([.. overlapResult.Errors]);
            }

            // Serializable isolation is the smallest robust guard against two concurrent
            // replaces for the same tutor: plain read-then-write (even inside one SaveChanges
            // transaction) can't detect two requests that both see "no existing rules" and both
            // insert — that's a classic write-skew/phantom anomaly, which only serializable
            // snapshot isolation catches. No new schema/column is needed for this.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            try
            {
                var existing = await db.WeeklyAvailabilityRules.Where(r => r.TutorUserId == tutorUserId).ToListAsync(ct);
                db.WeeklyAvailabilityRules.RemoveRange(existing);
                db.WeeklyAvailabilityRules.AddRange(newRules);

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex) when (IsSerializationConflict(ex))
            {
                // Don't call transaction.RollbackAsync() here: a serialization failure from
                // Postgres already leaves the underlying Npgsql transaction in a completed
                // state, and explicitly rolling back a transaction in that state throws. The
                // `await using` above disposes it safely either way.
                throw new WeeklyRulesConflictException("Weekly availability rules were modified concurrently; please retry.");
            }

            await signalPublisher.PublishCurrentSignalAsync(tutorUserId, ct);

            var response = new Response([.. newRules.Select(r => new RuleResponse(r.Id, r.DayOfWeek, r.StartTime, r.EndTime))]);
            return Result.Success(response);
        }

        // EF Core's execution strategy wraps serialization-failure/deadlock errors (but not,
        // e.g., unique-violations — those aren't classified as transient) in an extra
        // InvalidOperationException when they occur inside a manually-managed explicit
        // transaction, so the real PostgresException can be nested two levels deep.
        private static bool IsSerializationConflict(Exception ex)
        {
            for (var current = ex; current is not null; current = current.InnerException)
            {
                if (current is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected })
                {
                    return true;
                }
            }

            return false;
        }
    }
}
