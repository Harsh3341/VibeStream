import type { Metadata } from "next";
import { Bricolage_Grotesque, Familjen_Grotesk } from "next/font/google";
import { NavRail } from "@/components/nav-rail";
import "./globals.css";

// Distinctive pairing: a characterful display grotesque + a friendly, slightly quirky body grotesk —
// deliberately not Inter/Roboto/system.
const bricolage = Bricolage_Grotesque({
  subsets: ["latin"],
  variable: "--font-bricolage",
  weight: ["400", "600", "700", "800"],
});

const familjen = Familjen_Grotesk({
  subsets: ["latin"],
  variable: "--font-familjen",
  weight: ["400", "500", "600"],
});

export const metadata: Metadata = {
  title: "VibeStream",
  description: "Your listening, understood.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={`${bricolage.variable} ${familjen.variable}`}>
      <body className="min-h-screen">
        <div className="mx-auto flex min-h-screen w-full max-w-6xl flex-col gap-0 md:flex-row">
          <NavRail />
          <main className="flex-1 px-5 py-8 md:px-10 md:py-12">{children}</main>
        </div>
      </body>
    </html>
  );
}
