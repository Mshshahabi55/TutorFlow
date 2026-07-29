namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetHourlyRate + Tutor.SetTrialLesson (docs/adr/ADR-024,
// Accepted — Tutor Onboarding Wizard Step 4: Pricing). HourlyRate is
// optional here (null = leave unchanged) since the wizard's Pricing step
// may be revisited after the rate was already set by the existing
// hourly-rate endpoint; TrialLessonPrice follows the same single-currency
// (Rial/Toman) representation as HourlyRate — no currency field (ADR-019).
public sealed record SetTutorPricingCommand(
    Guid TutorId,
    decimal? HourlyRateAmount,
    bool TrialLessonAvailable,
    decimal? TrialLessonPriceAmount);
