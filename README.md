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

Run Ollama on the Windows host, pull the recommended model for this machine, then start the API with Ollama enabled:

```powershell
ollama pull qwen2.5:14b

$env:FIT_SCORING_PROVIDER = "Ollama"
$env:OLLAMA_BASE_URL = "http://host.docker.internal:11434/v1"
$env:OLLAMA_MODEL = "qwen2.5:14b"
docker-compose up -d --build api frontend
```

This machine was detected with an NVIDIA RTX 3080 Ti and 12 GB VRAM. Recommended local model: `qwen2.5:14b`; fallback: `llama3.1:8b` if latency or VRAM pressure becomes annoying.

### Docker with PostgreSQL

SQLite remains the default. To test PostgreSQL locally:

```powershell
$env:DATABASE_PROVIDER = "Postgres"
$env:DEFAULT_CONNECTION = "Host=postgres;Port=5432;Database=jobsearch;Username=jobsearch;Password=jobsearch"
docker-compose --profile postgres up -d postgres
Start-Sleep -Seconds 5
docker-compose --profile postgres up -d --build api frontend
```

PostgreSQL data is stored in the `jobsearch-postgres` Docker volume. To return to SQLite defaults, clear those environment variables in the current shell or open a new shell and run:

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

The backend uses EF Core and automatically applies migrations at startup. The active provider is controlled by `Database:Provider`.

Supported values:

- `Sqlite` - default local provider
- `Postgres` - optional local/production-like provider

- Default database file: `backend/JobSearch.Api/jobsearch.db`
- Docker SQLite database file: `/data/jobsearch.db` inside the API container
- Override with environment variables:

```powershell
$env:Database__Provider = "Sqlite"
$env:ConnectionStrings__DefaultConnection = "Data Source=/custom/path/jobsearch.db"
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

PostgreSQL example:

```powershell
$env:Database__Provider = "Postgres"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=jobsearch;Username=jobsearch;Password=jobsearch"
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

### Fit Scoring Configuration

The active provider is controlled by the `FitScoringProvider` key. Configuration is resolved in this order (later sources win):

1. Code fallback — `"Mock"` if nothing else is set
2. `appsettings.json` — committed local-development default (`"Mock"`)
3. Environment variables — override `appsettings.json` at runtime

A fresh local run should work without Ollama or other external AI dependencies.

Supported values:

- `Mock` — deterministic local scoring, no dependencies
- `OpenAI` — OpenAI-backed scoring (requires `OPENAI_API_KEY`)
- `Ollama` — locally running Ollama model (see [Local AI with Ollama](#local-ai-with-ollama))

To switch providers without editing `appsettings.json`, set the env var at runtime:

```powershell
# Run with mock scoring
$env:FitScoringProvider = "Mock"

# Run with OpenAI
$env:FitScoringProvider = "OpenAI"
$env:OPENAI_API_KEY = "<your-api-key>"
```

Optional OpenAI environment variable:

- `OpenAI__FitScoringModel` — overrides the default model (`gpt-4o-mini`)

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

`appsettings.json` keeps `FitScoringProvider` set to `"Mock"` so a fresh run works without external AI dependencies. To use Ollama locally, set:

```powershell
$env:FitScoringProvider = "Ollama"
$env:Ollama__BaseUrl = "http://localhost:11434/v1"
$env:Ollama__Model = "qwen2.5:14b"
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

For the Docker API container, use `host.docker.internal` so the container can reach Ollama running on the host:

```powershell
$env:FIT_SCORING_PROVIDER = "Ollama"
$env:OLLAMA_BASE_URL = "http://host.docker.internal:11434/v1"
$env:OLLAMA_MODEL = "qwen2.5:14b"
docker-compose up -d --force-recreate api
```

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
