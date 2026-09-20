// Mirrors the API DTOs (System.Text.Json serializes them camelCase). Kept by hand — small, stable,
// and it keeps the frontend build independent of the backend.

export interface UserProfile {
  id: string;
  spotifyUserId: string;
  displayName: string | null;
}

export interface NowPlaying {
  trackName: string;
  artistName: string;
  albumName: string;
  albumImageUrl: string | null;
  isPlaying: boolean;
  progressMs: number;
  durationMs: number;
}

export interface RecentPlay {
  trackId: string;
  trackName: string;
  artistName: string;
  albumName: string;
  albumImageUrl: string | null;
  playedAt: string;
}

export interface SessionSummary {
  id: string;
  startedAt: string;
  endedAt: string;
  trackCount: number;
  vibe: string | null;
}

export interface SessionTrack {
  position: number;
  trackId: string;
  trackName: string;
  artistName: string;
  albumImageUrl: string | null;
  playedAt: string;
}

export interface SessionDetail extends SessionSummary {
  tracks: SessionTrack[];
}

export interface TodaySummary {
  listeningSeconds: number;
  tracksPlayed: number;
  uniqueArtists: number;
  uniqueTracks: number;
  sessions: number;
}

export interface DailyPoint {
  date: string;
  listeningSeconds: number;
  trackCount: number;
}

export interface TopArtist {
  artistId: string;
  name: string;
  playCount: number;
}

export interface TopTrack {
  trackId: string;
  name: string;
  artistName: string;
  playCount: number;
}

export interface WeeklySummary {
  daily: DailyPoint[];
  topArtists: TopArtist[];
  topTracks: TopTrack[];
  peakHourUtc: number | null;
}

export interface Vibe {
  id: string;
  sessionId: string | null;
  name: string;
  energy: number;
  valence: number;
  danceability: number;
  confidence: number;
  createdAt: string;
}

export interface VibeShare {
  name: string;
  count: number;
  percentage: number;
}
