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


### Scheduled Remotive Job Import

The backend includes a hosted scheduled import worker that runs inside the existing API process. The default Docker stack does not add a worker container and remains dependency-light: SQLite is used for persistence, mock AI remains enabled, and the scheduled import is disabled unless you opt in.

The committed Docker defaults configure the import provider as Remotive while leaving the schedule off:

```yaml
JobImport__Enabled: ${JOB_IMPORT_ENABLED:-false}
JobImport__Provider: ${JOB_IMPORT_PROVIDER:-Remotive}
JobImport__IntervalMinutes: ${JOB_IMPORT_INTERVAL_MINUTES:-60}
JobImport__RemotiveBaseUrl: ${JOB_IMPORT_REMOTIVE_BASE_URL:-https://remotive.com}
JobImport__RemotiveCategory: ${JOB_IMPORT_REMOTIVE_CATEGORY:-}
JobImport__RemotiveSearchText: ${JOB_IMPORT_REMOTIVE_SEARCH_TEXT:-}
JobImport__RemotiveLimit: ${JOB_IMPORT_REMOTIVE_LIMIT:-50}
```

To enable scheduled Remotive imports in Docker, create or update a local `.env` file in the repository root (do not commit it):

```env
JOB_IMPORT_ENABLED=true
JOB_IMPORT_PROVIDER=Remotive
JOB_IMPORT_INTERVAL_MINUTES=60
JOB_IMPORT_REMOTIVE_BASE_URL=https://remotive.com
JOB_IMPORT_REMOTIVE_CATEGORY=Software Development
JOB_IMPORT_REMOTIVE_SEARCH_TEXT=senior full stack angular
JOB_IMPORT_REMOTIVE_LIMIT=50
```

Apply the setting by recreating the API container:

```powershell
docker-compose up -d --build api frontend
```

To disable the scheduled import again, set `JOB_IMPORT_ENABLED=false` or remove the job import entries from `.env`, then recreate the API container:

```powershell
docker-compose up -d --force-recreate api frontend
```

For non-Docker local runs, use the `JobImport` section in `backend/JobSearch.Api/appsettings.json`:

```json
{
  "JobImport": {
    "Enabled": false,
    "Provider": "Remotive",
    "IntervalMinutes": 60,
    "RemotiveBaseUrl": "https://remotive.com",
    "RemotiveCategory": "",
    "RemotiveSearchText": "",
    "RemotiveLimit": 50
  }
}
```

The importer reads public Remotive job data only. It does not scrape prohibited sites, generate recruiter outreach automatically, or submit applications.

The **My Profile** page includes editable Remotive import preferences that are stored with the candidate profile:

- **Remotive category** maps to the Remotive `category` query parameter. Leave it blank to use `JobImport:RemotiveCategory` from appsettings or environment variables.
- **Remotive search text** maps to the Remotive `search` query parameter. Leave it blank to use `JobImport:RemotiveSearchText`.
- **Remotive limit** maps to the Remotive `limit` query parameter. It must be a positive whole number when provided; leave it blank to use `JobImport:RemotiveLimit`.

Profile-stored Remotive preferences override the appsettings or Docker environment defaults only when they contain a value. Empty category/search fields and an empty limit fall back to the backend configuration values above.

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

- My Profile page with resume text and editable Remotive import preference controls
- Job list page with add job form
- Job detail page
- Simple application pipeline view
- `JobOpportunityService` for backend API calls

No authentication has been added yet. AI integration is optional via Mock, Ollama, or OpenAI configuration, and local SQLite persistence is enabled for backend job data.
