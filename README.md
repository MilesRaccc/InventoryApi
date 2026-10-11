# InventoryApi

A RESTful Web API for managing inventory products, built with ASP.NET Core and PostgreSQL. The project demonstrates API development, database integration, automated testing, and containerized deployment.

## Tech Stack

* **C# / .NET 9**
* **ASP.NET Core Web API**
* **Entity Framework Core 9**
* **PostgreSQL**
* **Docker and Docker Compose**
* **xUnit**
* **SQLite** for isolated API integration tests
* **GitHub Actions** for continuous integration

## Features

* CRUD operations for inventory products
* Data transfer objects (DTOs) to control API input and output
* Request validation and consistent error handling
* Filtering and searching products
* Sorting by supported fields and sort direction
* Pagination with metadata, including total item count and total pages
* PostgreSQL database integration through Entity Framework Core
* Database schema management with EF Core migrations
* Automated API tests
* Docker Compose setup for running the API and PostgreSQL together
* CI workflow for running tests and building the Docker image

## Getting Started

### Prerequisites

* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* Git

Docker Compose starts the API and PostgreSQL in separate containers, so a local .NET SDK or PostgreSQL installation is not required for the Docker-based workflow.

### 1. Clone the repository

```bash
git clone https://github.com/MilesRaccc/InventoryApi
cd InventoryApi
```

### 2. Configure environment variables

Copy the example environment file:

**PowerShell**

```powershell
Copy-Item .env.example .env
```

Open `.env` and configure the PostgreSQL database name, username, and password.

Do not commit `.env` or store real credentials in source control.

### 3. Start the application

From the repository root, run:

```bash
docker compose up --build
```

Docker Compose builds the API image, starts PostgreSQL, waits for the database health check, and starts the API.

Database migrations are applied automatically when the configured startup option is enabled in the Compose environment.

### 4. Access the API

By default, the API is available at:

* **Products endpoint:** `http://localhost:8080/api/products`
* **Swagger UI:** `http://localhost:8080/swagger`

Swagger UI is available when enabled by the application's development configuration.

To stop the containers:

```bash
docker compose down
```

The PostgreSQL data is stored in a Docker volume and persists when the containers are stopped and restarted.

To remove the containers and the stored database volume:

```bash
docker compose down -v
```

**Warning:** Removing the volume permanently deletes the database data stored in it.

## API Overview

The API exposes endpoints for managing products.

| Method   | Endpoint             | Description                                        |
| -------- | -------------------- | -------------------------------------------------- |
| `GET`    | `/api/products`      | Retrieve products, with supported query parameters |
| `GET`    | `/api/products/{id}` | Retrieve a product by ID                           |
| `POST`   | `/api/products`      | Create a product                                   |
| `PUT`    | `/api/products/{id}` | Update a product                                   |
| `DELETE` | `/api/products/{id}` | Delete a product                                   |

The collection endpoint supports query-based operations such as searching, filtering, sorting, and pagination.

Exact parameter names, validation rules, and supported sort fields are defined by the API implementation and can be explored through Swagger when available.

## Running Tests

The solution contains automated tests for API behavior, validation, and query functionality.

Run the test suite locally with the .NET SDK:

```bash
dotnet test InventoryApi.sln
```

Most API integration tests use SQLite to provide an isolated test database. A PostgreSQL-specific integration test verifies case-insensitive search behavior using PostgreSQL's `ILIKE` functionality.

### PostgreSQL integration test configuration

The PostgreSQL-specific test requires a separate test database and a connection string supplied through user secrets or the `INVENTORYAPI_TEST_CONNECTION` environment variable.

Example environment variable format:

```text
Host=localhost;Port=5432;Database=inventoryapi_test;Username=<username>;Password=<password>
```

Use a dedicated test database. Do not point integration tests at a database containing data you need to preserve.

## Continuous Integration

GitHub Actions runs the automated tests and builds the Docker image on repository pushes and pull requests.

This helps verify both application behavior and Docker build compatibility in a clean CI environment.

## Project Structure

```text
InventoryApi/
├── Controllers/
├── Data/
├── Models/
├── Migrations/
├── InventoryApi.Tests/
├── .github/
│   └── workflows/
│       └── tests.yml
├── Dockerfile
├── compose.yaml
├── .dockerignore
├── .env.example
├── InventoryApi.csproj
├── InventoryApi.sln
└── Program.cs
```

## Future Improvements

Potential next steps include:

* API authentication and authorization policies
* More comprehensive PostgreSQL integration tests
* API versioning
* Structured logging and observability
* Deployment to a cloud hosting environment

