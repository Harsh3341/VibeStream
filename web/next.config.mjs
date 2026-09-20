/** @type {import('next').NextConfig} */
const nextConfig = {
  // The API base the browser calls. Server-side rewrites aren't used — the SPA calls the API directly
  // with credentialed cookies, which is why the API enables CORS for this origin.
  images: {
    // Album art is served from Spotify's CDN.
    remotePatterns: [{ protocol: "https", hostname: "i.scdn.co" }],
  },
};

export default nextConfig;
