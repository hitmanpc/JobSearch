# AGENTS.md

## Project

This is a job-search automation command center for a senior full-stack software engineer.

The app helps track job opportunities, score job fit, generate recruiter messages, and organize the application pipeline.

## Tech Stack

- Frontend: Angular
- Backend: .NET 10 Web API
- Database: SQLite by default, optional PostgreSQL for local production-like testing
- AI integration: mock service by default, optional host-based Ollama, optional OpenAI
- Local containers: Docker Compose with API, Angular dev server, SQLite volume, and optional PostgreSQL profile
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
- Keep the default local setup dependency-light: SQLite and mock AI should work without external services.
- Prefer host-based Ollama over an Ollama container unless the user asks for containerized model serving.

## Backend Guidelines

- Use C# and .NET 10.
- Keep controllers thin.
- Put business logic in services.
- Use DTOs for API requests/responses.
- Add validation where appropriate.
- Use async methods where appropriate.
- Keep database provider selection configuration-driven via `Database:Provider`.
- Keep `Sqlite` as the default provider and `Postgres` as an opt-in provider.
- Keep `FitScoringProvider=Mock` as the committed default.
- Use `Ollama__BaseUrl=http://host.docker.internal:11434/v1` from containers when connecting to host Ollama.
- Recommended local Ollama model for this machine: `qwen2.5:14b`; fallback: `llama3.1:8b`.

## Frontend Guidelines

- Use Angular standalone components if the app is Angular 17+.
- Keep API calls in services.
- Keep components focused.
- Use simple state management first.
- Do not add NgRx unless requested.

## Docker Guidelines

- Use `docker-compose` for local container workflows in this repo.
- Default local stack should start with `docker-compose up -d --build`.
- Persist SQLite data in the Docker volume, not in a tracked repo file.
- Use the PostgreSQL compose profile only when explicitly testing Postgres behavior.
- Do not commit `.env` files or local database files.

## Review Checklist

Before finishing a task:

- Does the project build?
- Are tests passing?
- Did Docker still build if Docker files or backend configuration changed?
- Were unrelated files avoided?
- Is the README updated if setup changed?
- Is the change small enough to review?
