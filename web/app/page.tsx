"use client";

import { api } from "@/lib/api";
import { formatListeningTime, formatRelative, formatTrackLength } from "@/lib/format";
import type { NowPlaying, RecentPlay, TodaySummary, Vibe } from "@/lib/types";
import { useResource } from "@/components/use-resource";
import { Card, EmptyState, PageHeader, ResourceFallback, Stat, VibeChip } from "@/components/ui";

interface DashboardData {
  today: TodaySummary;
  nowPlaying: NowPlaying | null;
  vibe: Vibe | null;
  recent: RecentPlay[];
}

export default function DashboardPage() {
  const state = useResource<DashboardData>(async () => {
    const [today, nowPlaying, vibe, recent] = await Promise.all([
      api.getToday(),
      api.getNowPlaying(),
      api.getCurrentVibe(),
      api.getRecent(8),
    ]);
    return { today, nowPlaying, vibe, recent };
  });

  if (state.status !== "ready") {
    return (
      <>
        <PageHeader title="Dashboard" subtitle="Your listening, right now." />
        <ResourceFallback state={state} />
      </>
    );
  }

  const { today, nowPlaying, vibe, recent } = state.data;

  return (
    <div className="rise">
      <PageHeader title="Dashboard" subtitle="Your listening, right now." />

      <NowPlayingHero nowPlaying={nowPlaying} vibe={vibe} />

      <div className="mt-6 grid grid-cols-2 gap-4 md:grid-cols-4">
        <Stat label="Listening today" value={formatListeningTime(today.listeningSeconds)} />
        <Stat label="Tracks" value={today.tracksPlayed.toString()} hint={`${today.uniqueTracks} unique`} />
        <Stat label="Artists" value={today.uniqueArtists.toString()} />
        <Stat label="Sessions" value={today.sessions.toString()} />
      </div>

      <h2 className="mt-10 mb-3 font-display text-lg font-bold">Recent tracks</h2>
      {recent.length === 0 ? (
        <EmptyState title="No plays yet" hint="Once the worker imports your history, tracks appear here." />
      ) : (
        <Card className="p-0">
          <ul className="divide-y divide-border">
            {recent.map((play, index) => (
              <li key={`${play.trackId}-${index}`} className="flex items-center gap-3 px-4 py-3">
                <Artwork url={play.albumImageUrl} alt={play.albumName} size={40} />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{play.trackName}</p>
                  <p className="truncate text-xs text-muted">{play.artistName}</p>
                </div>
                <span className="shrink-0 text-xs text-muted">{formatRelative(play.playedAt)}</span>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}

function NowPlayingHero({ nowPlaying, vibe }: { nowPlaying: NowPlaying | null; vibe: Vibe | null }) {
  if (!nowPlaying) {
    return (
      <Card className="flex items-center justify-between gap-4">
        <div>
          <p className="text-xs font-medium uppercase tracking-wider text-muted">Now playing</p>
          <p className="mt-2 font-display text-xl font-bold">Nothing playing</p>
          <p className="text-sm text-muted">Start something on Spotify and it&apos;ll show up here.</p>
        </div>
        {vibe ? <VibeChip name={vibe.name} /> : null}
      </Card>
    );
  }

  const progress = nowPlaying.durationMs > 0 ? (nowPlaying.progressMs / nowPlaying.durationMs) * 100 : 0;

  return (
    <Card className="flex flex-col gap-4 sm:flex-row sm:items-center">
      <Artwork url={nowPlaying.albumImageUrl} alt={nowPlaying.albumName} size={96} rounded />
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="text-xs font-medium uppercase tracking-wider text-muted">
            {nowPlaying.isPlaying ? "Now playing" : "Paused"}
          </span>
          {vibe ? <VibeChip name={vibe.name} /> : null}
        </div>
        <p className="mt-1 truncate font-display text-2xl font-bold">{nowPlaying.trackName}</p>
        <p className="truncate text-sm text-muted">
          {nowPlaying.artistName} — {nowPlaying.albumName}
        </p>
        <div className="mt-3 flex items-center gap-2">
          <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-surface-2">
            <div className="h-full rounded-full bg-accent" style={{ width: `${progress}%` }} />
          </div>
          <span className="shrink-0 text-xs tabular-nums text-muted">
            {formatTrackLength(nowPlaying.progressMs)} / {formatTrackLength(nowPlaying.durationMs)}
          </span>
        </div>
      </div>
    </Card>
  );
}

function Artwork({
  url,
  alt,
  size,
  rounded,
}: {
  url: string | null;
  alt: string;
  size: number;
  rounded?: boolean;
}) {
  const radius = rounded ? "rounded-xl" : "rounded-md";

  if (!url) {
    return (
      <div
        className={`${radius} shrink-0 bg-surface-2`}
        style={{ width: size, height: size }}
        aria-hidden
      />
    );
  }

  // Plain img (not next/image) keeps CDN handling simple; album art is already sized by Spotify.
  // eslint-disable-next-line @next/next/no-img-element
  return (
    <img
      src={url}
      alt={alt}
      width={size}
      height={size}
      className={`${radius} shrink-0 object-cover`}
      style={{ width: size, height: size }}
    />
  );
}
