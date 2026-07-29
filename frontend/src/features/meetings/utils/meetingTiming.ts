export type MeetingTimingPhase = "upcoming" | "live" | "ended";

export interface MeetingTiming {
  phase: MeetingTimingPhase;
  /** A short, human label — "Starts in 2h 15m", "Live now", "Ended". */
  label: string;
}

function formatDuration(ms: number): string {
  const totalMinutes = Math.max(1, Math.round(ms / 60_000));
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours === 0) {
    return `${minutes}m`;
  }

  return minutes === 0 ? `${hours}h` : `${hours}h ${minutes}m`;
}

/** Derives the Join/Start button's live state from the Meeting's own StartsAtUtc/EndsAtUtc — never a fabricated "starting soon," just the real countdown. */
export function deriveMeetingTiming(startsAtUtc: string, endsAtUtc: string, now: Date): MeetingTiming {
  const nowMs = now.getTime();
  const startsMs = Date.parse(startsAtUtc);
  const endsMs = Date.parse(endsAtUtc);

  if (nowMs < startsMs) {
    return { phase: "upcoming", label: `Starts in ${formatDuration(startsMs - nowMs)}` };
  }

  if (nowMs <= endsMs) {
    return { phase: "live", label: "Live now" };
  }

  return { phase: "ended", label: "Ended" };
}
