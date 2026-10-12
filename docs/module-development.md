# My Workbench — Module Development Guide

This is a concise implementation guide. `docs/specification.md` remains the source of truth for product requirements.

## Current project status

The repository is in the verified invoice MVP state, with the Phase 6 hardening pass completed. The active business module is Invoices; roadmap modules remain intentionally deferred until explicitly requested.

## Adding a module

1. Confirm the module's scope is approved and its phase is scheduled. Do not implement future roadmap modules during the invoice MVP.
2. Create a backend project under `src/Modules/` using `MyWorkbench.Modules.<ModuleName>` naming.
3. Inside the project, use clear boundaries for `Domain`, `Application`, `Endpoints`, `Persistence`, `Contracts`, and module registration as needed. Do not add empty abstractions solely to match folders.
4. Create the module's database schema/context/migrations within its owning backend project. Keep other modules from directly using its DbContext or entity types.
5. Add a module registration method and register it explicitly from `MyWorkbench.Api`.
6. Add a module descriptor with a stable ID, user-facing name, description, allow-listed icon, local route path, API prefix, enabled state, and sort order.
7. Add the user interface under `src/my-workbench-web/src/app/modules/<module-id>/`. This is a feature folder in the existing Angular project, not a second application.
8. Add lazy-loaded, typed routes, API services, validation, loading/empty/error states, and accessible UI. Do not put business rules only in the browser.
9. Add unit, integration, frontend, and end-to-end tests for business rules and key user journeys.
10. Add only explicit cross-module contracts if required. Use stable IDs and avoid circular project references.
11. Update README/setup instructions and the specification only when a real architectural/product decision changes.
12. Run all relevant builds/tests and report exact results.

## Verified MVP commands

Use the current project checks as the baseline before introducing a new module:

```bash
dotnet test MyWorkbench.sln --nologo
cd src/my-workbench-web
npm run build -- --configuration development
```

## Module rules

- A module should own its business rules and persistence.
- The app shell may read registered module metadata; it must not know module-specific business logic.
- A disabled module must be hidden in navigation and reject API requests server-side.
- Do not execute arbitrary code or load module code dynamically from a database or HTTP response.
- Store uploads outside the public web root and serve them only through authenticated, authorized endpoints.
- Use UTC timestamps for audit data and explicit local-time-zone settings for user-facing schedules.
- Cross-module dashboard summaries should be optional and resilient when a module is disabled/unavailable.
- Keep the invoice MVP scope closed; do not create future roadmap modules until explicitly approved.
