"use client";

import { api } from "@/lib/api";
import { formatRelative, vibeColor } from "@/lib/format";
import type { Vibe } from "@/lib/types";
import { useResource } from "@/components/use-resource";
import { Card, EmptyState, PageHeader, ResourceFallback, VibeChip } from "@/components/ui";

export default function VibesPage() {
  const state = useResource(() => api.getVibeHistory(60));

  if (state.status !== "ready") {
    return (
      <>
        <PageHeader title="Vibes" subtitle="A deterministic read of every session's mood." />
        <ResourceFallback state={state} />
      </>
    );
  }

  const vibes = state.data;

  if (vibes.length === 0) {
    return (
      <>
        <PageHeader title="Vibes" subtitle="A deterministic read of every session's mood." />
        <EmptyState title="No vibes yet" hint="Each session gets a vibe once it's classified by the engine." />
      </>
    );
  }

  return (
    <div className="rise">
      <PageHeader title="Vibes" subtitle="A deterministic read of every session's mood." />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        {vibes.map((vibe) => (
          <VibeCard key={vibe.id} vibe={vibe} />
        ))}
      </div>
    </div>
  );
}

function VibeCard({ vibe }: { vibe: Vibe }) {
  const color = vibeColor(vibe.name);

  return (
    <Card className="space-y-4" >
      <div className="flex items-center justify-between gap-3">
        <VibeChip name={vibe.name} />
        <span className="text-xs text-muted">{formatRelative(vibe.createdAt)}</span>
      </div>

      <div className="space-y-2">
        <ScoreBar label="Energy" value={vibe.energy} color={color} />
        <ScoreBar label="Mood" value={vibe.valence} color={color} />
        <ScoreBar label="Danceability" value={vibe.danceability} color={color} />
      </div>

      <p className="text-xs text-muted">Confidence {Math.round(vibe.confidence * 100)}%</p>
    </Card>
  );
}

function ScoreBar({ label, value, color }: { label: string; value: number; color: string }) {
  const pct = Math.round(Math.min(Math.max(value, 0), 1) * 100);

  return (
    <div>
      <div className="mb-1 flex items-center justify-between text-xs">
        <span className="text-muted">{label}</span>
        <span className="tabular-nums text-muted">{pct}</span>
      </div>
      <div className="h-1.5 overflow-hidden rounded-full bg-surface-2">
        <div className="h-full rounded-full" style={{ width: `${pct}%`, backgroundColor: color }} />
      </div>
    </div>
  );
}
