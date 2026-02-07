.PHONY: help dev-infra dev-api dev-web migrate seed clean install build test lint \
        prod-build prod-up prod-down prod-logs prod-ps prod-restart \
        docker-build-api docker-build-web docker-push

# Default target
help:
	@echo "CommonHall Development & Production Commands"
	@echo ""
	@echo "Infrastructure (Development):"
	@echo "  dev-infra       Start Docker containers (PostgreSQL, Redis, Elasticsearch)"
	@echo "  dev-infra-down  Stop Docker containers"
	@echo "  dev-infra-logs  View Docker container logs"
	@echo ""
	@echo "Development:"
	@echo "  dev-api         Run the .NET API in development mode"
	@echo "  dev-web         Run the Next.js frontend in development mode"
	@echo "  dev             Run both API and web (requires separate terminals)"
	@echo ""
	@echo "Database:"
	@echo "  migrate         Run EF Core migrations"
	@echo "  migrate-add     Add a new migration (usage: make migrate-add name=MigrationName)"
	@echo "  seed            Seed the database with initial data"
	@echo ""
	@echo "Build & Test:"
	@echo "  install         Install all dependencies"
	@echo "  build           Build all projects"
	@echo "  test            Run all tests"
	@echo "  lint            Run linters"
	@echo ""
	@echo "Production:"
	@echo "  prod-build      Build production Docker images"
	@echo "  prod-up         Start production stack"
	@echo "  prod-down       Stop production stack"
	@echo "  prod-logs       View production logs"
	@echo "  prod-ps         List production containers"
	@echo "  prod-restart    Restart production stack"
	@echo ""
	@echo "Docker Images:"
	@echo "  docker-build-api  Build API Docker image"
	@echo "  docker-build-web  Build Web Docker image"
	@echo "  docker-push       Push images to registry"
	@echo ""
	@echo "Utilities:"
	@echo "  clean           Clean build artifacts"
	@echo "  clean-volumes   Remove Docker volumes (WARNING: destroys data)"

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

# =============================================================================
# Production Commands
# =============================================================================

# Docker image registry (override with REGISTRY=your-registry.com make docker-push)
REGISTRY ?= ghcr.io/your-org
VERSION ?= latest

# Build production images
prod-build: docker-build-api docker-build-web
	@echo "Production images built successfully"

# Start production stack
prod-up:
	docker compose -f infrastructure/docker/docker-compose.prod.yml up -d
	@echo "Production stack started"
	@echo "  Web: http://localhost"
	@echo "  API: http://localhost/api"

# Stop production stack
prod-down:
	docker compose -f infrastructure/docker/docker-compose.prod.yml down

# View production logs
prod-logs:
	docker compose -f infrastructure/docker/docker-compose.prod.yml logs -f

# List production containers
prod-ps:
	docker compose -f infrastructure/docker/docker-compose.prod.yml ps

# Restart production stack
prod-restart:
	docker compose -f infrastructure/docker/docker-compose.prod.yml restart

# Restart specific service
prod-restart-api:
	docker compose -f infrastructure/docker/docker-compose.prod.yml restart api

prod-restart-web:
	docker compose -f infrastructure/docker/docker-compose.prod.yml restart web

# =============================================================================
# Docker Image Commands
# =============================================================================

# Build API Docker image
docker-build-api:
	docker build \
		-t $(REGISTRY)/commonhall-api:$(VERSION) \
		-f apps/api/Dockerfile \
		.

# Build Web Docker image
docker-build-web:
	docker build \
		-t $(REGISTRY)/commonhall-web:$(VERSION) \
		-f apps/web/Dockerfile \
		.

# Push images to registry
docker-push:
	docker push $(REGISTRY)/commonhall-api:$(VERSION)
	docker push $(REGISTRY)/commonhall-web:$(VERSION)

# Tag and push with specific version
docker-release:
ifndef version
	$(error Version is required. Usage: make docker-release version=1.0.0)
endif
	docker tag $(REGISTRY)/commonhall-api:$(VERSION) $(REGISTRY)/commonhall-api:$(version)
	docker tag $(REGISTRY)/commonhall-web:$(VERSION) $(REGISTRY)/commonhall-web:$(version)
	docker push $(REGISTRY)/commonhall-api:$(version)
	docker push $(REGISTRY)/commonhall-web:$(version)

# =============================================================================
# Database Commands (Production)
# =============================================================================

# Run migrations in production
prod-migrate:
	docker compose -f infrastructure/docker/docker-compose.prod.yml exec api \
		dotnet CommonHall.Api.dll --migrate

# Create database backup
prod-backup:
	@mkdir -p backups
	docker compose -f infrastructure/docker/docker-compose.prod.yml exec postgres \
		pg_dump -U postgres commonhall > backups/commonhall_$(shell date +%Y%m%d_%H%M%S).sql
	@echo "Backup created in backups/"

# =============================================================================
# Health & Monitoring
# =============================================================================

# Check health of all services
health:
	@echo "Checking service health..."
	@curl -sf http://localhost:5000/health > /dev/null && echo "API: healthy" || echo "API: unhealthy"
	@curl -sf http://localhost:3000 > /dev/null && echo "Web: healthy" || echo "Web: unhealthy"

prod-health:
	@echo "Checking production health..."
	@curl -sf http://localhost/api/health > /dev/null && echo "API: healthy" || echo "API: unhealthy"
	@curl -sf http://localhost > /dev/null && echo "Web: healthy" || echo "Web: unhealthy"
