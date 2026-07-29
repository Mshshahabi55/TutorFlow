import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TutorOnboardingWizardPage } from "@/features/identity/pages/TutorOnboardingWizardPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import type { TutorDto } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

const BASE_TUTOR: TutorDto = {
  tutorId: TUTOR_ID,
  isApproved: false,
  isSuspended: false,
  isDiscoverable: false,
  hourlyRate: null,
  subject: null,
  language: null,
  location: null,
  offeredDurations: [],
  profileStatus: "Draft",
  displayName: null,
  headline: null,
  biography: null,
  country: null,
  city: null,
  otherLanguages: [],
  tutorSubjects: [],
  yearsOfExperience: null,
  education: null,
  certifications: null,
  teachingMethodology: null,
  lessonSpecialties: [],
  photoUrl: null,
  introVideoUrl: null,
  galleryImageUrls: [],
  trialLessonAvailable: false,
  trialLessonPrice: null,
};

function renderWizard() {
  return renderWithProviders(<TutorOnboardingWizardPage />, {
    initialEntries: [`/identity/tutors/${TUTOR_ID}/onboarding`],
    routePath: "/identity/tutors/:tutorId/onboarding",
  });
}

describe("TutorOnboardingWizardPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(BASE_TUTOR);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([]);
    // Every step's own autosave call — mocked resolved by default so any
    // test can navigate via "Continue" regardless of which step it cares
    // about; individual tests re-spy where they need to assert call args.
    vi.spyOn(identityService, "setTutorPersonalInfo").mockResolvedValue(undefined);
    vi.spyOn(identityService, "setTutorLanguage").mockResolvedValue(undefined);
    vi.spyOn(identityService, "setTutorTeachingInfo").mockResolvedValue(undefined);
    vi.spyOn(identityService, "setTutorSubject").mockResolvedValue(undefined);
    vi.spyOn(identityService, "setTutorMedia").mockResolvedValue(undefined);
    vi.spyOn(identityService, "setTutorPricing").mockResolvedValue(undefined);
    vi.spyOn(identityService, "submitTutorProfile").mockResolvedValue(undefined);
  });

  it("shows a Welcome screen before Step 1 for a Tutor who has never opened the wizard", async () => {
    renderWizard();

    expect(await screen.findByRole("heading", { name: /Welcome/i })).toBeInTheDocument();
    expect(screen.queryByLabelText("Display name")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Get started" }));

    expect(await screen.findByLabelText("Display name")).toBeInTheDocument();
  });

  it("shows the Personal Information step first, with a step-by-step progress indicator", async () => {
    renderWizard();

    await userEvent.click(await screen.findByRole("button", { name: "Get started" }));

    expect(await screen.findByLabelText("Display name")).toBeInTheDocument();
    // "Personal Information" (the active step) renders twice — once in the
    // desktop Stepper, once in the mobile-only progress heading; CSS
    // controls which is visible per viewport, jsdom renders both.
    expect(screen.getAllByText("Personal Information")).toHaveLength(2);
    expect(screen.getByText("Teaching Information")).toBeInTheDocument();
    expect(screen.getByText("Review & Publish")).toBeInTheDocument();
  });

  it("autosaves personal info (and the existing native-language field) and advances on Continue", async () => {
    const setPersonalInfo = vi.spyOn(identityService, "setTutorPersonalInfo").mockResolvedValue(undefined);
    const setLanguage = vi.spyOn(identityService, "setTutorLanguage").mockResolvedValue(undefined);

    renderWizard();

    await userEvent.click(await screen.findByRole("button", { name: "Get started" }));
    await userEvent.type(await screen.findByLabelText("Display name"), "Jane Doe");
    await userEvent.type(screen.getByLabelText("Native language"), "English");
    await userEvent.click(screen.getByRole("button", { name: "Continue" }));

    expect(setPersonalInfo).toHaveBeenCalledWith(
      TUTOR_ID,
      expect.objectContaining({ displayName: "Jane Doe" }),
    );
    expect(setLanguage).toHaveBeenCalledWith(TUTOR_ID, "English");
    expect(await screen.findByLabelText("Primary subject")).toBeInTheDocument();
  });

  it("adds and removes an additional subject row", async () => {
    renderWizard();

    await userEvent.click(await screen.findByRole("button", { name: "Get started" }));
    await userEvent.click(await screen.findByRole("button", { name: "Continue" }));
    await userEvent.click(await screen.findByRole("button", { name: "Add a subject" }));

    expect(screen.getByLabelText("Subject")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Remove subject 1" }));
    expect(screen.queryByLabelText("Subject")).not.toBeInTheDocument();
  });

  it("publishes a complete profile from the Review & Publish step", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      ...BASE_TUTOR,
      subject: "Mathematics",
      hourlyRate: 500_000,
    });
    const submitProfile = vi.spyOn(identityService, "submitTutorProfile").mockResolvedValue(undefined);
    window.localStorage.setItem(`tutorflow.onboardingStep.${TUTOR_ID}`, "6");

    renderWizard();

    await userEvent.click(await screen.findByRole("button", { name: "Publish profile" }));

    expect(submitProfile).toHaveBeenCalledWith(TUTOR_ID);
  });

  it("disables Publish when the profile is missing a required field", async () => {
    window.localStorage.setItem(`tutorflow.onboardingStep.${TUTOR_ID}`, "6");

    renderWizard();

    expect(await screen.findByRole("button", { name: "Publish profile" })).toBeDisabled();
  });

  it("resumes at the last-visited step from localStorage", async () => {
    window.localStorage.setItem(`tutorflow.onboardingStep.${TUTOR_ID}`, "3");

    renderWizard();

    expect(await screen.findByLabelText("Hourly rate (Toman)")).toBeInTheDocument();
  });
});
