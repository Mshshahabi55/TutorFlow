import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TutorOfferingPage } from "@/features/identity/pages/TutorOfferingPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

// 400 Rial = 40 Toman (ADR-019: wire value is Rial; the form displays and
// accepts Toman).
function mockTutor() {
  vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
    tutorId: TUTOR_ID,
    isApproved: true,
    isSuspended: false,
    isDiscoverable: true,
    hourlyRate: 400,
    subject: "Mathematics",
    language: "English",
    location: "Remote",
    offeredDurations: ["00:30:00", "01:00:00"],
  });
}

function renderPage() {
  return renderWithProviders(<TutorOfferingPage />, {
    initialEntries: [`/identity/tutors/${TUTOR_ID}/edit`],
    routePath: "/identity/tutors/:tutorId/edit",
  });
}

describe("TutorOfferingPage", () => {
  it("loads the current offering into the form", async () => {
    mockTutor();

    renderPage();

    expect(await screen.findByDisplayValue("Mathematics")).toBeInTheDocument();
    expect(screen.getByDisplayValue("English")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Remote")).toBeInTheDocument();
    // 400 Rial (the wire value) displays as 40 Toman in the form.
    expect(screen.getByLabelText("Hourly rate (Toman)")).toHaveDisplayValue("40");
    expect(screen.getByDisplayValue("30, 60")).toBeInTheDocument();
  });

  it("converts an edited hourly rate from Toman to Rial when saved", async () => {
    mockTutor();
    const setHourlyRate = vi.spyOn(identityService, "setTutorHourlyRate").mockResolvedValue(undefined);

    renderPage();

    const rateField = await screen.findByLabelText("Hourly rate (Toman)");
    await userEvent.clear(rateField);
    await userEvent.type(rateField, "50000");
    await userEvent.click(screen.getByRole("button", { name: "Save changes" }));

    expect(await screen.findByText("Offering updated.")).toBeInTheDocument();
    // 50,000 Toman entered -> 500,000 Rial sent over the wire.
    expect(setHourlyRate).toHaveBeenCalledWith(TUTOR_ID, 500_000);
  });

  it("only submits the field that changed", async () => {
    mockTutor();
    const setSubject = vi.spyOn(identityService, "setTutorSubject").mockResolvedValue(undefined);
    const setLanguage = vi.spyOn(identityService, "setTutorLanguage");

    renderPage();

    const subjectField = await screen.findByDisplayValue("Mathematics");
    await userEvent.clear(subjectField);
    await userEvent.type(subjectField, "Physics");
    await userEvent.click(screen.getByRole("button", { name: "Save changes" }));

    expect(await screen.findByText("Offering updated.")).toBeInTheDocument();
    expect(setSubject).toHaveBeenCalledWith(TUTOR_ID, "Physics");
    expect(setLanguage).not.toHaveBeenCalled();
  });

  it("notifies that there is nothing to save when no field changed", async () => {
    mockTutor();

    renderPage();

    await screen.findByDisplayValue("Mathematics");
    await userEvent.click(screen.getByRole("button", { name: "Save changes" }));

    expect(await screen.findByText("Nothing to save — no field changed.")).toBeInTheDocument();
  });
});
