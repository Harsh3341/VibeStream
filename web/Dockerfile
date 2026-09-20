# Next.js image. NEXT_PUBLIC_* values are inlined at BUILD time, so the API base is a build arg —
# the browser (on the host) must reach the API at this URL. Context is the repo root.

FROM node:22-alpine AS build
WORKDIR /app

ARG NEXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5062
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL

COPY web/package.json ./
RUN npm install
COPY web/ .
RUN npm run build

EXPOSE 3000
CMD ["npm", "run", "start"]
