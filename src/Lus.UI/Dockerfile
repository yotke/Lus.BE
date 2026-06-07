# ───────────────────────────────────────────────────────────────────
# Stage 1: build the Angular app
# ───────────────────────────────────────────────────────────────────
FROM node:20-alpine AS builder
WORKDIR /app

# Allow selecting development vs production via --build-arg
ARG BUILD_CONFIGURATION=production
ENV NG_CLI_ANALYTICS=ci

# 1) Copy only the files needed to resolve dependencies (better layer cache)
COPY package.json package-lock.json ./

# 2) Install node modules
RUN npm ci

# 3) Copy the rest of the source
COPY . .

# 4) Build into dist/loz-information (browser builder => flat output, no /browser subdir)
RUN npm run build -- --configuration=${BUILD_CONFIGURATION}

# ───────────────────────────────────────────────────────────────────
# Stage 2: serve with nginx
# ───────────────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html

# 1) Remove default content
RUN rm -rf ./*

# 2) Copy the built Angular app
COPY --from=builder /app/dist/loz-information .

# 3) Custom nginx configuration (SPA fallback + caching + security headers)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 4) Entrypoint
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
