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

- `http://localhost:5196`
- `https://localhost:7196`

Available endpoints:

- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `POST /api/jobs`
- `PATCH /api/jobs/{id}/status`

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

No authentication, AI integration, or database integration has been added yet.
