// Small presentation helpers, kept in one place so pages stay declarative.

export function formatListeningTime(totalSeconds: number): string {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);

  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }
  if (minutes > 0) {
    return `${minutes}m`;
  }
  return `${totalSeconds}s`;
}

export function formatTrackLength(ms: number): string {
  const totalSeconds = Math.round(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;

  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

export function formatRelative(iso: string): string {
  const then = new Date(iso).getTime();
  const diffSeconds = Math.round((Date.now() - then) / 1000);

  if (diffSeconds < 60) return "just now";
  if (diffSeconds < 3600) return `${Math.floor(diffSeconds / 60)}m ago`;
  if (diffSeconds < 86400) return `${Math.floor(diffSeconds / 3600)}h ago`;
  return `${Math.floor(diffSeconds / 86400)}d ago`;
}

export function formatClockHour(hourUtc: number): string {
  const suffix = hourUtc < 12 ? "AM" : "PM";
  const twelve = hourUtc % 12 === 0 ? 12 : hourUtc % 12;
  return `${twelve} ${suffix}`;
}

export function formatDayLabel(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { weekday: "short" });
}

export function formatSessionSpan(startIso: string, endIso: string): string {
  const start = new Date(startIso);
  const end = new Date(endIso);
  const minutes = Math.max(1, Math.round((end.getTime() - start.getTime()) / 60000));
  const label = minutes >= 60 ? `${Math.floor(minutes / 60)}h ${minutes % 60}m` : `${minutes}m`;

  const day = start.toLocaleDateString(undefined, { month: "short", day: "numeric" });
  const time = start.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
  return `${day} · ${time} · ${label}`;
}

// Per-vibe hues so a label reads the same everywhere (chips, session cards, DNA bars).
const VIBE_COLORS: Record<string, string> = {
  "High Energy": "#ff6a3d",
  "Dark Energy": "#a06bff",
  Chill: "#31d6c0",
  "Late Night": "#6b83ff",
  "Feel Good": "#ffc94d",
  Focus: "#4da6ff",
};

export function vibeColor(name: string | null): string {
  if (!name) return "#9797a3";
  return VIBE_COLORS[name] ?? "#c8ff2f";
}
