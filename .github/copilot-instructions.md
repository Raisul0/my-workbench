# GitHub Copilot Instructions — My Workbench

Use `docs/specification.md` as the product and architecture source of truth. Read the sections relevant to the current task before editing code.

## Product scope
- The MVP is the shared application shell, authentication, module registry, and the Invoices module.
- Work & Client Projects, Freelance Opportunities, Career & Job Applications, Automation Center, and cross-module dashboard aggregation are future roadmap phases. Do not implement these until explicitly requested after the invoice MVP is stable.

## Architecture constraints
- Use C# / .NET 10 / ASP.NET Core Web API for the backend.
- Use exactly one Angular frontend project named `my-workbench-web`.
- Each implemented business module has a separate backend `.csproj` and its own feature folder under `src/my-workbench-web/src/app/modules/`.
- Use a modular monolith. Do not add microservices, separate API hosts, or separate frontend projects by default.
- Keep module domain models, application logic, persistence, endpoints, and tests within their owning module. Avoid cross-module DbContext/entity coupling and circular project references.
- Register modules explicitly. The API may return metadata for locally compiled/allow-listed modules, but must never load arbitrary runtime code or frontend import paths from API data.

## Frontend module structure
Each module in `app/modules/[ModuleName]/` must follow this folder structure:
```
app/modules/[ModuleName]/
├── pages/
│   ├── home/
│   │   ├── [module-name]-home.component.ts
│   │   ├── [module-name]-home.component.html
│   │   ├── [module-name]-home.component.scss
│   │   └── [module-name]-home.component.spec.ts
│   ├── detail/
│   │   ├── [module-name]-detail.component.ts
│   │   ├── [module-name]-detail.component.html
│   │   ├── [module-name]-detail.component.scss
│   │   └── [module-name]-detail.component.spec.ts
│   └── [other-pages]/
├── models/
│   ├── [entity-name].model.ts
│   ├── [request-dto].model.ts
│   └── [response-dto].model.ts
├── services/
│   ├── [module-name].api.service.ts
│   ├── [module-name].data.service.ts
│   └── index.ts (barrel export)
└── [module-name].module.ts
```
- Each page component gets its own folder under `pages/` with .ts, .html, .scss, and .spec.ts files
- Use consistent naming: `[module-name]-[page-name].component.*`
- Models (interfaces/types) go in `models/` folder
- API and business logic services go in `services/` folder
- Use barrel exports (index.ts) in `services/` and `models/` for convenient imports
- Shared services extend the `ApiService` base class from `@shared/services`
- Do NOT create unnecessary guide/documentation files for services; inline JSDoc comments are sufficient

## Engineering standards
- Inspect repository state, SDK versions, existing conventions, and git changes before implementation. Preserve existing work.
- Implement one phase at a time, list expected file changes first, and do not silently move on to later phases.
- Use nullable reference types, async I/O, dependency injection, cancellation tokens when appropriate, structured logs, validation, and typed DTOs.
- Always add meaningful application logging for critical flows and failures; use structured log messages with context fields.
- Always add try/catch handling for operations that can fail (startup, I/O, external calls, persistence) and log exceptions with actionable context before rethrowing or returning safe error responses.
- Keep endpoint handlers thin and keep business logic out of Angular components.
- Add tests for every business rule and critical failure path. Run backend build/tests, Angular checks, and relevant end-to-end tests before marking a phase complete.
- Do not report a test as passed unless the command was actually run and passed. Report failures and remaining limitations honestly.
- Never add secrets, customer/private data, uploaded files, generated PDFs, or local database contents to source control.
- **Do not automatically create documentation or guide files.** Only create summary/guide documents when explicitly requested by the user. Do not generate markdown files for planning, notes, or workflow tracking unless the user asks for them.

## Invoice-specific rules
- Uploaded invoice images/PDFs are backgrounds. Users position field bindings manually; do not claim automatic layout understanding/OCR exists.
- Server-side code owns invoice calculations, invoice numbering, file validation, authorization, and final PDF composition.
- Generated invoices and the template versions they reference are immutable.
- Validate upload signatures/content, size and page limits; store files privately using generated storage keys.
- Preserve PDF template content where technically supported and verify with actual image-backed and PDF-backed fixtures.

## External platforms and automation safety
- Future Upwork/Freelancer/LinkedIn-related features start with manual tracking and copy-ready drafts.
- Do not scrape authenticated websites, bypass controls, store platform passwords/cookies, or automatically submit proposals/applications by default.
- Later integrations must be separately authorized, use approved access methods, and comply with current platform terms and API limits.
- Never evaluate arbitrary user code or run user-supplied shell/SQL. Automation actions must come from an allow-list, with bounded retries/timeouts, audit history, redacted logs, secret references (not raw credentials), and human approval for external/high-impact side effects.
