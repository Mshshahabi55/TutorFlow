import { describe, expect, it } from "vitest";
import { TUTOR_ONBOARDING_DEFAULT_VALUES, tutorOnboardingSchema } from "@/features/identity/validation/tutorOnboardingSchema";

function repeatedCommaList(count: number, entryLength = 3): string {
  return Array.from({ length: count }, (_, i) => `${i}`.padStart(entryLength, "x")).join(", ");
}

describe("tutorOnboardingSchema — collection limits (Merge Readiness audit Critical 2)", () => {
  it("accepts exactly 20 comma-separated other languages", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      otherLanguages: repeatedCommaList(20),
    });

    expect(result.success).toBe(true);
  });

  it("rejects 21 comma-separated other languages", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      otherLanguages: repeatedCommaList(21),
    });

    expect(result.success).toBe(false);
  });

  it("rejects 21 comma-separated lesson specialties", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      lessonSpecialties: repeatedCommaList(21),
    });

    expect(result.success).toBe(false);
  });

  it("rejects a single other-language entry exceeding 200 characters", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      otherLanguages: "a".repeat(201),
    });

    expect(result.success).toBe(false);
  });

  it("accepts exactly 20 additional subjects", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      tutorSubjects: Array.from({ length: 20 }, (_, i) => ({ subject: `Subject ${i}`, level: "" })),
    });

    expect(result.success).toBe(true);
  });

  it("rejects 21 additional subjects", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      tutorSubjects: Array.from({ length: 21 }, (_, i) => ({ subject: `Subject ${i}`, level: "" })),
    });

    expect(result.success).toBe(false);
  });

  it("rejects a subject entry exceeding 200 characters", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      tutorSubjects: [{ subject: "a".repeat(201), level: "" }],
    });

    expect(result.success).toBe(false);
  });

  it("rejects 21 gallery image URLs", () => {
    const urls = Array.from({ length: 21 }, (_, i) => `https://example.com/${i}.jpg`).join(", ");
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      galleryImageUrls: urls,
    });

    expect(result.success).toBe(false);
  });
});

describe("tutorOnboardingSchema — media URL scheme (Merge Readiness audit High 1)", () => {
  it("accepts an empty photo URL (optional field)", () => {
    const result = tutorOnboardingSchema.safeParse(TUTOR_ONBOARDING_DEFAULT_VALUES);

    expect(result.success).toBe(true);
  });

  it("accepts an https photo URL", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      photoUrl: "https://example.com/photo.jpg",
    });

    expect(result.success).toBe(true);
  });

  it.each(["javascript:alert(1)", "data:text/html,<script>alert(1)</script>", "file:///etc/passwd", "not a url"])(
    "rejects a photo URL with scheme/shape %s",
    (url) => {
      const result = tutorOnboardingSchema.safeParse({
        ...TUTOR_ONBOARDING_DEFAULT_VALUES,
        photoUrl: url,
      });

      expect(result.success).toBe(false);
    },
  );

  it("rejects a non-http intro video URL", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      introVideoUrl: "javascript:alert(1)",
    });

    expect(result.success).toBe(false);
  });

  it("rejects a gallery image list containing one non-http URL", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      galleryImageUrls: "https://example.com/1.jpg, javascript:alert(1)",
    });

    expect(result.success).toBe(false);
  });

  it("accepts a gallery image list of all-valid http(s) URLs", () => {
    const result = tutorOnboardingSchema.safeParse({
      ...TUTOR_ONBOARDING_DEFAULT_VALUES,
      galleryImageUrls: "https://example.com/1.jpg, http://example.com/2.jpg",
    });

    expect(result.success).toBe(true);
  });
});
