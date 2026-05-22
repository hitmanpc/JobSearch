# JobSearch

Job-search automation command center for tracking opportunities, scoring fit, and organizing an application pipeline.

## Backend

The initial backend skeleton is under `backend/`:

- `JobSearch.Api` - .NET 10 Web API
- `JobSearch.Domain` - domain entities and enums
- `JobSearch.Application` - DTOs, service logic, and in-memory repository
- `JobSearch.Tests` - xUnit tests for basic job workflows

### Prerequisites

- .NET 10 SDK

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

### Fit Scoring Configuration

Fit scoring uses the mock provider by default for local development.

Supported configuration values:

- `FitScoringProvider=Mock` - uses deterministic local scoring
- `FitScoringProvider=OpenAI` - uses the OpenAI-backed scoring service
- `FitScoringProvider=Ollama` - uses a locally running Ollama model

Required environment variables when using OpenAI:

- `OPENAI_API_KEY` - OpenAI API key used by the backend

Optional environment variables:

- `OpenAI__FitScoringModel` - overrides the default fit scoring model

PowerShell example:

```powershell
$env:FitScoringProvider = "OpenAI"
$env:OPENAI_API_KEY = "<your-api-key>"
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

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
ollama pull llama3.1:8b
```

**3. Start the backend with Ollama**

```powershell
$env:FitScoringProvider = "Ollama"
$env:OLLAMA_BASE_URL = "http://localhost:11434/v1"
$env:OLLAMA_MODEL = "llama3.1:8b"
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
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

No authentication, AI integration, or database integration has been added yet.
