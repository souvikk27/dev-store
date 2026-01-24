# Makefile for intelli-dev-store project

# Variables
PROJECT_DIR = intelli-dev-store
LIBS_PROJECT = Intellidevstore.Libs
SOLUTION = $(PROJECT_DIR)/intelli-dev-store.slnx
STARTUP_PROJECT = $(PROJECT_DIR)/intelli-dev-store.csproj
LIBS_CSPROJ = $(LIBS_PROJECT)/Intellidevstore.Libs.csproj

# Default target
.PHONY: help
help:
	@echo "Available targets:"
	@echo "  make build          - Build the solution"
	@echo "  make run            - Run the application"
	@echo "  make watch          - Run with hot reload"
	@echo "  make clean          - Clean build artifacts"
	@echo "  make restore        - Restore NuGet packages"
	@echo "  make rebuild        - Clean and build"
	@echo "  make migration      - Create a new migration (use NAME=MigrationName)"
	@echo "  make migrate        - Apply migrations to database"
	@echo "  make migration-remove - Remove the last migration"
	@echo "  make migration-list - List all migrations"
	@echo "  make db-update      - Update database to latest migration"
	@echo "  make db-drop        - Drop the database"
	@echo "  make test           - Run tests (if any)"
	@echo "  make format         - Format code with dotnet format"
	@echo "  make lint           - Check code formatting"

# Build targets
.PHONY: restore
restore:
	@echo "Restoring NuGet packages..."
	dotnet restore $(SOLUTION)

.PHONY: build
build: restore
	@echo "Building solution..."
	dotnet build $(SOLUTION) --no-restore

.PHONY: clean
clean:
	@echo "Cleaning build artifacts..."
	dotnet clean $(SOLUTION)
	@if exist "$(PROJECT_DIR)\bin" rmdir /s /q "$(PROJECT_DIR)\bin"
	@if exist "$(PROJECT_DIR)\obj" rmdir /s /q "$(PROJECT_DIR)\obj"
	@if exist "$(LIBS_PROJECT)\bin" rmdir /s /q "$(LIBS_PROJECT)\bin"
	@if exist "$(LIBS_PROJECT)\obj" rmdir /s /q "$(LIBS_PROJECT)\obj"

.PHONY: rebuild
rebuild: clean build

# Run targets
.PHONY: run
run:
	@echo "Running application..."
	dotnet run --project $(STARTUP_PROJECT)

.PHONY: watch
watch:
	@echo "Running with hot reload..."
	dotnet watch --project $(STARTUP_PROJECT)

# Migration targets
.PHONY: migration
migration:
	@if not defined NAME (echo Error: Please specify migration name with NAME=MigrationName && exit /b 1)
	@echo "Creating migration: $(NAME)..."
	cd $(PROJECT_DIR) && dotnet ef migrations add $(NAME) --project ../$(LIBS_PROJECT)

.PHONY: migrate
migrate: db-update

.PHONY: db-update
db-update:
	@echo "Applying migrations to database..."
	cd $(PROJECT_DIR) && dotnet ef database update --project ../$(LIBS_PROJECT)

.PHONY: migration-remove
migration-remove:
	@echo "Removing last migration..."
	cd $(PROJECT_DIR) && dotnet ef migrations remove --project ../$(LIBS_PROJECT) --force

.PHONY: migration-list
migration-list:
	@echo "Listing migrations..."
	cd $(PROJECT_DIR) && dotnet ef migrations list --project ../$(LIBS_PROJECT)

.PHONY: db-drop
db-drop:
	@echo "Dropping database..."
	cd $(PROJECT_DIR) && dotnet ef database drop --project ../$(LIBS_PROJECT) --force

# Code quality targets
.PHONY: format
format:
	@echo "Formatting code..."
	dotnet format $(SOLUTION)

.PHONY: lint
lint:
	@echo "Checking code formatting..."
	dotnet format $(SOLUTION) --verify-no-changes

# Test targets
.PHONY: test
test:
	@echo "Running tests..."
	dotnet test $(SOLUTION)

# Development helpers
.PHONY: dev
dev: restore build run

.PHONY: setup
setup: restore build
	@echo "Project setup complete!"
	@echo "Run 'make migrate' to apply database migrations"
	@echo "Run 'make run' to start the application"
