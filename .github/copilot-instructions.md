# GitHub Copilot Instructions

## Project

This is a job-search automation command center for a senior full-stack software engineer.

The app helps track job opportunities, score job fit, generate recruiter messages, and manage the application pipeline.

## Tech Stack

- Frontend: Angular
- Backend: .NET 8 Web API
- Database: start simple, later PostgreSQL or DynamoDB
- AI: mock services first, OpenAI integration later
- Hosting target: IONOS frontend, AWS backend later

## Coding Rules

- Make small, focused changes.
- Do not rewrite unrelated files.
- Do not add large dependencies without asking.
- Keep backend controllers thin.
- Put business logic in services.
- Use DTOs for API requests and responses.
- Keep frontend API calls inside Angular services.
- Prefer standalone Angular components if using Angular 17+.
- Do not add NgRx unless specifically requested.
- Add tests for business logic.
- Do not implement scraping for LinkedIn, Indeed, or sites that prohibit automation.
- Do not auto-submit job applications.
- User must review all generated outreach messages before sending.

## Backend

- Use clean C# naming.
- Use async methods for API/repository operations.
- Keep domain models separate from API contracts.
- Add unit tests for scoring, status changes, and message generation.

## Frontend

- Use typed models.
- Keep components focused.
- Avoid putting HTTP logic directly in components.
- Use simple services before adding global state.
- Keep UI clean and recruiter-demo friendly.

## Validation

Before completing a task:
- Build the backend.
- Build the frontend.
- Run available tests.
- Update README if setup or behavior changed.