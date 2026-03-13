.PHONY: up down restart build logs ps clean frontend-shell backend-shell

# Docker Compose commands
up:
	docker compose up -d

down:
	docker compose down

restart:
	docker compose restart

build:
	docker compose build

logs:
	docker compose logs -f

ps:
	docker compose ps

clean:
	docker compose down -v --rmi local --remove-orphans
	rm -rf frontend/dist
	rm -rf backend/bin backend/obj

# Local development (non-docker)
frontend-dev:
	cd frontend && bun run dev

backend-dev:
	cd backend && dotnet run

# Shell access
frontend-shell:
	docker compose exec frontend sh

backend-shell:
	docker compose exec backend sh
