using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Messaging.Domain;
using TutorFlow.Modules.Messaging.Persistence;

namespace TutorFlow.Modules.Messaging.Features.SendMessage;

public static class SendMessage
{
    // No sender field — SenderUserId is never read from a request body, only ever derived from
    // the authenticated principal at the endpoint layer (ADR-0030).
    public sealed record Command(string Text);

    public sealed record Response(Guid Id, Guid SenderUserId, string Body, DateTimeOffset CreatedAtUtc, long SequenceNumber);

    public enum Outcome
    {
        Sent,
        NotFound
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Text).NotEmpty().WithMessage("Message body must not be empty or whitespace-only.");
            RuleFor(x => x.Text).MaximumLength(5000);
        }
    }

    internal sealed class Handler(MessagingDbContext db, TimeProvider clock)
    {
        // NotFound covers both "no such conversation" and "caller is not a participant" — same
        // uniform-404 rule as GetMessages (ADR-0030).
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(
            Guid callerUserId, Guid conversationId, Command command, CancellationToken ct)
        {
            var conversation = await db.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => new { c.StudentUserId, c.TutorUserId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null || (conversation.StudentUserId != callerUserId && conversation.TutorUserId != callerUserId))
            {
                return (Outcome.NotFound, null);
            }

            var message = Message.Create(conversationId, callerUserId, command.Text.Trim(), clock);
            db.Messages.Add(message);
            await db.SaveChangesAsync(ct);

            // Best-effort activity-timestamp update, deliberately not wrapped in a transaction
            // with the insert above (ADR-0030): if this second statement fails, the message
            // itself is already real and visible — only the conversation list's sort position
            // would be mildly stale, a cosmetic degradation, never a correctness one.
            await db.Conversations
                .Where(c => c.Id == conversationId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastMessageAtUtc, clock.GetUtcNow()), ct);

            return (Outcome.Sent, new Response(message.Id, message.SenderUserId, message.Body, message.CreatedAtUtc, message.SequenceNumber));
        }
    }
}
