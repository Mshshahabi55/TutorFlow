import { describe, expect, it } from "vitest";
import { encouragingMessageForProgress } from "@/features/identity/utils/onboardingProgressMessage";

describe("encouragingMessageForProgress", () => {
  it("returns a starting message at 0%", () => {
    expect(encouragingMessageForProgress(0)).toBe("Let's get your profile started.");
  });

  it("returns a keep-going message once past a quarter", () => {
    expect(encouragingMessageForProgress(25)).toBe("Nice progress — keep going.");
  });

  it("returns a halfway message at 50%", () => {
    expect(encouragingMessageForProgress(50)).toBe("Halfway there!");
  });

  it("returns an almost-done message at 75%", () => {
    expect(encouragingMessageForProgress(75)).toBe("Almost done!");
  });

  it("returns a completion message at 100%", () => {
    expect(encouragingMessageForProgress(100)).toBe("All set — ready to publish!");
  });
});
