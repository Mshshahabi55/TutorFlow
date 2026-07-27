import { describe, expect, it } from "vitest";
import { deriveProfileCompletion } from "@/features/identity/utils/profileCompletion";
import type { TutorDto } from "@/services/api/dtos";

const BASE_TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: null,
  subject: null,
  language: null,
  location: null,
  offeredDurations: [],
};

describe("deriveProfileCompletion", () => {
  it("marks every item incomplete for a brand new, unfilled Tutor listing", () => {
    const result = deriveProfileCompletion(BASE_TUTOR, false);

    expect(result.completedCount).toBe(0);
    expect(result.totalCount).toBe(5);
    expect(result.isComplete).toBe(false);
    expect(result.items.every((item) => !item.done)).toBe(true);
  });

  it("marks every item complete once every real field is filled and approved", () => {
    const result = deriveProfileCompletion(
      {
        ...BASE_TUTOR,
        isApproved: true,
        hourlyRate: 500_000,
        subject: "Mathematics",
        language: "English",
        offeredDurations: ["01:00:00"],
      },
      true,
    );

    expect(result.completedCount).toBe(5);
    expect(result.isComplete).toBe(true);
  });

  it("marks 'Profile completed' incomplete when a session duration is set but no hourly rate is", () => {
    const result = deriveProfileCompletion(
      { ...BASE_TUTOR, offeredDurations: ["01:00:00"] },
      false,
    );

    expect(result.items[0]).toEqual({ label: "Profile completed", done: false });
  });

  it("derives 'Availability added' from the caller's hasAvailability signal, not a Tutor field", () => {
    const withoutAvailability = deriveProfileCompletion(BASE_TUTOR, false);
    const withAvailability = deriveProfileCompletion(BASE_TUTOR, true);

    expect(withoutAvailability.items[1]).toEqual({ label: "Availability added", done: false });
    expect(withAvailability.items[1]).toEqual({ label: "Availability added", done: true });
  });
});
