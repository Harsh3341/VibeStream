"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { api } from "@/lib/api";
import { formatRelative, formatSessionSpan } from "@/lib/format";
import { useResource } from "@/components/use-resource";
import { Card, EmptyState, PageHeader, ResourceFallback, VibeChip } from "@/components/ui";

export default function SessionDetailPage() {
  const params = useParams<{ id: string }>();
  const state = useResource(() => api.getSession(params.id));

  if (state.status !== "ready") {
    return (
      <>
        <BackLink />
        <PageHeader title="Session" />
        <ResourceFallback state={state} />
      </>
    );
  }

  const session = state.data;

  if (!session) {
    return (
      <>
        <BackLink />
        <PageHeader title="Session" />
        <EmptyState title="Session not found" hint="It may have been rebuilt. Head back to the list." />
      </>
    );
  }

  return (
    <div className="rise">
      <BackLink />
      <PageHeader
        title={`${session.trackCount} ${session.trackCount === 1 ? "track" : "tracks"}`}
        subtitle={formatSessionSpan(session.startedAt, session.endedAt)}
      />

      <div className="mb-6">
        <VibeChip name={session.vibe} />
      </div>

      <Card className="p-0">
        <ol className="divide-y divide-border">
          {session.tracks.map((track) => (
            <li key={track.position} className="flex items-center gap-3 px-4 py-3">
              <span className="w-6 shrink-0 text-right text-xs tabular-nums text-muted">
                {track.position + 1}
              </span>
              <Cover url={track.albumImageUrl} alt={track.trackName} />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{track.trackName}</p>
                <p className="truncate text-xs text-muted">{track.artistName}</p>
              </div>
              <span className="shrink-0 text-xs text-muted">{formatRelative(track.playedAt)}</span>
            </li>
          ))}
        </ol>
      </Card>
    </div>
  );
}

function BackLink() {
  return (
    <Link href="/sessions" className="mb-4 inline-block text-sm text-muted hover:text-text">
      ← Sessions
    </Link>
  );
}

function Cover({ url, alt }: { url: string | null; alt: string }) {
  if (!url) {
    return <div className="h-10 w-10 shrink-0 rounded-md bg-surface-2" aria-hidden />;
  }
  // eslint-disable-next-line @next/next/no-img-element
  return <img src={url} alt={alt} className="h-10 w-10 shrink-0 rounded-md object-cover" />;
}
