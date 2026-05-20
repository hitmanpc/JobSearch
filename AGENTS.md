# AGENTS.md

## Project

This is a job-search automation command center for a senior full-stack software engineer.

The app helps track job opportunities, score job fit, generate recruiter messages, and organize the application pipeline.

## Tech Stack

- Frontend: Angular
- Backend: .NET 10 Web API
- Database: start with in-memory or SQLite, later PostgreSQL or DynamoDB
- AI integration: mock service first, OpenAI integration later
- Deployment target: frontend on IONOS, backend on AWS Lambda/API Gateway or Render

## Rules for Codex

- Make small, focused changes.
- Do not rewrite unrelated files.
- Do not introduce large dependencies without explaining why.
- Prefer clean, readable code over clever abstractions.
- Add tests for business logic.
- Keep domain models separate from API controllers.
- Do not implement real job-site scraping.
- Do not scrape LinkedIn, Indeed, or sites that prohibit automation.
- Do not auto-submit applications.
- The user must review generated messages and applications.

## Backend Guidelines

- Use C# and .NET 10.
- Keep controllers thin.
- Put business logic in services.
- Use DTOs for API requests/responses.
- Add validation where appropriate.
- Use async methods where appropriate.

## Frontend Guidelines

- Use Angular standalone components if the app is Angular 17+.
- Keep API calls in services.
- Keep components focused.
- Use simple state management first.
- Do not add NgRx unless requested.

## Review Checklist

Before finishing a task:

- Does the project build?
- Are tests passing?
- Were unrelated files avoided?
- Is the README updated if setup changed?
- Is the change small enough to review?