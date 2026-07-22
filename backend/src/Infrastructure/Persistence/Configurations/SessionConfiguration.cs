using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(new ValueConverter<SessionId, Guid>(id => id.Value, value => SessionId.From(value)))
            .ValueGeneratedNever();

        builder.Property(s => s.TutorId)
            .HasConversion(new ValueConverter<TutorId, Guid>(id => id.Value, value => TutorId.From(value)));

        builder.Property(s => s.StudentId)
            .HasConversion(new ValueConverter<StudentId, Guid>(id => id.Value, value => StudentId.From(value)));

        builder.Property(s => s.ParentGuardianId)
            .HasConversion(new ValueConverter<ParentGuardianId?, Guid?>(
                id => id == null ? null : id.Value,
                value => value == null ? null : ParentGuardianId.From(value.Value)));

        builder.Property(s => s.AvailabilitySlotId)
            .HasConversion(new ValueConverter<AvailabilitySlotId, Guid>(id => id.Value, value => AvailabilitySlotId.From(value)));

        // ADR-014: the physical, storage-layer enforcement of CONST-1. At
        // most one *live* Session may reference a given Availability Slot;
        // a second concurrent booking attempt's INSERT is rejected by the
        // database itself, independent of the in-memory check-then-act
        // race. Filtered (Phase 4.6, DOMAIN_MODEL.md Open Question 7
        // resolved) to exclude Cancelled (2): cancelling a Session reopens
        // its slot (AvailabilitySlot.Reopen()), so a slot may accumulate
        // any number of Cancelled Session rows over time — the constraint
        // only needs to prevent two *live* (Scheduled/Completed/NoShow)
        // Sessions from ever referencing the same slot at once, which is
        // the actual invariant CONST-1 protects. Completed and NoShow
        // deliberately remain inside the constraint (still "live" in this
        // sense) — see docs/phases/PHASE-046-REPORT.md Section 6 for why
        // only Cancelled was decided to reopen a slot, not those two.
        builder.HasIndex(s => s.AvailabilitySlotId)
            .IsUnique()
            .HasFilter("\"Status\" <> 2");

        // Same-bounded-context referential integrity (Session and
        // AvailabilitySlot are both owned by Scheduling & Booking, so a real
        // FK is appropriate — unlike TutorId/StudentId/ParentGuardianId,
        // which cross into Identity & Relationship and are deliberately left
        // as plain columns per ADR-002's context-isolation rule). Restrict,
        // not Cascade: whether any entity may ever be deleted is unresolved
        // (DOMAIN_DATA_MODEL.md Open Question 11) — Restrict avoids
        // orphaning a Session without inventing a deletion policy.
        builder.HasOne<AvailabilitySlot>()
            .WithMany()
            .HasForeignKey(s => s.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lookup-query support for ISessionRepository.GetByTutorIdAsync /
        // GetByStudentIdAsync.
        builder.HasIndex(s => s.TutorId);
        builder.HasIndex(s => s.StudentId);

        builder.Property(s => s.ScheduledTimeUtc);

        builder.Property(s => s.Duration)
            .HasConversion(new ValueConverter<SessionDuration, TimeSpan>(d => d.Value, value => SessionDuration.Of(value)));

        builder.Ignore(s => s.EndTimeUtc);

        builder.Property(s => s.DeliveryMode);

        builder.Property(s => s.Status);

        // Phase 4.6: what this Session cost, captured at booking time.
        // Nullable — mirrors Tutor.HourlyRate's own nullability exactly
        // (see Session.Price's own doc comment) — and existing rows (none
        // in tutorflow_dev; 34 disposable test rows in tutorflow_test as of
        // this migration) get NULL, which is not a guessed default but an
        // honest "this historical fact was never recorded" marker, the
        // same reasoning that already makes HourlyRate itself nullable.
        // Precision matches HourlyRate/RialAmount's own numeric(12,0)
        // exactly, for the identical reason (ADR-019 Addendum 1).
        builder.Property(s => s.Price)
            .HasConversion(new ValueConverter<SessionPrice?, decimal?>(
                price => price == null ? null : price.Amount,
                amount => amount == null ? null : SessionPrice.Of(amount.Value)))
            .HasPrecision(12, 0);

        builder.Ignore(s => s.DomainEvents);
    }
}
