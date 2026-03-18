SHELL := /bin/bash
BACKEND_SOLUTION := src/backend/StoryTime.slnx
FRONTEND_DIR := src/frontend

.PHONY: traceability lint build test test-governance frontend-coverage validate-env test-coverage backend-coverage verify up down

traceability:
	python3 scripts/traceability-check.py

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

test-governance:
	python3 scripts/check-test-governance.py

frontend-coverage:
	cd $(FRONTEND_DIR) && npm run test:unit -- --coverage

validate-env:
	python3 scripts/validate-env-examples.py

test-coverage:
	dotnet test $(BACKEND_SOLUTION) --nologo --collect:"XPlat Code Coverage;Format=cobertura;ExcludeByFile=**/obj/**,**/*.g.cs,**/*.generated.cs"

backend-coverage:
	python3 scripts/check-backend-coverage.py

verify: lint build traceability test test-governance frontend-coverage validate-env test-coverage backend-coverage

up:
	docker compose up --build -d

down:
	docker compose down
