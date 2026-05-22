# GitHub Copilot Instructions

## Project

JobSearch is a job-search automation command center for a senior full-stack software engineer. It tracks opportunities, scores fit, generates recruiter messages, and organizes the application pipeline.

## Repository Structure

- `backend/JobSearch.Api` - .NET 10 Web API entry point and thin controllers.
- `backend/JobSearch.Application` - DTOs, application services, repository abstractions, and fit-scoring logic.
- `backend/JobSearch.Domain` - domain entities and enums.
- `backend/JobSearch.Tests` - xUnit test project for application and service behavior.
- `frontend/job-search-ui` - Angular application and UI services.

## Coding Conventions

- Make small, focused changes.
- Do not rewrite unrelated files.
- Keep controllers thin and put business logic in services.
- Use DTOs for API request and response contracts.
- Keep domain models separate from API contracts.
- Keep frontend HTTP calls in Angular services, not components.
- Prefer standalone Angular components.
- Do not add NgRx unless specifically requested.
- Add tests for business logic and behavior changes.
- Use clean C# naming and async methods for I/O-bound operations.
- Do not implement scraping for LinkedIn, Indeed, or other prohibited sites.
- Do not auto-submit applications or recruiter messages.
- Generated outreach or AI output must remain reviewable by the user.

## Testing

- Backend tests use xUnit with `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`.
- Run backend tests from Windows PowerShell, not WSL.
- Preferred command from the repository root: `dotnet test .\backend\JobSearch.sln`.
- The backend solution also builds with `dotnet build .\backend\JobSearch.sln`.

## Build And Run

### Backend

From the repository root:

```powershell
dotnet restore backend/JobSearch.sln
dotnet build backend/JobSearch.sln
dotnet run --project backend/JobSearch.Api/JobSearch.Api.csproj
```

Default API endpoint: `http://localhost:5000`.

Fit scoring defaults to the mock provider for local development. To use OpenAI-backed scoring, set `FitScoringProvider=OpenAI` and `OPENAI_API_KEY` before starting the API.

### Frontend

From `frontend/job-search-ui`:

```powershell
npm install
npm start
```

Other available scripts: `npm run build`, `npm run watch`, and `npm test`.

## Validation Checklist

Before completing a task, confirm the relevant code builds, backend tests pass, and the README is updated if setup or behavior changed.
