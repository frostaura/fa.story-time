SHELL := /bin/bash
BACKEND_SOLUTION := src/backend/StoryTime.slnx
FRONTEND_DIR := src/frontend

.PHONY: lint build test test-coverage up down

lint:
	dotnet format $(BACKEND_SOLUTION) --verify-no-changes
	cd $(FRONTEND_DIR) && npm run lint

build:
	dotnet build $(BACKEND_SOLUTION) --nologo
	cd $(FRONTEND_DIR) && npm run build

test:
	dotnet test $(BACKEND_SOLUTION) --nologo
	cd $(FRONTEND_DIR) && npm run test
	cd $(FRONTEND_DIR) && npm run test:browser-e2e

test-coverage:
	dotnet test $(BACKEND_SOLUTION) --nologo --collect:"XPlat Code Coverage;Format=cobertura;ExcludeByFile=**/obj/**,**/*.g.cs,**/*.generated.cs"

up:
	docker compose up --build -d

down:
	docker compose down
