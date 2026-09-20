"use client";

import { api } from "@/lib/api";
import { formatRelative } from "@/lib/format";
import { useResource } from "@/components/use-resource";
import { Card, EmptyState, PageHeader, ResourceFallback } from "@/components/ui";

export default function ListeningPage() {
  const state = useResource(() => api.getRecent(100));

  if (state.status !== "ready") {
    return (
      <>
        <PageHeader title="Listening" subtitle="Everything you've played, most recent first." />
        <ResourceFallback state={state} />
      </>
    );
  }

  const plays = state.data;

  return (
    <div className="rise">
      <PageHeader title="Listening" subtitle="Everything you've played, most recent first." />

      {plays.length === 0 ? (
        <EmptyState title="No plays yet" hint="The worker imports your recently-played on each cycle." />
      ) : (
        <Card className="p-0">
          <ul className="divide-y divide-border">
            {plays.map((play, index) => (
              <li key={`${play.trackId}-${index}`} className="flex items-center gap-3 px-4 py-3">
                <span className="w-6 shrink-0 text-right text-xs tabular-nums text-muted">
                  {index + 1}
                </span>
                <Cover url={play.albumImageUrl} alt={play.albumName} />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{play.trackName}</p>
                  <p className="truncate text-xs text-muted">
                    {play.artistName} — {play.albumName}
                  </p>
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

function Cover({ url, alt }: { url: string | null; alt: string }) {
  if (!url) {
    return <div className="h-10 w-10 shrink-0 rounded-md bg-surface-2" aria-hidden />;
  }
  // eslint-disable-next-line @next/next/no-img-element
  return <img src={url} alt={alt} className="h-10 w-10 shrink-0 rounded-md object-cover" />;
}
