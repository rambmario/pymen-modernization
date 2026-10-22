# syntax=docker/dockerfile:1
# Local compose image: Angular build + nginx (proxy /api → API).
# Azure Static Web Apps deploy uses the static output from `npm run build` (Paso 5).

FROM node:22-alpine AS build
WORKDIR /web

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist/web/browser /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
  CMD wget -qO- http://127.0.0.1/healthz >/dev/null || exit 1
