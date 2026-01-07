# Getting started

## Prerequisites

-   .NET SDK 8.0+
-   At least 1 SMTP configuration (host/port/email + password)
-   Environment variables (see [Configuration](configuration.md))

## Run locally

From the repo root:

```bash
cd API
dotnet restore
dotnet run
```

Local URLs (from `API/Properties/launchSettings.json`):

-   Swagger UI (root): `http://localhost:5108/`
-   Health check: `GET http://localhost:5108/test`

## Run tests

From the repo root:

```bash
dotnet test API/API.sln
```

Or:

```bash
cd API
dotnet test
```
