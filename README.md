# Intelli Dev Store

A .NET 10 application with Entity Framework Core and PostgreSQL.

## Prerequisites

- .NET 10 SDK
- PostgreSQL database
- Make (for Windows, install via chocolatey: `choco install make`)

## Quick Start

```bash
# Setup the project
make setup

# Apply database migrations
make migrate

# Run the application
make run
```

## Available Commands

### Build Commands

- `make build` - Build the solution
- `make clean` - Clean build artifacts
- `make restore` - Restore NuGet packages
- `make rebuild` - Clean and build from scratch

### Run Commands

- `make run` - Run the application
- `make watch` - Run with hot reload (auto-restart on file changes)
- `make dev` - Restore, build, and run in one command

### Database Migration Commands

- `make migration NAME=MigrationName` - Create a new migration
- `make migrate` - Apply all pending migrations to database
- `make db-update` - Same as migrate
- `make migration-remove` - Remove the last migration
- `make migration-list` - List all migrations
- `make db-drop` - Drop the database (use with caution!)

### Code Quality Commands

- `make format` - Format code using dotnet format
- `make lint` - Check code formatting without making changes
- `make test` - Run tests

### Help

- `make help` - Show all available commands

## Examples

### Creating and applying a migration

```bash
# Create a new migration
make migration NAME=AddUserTable

# Apply the migration to database
make migrate
```

### Development workflow

```bash
# Start development with hot reload
make watch
```

### Clean rebuild

```bash
# Clean and rebuild everything
make rebuild
```

## Project Structure

- `intelli-dev-store/` - Main web application project
- `Intellidevstore.Libs/` - Shared library with database context and entities
- `Directory.Build.props` - Global MSBuild properties for Roslyn compatibility

## Configuration

Update your database connection string in `intelli-dev-store/appsettings.json` or `appsettings.Development.json`.

## Notes

- The project uses .NET 10 with Entity Framework Core 10
- Global query filters are configured for soft delete functionality
- The `Directory.Build.props` file ensures Roslyn/CodeAnalysis compatibility with .NET 10
