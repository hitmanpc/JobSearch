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


### Database (SQLite)

The backend uses EF Core with SQLite and automatically applies migrations at startup.

- Default database file: `backend/JobSearch.Api/jobsearch.db`
- Override with connection string environment variable:

```powershell
$env:ConnectionStrings__DefaultConnection = "Data Source=/custom/path/jobsearch.db"
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

### Fit Scoring Configuration

Fit scoring uses the mock provider by default for local development.

Supported configuration values:

- `FitScoringProvider=Mock` - uses deterministic local scoring
- `FitScoringProvider=OpenAI` - uses the OpenAI-backed scoring service

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

No authentication has been added yet. AI integration is optional via OpenAI configuration, and local SQLite persistence is enabled for backend job data.
