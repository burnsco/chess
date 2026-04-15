#!/bin/bash
set -e

# --- Config ---
REMOTE_HOST="${REMOTE_HOST:-192.168.2.124}"
REMOTE_PORT="${REMOTE_PORT:-2222}"
REMOTE_USER="${REMOTE_USER:-cburns}"
REMOTE_DOCKER_PATH="${REMOTE_DOCKER_PATH:-/home/cburns/docker/chess}"

FRONTEND_IMAGE="ghcr.io/burnsco/chess-frontend:latest"
BACKEND_IMAGE="ghcr.io/burnsco/chess-backend:latest"

echo "🚀 Starting local build for Chess..."

# --- Build Frontend ---
echo "📦 Building Frontend..."
docker build --network=host --target production -t "$FRONTEND_IMAGE" ./frontend

# --- Build Backend ---
echo "📦 Building Backend..."
docker build --network=host --target production -t "$BACKEND_IMAGE" ./backend

# --- Push to GHCR (Optional but recommended for consistency) ---
echo "⬆️  Pushing images to GHCR..."
docker push "$FRONTEND_IMAGE"
docker push "$BACKEND_IMAGE"

# --- Deploy to Server ---
echo "🚢 Deploying to $REMOTE_HOST..."

# 1. Update the local compose.yml to the remote path if needed, or just trigger pull
# Since the server already has compose.yml, we just tell it to pull and restart.
ssh -p "$REMOTE_PORT" "$REMOTE_USER@$REMOTE_HOST" "cd $REMOTE_DOCKER_PATH && docker compose pull && docker compose up -d --wait"

echo "✅ Deployment complete!"
