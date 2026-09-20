import { LOGIN_URL } from "@/lib/api";
import { vibeColor } from "@/lib/format";
import { isUnauthorized, type ResourceState } from "./use-resource";

export function PageHeader({ title, subtitle }: { title: string; subtitle?: string }) {
  return (
    <header className="mb-8">
      <h1 className="font-display text-3xl font-extrabold tracking-tight md:text-4xl">{title}</h1>
      {subtitle ? <p className="mt-1 text-sm text-muted">{subtitle}</p> : null}
    </header>
  );
}

export function Card({ className = "", children }: { className?: string; children: React.ReactNode }) {
  return (
    <div className={`rounded-2xl border border-border bg-surface p-5 ${className}`}>{children}</div>
  );
}

export function Stat({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Card>
      <p className="text-xs font-medium uppercase tracking-wider text-muted">{label}</p>
      <p className="mt-2 font-display text-3xl font-bold tabular-nums">{value}</p>
      {hint ? <p className="mt-1 text-xs text-muted">{hint}</p> : null}
    </Card>
  );
}

export function VibeChip({ name }: { name: string | null }) {
  const color = vibeColor(name);
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-semibold"
      style={{ borderColor: `${color}55`, color, backgroundColor: `${color}14` }}
    >
      <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: color }} />
      {name ?? "Unclassified"}
    </span>
  );
}

// A neutral panel used by loading / empty states so the page keeps its shape.
export function Panel({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-40 flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-border bg-surface/40 px-6 py-10 text-center">
      {children}
    </div>
  );
}

export function EmptyState({ title, hint }: { title: string; hint?: string }) {
  return (
    <Panel>
      <p className="font-display text-lg font-semibold">{title}</p>
      {hint ? <p className="max-w-sm text-sm text-muted">{hint}</p> : null}
    </Panel>
  );
}

function LoadingState() {
  return (
    <Panel>
      <span className="h-6 w-6 animate-spin rounded-full border-2 border-border border-t-accent" />
      <p className="text-sm text-muted">Loading…</p>
    </Panel>
  );
}

function ConnectPrompt() {
  return (
    <Panel>
      <p className="font-display text-lg font-semibold">Connect Spotify to continue</p>
      <p className="max-w-sm text-sm text-muted">
        VibeStream reads your listening history to build sessions, analytics, and vibes.
      </p>
      <a
        href={LOGIN_URL}
        className="mt-3 rounded-full bg-accent px-5 py-2 text-sm font-semibold text-accent-ink transition-opacity hover:opacity-90"
      >
        Connect Spotify
      </a>
    </Panel>
  );
}

function ErrorState() {
  return (
    <Panel>
      <p className="font-display text-lg font-semibold">Something went wrong</p>
      <p className="max-w-sm text-sm text-muted">
        We couldn&apos;t load this right now. Make sure the API is running, then refresh.
      </p>
    </Panel>
  );
}

// Renders the non-ready branches of a resource so pages can guard with one line and then use the data.
export function ResourceFallback({ state }: { state: ResourceState<unknown> }) {
  if (state.status === "loading") {
    return <LoadingState />;
  }
  if (state.status === "error") {
    return isUnauthorized(state.error) ? <ConnectPrompt /> : <ErrorState />;
  }
  return null;
}
