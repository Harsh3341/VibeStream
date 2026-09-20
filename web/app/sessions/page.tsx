"use client";

import Link from "next/link";
import { api } from "@/lib/api";
import { formatSessionSpan } from "@/lib/format";
import { useResource } from "@/components/use-resource";
import { Card, EmptyState, PageHeader, ResourceFallback, VibeChip } from "@/components/ui";

export default function SessionsPage() {
  const state = useResource(() => api.getSessions(100));

  if (state.status !== "ready") {
    return (
      <>
        <PageHeader title="Sessions" subtitle="Runs of listening, grouped by the 15-minute gap rule." />
        <ResourceFallback state={state} />
      </>
    );
  }

  const sessions = state.data;

  return (
    <div className="rise">
      <PageHeader title="Sessions" subtitle="Runs of listening, grouped by the 15-minute gap rule." />

      {sessions.length === 0 ? (
        <EmptyState title="No sessions yet" hint="Sessions form once enough listening history is imported." />
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {sessions.map((session) => (
            <Link key={session.id} href={`/sessions/${session.id}`}>
              <Card className="h-full transition-colors hover:border-muted">
                <div className="flex items-start justify-between gap-3">
                  <p className="font-display text-lg font-bold">
                    {session.trackCount} {session.trackCount === 1 ? "track" : "tracks"}
                  </p>
                  <VibeChip name={session.vibe} />
                </div>
                <p className="mt-2 text-sm text-muted">
                  {formatSessionSpan(session.startedAt, session.endedAt)}
                </p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
