/**
 * A pure lookup from "percent of wizard steps completed" to a short,
 * encouraging line shown alongside the mobile progress bar — thresholds
 * are a presentation choice only, not tied to any Domain rule.
 */
export function encouragingMessageForProgress(percent: number): string {
  if (percent >= 100) {
    return "All set — ready to publish!";
  }
  if (percent >= 75) {
    return "Almost done!";
  }
  if (percent >= 50) {
    return "Halfway there!";
  }
  if (percent >= 25) {
    return "Nice progress — keep going.";
  }
  return "Let's get your profile started.";
}
