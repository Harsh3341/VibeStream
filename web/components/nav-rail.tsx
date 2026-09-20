"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const LINKS = [
  { href: "/", label: "Dashboard" },
  { href: "/listening", label: "Listening" },
  { href: "/sessions", label: "Sessions" },
  { href: "/analytics", label: "Analytics" },
  { href: "/vibes", label: "Vibes" },
  { href: "/settings", label: "Settings" },
];

export function NavRail() {
  const pathname = usePathname();

  const isActive = (href: string) => (href === "/" ? pathname === "/" : pathname.startsWith(href));

  return (
    <nav className="flex shrink-0 flex-col gap-1 border-b border-border px-5 py-6 md:w-56 md:border-b-0 md:border-r md:px-6 md:py-12">
      <Link href="/" className="mb-6 flex items-baseline gap-1 md:mb-10">
        <span className="font-display text-2xl font-extrabold tracking-tight">Vibe</span>
        <span className="font-display text-2xl font-extrabold tracking-tight text-accent">Stream</span>
      </Link>

      <div className="flex gap-1 overflow-x-auto md:flex-col md:gap-0.5">
        {LINKS.map((link) => (
          <Link
            key={link.href}
            href={link.href}
            className={`rounded-lg px-3 py-2 text-sm font-medium whitespace-nowrap transition-colors ${
              isActive(link.href)
                ? "bg-surface-2 text-text"
                : "text-muted hover:bg-surface hover:text-text"
            }`}
          >
            {link.label}
          </Link>
        ))}
      </div>
    </nav>
  );
}
