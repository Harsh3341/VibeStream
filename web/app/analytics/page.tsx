"use client";

import { api } from "@/lib/api";
import { formatClockHour, formatDayLabel, formatListeningTime, vibeColor } from "@/lib/format";
import type { VibeShare, WeeklySummary } from "@/lib/types";
import { useResource } from "@/components/use-resource";
import { Card, EmptyState, PageHeader, ResourceFallback } from "@/components/ui";

interface AnalyticsData {
  weekly: WeeklySummary;
  dna: VibeShare[];
}

export default function AnalyticsPage() {
  const state = useResource<AnalyticsData>(async () => {
    const [weekly, dna] = await Promise.all([api.getWeekly(), api.getDna(30)]);
    return { weekly, dna };
  });

  if (state.status !== "ready") {
    return (
      <>
        <PageHeader title="Analytics" subtitle="Your last seven days, and your listening DNA." />
        <ResourceFallback state={state} />
      </>
    );
  }

  const { weekly, dna } = state.data;
  const peak = weekly.peakHourUtc;

  return (
    <div className="rise space-y-8">
      <PageHeader title="Analytics" subtitle="Your last seven days, and your listening DNA." />

      <section>
        <div className="mb-3 flex items-baseline justify-between">
          <h2 className="font-display text-lg font-bold">7-day listening</h2>
          {peak !== null ? (
            <span className="text-sm text-muted">
              Peak hour · <span className="text-text">{formatClockHour(peak)}</span>
            </span>
          ) : null}
        </div>
        <WeeklyChart weekly={weekly} />
      </section>

      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        <TopList
          title="Top artists"
          rows={weekly.topArtists.map((a) => ({ key: a.artistId, name: a.name, count: a.playCount }))}
        />
        <TopList
          title="Top tracks"
          rows={weekly.topTracks.map((t) => ({
            key: t.trackId,
            name: t.name,
            sub: t.artistName,
            count: t.playCount,
          }))}
        />
      </div>

      <section>
        <h2 className="mb-3 font-display text-lg font-bold">Listening DNA</h2>
        {dna.length === 0 ? (
          <EmptyState title="Not enough data yet" hint="Vibes accrue as your sessions get classified." />
        ) : (
          <Card className="space-y-3">
            {dna.map((share) => (
              <div key={share.name}>
                <div className="mb-1 flex items-center justify-between text-sm">
                  <span className="font-medium">{share.name}</span>
                  <span className="tabular-nums text-muted">{share.percentage}%</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-surface-2">
                  <div
                    className="h-full rounded-full"
                    style={{ width: `${share.percentage}%`, backgroundColor: vibeColor(share.name) }}
                  />
                </div>
              </div>
            ))}
          </Card>
        )}
      </section>
    </div>
  );
}

function WeeklyChart({ weekly }: { weekly: WeeklySummary }) {
  const max = Math.max(1, ...weekly.daily.map((day) => day.listeningSeconds));

  return (
    <Card>
      <div className="flex h-44 items-end gap-2">
        {weekly.daily.map((day) => {
          const heightPct = (day.listeningSeconds / max) * 100;
          return (
            <div key={day.date} className="flex flex-1 flex-col items-center gap-2">
              <div className="flex w-full flex-1 items-end">
                <div
                  className="w-full rounded-t-md bg-accent/80 transition-all"
                  style={{ height: `${Math.max(heightPct, 2)}%` }}
                  title={formatListeningTime(day.listeningSeconds)}
                />
              </div>
              <span className="text-xs text-muted">{formatDayLabel(day.date)}</span>
            </div>
          );
        })}
      </div>
    </Card>
  );
}

interface TopRow {
  key: string;
  name: string;
  sub?: string;
  count: number;
}

function TopList({ title, rows }: { title: string; rows: TopRow[] }) {
  return (
    <section>
      <h2 className="mb-3 font-display text-lg font-bold">{title}</h2>
      {rows.length === 0 ? (
        <EmptyState title="Nothing yet" />
      ) : (
        <Card className="p-0">
          <ol className="divide-y divide-border">
            {rows.map((row, index) => (
              <li key={row.key} className="flex items-center gap-3 px-4 py-3">
                <span className="w-5 shrink-0 text-right font-display text-sm font-bold text-accent">
                  {index + 1}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{row.name}</p>
                  {row.sub ? <p className="truncate text-xs text-muted">{row.sub}</p> : null}
                </div>
                <span className="shrink-0 text-xs tabular-nums text-muted">{row.count} plays</span>
              </li>
            ))}
          </ol>
        </Card>
      )}
    </section>
  );
}
