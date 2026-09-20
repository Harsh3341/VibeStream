import type {
  NowPlaying,
  RecentPlay,
  SessionDetail,
  SessionSummary,
  TodaySummary,
  UserProfile,
  Vibe,
  VibeShare,
  WeeklySummary,
} from "./types";

// The API origin. Overridable at build time; defaults to the local dev backend.
export const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://127.0.0.1:5062";

// Where the browser navigates to start the Spotify OAuth flow (a full-page redirect, not fetch).
export const LOGIN_URL = `${API_BASE}/api/v1/auth/spotify/login`;

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

// Every request carries the auth cookie (credentials: "include") — that's the whole point of the
// HttpOnly-cookie design: the SPA never handles the token, the browser attaches it.
async function request<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    credentials: "include",
    headers: { Accept: "application/json" },
    cache: "no-store",
  });

  if (response.status === 204) {
    return null as T;
  }

  if (!response.ok) {
    throw new ApiError(response.status, `Request to ${path} failed with ${response.status}`);
  }

  return (await response.json()) as T;
}

// For endpoints that legitimately have "nothing yet" (404/204) — returns null instead of throwing.
async function requestMaybe<T>(path: string): Promise<T | null> {
  try {
    return await request<T>(path);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export const api = {
  getProfile: () => request<UserProfile>("/api/v1/me"),
  getNowPlaying: () => requestMaybe<NowPlaying>("/api/v1/me/now-playing"),
  getRecent: (limit = 50) => request<RecentPlay[]>(`/api/v1/listening/recent?limit=${limit}`),
  getSessions: (limit = 50) => request<SessionSummary[]>(`/api/v1/sessions?limit=${limit}`),
  getSession: (id: string) => requestMaybe<SessionDetail>(`/api/v1/sessions/${id}`),
  getToday: () => request<TodaySummary>("/api/v1/analytics/today"),
  getWeekly: () => request<WeeklySummary>("/api/v1/analytics/weekly"),
  getCurrentVibe: () => requestMaybe<Vibe>("/api/v1/vibes/current"),
  getVibeHistory: (limit = 50) => request<Vibe[]>(`/api/v1/vibes/history?limit=${limit}`),
  getDna: (days = 30) => request<VibeShare[]>(`/api/v1/vibes/dna?days=${days}`),
  logout: () =>
    fetch(`${API_BASE}/api/v1/auth/logout`, { method: "POST", credentials: "include" }),
};
