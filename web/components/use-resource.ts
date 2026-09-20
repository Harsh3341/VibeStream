"use client";

import { useEffect, useState } from "react";
import { ApiError } from "@/lib/api";

export type ResourceState<T> =
  | { status: "loading" }
  | { status: "error"; error: unknown }
  | { status: "ready"; data: T };

// Minimal client-side data hook: run the fetcher, expose loading/error/ready. No external data lib —
// the app is small and every read is a single authenticated GET.
export function useResource<T>(fetcher: () => Promise<T>): ResourceState<T> {
  const [state, setState] = useState<ResourceState<T>>({ status: "loading" });

  useEffect(() => {
    let active = true;
    setState({ status: "loading" });

    fetcher()
      .then((data) => {
        if (active) {
          setState({ status: "ready", data });
        }
      })
      .catch((error) => {
        if (active) {
          setState({ status: "error", error });
        }
      });

    return () => {
      active = false;
    };
    // Fetchers are created per render; pages pass stable closures. Intentionally run once on mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return state;
}

export function isUnauthorized(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401;
}
