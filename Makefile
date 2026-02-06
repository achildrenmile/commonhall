.PHONY: help dev-infra dev-api dev-web migrate seed clean install build test lint

# Default target
help:
	@echo "CommonHall Development Commands"
	@echo ""
	@echo "Infrastructure:"
	@echo "  dev-infra      Start Docker containers (PostgreSQL, Redis, Elasticsearch)"
	@echo "  dev-infra-down Stop Docker containers"
	@echo "  dev-infra-logs View Docker container logs"
	@echo ""
	@echo "Development:"
	@echo "  dev-api        Run the .NET API in development mode"
	@echo "  dev-web        Run the Next.js frontend in development mode"
	@echo "  dev            Run both API and web (requires separate terminals)"
	@echo ""
	@echo "Database:"
	@echo "  migrate        Run EF Core migrations"
	@echo "  migrate-add    Add a new migration (usage: make migrate-add name=MigrationName)"
	@echo "  seed           Seed the database with initial data"
	@echo ""
	@echo "Build & Test:"
	@echo "  install        Install all dependencies"
	@echo "  build          Build all projects"
	@echo "  test           Run all tests"
	@echo "  lint           Run linters"
	@echo ""
	@echo "Utilities:"
	@echo "  clean          Clean build artifacts"

# Infrastructure commands
dev-infra:
	docker compose -f infrastructure/docker/docker-compose.yml up -d
	@echo "Waiting for services to be healthy..."
	@sleep 5
	@echo "Infrastructure started!"
	@echo "  PostgreSQL: localhost:5432"
	@echo "  Redis: localhost:6379"
	@echo "  Elasticsearch: localhost:9200"

dev-infra-down:
	docker compose -f infrastructure/docker/docker-compose.yml down

dev-infra-logs:
	docker compose -f infrastructure/docker/docker-compose.yml logs -f

# Development commands
dev-api:
	cd apps/api && dotnet watch run --project CommonHall.Api

dev-web:
	cd apps/web && pnpm dev

dev:
	@echo "Run 'make dev-api' and 'make dev-web' in separate terminals"

# Database commands
migrate:
	cd apps/api && dotnet ef database update --project CommonHall.Infrastructure --startup-project CommonHall.Api

migrate-add:
ifndef name
	$(error Migration name is required. Usage: make migrate-add name=MigrationName)
endif
	cd apps/api && dotnet ef migrations add $(name) --project CommonHall.Infrastructure --startup-project CommonHall.Api

seed:
	cd apps/api && dotnet run --project CommonHall.Api -- --seed

# Build commands
install:
	pnpm install
	cd apps/api && dotnet restore

build:
	pnpm build
	cd apps/api && dotnet build

build-api:
	cd apps/api && dotnet build

build-web:
	cd apps/web && pnpm build

# Test commands
test:
	pnpm test
	cd apps/api && dotnet test

test-api:
	cd apps/api && dotnet test

test-web:
	cd apps/web && pnpm test

# Lint commands
lint:
	pnpm lint
	cd apps/api && dotnet format --verify-no-changes

lint-fix:
	pnpm lint --fix
	cd apps/api && dotnet format

# Clean commands
clean:
	rm -rf node_modules
	rm -rf apps/web/node_modules
	rm -rf apps/web/.next
	rm -rf packages/*/node_modules
	rm -rf packages/*/dist
	cd apps/api && dotnet clean
	find apps/api -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find apps/api -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true

# Docker volume cleanup (WARNING: destroys data)
clean-volumes:
	docker compose -f infrastructure/docker/docker-compose.yml down -v
	@echo "All Docker volumes have been removed"
