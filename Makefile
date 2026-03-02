SHELL := /bin/bash
BACKEND_SOLUTION := src/backend/StoryTime.slnx
FRONTEND_DIR := src/frontend

.PHONY: lint build test up down

lint:
	dotnet format $(BACKEND_SOLUTION) --verify-no-changes
	cd $(FRONTEND_DIR) && npm run lint

build:
	dotnet build $(BACKEND_SOLUTION) --nologo
	cd $(FRONTEND_DIR) && npm run build

test:
	dotnet test $(BACKEND_SOLUTION) --nologo
	cd $(FRONTEND_DIR) && npm run test

up:
	docker compose up --build -d

down:
	docker compose down
