# JobSearch

Job-search automation command center for tracking opportunities, scoring fit, and organizing an application pipeline.

## Backend

The initial backend skeleton is under `backend/`:

- `JobSearch.Api` - .NET 10 Web API
- `JobSearch.Domain` - domain entities and enums
- `JobSearch.Application` - DTOs, service logic, and EF Core persistence services
- `JobSearch.Tests` - xUnit tests for basic job workflows

### Prerequisites

- .NET 10 SDK

## Docker Local Development

The repository includes a local Docker setup for the API, Angular dev server, SQLite persistence, optional PostgreSQL, and host-based Ollama.

### Start the default stack

```powershell
docker-compose up -d --build
```

Default services:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5000`
- Database: SQLite stored in the `jobsearch-sqlite` Docker volume
- Fit scoring: `Mock`

Stop the stack:

```powershell
docker-compose down
```

### Docker with host Ollama

Run Ollama on the Windows host, pull the recommended model for this machine, then start the API with Ollama enabled.

Create a `.env` file in the repository root (do not commit it):

```env
FIT_SCORING_PROVIDER=Ollama
OLLAMA_BASE_URL=http://host.docker.internal:11434/v1
OLLAMA_MODEL=qwen2.5:14b
```

Then pull the model and start the stack:

```powershell
ollama pull qwen2.5:14b
docker-compose up -d --build api frontend
```

This machine was detected with an NVIDIA RTX 3080 Ti and 12 GB VRAM. Recommended local model: `qwen2.5:14b`; fallback: `llama3.1:8b` if latency or VRAM pressure becomes annoying.

### Docker with PostgreSQL

SQLite remains the default. To test PostgreSQL locally, create or update a `.env` file in the repository root (do not commit it):

```env
DATABASE_PROVIDER=Postgres
DEFAULT_CONNECTION=Host=postgres;Port=5432;Database=jobsearch;Username=jobsearch;Password=jobsearch
```

Then start the stack:

```powershell
docker-compose --profile postgres up -d postgres
# wait a few seconds for Postgres to be ready
docker-compose --profile postgres up -d --build api frontend
```

PostgreSQL data is stored in the `jobsearch-postgres` Docker volume. To return to SQLite defaults, remove the `DATABASE_PROVIDER` and `DEFAULT_CONNECTION` entries from `.env` and run:

```powershell
docker-compose up -d --force-recreate api frontend
```

### Run Locally

From the repository root:

```powershell
dotnet restore backend/JobSearch.sln
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

The API listens on:

- `http://localhost:5000`

Available endpoints:

- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `POST /api/jobs`
- `PATCH /api/jobs/{id}/status`
- `POST /api/jobs/{id}/score`


### Database Configuration

The backend uses EF Core and automatically applies migrations at startup. The active provider is controlled by `Database:Provider` in `appsettings.json`.

Supported values:

- `Sqlite` - default local provider
- `Postgres` - optional local/production-like provider

- Default database file: `backend/JobSearch.Api/jobsearch.db`
- Docker SQLite database file: `/data/jobsearch.db` inside the API container

To change the provider or connection string for a local run, edit `backend/JobSearch.Api/appsettings.json`:

```json
{
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=jobsearch.db"
  }
}
```

PostgreSQL example:

```json
{
  "Database": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=jobsearch;Username=jobsearch;Password=jobsearch"
  }
}
```

### Fit Scoring Configuration

The active provider is controlled by the `FitScoringProvider` key in `appsettings.json`. A fresh local run works without Ollama or other external AI dependencies.

Supported values:

- `Mock` — deterministic local scoring, no dependencies
- `OpenAI` — OpenAI-backed scoring (requires an API key)
- `Ollama` — locally running Ollama model (see [Local AI with Ollama](#local-ai-with-ollama))

To switch providers, edit `backend/JobSearch.Api/appsettings.json`:

```json
{
  "FitScoringProvider": "Mock"
}
```

OpenAI example:

```json
{
  "FitScoringProvider": "OpenAI",
  "OpenAI": {
    "ApiKey": "<your-api-key>",
    "FitScoringModel": "gpt-4o-mini"
  }
}
```

`FitScoringModel` is optional and defaults to `gpt-4o-mini`.

### Local AI with Ollama

Ollama lets you run models locally without an API key. It exposes an OpenAI-compatible API at `http://localhost:11434/v1`.

**1. Install Ollama**

Download from [ollama.com](https://ollama.com) and follow the installer for your OS.

**2. Choose and pull a model**

Recommended models by available VRAM:

| VRAM           | Recommended model                        |
|----------------|------------------------------------------|
| < 4 GB         | `phi3:mini`                              |
| 4–6 GB         | `llama3.2:3b` or `mistral:7b-q4`         |
| 8 GB           | `llama3.1:8b` or `mistral:7b`            |
| 12–16 GB       | `llama3.1:13b` or `qwen2.5:14b`          |
| 24 GB+         | `qwen2.5:32b` or `llama3.1:70b-q4`       |
| Apple M-series | `llama3.1:8b` or larger (unified memory) |

```bash
ollama pull qwen2.5:14b
```

**3. Start the backend**

`appsettings.json` keeps `FitScoringProvider` set to `"Mock"` so a fresh run works without external AI dependencies. To use Ollama locally, edit `backend/JobSearch.Api/appsettings.json`:

```json
{
  "FitScoringProvider": "Ollama",
  "Ollama": {
    "BaseUrl": "http://localhost:11434/v1",
    "Model": "qwen2.5:14b"
  }
}
```

Then run as normal:

```powershell
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

For the Docker API container, use a `.env` file in the repository root so the container can reach Ollama running on the host (see [Docker with host Ollama](#docker-with-host-ollama)).

Example create request:

```http
POST /api/jobs
Content-Type: application/json

{
  "company": "Acme",
  "title": "Senior Full Stack Engineer",
  "location": "Chicago, IL",
  "remoteType": "Hybrid",
  "url": "https://example.com/jobs/1",
  "description": "Build useful things.",
  "fitScore": 90
}
```

Example status update:

```http
PATCH /api/jobs/{id}/status
Content-Type: application/json

{
  "status": "Interested"
}
```


### Scheduled Job Import

The backend includes a hosted scheduled import worker. It is disabled by default and currently uses a no-op importer so the app never scrapes job sites or auto-submits applications.

For local configuration, use the `JobImport` section in `backend/JobSearch.Api/appsettings.json`:

```json
{
  "JobImport": {
    "Enabled": false,
    "IntervalMinutes": 60
  }
}
```

For Docker, enable or tune the worker with environment variables in a local `.env` file:

```env
JOB_IMPORT_ENABLED=true
JOB_IMPORT_INTERVAL_MINUTES=30
```

### Tests

```powershell
dotnet test backend/JobSearch.sln
```

## Frontend

The initial Angular frontend is under `frontend/job-search-ui/`.

### Prerequisites

- Node.js and npm

### Run Locally

Start the backend first. The frontend is configured to call:

- `http://localhost:5000/api`

From the frontend project folder:

```powershell
cd frontend/job-search-ui
npm install
npm start
```

The Angular dev server usually listens on:

- `http://localhost:4200`

### Frontend Features

- Job list page with add job form
- Job detail page
- Simple application pipeline view
- `JobOpportunityService` for backend API calls

No authentication has been added yet. AI integration is optional via Mock, Ollama, or OpenAI configuration, and local SQLite persistence is enabled for backend job data.
