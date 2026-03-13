# Build stage
FROM oven/bun:1.1 AS build
WORKDIR /app
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile
COPY . .
RUN bun run build

# Production stage
FROM nginx:stable-alpine
COPY --from=build /app/dist /usr/share/nginx/html
# Copy a custom nginx config if needed, but the default works for basic React apps.
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
