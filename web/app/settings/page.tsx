"use client";

import { api, LOGIN_URL } from "@/lib/api";
import { useResource } from "@/components/use-resource";
import { Card, PageHeader, ResourceFallback } from "@/components/ui";

export default function SettingsPage() {
  const state = useResource(() => api.getProfile());

  if (state.status !== "ready") {
    return (
      <>
        <PageHeader title="Settings" subtitle="Your Spotify connection." />
        <ResourceFallback state={state} />
      </>
    );
  }

  const profile = state.data;

  const handleLogout = async () => {
    await api.logout();
    window.location.reload();
  };

  return (
    <div className="rise">
      <PageHeader title="Settings" subtitle="Your Spotify connection." />

      <Card className="flex flex-col gap-5">
        <div className="flex items-center gap-2">
          <span className="h-2 w-2 rounded-full bg-accent" />
          <span className="text-sm font-semibold">Connected to Spotify</span>
        </div>

        <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <dt className="text-xs uppercase tracking-wider text-muted">Display name</dt>
            <dd className="mt-1 font-medium">{profile.displayName ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-xs uppercase tracking-wider text-muted">Spotify user ID</dt>
            <dd className="mt-1 font-mono text-sm">{profile.spotifyUserId}</dd>
          </div>
        </dl>

        <div className="flex flex-wrap gap-3 pt-1">
          <a
            href={LOGIN_URL}
            className="rounded-full border border-border px-4 py-2 text-sm font-medium text-text transition-colors hover:bg-surface-2"
          >
            Reconnect
          </a>
          <button
            type="button"
            onClick={handleLogout}
            className="rounded-full border border-border px-4 py-2 text-sm font-medium text-muted transition-colors hover:bg-surface-2 hover:text-text"
          >
            Log out
          </button>
        </div>

        <p className="text-xs text-muted">
          VibeStream stores your Spotify tokens server-side only — they never reach the browser.
        </p>
      </Card>
    </div>
  );
}
