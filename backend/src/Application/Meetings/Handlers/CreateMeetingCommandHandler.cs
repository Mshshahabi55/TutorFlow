using TutorFlow.Application.Common;
using TutorFlow.Application.Meetings.Commands;
using TutorFlow.Application.Meetings.DTOs;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Application.Meetings.Validators;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Meetings.Handlers;

// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md: "Start
// Lesson" — the only way a Meeting comes into existence, always the
// Session's own Tutor, always idempotent (a repeat call returns the
// already-created Meeting rather than a duplicate).
public sealed class CreateMeetingCommandHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingProviderResolver _providerResolver;
    private readonly IMeetingProviderSettingsCatalog _settingsCatalog;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMeetingCommandHandler(
        ISessionRepository sessionRepository,
        IMeetingRepository meetingRepository,
        IMeetingProviderResolver providerResolver,
        IMeetingProviderSettingsCatalog settingsCatalog,
        ICurrentUserProvider currentUserProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _meetingRepository = meetingRepository;
        _providerResolver = providerResolver;
        _settingsCatalog = settingsCatalog;
        _currentUserProvider = currentUserProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MeetingDto>> Handle(CreateMeetingCommand command, CancellationToken cancellationToken = default)
    {
        var validation = CreateMeetingCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<MeetingDto>(validation.Error);
        }

        var sessionId = SessionId.From(command.SessionId);
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<MeetingDto>(new Error(
                "CreateMeetingCommand.SessionNotFound", "Session was not found.", ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyOwnTutorId(session.TutorId.Value, "CreateMeetingCommand.Forbidden") is { } ownershipError)
        {
            return Result.Failure<MeetingDto>(ownershipError);
        }

        if (session.Status != SessionStatus.Scheduled)
        {
            return Result.Failure<MeetingDto>(new Error(
                "CreateMeetingCommand.SessionNotScheduled",
                "A meeting can only be started for a Scheduled session.",
                ErrorType.Domain));
        }

        if (session.DeliveryMode != DeliveryMode.Online)
        {
            return Result.Failure<MeetingDto>(new Error(
                "CreateMeetingCommand.SessionNotOnline",
                "This session's delivery mode is In-Person — no online meeting is needed.",
                ErrorType.Domain));
        }

        var existing = await _meetingRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(MeetingDto.FromDomain(existing));
        }

        var provider = ResolveProviderFor(session.TutorId);
        if (!_settingsCatalog.IsConfigured(provider))
        {
            return Result.Failure<MeetingDto>(new Error(
                "CreateMeetingCommand.ProviderNotConfigured",
                "This meeting provider isn't configured yet. Ask an administrator to finish setup.",
                ErrorType.Infrastructure));
        }

        var now = _dateTimeProvider.UtcNow;
        ProviderMeetingResult providerResult;
        try
        {
            providerResult = await _providerResolver.Resolve(provider).CreateMeetingAsync(
                new CreateProviderMeetingRequest(sessionId, session.ScheduledTimeUtc, session.EndTimeUtc), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result.Failure<MeetingDto>(new Error(
                "CreateMeetingCommand.ProviderCallFailed",
                "Couldn't create the meeting with the configured provider. Please try again.",
                ErrorType.Infrastructure));
        }

        var meeting = Meeting.Create(
            sessionId,
            provider,
            providerResult.ProviderMeetingId,
            providerResult.JoinUrl,
            providerResult.HostUrl,
            session.ScheduledTimeUtc,
            session.EndTimeUtc,
            now);

        await _meetingRepository.AddAsync(meeting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { meeting }, cancellationToken);

        return Result.Success(MeetingDto.FromDomain(meeting));
    }

    // docs/adr/ADR-023-...'s "Provider selector": designed for a future
    // per-Tutor override without building one yet — this is the one and
    // only method a future phase changes (to read a real per-Tutor value),
    // with no change to IMeetingProvider, IMeetingProviderResolver, the
    // Meeting aggregate, or any endpoint. tutorId is intentionally unused
    // today.
    private MeetingProviderOption ResolveProviderFor(TutorId tutorId) => _settingsCatalog.DefaultProvider;
}
