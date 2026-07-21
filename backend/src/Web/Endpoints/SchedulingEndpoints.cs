using TutorFlow.Application.Authorization;
using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Web.Authorization;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only — see IdentityEndpoints.
public static class SchedulingEndpoints
{
    private const string Tag = "Scheduling";

    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/availability-slots", async (
            DeclareAvailabilityCommand command,
            DeclareAvailabilityCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(DeclareAvailabilityCommandHandler)))
            .WithApiResultMetadata<AvailabilitySlotDto>("DeclareAvailability", Tag, "Declares a new Availability Slot for a Tutor.")
            .RequirePermission(Permission.DeclareAvailability);

        app.MapPost("/sessions", async (
            BookSessionCommand command,
            BookSessionCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(BookSessionCommandHandler)))
            .WithApiResultMetadata<SessionDto>("BookSession", Tag, "Books a Session against an open Availability Slot.")
            .RequirePermission(Permission.BookSession);

        app.MapPost("/sessions/{sessionId:guid}/reschedule", async (
            Guid sessionId,
            RescheduleSessionRequest request,
            RescheduleSessionCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(
                new RescheduleSessionCommand(sessionId, request.NewScheduledTimeUtc),
                cancellationToken))
                .ToApiResult(logger, nameof(RescheduleSessionCommandHandler)))
            .WithApiResultMetadata("RescheduleSession", Tag, "Reschedules a Scheduled Session to a new time.")
            .RequirePermission(Permission.RescheduleSession);

        app.MapPost("/sessions/{sessionId:guid}/cancel", async (
            Guid sessionId,
            CancelSessionCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new CancelSessionCommand(sessionId), cancellationToken))
                .ToApiResult(logger, nameof(CancelSessionCommandHandler)))
            .WithApiResultMetadata("CancelSession", Tag, "Cancels a Scheduled Session.")
            .RequirePermission(Permission.CancelSession);

        app.MapPost("/sessions/{sessionId:guid}/complete", async (
            Guid sessionId,
            CompleteSessionCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new CompleteSessionCommand(sessionId), cancellationToken))
                .ToApiResult(logger, nameof(CompleteSessionCommandHandler)))
            .WithApiResultMetadata("CompleteSession", Tag, "Marks a Scheduled Session as Completed.")
            .RequirePermission(Permission.CompleteSession);

        app.MapPost("/sessions/{sessionId:guid}/no-show", async (
            Guid sessionId,
            MarkSessionNoShowCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new MarkSessionNoShowCommand(sessionId), cancellationToken))
                .ToApiResult(logger, nameof(MarkSessionNoShowCommandHandler)))
            .WithApiResultMetadata("MarkSessionNoShow", Tag, "Marks a Scheduled Session as No-Show.")
            .RequirePermission(Permission.MarkSessionNoShow);

        app.MapGet("/availability-slots/{availabilitySlotId:guid}", async (
            Guid availabilitySlotId,
            GetAvailabilitySlotByIdQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetAvailabilitySlotByIdQuery(availabilitySlotId), cancellationToken))
                .ToApiResult(logger, nameof(GetAvailabilitySlotByIdQueryHandler)))
            .WithApiResultMetadata<AvailabilitySlotDto>("GetAvailabilitySlotById", Tag, "Fetches a single Availability Slot by id.");

        app.MapGet("/sessions/{sessionId:guid}", async (
            Guid sessionId,
            GetSessionByIdQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetSessionByIdQuery(sessionId), cancellationToken))
                .ToApiResult(logger, nameof(GetSessionByIdQueryHandler)))
            .WithApiResultMetadata<SessionDto>("GetSessionById", Tag, "Fetches a single Session by id.");

        app.MapGet("/students/{studentId:guid}/schedule", async (
            Guid studentId,
            GetStudentScheduleQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetStudentScheduleQuery(studentId), cancellationToken))
                .ToApiResult(logger, nameof(GetStudentScheduleQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<SessionDto>>("GetStudentSchedule", Tag, "Lists every Session for a Student.");

        app.MapGet("/tutors/{tutorId:guid}/schedule", async (
            Guid tutorId,
            GetTutorScheduleQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetTutorScheduleQuery(tutorId), cancellationToken))
                .ToApiResult(logger, nameof(GetTutorScheduleQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<SessionDto>>("GetTutorSchedule", Tag, "Lists every Session for a Tutor.");

        app.MapGet("/tutors/{tutorId:guid}/availability-slots", async (
            Guid tutorId,
            GetTutorAvailabilitySlotsQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetTutorAvailabilitySlotsQuery(tutorId), cancellationToken))
                .ToApiResult(logger, nameof(GetTutorAvailabilitySlotsQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<AvailabilitySlotDto>>(
                "GetTutorAvailabilitySlots", Tag, "Lists every Availability Slot for a Tutor (serves both List and Calendar views).");

        return app;
    }

    // Minimal request shape needed only because RescheduleSessionCommand's
    // SessionId is bound from the route, not the body — this is the
    // "map request → Command" step, not business logic.
    internal sealed record RescheduleSessionRequest(DateTime NewScheduledTimeUtc);
}
