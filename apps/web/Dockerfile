# CommonHall Web - Production Dockerfile
# Simplified build for Next.js 14 application

# =============================================================================
# Stage 1: Dependencies & Build
# =============================================================================
FROM node:20-alpine AS builder

WORKDIR /app

# Install pnpm
RUN corepack enable && corepack prepare pnpm@latest --activate

# Copy workspace configuration
COPY pnpm-workspace.yaml ./

# Copy all package.json files
COPY package.json ./
COPY apps/web/package.json ./apps/web/
COPY packages/shared-types/package.json ./packages/shared-types/
COPY packages/ui/package.json ./packages/ui/

# Install dependencies without lock file
RUN pnpm install --no-frozen-lockfile

# Copy shared packages source
COPY packages/shared-types/ ./packages/shared-types/
COPY packages/ui/ ./packages/ui/

# Build shared packages (ignore errors if they don't have src)
RUN pnpm --filter @commonhall/shared-types build || echo "shared-types build skipped"
RUN pnpm --filter @commonhall/ui build || echo "ui build skipped"

# Copy web app source
COPY apps/web/ ./apps/web/

# Set production environment for build
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Bake in the API URL at build time (empty = same origin, relative URLs)
ARG NEXT_PUBLIC_API_URL=""
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}

# Build the Next.js application
WORKDIR /app/apps/web
RUN pnpm build

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM node:20-alpine AS runner

WORKDIR /app

# Set production environment
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# Create non-root user for security
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Create public directory (may be empty if no static assets)
RUN mkdir -p ./public

# Copy standalone build output
COPY --from=builder --chown=nextjs:nodejs /app/apps/web/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/web/.next/static ./apps/web/.next/static

# Switch to non-root user
USER nextjs

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:3000/ || exit 1

# Start the application
CMD ["node", "apps/web/server.js"]
