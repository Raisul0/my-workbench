# My Workbench

One place to manage your work, build your business, and automate your life.

## Invoice MVP status

This repository is in the verified Phase 6 hardening state for the invoice MVP. The current implementation includes:

- a .NET 10 ASP.NET Core API host with explicit module registration and secure auth shell
- a single Angular frontend project at `frontend/my-workbench-web`
- a module registry that exposes only enabled, allow-listed modules
- an invoice module with invoice drafts, line items, settings, template upload, layout binding, preview, and PDF generation
- server-side invoice numbering, totals, validation, file storage, template versioning, and private document handling
- a template editor with drag/resize field placement and preview flows in the Angular frontend
- backend and frontend verification that the invoice MVP builds and tests successfully

## Prerequisites

- .NET SDK 10.0.401 or later compatible feature band
- Node.js 24.15.0+ (Angular 22 requires Node 24.15+)
- npm 10+

## Run locally

### One-click startup in Visual Studio

Open the solution in Visual Studio 2026 and set this project as the startup project:

- `MyWorkbench.DevLauncher`

This launcher starts both the ASP.NET API and the Angular frontend together. It is the recommended way to launch the app with a single click.

- Backend URL: `http://localhost:5109`
- Frontend URL: `http://localhost:4200`

### Manual startup (if needed)

#### 1) Start the API in Visual Studio

Open the solution and set `MyWorkbench.Api` as the startup project.

- Project to run: `src/MyWorkbench.Api/MyWorkbench.Api.csproj`
- Launch profile: `http`
- Local URL: `http://localhost:5109`

#### 2) Start the Angular app

From a terminal in the repo root:

```bash
cd frontend/my-workbench-web
npm install
npm start
```

This starts the frontend at `http://localhost:4200` and proxies `/api` requests to the ASP.NET API automatically.

### 3) Open the site

Open the browser at:

- `http://localhost:4200`

### Backend verification

```bash
dotnet restore MyWorkbench.sln
dotnet build MyWorkbench.sln
dotnet test MyWorkbench.sln --nologo
```

### Frontend verification

```bash
cd frontend/my-workbench-web
npm run build -- --configuration development
```

## Verified results

The current project state has been validated with fresh commands:

- `dotnet test MyWorkbench.sln --nologo` — passed: 11 succeeded, 0 failed
- `cd frontend/my-workbench-web; npm run build -- --configuration development` — succeeded and generated the Angular bundle

## Solution layout

- `src/MyWorkbench.Api` — ASP.NET Core API host and module composition
- `src/MyWorkbench.Shared` — shared module contract types and module metadata
- `src/MyWorkbench.Infrastructure` — shared infrastructure plumbing and storage conventions
- `src/Modules/MyWorkbench.Modules.Invoices` — invoice domain, application logic, persistence, endpoints, and PDF rendering integration
- `frontend/my-workbench-web` — the only Angular application in the repo
- `tests/MyWorkbench.UnitTests` — validation for invoice and module rules

## Architecture notes

- The invoice MVP is the active feature scope. Future roadmap modules remain intentionally deferred.
- The app shell uses a single Angular project and local allow-listed module metadata.
- Module visibility is controlled via server-side registration and `Enabled` metadata.
- No roadmap module backend projects or active routes are added during the invoice MVP.
- Uploaded invoice template files remain background assets until the user binds fields manually in the template editor.
