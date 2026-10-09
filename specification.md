# My Workbench
## Technical Specification and Copilot Implementation Brief

**Document version:** 1.1  
**Date:** 9 October 2026  
**Status:** MVP scoped; future modules defined as a roadmap  
**Backend:** C# / .NET 10 / ASP.NET Core Web API  
**Frontend:** Angular 22+ / TypeScript / Angular Material 3  
**Database:** Microsoft SQL Server  
**Initial functional module:** Invoice Template Designer and PDF Generator  
**Product name:** My Workbench  
**Long-term purpose:** Personal work operating system for projects, freelancing, career management, and automation

---

## 1. Instructions for GitHub Copilot

Treat this specification as the source of truth for the initial implementation. Build the solution in small, verifiable phases; do not attempt to generate the entire application in one response.

Before changing files:

1. Inspect the existing repository and identify its current structure, SDKs, conventions, and uncommitted changes.
2. If the repository is empty, create the structure defined in Section 5. If it already contains a solution, preserve working code and adapt the structure instead of overwriting it.
3. Propose a short implementation plan for the current phase, including the files expected to change.
4. Implement only the current phase and its dependencies.
5. Build the .NET solution, run the relevant tests, run frontend checks, and fix errors before declaring the phase complete.
6. Update the README and any module-development notes when setup or architecture changes.

Implementation rules:

- Use .NET 10 and compatible stable package versions. Use the latest supported servicing patch installed in the development environment; do not downgrade to .NET 8 or .NET 9.
- Use the latest stable Angular major compatible with the available Node.js LTS environment. This specification assumes Angular 22 or newer when available as stable. Do not use deprecated Create React App patterns or introduce a second frontend project.
- Use C# nullable reference types, asynchronous I/O, dependency injection, structured logging, and cancellation tokens for long-running I/O where appropriate.
- Keep module boundaries explicit. Do not turn the application into microservices or create a separate web host for each module.
- Do not put business logic in Angular components or ASP.NET endpoint handlers. Keep endpoint handlers thin and delegate to application services.
- Do not use arbitrary reflection, dynamic code execution, or scripting to resolve invoice fields. Use an explicit, allow-listed field-binding registry.
- Do not silently clip invoice content, silently fall back to another template, or report successful PDF generation when it failed. Surface actionable validation errors.
- Do not hardcode credentials, connection strings containing secrets, absolute local paths, or production URLs.
- Do not commit uploaded invoice templates, generated PDFs, user data, secrets, build output, or local database files to source control.
- Avoid unnecessary dependencies. Check the license and platform support of any new PDF, image-processing, authentication, or UI library before adding it.
- Use real implementation code rather than placeholder methods, fake repositories, sample-only endpoints, or `TODO` comments for required features.
- If an external library API differs from this specification, consult the library's current official documentation and implement the equivalent supported behavior. Document material deviations.
- Keep the code readable for a professional C# developer. Prefer explicit names, small methods, and tests over clever abstractions.

At the end of every phase, report: files/projects changed, features completed, build/test commands and results, any known limitations, and the next phase that is ready to begin.

## 2. Product Vision and Goals

**My Workbench** is a private personal work operating system: one secure website for organizing the user's professional work, freelance business, career opportunities, documents, and future automations. It begins with an invoice PDF generator and grows through independently developed modules without becoming a collection of disconnected applications.

The product is intended for one primary user in v1. Its architecture should support the user's evolving workflows while keeping each module focused, testable, and independently organized. “Modular” means separate backend projects and separate frontend feature areas inside one Angular application—not separate deployments, microservices, or multiple frontends.

### Primary goals

1. Provide one website, one Angular frontend project, one ASP.NET Core API host, one authentication experience, and a consistent navigation shell.
2. Keep every implemented business module in its own .NET project with a clear domain, application services, persistence boundary, API endpoints, tests, and explicit registration.
3. Keep every module's views, routes, API clients, and components inside the single Angular project, grouped by feature and lazy-loaded.
4. Deliver the Invoice Template Designer and PDF Generator as the first complete business module.
5. Establish stable module metadata, settings, storage, logging, and security conventions so additional modules can be added without rewriting existing modules.
6. Plan for tracking clients and projects, freelance opportunities, job applications, and user-controlled automations, while keeping these out of the first release until explicitly scheduled.
7. Keep private documents, proposals, CVs, job notes, invoices, and platform credentials secure. Do not send personal data to external services without an explicit user-triggered feature and configuration.
8. Start with one authenticated administrator. Do not build multi-tenant SaaS, team collaboration, or organization-wide permissions into v1.

### Product module roadmap

The left sidebar is the stable entry point. Only modules that are actually implemented, registered, enabled, and authorized may appear as usable destinations. Roadmap modules are design commitments for future phases, not empty pages to create during the invoice MVP.

| Module | Intended responsibility | Delivery scope |
|---|---|---|
| Dashboard / Work Overview | Personal summary, quick actions, recently changed work, upcoming follow-ups, and module shortcuts | Minimal real-data dashboard in v1; expand as modules arrive |
| Invoices | Reusable invoice designs, invoice data entry, PDF preview/generation, invoice history, and issuer settings | **First functional module / MVP** |
| Work & Client Projects | Client/contact records, engagements, projects, milestones, tasks, deliverables, project notes, and related documents | Future phase |
| Freelance Opportunities | Track opportunities discovered on Upwork, Freelancer, referrals, and other sources; proposal drafts, bid details, deadlines, statuses, and follow-ups | Future phase; manual entry first |
| Career & Job Applications | Track roles, companies, application status, CV/cover-letter versions, recruiter contacts, interviews, notes, and follow-ups | Future phase; manual entry first |
| Automation Center | User-configured schedules, approved triggers/actions, execution history, retries, and notifications | Future phase, after a secure automation design is approved |
| Finance extensions | Payment tracking, expenses, income summaries, and project-related financial views | Future phase; not a replacement for a full accounting system |

### MVP scope

The first release is complete when the shared application shell and secure login work, the module registry is extensible, and the Invoices module can upload a design, configure its field layout, save versioned templates, create invoices, calculate totals, generate PDFs, and retrieve invoice history. Build the architecture to support later modules, but do not implement future module business logic in the MVP.

### Not in the initial release

- Automatic OCR or AI interpretation of an uploaded invoice design.
- Work/client project management, freelance application tracking, job application tracking, or automation execution beyond architectural extension points.
- Automatically scraping Upwork, Freelancer, LinkedIn, or other sites; automatically submitting proposals or job applications; bypassing platform restrictions; or storing platform passwords/cookies.
- Sending invoices by email, online payments, accounting integrations, recurring invoices, multi-user company permissions, or public SaaS tenancy.
- Electronic invoice compliance, tax filing, or a claim that generated invoices meet any country's legal/e-invoicing standard.
- Separate frontend projects, microservices, message brokers, Kubernetes, or arbitrary plugin code loaded at runtime.
- Editing the contents of an existing generated PDF. Generated invoice records are immutable; create a corrected invoice or a new draft instead.

## 3. Important Product Behavior

An uploaded invoice design is a **background**, not a fully understood form. The application must not claim it can automatically identify every label, blank, or table. The user uploads an image/PDF, then manually adds and positions field bindings and a line-item table in the template editor. Automatic field detection may be considered in a later phase.

The invoice generator overlays text and table rows onto the selected background and exports a PDF. For PDF templates, preserve the chosen source page as the background and overlay data; do not rasterize the PDF unnecessarily for final output. A PDF.js-based frontend preview may render a visual canvas for the editor, while the backend PDF engine composes the downloadable result.

The initial template design supports a selected page from an uploaded PDF or a single image. When a generated invoice spans multiple pages, the selected background repeats on each output page. Invoice fields can be configured as first-page-only, every-page, or last-page-only. The line-item region splits rows across pages and totals appear on the final page. Validate this behavior with automated fixtures before considering the PDF engine complete.

## 4. Technology Decisions

| Area | Decision | Guidance |
|---|---|---|
| Runtime | .NET 10 LTS | Use latest compatible servicing patches. |
| Backend | ASP.NET Core Web API | Prefer grouped Minimal API endpoints or thin controllers. Pick one style and use it consistently. |
| Language | C# 14 where supported by the .NET 10 SDK | Nullable reference types enabled. |
| Frontend | Angular, standalone components, TypeScript | One Angular application; lazy-load feature routes. |
| UI toolkit | Angular Material 3 with a custom theme | Do not add a second full component system. Use Angular CDK where useful for drag/drop. |
| Icons | `lucide-angular` or a consistent icon set | Use accessible labels; avoid emoji as functional icons. |
| Frontend preview | PDF.js | Render the uploaded PDF page in the template editor. |
| PDF composition | PDFsharp Core 6.x or a later compatible stable release | MIT-licensed; verify PDF import and overlay compatibility using test templates. |
| Data access | EF Core 10 with SQL Server provider | Code-first migrations; decimal types for money. |
| Database | SQL Server | Connection string from user-secrets/environment configuration. |
| File storage | `IFileStorage` abstraction with local filesystem implementation for v1 | Store outside the web root; only expose files through authorized API endpoints. |
| Authentication | ASP.NET Core Identity with secure cookies | Single administrator in v1; use anti-forgery protection for state-changing requests. |
| Logging | `ILogger<T>` with structured logging | Never write invoice bodies, passwords, or file contents to logs. |
| Testing | xUnit for backend; Angular's supported unit-test runner; Playwright for key browser flows | Test calculations, template mapping, upload validation, PDF output, and navigation. |

Do not introduce QuestPDF solely for this requirement without a deliberate library review: the central operation is overlaying text on an existing PDF page, and PDFsharp's import/drawing model is a direct fit. Re-check current licensing and APIs when package versions are selected. The final renderer must be verified with both image-backed and PDF-backed fixtures because PDF feature compatibility can vary by source document.

## 5. Solution Architecture and Project Layout

Use a **modular monolith**. Modules are separate .NET projects and separately testable, but run inside a single ASP.NET Core process and share a deployment unit in v1. The product deliberately has **one Angular frontend project**. Avoid premature microservices and avoid making the module system execute arbitrary code at runtime.

```text
MyWorkbench/
├── MyWorkbench.sln
├── global.json                           # pin an installed .NET 10 SDK feature band
├── Directory.Build.props                 # nullable, implicit usings, analyzers
├── .editorconfig
├── .gitignore
├── README.md
├── docs/
│   ├── specification.md
│   └── module-development.md
├── src/
│   ├── MyWorkbench.Api/                  # HTTP host, auth pipeline, OpenAPI, module composition
│   ├── MyWorkbench.Shared/               # minimal module contracts and shared DTO primitives
│   ├── MyWorkbench.Infrastructure/       # identity, storage, configuration, shared technical services
│   └── Modules/
│       └── MyWorkbench.Modules.Invoices/
│           ├── Domain/
│           ├── Application/
│           ├── Endpoints/
│           ├── Persistence/
│           ├── Pdf/
│           ├── Contracts/
│           └── ModuleRegistration.cs
│       # Add Projects, Opportunities, Career, and Automations backend projects only in their future phases.
├── frontend/
│   └── my-workbench-web/                 # the only Angular project
│       └── src/app/
│           ├── core/                     # auth, API client, guards, shell, interceptors
│           ├── shared/                   # reusable controls, models, pipes
│           ├── modules/
│           │   ├── dashboard/
│           │   └── invoices/             # invoice pages, components, services, routes
│           │       # Add feature folders for projects/opportunities/career/automations later.
│           └── app.routes.ts
└── tests/
    ├── MyWorkbench.UnitTests/
    ├── MyWorkbench.IntegrationTests/
    └── frontend-e2e/
```

The API host owns composition and hosting; it must not own module-specific business logic. Each module owns its domain entities, rules, application services, endpoint groups, persistence configuration, migrations or schema conventions, module registration, and unit tests. The shared project should remain small and must not become a dumping ground for module-specific helpers.

### Frontend/project boundary clarification

The user requires a single frontend project. Therefore, Invoices views and components must live in `frontend/my-workbench-web` under `src/app/modules/invoices/`; they must **not** be created as a separate Angular application or as a frontend `.csproj`. The corresponding backend logic lives in `MyWorkbench.Modules.Invoices.csproj`. The same rule applies to future modules: for example, `MyWorkbench.Modules.Opportunities.csproj` will have its Angular screens in `src/app/modules/opportunities/` inside the existing frontend.

### Responsibilities

**MyWorkbench.Api**

- Configures ASP.NET Core, authentication, authorization, anti-forgery, exception handling, OpenAPI, logging, health checks, and API version conventions if used.
- Registers shared infrastructure and explicitly registers implemented/enabled modules.
- Exposes `GET /api/modules` based on registered modules and configuration.
- Serves the Angular build or is deployed behind a same-origin reverse proxy in production. Development uses Angular's proxy to forward `/api` requests.
- Must not contain invoice calculations, invoice template layouts, PDF composition, proposal logic, career workflow logic, or automation execution logic.

**MyWorkbench.Shared**

- Contains only stable cross-module contracts, such as `ModuleDescriptor`, module registration conventions, common ID/response primitives where justified, and optional dashboard-provider interfaces introduced only when needed.
- Must not contain entities owned by an individual module or introduce abstractions before a second real use case requires them.

**MyWorkbench.Infrastructure**

- Owns authentication setup, storage abstraction and implementation, common configuration, and genuinely cross-cutting technical services.
- Defines interfaces such as `IFileStorage`; module code must not depend on hard-coded physical paths.
- Keeps secrets out of source control and never gives modules unrestricted access to secret values they do not need.

**MyWorkbench.Modules.Invoices**

- Owns invoice templates, immutable template versions, layout validation, invoice entities/calculations, invoice endpoints, the explicit template-field binding registry, and PDF rendering.
- Owns its EF Core configurations and migration/schema boundary (for example, an `Invoices` schema).
- Exposes an explicit registration method such as `AddInvoicesModule` and `MapInvoicesModule` called by the API host.
- Must not reference Angular source code or depend on UI components.

**Angular frontend**

- Owns the app shell, sidebar, dashboard, lazy-loaded feature routes, module-specific screens, typed API clients, client-side form validation, and the invoice template visual editor.
- Does not perform authoritative totals, invoice numbering, file signature checks, or final PDF composition. These are backend responsibilities.
- Uses typed request/response models and a centralized API base/proxy configuration.

### Future module boundaries and integration rules

- Each future business module gets its own backend `.csproj` and its own Angular feature folder in the existing frontend. It does not get its own API host, Angular workspace, database server, or deployment by default.
- Each module owns its schema/context and business rules. Do not let one module directly manipulate another module's EF Core entities or DbContext.
- When a relationship crosses module boundaries, store a stable external/module-owned identifier and use an explicit application contract to retrieve the needed display data. Avoid circular project references and cross-module database joins in business logic.
- Module metadata must be explicit and allow-listed. A module ID from configuration may enable a compiled module; it must never select an arbitrary .NET type, assembly path, JavaScript import path, or executable file.
- Keep shared file storage private. Modules store storage keys and metadata, not public filesystem paths. All downloads require an authenticated, authorized API request.
- Use common conventions for UTC timestamps, IDs, structured logging, error responses, validation, paging, and tests; do not force every module into an overly generic repository or generic workflow engine.
- Future integration with external services must be opt-in, separately configured, documented, and tested. Platform integrations must use approved APIs or user-authorized flows and comply with the relevant platform terms. Do not build scraping or auto-submit behavior by default.

### Planned future backend projects (do not create during the invoice MVP)

| Project | Owns | Future frontend folder |
|---|---|---|
| `MyWorkbench.Modules.Projects` | Clients, contacts, projects, milestones, tasks, deliverables, work logs, and project document references | `src/app/modules/projects/` |
| `MyWorkbench.Modules.Opportunities` | Freelance opportunity records, proposal drafts/versions, bid details, pipeline status, platform/source metadata, and follow-ups | `src/app/modules/opportunities/` |
| `MyWorkbench.Modules.Career` | Job applications, resume/cover-letter versions, recruiter contacts, interviews, application history, and follow-ups | `src/app/modules/career/` |
| `MyWorkbench.Modules.Automations` | Automation definitions, allow-listed triggers/actions, execution state/history, retry policy, and approval requirements | `src/app/modules/automations/` |

Create these projects only when their respective implementation phase is approved. The MVP must not show dead links or imply these features are already available.

## 6. Module Registry and Sidebar

Create a small backend module descriptor with, at minimum:

- `id`: stable, lowercase identifier such as `invoices`.
- `displayName`: user-facing name such as `Invoices`.
- `description`: short explanation.
- `icon`: allow-listed icon key.
- `routePath`: frontend route such as `/invoices`.
- `apiBasePath`: module API prefix.
- `enabled`: resolved server-side from configuration and registration.
- `sortOrder`: integer used to order navigation.

`GET /api/modules` returns the enabled descriptors for the authenticated user. The Angular app maps module IDs to locally compiled, allow-listed lazy routes. Do not accept a server-provided arbitrary JavaScript import path or load executable code from the API.

Configuration example:

```json
{
  "Modules": {
    "Invoices": {
      "Enabled": true
    }
  }
}
```

The sidebar must only display modules that are implemented, registered, enabled, and authorized. A hidden sidebar item is not an authorization control: disabled modules must also reject their API requests server-side. Unknown module IDs should be ignored and logged safely rather than crashing app startup. Do not display roadmap modules as clickable placeholders; add them only when their backend and frontend are implemented.

## 7. Authentication, Profile, and Settings

This is a personal site, but invoice/customer information and uploaded documents are private. The v1 site must require authentication for all application pages except login and health checks.

- Use ASP.NET Core Identity with cookie authentication.
- Seed one administrator only through local user-secrets or deployment environment variables. Do not create a known default password in source code.
- Configure secure, HttpOnly cookies, an appropriate SameSite setting, HTTPS in non-development environments, and anti-forgery validation for mutation requests.
- For local development, configure Angular's dev proxy so `/api` calls use the same browser origin and cookie behavior is predictable.
- Provide `/api/auth/me`, login, and logout endpoints; return generic login errors that do not reveal account existence.
- Store no authentication token in browser local storage.
- Rate-limit login and expensive upload/PDF-generation endpoints.
- The personal profile should have configurable locale/time zone defaults. Initial defaults may be `en-MY`, `Asia/Kuala_Lumpur`, and `MYR`, but none is hard-coded as an unchangeable business rule.
- A business/issuer profile stores the name, address, email, phone, registration number, tax number/label, and optional default terms to prefill invoices. Generated invoices snapshot the data used at the time of generation.

## 8. Primary User Experience

### Main application shell

- Desktop layout with a fixed/collapsible left sidebar, compact top header, breadcrumb/title area, and a central content area.
- On narrow screens, turn the sidebar into a drawer and stack forms. The invoice template editor is optimized for desktop/tablet but must not make the rest of the app unusable on mobile.
- The sidebar has Dashboard at the top, then only enabled modules grouped by purpose as needed (for example, Work, Opportunities, Career, Documents/Finance, Automations). In the initial release, only Dashboard and Invoices need to be functional.
- Include loading, empty, error, success, disabled, and confirmation states for all major flows.
- Use one consistent design system. Preferred direction: light-first surfaces, neutral/slate text, restrained indigo/blue accent, subtle borders, moderate corner radii, restrained shadows, and generous spacing. Avoid excessive gradients, glass effects, and decorative animation.
- Suggested tokens: background `#F7F8FC`, surface `#FFFFFF`, primary `#4F46E5`, text `#101828`, muted text `#667085`, border `#E4E7EC`, success `#16A34A`, danger `#DC2626`. Tokens must be centralized and themeable.
- Use accessible labels and keyboard interaction. Aim for WCAG 2.2 AA contrast and focus behavior.
- Use consistent record list patterns across modules: search, status filters, sorting, pagination, clear empty states, and a predictable details/edit experience where relevant.
- Use confirmations for destructive or externally visible actions. Clearly distinguish draft, completed, archived, failed, and awaiting-review statuses.

### Dashboard / Work Overview

- In v1, provide a welcome view, a prominent `Create invoice` action, enabled-module shortcuts, and recent invoices when records exist.
- When future modules are implemented, the dashboard may aggregate accurate counts, recent activity, upcoming deadlines/follow-ups, and tasks due soon through explicit module dashboard contracts or purpose-built read models.
- Do not couple the dashboard directly to every module's database context. Modules should provide small, typed summaries only where needed.
- Do not invent charts, counts, recommendations, or fake metrics for an empty database. An unavailable/disabled module must not appear to have data.
- A future “Today / Next actions” area can surface overdue follow-ups, proposal deadlines, job interview dates, project milestones, and automation failures. Every item must deep-link to its owning module and show the source/status.

### Initial invoice module navigation

- `Overview`: recent invoices, quick actions, and template count.
- `Create invoice`: data-entry workflow.
- `Templates`: list, create, edit, archive, and preview template layouts.
- `Invoice history`: searchable and paginated list with view/download actions.
- `Settings`: issuer/business profile and invoice defaults.

### Future module UX expectations

- **Work & Client Projects:** list/detail views for clients and projects; project status, milestones, tasks, deliverables, notes, document references, and links to associated opportunity/invoice records when explicit integration exists.
- **Freelance Opportunities:** pipeline/list and detail views; source platform, source URL, budget/rate, deadline, proposal draft, status, next follow-up, activity history, and final outcome. Manual entry and copy-ready proposal drafting come first.
- **Career & Job Applications:** pipeline/list and detail views; company/job facts, job posting URL, status, CV/cover-letter artifact versions, contacts, interview milestones, notes, and follow-up reminders. Manual entry comes first.
- **Automation Center:** automation list, readably configured trigger/condition/action steps, enable/disable control, approval queue when needed, and an execution history with inputs/outputs redacted appropriately. Do not expose an arbitrary-code editor.

## 9. Invoice Template Designer

### 9.1 Upload flow

The template creation flow is a short stepper:

1. Upload source design.
2. Choose source page and page setup.
3. Place and configure fields/table region.
4. Preview using sample data.
5. Name and save the template.

Support:

- PDF template source. Detect page count and dimensions, display page thumbnails, and let the user select one source page to serve as the repeated page background.
- PNG and JPEG image sources. For images, let the user choose A4 or US Letter and portrait/landscape orientation in v1. Fit the image using a user-selectable `contain` or `cover` policy; show the chosen crop/fit in preview.
- Reject unsupported content, encrypted/unreadable PDFs, invalid files, and pages with unsupported dimensions using an actionable message.
- The original file is stored privately under an application-generated key. Never use the original filename as a storage path.
- Initial default limits: image 10 MB, PDF 20 MB, configurable by app settings; maximum PDF page count examined for upload is configurable. Do not decode arbitrarily large image dimensions or process unlimited page counts.
- Accept only allow-listed file extensions and validate actual file signature/content server-side. Do not trust the browser MIME type or filename alone.

### 9.2 Editor layout

Use a three-panel desktop editor:

- **Left panel:** field palette grouped into Invoice, Issuer, Customer, Totals, Page, Static Text, and Line Items.
- **Center canvas:** template page with background and positioned overlays. Keep its aspect ratio correct at all zoom levels.
- **Right panel:** property inspector for the selected overlay or line-item table.
- **Top toolbar:** template name, save, undo/redo if implemented, zoom in/out, fit-to-page, preview, and status for unsaved changes.

Required interactions:

- Drag a field from the palette onto the template, or select a field and click the page to place it.
- Select/move/resize a placed field; delete it with a clear control and keyboard Delete/Backspace when focus is not in a text input.
- Show selection handles and a visible outline for the selected element without including editor controls in the generated PDF.
- Edit binding, display label where relevant, x/y/width/height, font family, font size, font color, bold/italic, text alignment, vertical alignment, single-line/wrap behavior, formatting, and page repeat policy.
- Use normalized page coordinates (0.0-1.0) with a top-left origin in stored layout data. A single tested conversion function maps normalized positions to the output PDF's page point coordinates. Coordinates must survive zoom, viewport size changes, and different page dimensions.
- Offer a zoom-to-fit control and keep the editor canvas scrollable; never stretch the document to a non-proportional ratio.
- A preview uses realistic sample values. Make clear that sample preview values are not saved invoice data.
- Warn about overlapping fields and fields outside the printable page. Overlap warnings should be advisory because some intentional overlap is valid.
- Preserve the exact layout version when it is used by an invoice.

### 9.3 Field types and bindings

Use an explicit allow-list. Suggested bindings:

**Invoice**

- `invoice.number`
- `invoice.issueDate`
- `invoice.dueDate`
- `invoice.currencyCode`
- `invoice.notes`
- `invoice.terms`

**Issuer**

- `issuer.name`
- `issuer.address`
- `issuer.email`
- `issuer.phone`
- `issuer.registrationNumber`
- `issuer.taxNumber`
- `issuer.taxLabel`

**Customer**

- `customer.name`
- `customer.address`
- `customer.email`
- `customer.phone`
- `customer.registrationNumber`
- `customer.taxNumber`

**Totals**

- `totals.subtotal`
- `totals.discountTotal`
- `totals.taxTotal`
- `totals.grandTotal`

**Page**

- `page.number`
- `page.count`

The line-item table is configured separately with columns from `items[].description`, `items[].quantity`, `items[].unitPrice`, `items[].discountPercent`, `items[].taxRate`, `items[].lineSubtotal`, `items[].taxAmount`, and `items[].total`.

Field types should include `Text`, `Date`, `Money/Number`, `StaticText`, `PageNumber`, and `LineItemsTable`. More types may be added through a clear registry. The client should be given field metadata from a stable contract or a compiled matching registry. The server must validate binding names and types again before rendering. Never resolve arbitrary property paths supplied by the user through reflection.

### 9.4 Field formatting and layout

- Date format selection must use a small allow-list of known formats; no executable format expressions.
- Money fields format using the invoice currency code and configured currency precision.
- Text layout must not spill into neighboring elements. Support wrapping where useful and a configured overflow strategy such as shrink-to-fit within a minimum font size or return a preview/generation warning. Do not silently omit text.
- Use bundled/licensed fonts and a portable PDFsharp font resolver so output does not depend on the machine's installed fonts. Choose fonts with the needed glyph coverage for user-entered names, addresses, and currency characters.
- Validate font size and dimensions to prevent invalid or extreme values.
- Table column widths must be positive and collectively fit within the table region. Row height, header settings, borders, padding, alignment, and wrapping are configurable.
- If the uploaded background already contains table lines and headings, default the table overlay to transparent/no borders; let the user enable generated headers/borders if desired.
- Support a page repeat policy for normal fields: `FirstPage`, `EveryPage`, or `LastPage`. Totals normally use `LastPage`; issuer/customer/invoice header fields can be configured to repeat.

### 9.5 Template versioning

- A template has a stable identity and one active version.
- Every saved material layout/background change creates a new immutable template version, or otherwise preserves all versions already referenced by invoices.
- A version contains the source file storage key, source type, chosen PDF page (when applicable), output page width/height in PDF points, layout schema version, layout JSON, and timestamps.
- Draft invoices can use the active version, but a generated invoice references the exact immutable version used to produce its PDF.
- Archive templates instead of deleting a template that is referenced by invoice records.
- Layout JSON must contain a `schemaVersion`; add a tested migration strategy for future layout-schema changes.

## 10. Invoice Creation and Generation

### 10.1 Invoice entry screen

Create a single-page or stepped form with these sections:

**Document details**

- Template (required).
- Invoice number (assigned by server when the draft is first saved; configurable prefix and sequence).
- Issue date (default local current date).
- Due date (optional).
- Currency code (default from settings, such as MYR; user-selectable from supported/configured currencies).

**Issuer**

- Prefill from the saved business profile; copy the values into the invoice snapshot so a later profile edit does not change an existing invoice.

**Customer / bill-to**

- Name (required), address, email, phone, registration number, and tax number as optional fields.
- Customer details are entered per invoice in v1. Reusable customer directory may be added later without changing the invoice model's snapshot behavior.

**Line items**

- Description (required).
- Quantity (decimal, greater than zero).
- Unit price (decimal, zero or greater).
- Discount percentage (optional, 0-100; default 0).
- Tax rate (optional, 0-100; default from settings if configured).
- Add, remove, and reorder lines.
- Show live subtotal, discount, tax, and grand total.
- Support notes and payment/terms text.

### 10.2 Calculation rules

- Calculate authoritatively on the backend. Frontend live calculations are for convenience and must use equivalent formulas.
- Use C# `decimal`, never `float` or `double`, for money and quantities.
- V1 tax is **tax-exclusive**. If inclusive tax is required later, add an explicit setting and tests rather than infer it.
- For each line: `lineSubtotal = quantity × unitPrice`; `discountAmount = lineSubtotal × discountPercent / 100`; `taxableAmount = lineSubtotal - discountAmount`; `taxAmount = taxableAmount × taxRate / 100`; `lineTotal = taxableAmount + taxAmount`.
- Invoice subtotal is the sum of line subtotals. Discount total is the sum of line discounts. Tax total is the sum of line tax. Grand total is the sum of line totals.
- Round at a documented consistent point using currency precision; default to two fractional digits and `MidpointRounding.AwayFromZero`. Currency precision must be configurable because not all currencies use two decimals.
- Do not trust client-submitted calculated totals. Accept input line data, recalculate on the server, and return calculated values.
- Validate quantity, unit price, discount, tax, date values, currency code, description length, line count, and invoice notes lengths. Limit the line-item count (suggested max 500) and configuration values to reasonable bounds.
- Generate invoice numbers under a database transaction with concurrency-safe sequence allocation. Numbers are never reused; gaps caused by abandoned drafts are acceptable.
- Store the exact totals used for a generated invoice. A generated invoice is immutable; drafts may be edited.

### 10.3 Invoice states and operations

Use statuses such as `Draft`, `Generated`, and `Archived`.

- Save draft: validates required draft fields, stores invoice header and line items, and reserves an invoice number if assigned at first save.
- Preview: generates a temporary PDF using the selected template version and current draft data; does not mark the invoice as generated.
- Generate: validates all data and template bindings, calculates totals on the server, renders a PDF, stores the file, stores its SHA-256 checksum, and changes status to `Generated` only after successful persistence.
- Download/view: serves only an authorized saved PDF through an API endpoint; do not expose the local filesystem path.
- Editing a generated invoice is prohibited. An archived invoice remains readable/downloadable but does not appear in default active lists.
- If PDF rendering or file persistence fails, return a failure response and do not leave the invoice marked as generated. Clean up temporary files on errors.

### 10.4 Multi-page output

- Use the template's selected background page on every output page.
- Split line-item rows according to the configured table region's usable height, row height, header height, and page margins.
- Repeat table headers on subsequent pages when configured.
- Render fields according to their `FirstPage`, `EveryPage`, or `LastPage` policy.
- Place grand totals on the final page by default.
- Render `page.number` and `page.count` after calculating the total page count; if the implementation requires a two-pass render, keep the process deterministic.
- Descriptions may wrap to a configured number of lines or use a documented shrink/overflow strategy. Do not let one long description draw over adjacent rows.
- Include integration tests with a one-page invoice and an invoice that spills to multiple pages.

## 11. Data Model

Implement entities with GUID or another consistent generated key strategy. Apply UTC timestamps (`DateTimeOffset`) for audit timestamps and date-only types for issue/due dates where appropriate. Use EF Core configurations rather than relying entirely on conventions.

### `InvoiceTemplate`

- `Id`, `Name`, `Description`, `ActiveVersionId`, `IsArchived`, `CreatedAtUtc`, `UpdatedAtUtc`.
- Unique or otherwise unambiguous active name policy; define whether duplicate names are allowed.

### `InvoiceTemplateVersion`

- `Id`, `TemplateId`, `VersionNumber`, `SourceType` (`Image` or `Pdf`), `SourceStorageKey`, `SelectedPdfPageNumber` nullable, `PageWidthPoints`, `PageHeightPoints`, `LayoutSchemaVersion`, `LayoutJson`, `CreatedAtUtc`.
- Unique constraint on `(TemplateId, VersionNumber)`.
- Preserve all versions referenced by invoices.

### `Invoice`

- `Id`, `InvoiceNumber`, `Status`, `TemplateVersionId`, `IssueDate`, nullable `DueDate`, `CurrencyCode`.
- Issuer snapshot fields and customer snapshot fields.
- `Notes`, `Terms`, `Subtotal`, `DiscountTotal`, `TaxTotal`, `GrandTotal`.
- `PdfStorageKey`, `PdfSha256`, `CreatedAtUtc`, `UpdatedAtUtc`, nullable `GeneratedAtUtc`, optional concurrency token.
- Unique index on `InvoiceNumber`.

### `InvoiceLineItem`

- `Id`, `InvoiceId`, `SortOrder`, `Description`, `Quantity`, `UnitPrice`, `DiscountPercent`, `TaxRate`, `LineSubtotal`, `DiscountAmount`, `TaxAmount`, `LineTotal`.
- Money/quantity columns use appropriate SQL decimal precision (choose and document a consistent precision such as `decimal(19,4)` for values and currency-rounded totals).
- Recalculate totals on draft updates and on generation. Generated invoice rows cannot be changed.

### `BusinessProfile`

- One active profile for v1: `Id`, issuer fields, tax label, default currency, invoice prefix, next sequence, default tax rate, default terms, locale/time zone settings, and timestamps.
- Sequence allocation must be concurrency safe. Do not implement invoice numbers by merely reading a number and incrementing it in application memory.

### Identity and storage metadata

- Use ASP.NET Core Identity tables for the administrator account.
- Files are stored via storage keys in the database and under a non-public storage directory. Do not store arbitrary filesystem paths supplied by a request.
- Keep original filename only as sanitized display metadata if it is useful; never use it for path construction.

### Future module data domains (roadmap only; do not implement in v1)

Use these boundaries to keep future work consistent. They are planning contracts, not instructions to add unused tables to the MVP database. Each module owns its schema and migrations.

**Projects module (`MyWorkbench.Modules.Projects`)**

- `ClientOrganization`: `Id`, `Name`, `Website`, `Industry`, `Status`, notes, timestamps.
- `ClientContact`: `Id`, `ClientOrganizationId`, name, role, email, phone, preferred contact method, notes.
- `WorkProject`: `Id`, optional client ID, name, summary/scope, status, start/due/completion dates, currency/budget when applicable, external references, timestamps.
- `ProjectMilestone`: `Id`, `ProjectId`, title, due/completed dates, status, sort order.
- `ProjectTask`: `Id`, `ProjectId`, optional `MilestoneId`, title, description, status, priority, due date, optional estimate, completed date.
- `ProjectDocumentReference` and `WorkLog`: store private storage keys/metadata and time records, not public URLs or arbitrary filesystem paths.
- Keep client/project links from other modules optional and via stable IDs or explicit contracts; never create circular project references to invoices/opportunities/career modules.

**Freelance opportunities module (`MyWorkbench.Modules.Opportunities`)**

- `FreelanceOpportunity`: `Id`, platform/source name, source URL, title, client label, summary, skills/tags, source-discovered date, submission deadline, budget/rate range, currency, status, fit notes, next follow-up date, outcome, timestamps.
- `ProposalDraft` or `ProposalVersion`: `Id`, `OpportunityId`, version number, proposal text, optional proposed price/terms, created/updated timestamps, and selected/submitted marker. Keep version history so edits do not erase earlier drafts.
- `OpportunityActivity`: timestamped status changes, notes, follow-ups, submitted date, reply date, and outcome changes.
- Suggested status pipeline: `Discovered`, `Reviewing`, `Preparing`, `ReadyToSubmit`, `Submitted`, `InDiscussion`, `Won`, `Lost`, `Withdrawn`, `Archived`. Make the pipeline configurable only if there is a real user need; don't build a generic workflow engine for v1.
- Store a source URL for convenience, but do not scrape behind authentication or submit bids automatically. The first version of this future module is manual tracking, paste-in details, proposal drafting, and reminders.

**Career module (`MyWorkbench.Modules.Career`)**

- `JobApplication`: `Id`, source/platform, posting URL, company, job title, location, work arrangement, employment type, compensation range/currency when known, date discovered, applied date, status, notes, next follow-up date, and timestamps.
- `ApplicationArtifact`: `Id`, `JobApplicationId`, artifact type (`Resume`, `CoverLetter`, `Portfolio`, `Other`), version label, private storage key or safely stored text, content metadata, and created timestamp.
- `RecruiterContact`, `Interview`, and `ApplicationActivity` track contacts, interview schedules/notes, stage transitions, communications, and final outcome. Sensitive notes/documents must be accessible only to the authenticated user.
- Suggested application statuses: `Saved`, `Researching`, `Preparing`, `Applied`, `RecruiterScreen`, `Interviewing`, `Assessment`, `Offer`, `Rejected`, `Withdrawn`, `NoResponse`, `Archived`.
- Begin with manual entry and user-managed document versions. Do not auto-apply, send messages, or upload CVs to third-party services in the initial career module.

**Automation module (`MyWorkbench.Modules.Automations`)**

- `AutomationDefinition`: name, description, enabled flag, trigger type/configuration, conditions, allow-listed action definitions, owner, timezone, concurrency policy, approval policy, retry policy, and timestamps.
- `AutomationRun`: definition ID/version, triggered time, status, start/end times, safe diagnostic summary, correlation ID, retry count, and final result metadata.
- `AutomationStepRun`: per-step status, timing, redacted result/error, and safe retry metadata.
- Persist a definition schema version and immutable/versioned definitions. Editing a definition must not retroactively mutate an in-progress run.
- Triggers may eventually include manual, schedule, or a registered application event. Actions must come from a compiled allow-list or vetted connector registry. Never `eval` user code or execute arbitrary shell/PowerShell/SQL supplied by a user.
- External side effects such as sending messages, submitting forms, or modifying external records require an explicit approval policy. The default for high-impact external actions is human approval before execution.
- Automation runs need timeouts, cancellation, bounded retries, idempotency keys where feasible, audit history, rate limits, and redaction of secrets and personal data in logs.
- Store only secret references in automation definitions. Secret values belong in an approved secret store/configuration provider and must not be exposed to the frontend, logs, database exports, or prompt context.
- Respect third-party terms, user authorization, API limits, and applicable law. Do not implement automated website scraping or automated Upwork/Freelancer/LinkedIn application submission by default.

**Cross-module integration principles**

- A project may eventually originate from a won freelance opportunity; an invoice may eventually link to a project; a dashboard may summarize opportunity/job/project follow-ups. Implement those connections through stable IDs and explicit contracts only when both modules exist.
- Avoid making modules mandatory dependencies on one another. Where an optional module is disabled, core functionality in another module should continue to work with optional sections hidden or read-only.
- Cross-module dashboard summaries must be derived from real data, read-only, and resilient when a module is disabled or fails.
- Prefer module-owned activity history with a small optional summary contract over a central table that assumes all module events share the same business model.

## 12. API Contract

Use consistent JSON casing, async operations, validation, and `ProblemDetails` responses. Add OpenAPI/Swagger for development. Require authentication for all business endpoints.

### Shared endpoints

- `GET /api/health` — health/readiness information without exposing secrets.
- `GET /api/auth/me` — current administrator profile.
- `POST /api/auth/login` — authenticate and issue secure cookie.
- `POST /api/auth/logout` — sign out.
- `GET /api/modules` — list enabled modules and their UI metadata.

### Profile/settings

- `GET /api/invoice-settings/business-profile`
- `PUT /api/invoice-settings/business-profile`

### Templates

- `GET /api/invoice-templates` — list templates and active versions.
- `POST /api/invoice-templates` — upload a source file and create a template/version (`multipart/form-data`).
- `GET /api/invoice-templates/{templateId}` — metadata and active layout.
- `GET /api/invoice-templates/{templateId}/source` — authorized source retrieval/preview; never return a physical path.
- `PUT /api/invoice-templates/{templateId}/layout` — validate and save a new layout version.
- `POST /api/invoice-templates/{templateId}/preview` — render a sample PDF from test/sample data and the specified version/layout.
- `POST /api/invoice-templates/{templateId}/versions` — upload/replace the design background and create a new version, if background replacement is supported separately from layout editing.
- `POST /api/invoice-templates/{templateId}/archive` — archive instead of hard delete.

### Invoices

- `GET /api/invoices?search=&status=&page=&pageSize=` — paginated history.
- `POST /api/invoices` — create draft.
- `GET /api/invoices/{invoiceId}` — retrieve invoice and lines.
- `PUT /api/invoices/{invoiceId}` — update a draft only.
- `POST /api/invoices/{invoiceId}/preview` — render a temporary PDF for a draft.
- `POST /api/invoices/{invoiceId}/generate` — produce and persist final PDF.
- `GET /api/invoices/{invoiceId}/pdf` — view/download the generated PDF.
- `POST /api/invoices/{invoiceId}/archive` — archive a generated invoice.

API contract requirements:

- Return a stable ID for records; do not use invoice number as the only internal identifier.
- Use `400` for validation, `401` for unauthenticated requests, `403` for forbidden/disabled module access, `404` for missing records, `409` for invalid state/concurrency conflicts, and `413` for oversized uploads where the hosting stack supports it.
- Return validation details keyed by field where possible.
- Stream PDFs instead of base64-encoding them inside JSON.
- Add pagination limits and a maximum `pageSize`.
- Validate that the invoice owner/current single-user context is authorized for the requested record even though v1 has one account.
- Keep routes and DTOs versionable; never expose EF entities directly from endpoints.

## 13. PDF Rendering Design

Create an abstraction such as `IInvoicePdfRenderer` with a method that receives a validated immutable template version and an invoice snapshot and returns PDF bytes or a temporary output stream. Keep PDFsharp details behind this interface.

Rendering pipeline:

1. Load and validate the stored template version and its source file.
2. Resolve only allow-listed field bindings.
3. Create output pages with dimensions from the template version.
4. Draw the selected PDF page as a form/background or draw the image as a background with the configured fit policy.
5. Draw fields and line-item rows using normalized coordinates and the shared coordinate mapper.
6. Compute pagination, then render page count/page number fields as required.
7. Confirm the output is non-empty and starts as a readable PDF; store it using a generated file key and calculate its SHA-256 checksum.
8. Return `application/pdf` with a safe content-disposition filename such as `INV-2026-0001.pdf`.

Implementation constraints:

- For images, accept formats the selected PDF library supports reliably (PNG/JPEG for v1). Perform file signature checks and dimension limits before drawing.
- PDFsharp Core does not act as the browser preview renderer; use PDF.js on the frontend for that responsibility.
- The PDFsharp Core build needs explicit font handling for portable behavior. Bundle appropriate licensed fonts and implement/test an `IFontResolver` rather than relying on fonts available on the developer's machine.
- The coordinate mapper must be unit-tested for A4 portrait, A4 landscape, Letter, and the selected PDF page dimensions.
- Test with PDFs from more than one source application, including PDFs with rotation metadata and unusual dimensions. Normalize or reject unsupported rotations explicitly; do not silently misplace data.
- Use timeouts/resource limits for parsing and rendering untrusted uploads. Do not run external commands with upload-derived shell arguments.
- Do not log extracted customer data or the PDF bytes.

## 14. File Upload, Storage, and Security Requirements

Treat every uploaded PDF/image as untrusted input.

- Enforce a server-side maximum file size and a separate limit on decoded image dimensions and PDF page count.
- Allow-list `.pdf`, `.png`, and `.jpg`/`.jpeg` (subject to implemented signature support). Do not rely on extension or browser-provided MIME alone. Validate magic bytes and actually parse the content.
- Generate storage keys internally (for example, GUID-based); store files outside `wwwroot` and outside the source tree.
- Disallow path traversal and never combine the client filename with a storage root.
- Use safe display names and HTML-encode untrusted metadata in UI/log output.
- Restrict file download endpoints to authenticated users and verify record access before opening a file.
- Configure request body limits consistently across Kestrel, IIS/reverse proxy, and ASP.NET multipart settings when deployed behind them.
- Keep PDF/image libraries current. Provide an `IFileMalwareScanner` seam so a scanner can be configured where available. If no scanner is configured for a local-only v1, document that uploaded files are not malware-scanned; do not pretend they are.
- Use HTTPS in production, secure cookies, anti-forgery validation, appropriate CORS policy (do not use permissive wildcard CORS with credentials), request rate limits, and a restrictive Content Security Policy.
- Do not store secrets in `appsettings.json`; commit only safe examples and use Secret Manager/environment variables.
- Handle exceptions centrally and return generic client errors; detailed exceptions belong only in protected development logs.
- If the app becomes internet-accessible, review threat modeling, malware scanning, backup/recovery, data retention, and rate limiting before public deployment.

## 15. Frontend Engineering Standards

- Angular standalone components and lazy-loaded routes.
- Use Angular Reactive Forms for invoice entry and template properties.
- Use Angular signals for local UI state where appropriate and RxJS for asynchronous API flows. Do not add NgRx unless shared state actually justifies it.
- Use typed API services; keep direct `HttpClient` calls out of presentation components.
- Use route guards for unauthenticated pages and a central HTTP interceptor for consistent API error handling and anti-forgery integration.
- Handle loading, empty, validation, retryable failure, and success states in every data-driven screen.
- Debounce template auto-preview and avoid rendering a PDF on every pointer movement; pointer movement should update the local overlay, and preview generation should be explicit or debounced.
- Track unsaved layout changes and warn before navigating away.
- Keep editor coordinates in normalized page units, not browser pixels. Convert normalized coordinates to CSS positions only for display.
- Revoke temporary object URLs when no longer needed and clean up PDF.js resources/workers on component destruction.
- Use Angular's supported test configuration and a lockfile; do not pin incompatible versions manually.
- Avoid storing invoice details in persistent browser storage. A refresh should reload server data, except for an explicitly designed temporary unsaved draft cache.

## 16. Backend Engineering Standards

- Use dependency injection and explicit module registration.
- Keep endpoint methods thin; place rules in invoice services/domain methods.
- Use DTOs and mapping functions rather than returning EF entities.
- Apply EF Core transactions for invoice number allocation and final generation state changes.
- Use `AsNoTracking` for read-only query paths when applicable and project list DTOs in SQL rather than loading full PDF/file payloads.
- Use async database/file operations and cancellation tokens.
- Keep the application culture-neutral for calculations: parse/serialize numbers invariantly in the API and format according to configured locale at presentation time.
- Never use floating-point numeric types for invoice amounts.
- Validate template JSON with a typed schema/versioned DTO, explicit size limits, and binding allow-list.
- Use optimistic concurrency for draft updates if necessary, and return a conflict response rather than overwriting silently.
- Add indexes for invoice number, issue date, status, and common list/search fields where they are used.
- Log a correlation/request ID and operation outcome, not full invoice content.

## 17. Testing Requirements

### Backend unit tests

- Line subtotal, discount, tax, and grand-total calculations.
- Currency rounding and non-two-decimal precision configuration.
- Invalid quantities, prices, tax rates, discounts, currencies, dates, and oversized descriptions.
- Invoice number generation under concurrent requests and no reuse of allocated numbers.
- Template field binding allow-list and missing-value behavior.
- Normalized-to-PDF coordinate mapping and page orientation.
- Template layout schema validation and migration version checks.
- Pagination, table row splitting, repeated headers, page repeat policies, and page number/count fields.
- Filename generation and safe storage key handling.

### Backend integration tests

- Authentication required for invoice endpoints.
- Valid PNG/JPEG/PDF template upload succeeds.
- Spoofed MIME type, wrong signature, unsupported extension, oversized source, unreadable PDF, and excessive PDF pages fail safely.
- Create/edit a draft; preview it; generate it; retrieve PDF; archive it.
- Generated invoices cannot be edited.
- Failed PDF generation does not leave the invoice in `Generated` state and cleans up temporary artifacts.
- Template updates do not alter the version referenced by an already-generated invoice.
- PDF response has `application/pdf`, a safe filename, and non-empty valid content.

### Frontend tests and end-to-end tests

- Login, shell, sidebar module loading, and disabled-module behavior.
- Create/edit/save template layouts; field placement and property inspector behavior.
- Invoice form validation and displayed totals.
- Preview PDF, generate invoice, and download from history.
- Responsive navigation and basic keyboard interactions in the template editor.

### Manual verification before release

- Inspect a downloaded image-backed PDF and PDF-backed PDF at 100% zoom.
- Compare field alignment at browser preview size and in the generated PDF.
- Try a long customer name/address, a large amount, non-ASCII characters, a long line-item description, empty optional fields, and multiple pages.
- Confirm no fields are clipped without warning and no editor selection outline appears in the final PDF.

## 18. Definition of Done

The **v1 invoice MVP** is complete only when all of the following are true. Future roadmap modules are not acceptance criteria for this release:

- The solution builds cleanly using the .NET 10 SDK.
- The Angular application builds using its locked, compatible dependency versions.
- Authentication protects the app and API.
- The Dashboard and sidebar are functional; the Invoices option is driven by the module registry. No unimplemented roadmap module is shown as an active feature.
- Project/module names use the `MyWorkbench` naming convention and all invoice frontend files live inside the single Angular application.
- A documented module-development guide explains how future modules get a backend project, a frontend feature folder, tests, registration, configuration, and navigation metadata without changing unrelated module logic.
- Future opportunity, career, and automation modules are documented as roadmap-only and are not represented by fake records, stub pages, or incomplete active routes.
- A user can upload an image or select a page of a PDF as an invoice template background.
- A user can manually position and style invoice fields and a line-item table, save the template, and preview sample output.
- Template versions used by existing invoices remain unchanged after later template edits.
- A user can create a draft, add multiple items, see totals, preview, generate, and download a real PDF.
- The generated PDF visually matches the chosen template background and correctly positions dynamic fields and line items.
- Multipage invoices, page repeat settings, and totals-on-final-page behavior work for tested fixtures.
- Generated invoices are immutable and their PDF files are stored privately with a checksum.
- Upload validation, authentication, and file authorization tests pass.
- The README explains prerequisites, setup, user-secret configuration, database migrations, running both projects, tests, and limitations.
- No secrets or user-generated files are committed to git.
- The v1 does not scrape third-party platforms, auto-submit freelance proposals/job applications, or execute arbitrary automation code.

## 19. Phased Copilot Implementation Plan

Run prompts one phase at a time in Copilot Chat. **Phases 0-6 are the invoice MVP. Phases 7+ are future roadmap work and must not be started until requested after the MVP is stable.** Keep this specification available as context. Do not ask Copilot to implement all phases simultaneously.

### Phase 0 — Inspect and plan

```text
Read docs/specification.md fully. First inspect the repository, installed .NET SDKs, Node.js version, existing solution, and git status. Do not overwrite existing work. Return a concise architecture/implementation plan for Phase 1 only, identify any compatibility risks, and list the files you expect to create or change. Do not implement code yet.
```

### Phase 1 — Foundation and application shell

```text
Implement Phase 1 from docs/specification.md: create or adapt the `MyWorkbench.sln` .NET 10 solution and the single `my-workbench-web` Angular frontend; add shared module contracts, explicit backend module registration, the enabled-module API, secure authentication plumbing, Angular shell, responsive sidebar, dashboard placeholder, Angular dev API proxy, configuration examples, and README setup instructions. Create the Invoices module registration and placeholder route but do not implement invoice/PDF features yet. Do not create project/opportunity/career/automation backend projects or active frontend routes in the MVP. Use stable compatible versions, avoid secrets, add tests for module registration/auth configuration where practical, then build and run checks. Report exact commands and results. Do not proceed to Phase 2.
```

### Phase 2 — Invoice domain, persistence, settings, and API

```text
Implement Phase 2 from docs/specification.md in MyWorkbench.Modules.Invoices: entities, EF Core context/migrations, business profile, invoice drafts/lines, authoritative decimal calculations, concurrency-safe invoice number allocation, DTOs, validation, and invoice/settings endpoints. Keep endpoint handlers thin. Add unit and integration tests for calculations, validation, numbering, state transitions, and authentication. Do not implement the visual template editor or PDF renderer yet. Run the build and all relevant tests before finishing.
```

### Phase 3 — Template source upload, storage, and template versioning

```text
Implement Phase 3 from docs/specification.md: secure private file storage, upload validation, PDF/image source inspection, selection of a PDF page, page-dimension metadata, versioned template entities, layout schema validation, template list/detail/upload/archive endpoints, and test fixtures. Use a generated storage key, enforce configurable limits, and validate file signatures and parsed content server-side. Add negative tests for spoofed MIME types, invalid files, size limits, encrypted/unreadable PDFs, and excessive page count. Do not claim files are malware-scanned unless a scanner is actually configured.
```

### Phase 4 — PDF rendering engine

```text
Implement Phase 4 from docs/specification.md: introduce IInvoicePdfRenderer, select and pin a compatible stable PDFsharp Core version, implement image/PDF background composition, text/number/date/money field rendering, bundled font resolution, normalized coordinate mapping, line-item table layout, multipage splitting, field repeat policies, page numbering, totals on the last page, output checksum, and cleanup/error behavior. Keep PDFsharp-specific code behind the renderer abstraction. Test A4 portrait/landscape, Letter, source PDF page sizes, long text, non-ASCII characters, one-page invoices, and multipage invoices. Do not implement the Angular template editor yet.
```

### Phase 5 — Angular template editor and invoice UX

```text
Implement Phase 5 from docs/specification.md in the existing single Angular project. Add the template stepper, upload UI, PDF.js/image canvas preview, normalized drag/resize overlays, field palette, properties inspector, line-item table-region configuration, preview, template version save, invoice entry form, live total display, draft save, invoice history, generated PDF preview/download, settings, loading/empty/error states, responsive styling, and keyboard behavior. Use typed API services and Reactive Forms; do not put authoritative calculations or PDF composition in the browser. Add frontend tests and Playwright flows for template creation and invoice generation. Do not create a separate frontend project.
```

### Phase 6 — End-to-end validation and hardening

```text
Review docs/specification.md against the current implementation line by line. Identify missing acceptance criteria, security gaps, incomplete TODOs, fake code, untested critical paths, and frontend/backend contract mismatches. Fix the highest-priority gaps, run the complete .NET build/tests, Angular build/tests, and Playwright suite. Verify actual generated PDFs from both image-backed and PDF-backed templates. Update README and docs/module-development.md with tested setup and module-addition steps. Report passed/failed commands honestly and list any remaining limitations. Do not add unrelated features.
```

### Phase 7 — Work & Client Projects (future; only when explicitly requested)

```text
Implement the Work & Client Projects roadmap described in docs/specification.md as a new MyWorkbench.Modules.Projects backend project and a feature folder under the existing single Angular frontend at src/app/modules/projects/. Do not create a second frontend or API host. Add clients/contacts, projects, milestones, project tasks, document references, and work logs with clear statuses, search, pagination, validation, and tests. Keep module-owned entities and schema isolated. Do not change invoice calculation/PDF behavior. If invoice/project linking is included, use optional stable IDs and explicit contracts rather than direct cross-module DbContext references. Update module registration, sidebar metadata, README, module-development.md, and tests. Build and test before finishing.
```

### Phase 8 — Freelance Opportunities (future; only when explicitly requested)

```text
Implement the Freelance Opportunities roadmap in docs/specification.md as MyWorkbench.Modules.Opportunities and a lazy-loaded feature folder in the existing Angular frontend. Provide manual create/edit/search/filter for opportunities from Upwork, Freelancer, referrals, and other sources; store the source URL, platform, client/title, budget/rate and currency when known, skills, deadline, status pipeline, proposal drafts/version history, activity history, outcome, and follow-up reminders. Start with manual entry and copy-ready drafts. Do not scrape authenticated websites, bypass platform controls, store site passwords/cookies, or auto-submit proposals. Any later platform integration must be separately authorized, use approved access methods, comply with current platform rules, and be feature-flagged. Add tests for status changes, validation, filters, and follow-up behavior. Do not change existing invoice flows.
```

### Phase 9 — Career & Job Applications (future; only when explicitly requested)

```text
Implement the Career & Job Applications roadmap in docs/specification.md as MyWorkbench.Modules.Career and a lazy-loaded folder in the current Angular frontend. Add manual tracking for company, role, source URL, location/work arrangement, compensation when known, pipeline status, discovered/applied dates, next follow-up, recruiter contacts, interviews, notes, outcome, and private versioned resume/cover-letter artifacts. Include search/filter, activity history, and useful reminders. Never auto-apply or upload/send documents to third-party sites. Protect private CVs, salary data, contact details, and notes behind authorization. Add backend, integration, frontend, and browser-flow tests. Do not modify the single-frontend architecture.
```

### Phase 10 — Automation Center (future; only after explicit security review)

```text
Implement MyWorkbench.Modules.Automations only after reviewing the security rules in docs/specification.md. Provide versioned automation definitions using registered triggers and an allow-list of typed actions; start with low-risk internal actions such as reminders/notifications and scheduled maintenance. Include enable/disable, timezone-aware schedules, manual run, validation, execution history, timeouts, cancellation, bounded retries, idempotency where feasible, approval gates for external side effects, and redacted diagnostics. Never evaluate arbitrary code or execute arbitrary shell/SQL from user input. Never store credential values in the definitions/database/frontend/logs; store secret references only. Add strict tests for authorization, schedule handling, duplicate execution, retry limits, approval requirements, cancellation, and secret redaction. Do not automate third-party website scraping or proposal/job application submission by default.
```

### Phase 11 — Cross-module Work Overview (future; after the relevant modules exist)

```text
Extend the Dashboard only after the underlying modules exist. Use small typed dashboard summary contracts from registered modules to show real recent activity, overdue follow-ups, project milestones, opportunity deadlines, job interviews, invoice status, and automation failures. Do not read other module DbContexts directly or make one module required for another. Handle disabled/unavailable modules gracefully, use deep links to the owning module, and ensure each metric is backed by real data. Add integration tests for disabled modules, empty databases, and one module failing to provide a summary.
```

## 20. Final Review Checklist for Copilot

Before calling the implementation complete, answer each question with evidence from the code/tests:

- Is there exactly one Angular frontend project named `my-workbench-web`?
- Does the solution use the `MyWorkbench` naming convention?
- Are only Dashboard and the implemented Invoices module shown in the MVP sidebar?
- Are future Projects, Opportunities, Career, and Automations modules clearly deferred until separately requested?
- Does the invoice backend live in its own .NET project?
- Can the API host register/disable a backend module explicitly?
- Does the sidebar render only enabled module descriptors while server-side endpoints still enforce module availability?
- Is the uploaded template merely a background until the user maps fields manually?
- Are PDF preview and PDF composition correctly separated (PDF.js in browser, PDFsharp in backend)?
- Are PDF fonts portable and tested on a clean machine/container?
- Are invoice totals calculated on the server with decimals and consistent rounding?
- Is invoice number allocation safe under concurrency?
- Are template versions and generated invoice PDFs immutable and private?
- Are upload types, signatures, sizes, dimensions, and page count validated on the server?
- Can any user-controlled value become a filesystem path, SQL fragment, code import, or unvalidated field binding? It must not.
- Does a PDF rendering failure leave database/file state consistent?
- Do tests cover multipage invoices and non-ASCII text?
- Have both build pipelines and the critical end-to-end user path actually been run?
- Does the implementation avoid cross-module DbContext/entity coupling and arbitrary runtime plugin loading?
- Are future external platform interactions opt-in, user-authorized, and constrained by current platform rules?
- Would an automation require explicit approval for external/high-impact actions, keep secrets out of definitions/logs, and enforce timeouts/retries/idempotency?

## 21. Official References

Use current official documentation during implementation; package APIs and support windows change.

- .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- What's new in .NET 10: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview
- Angular release/support policy: https://angular.dev/reference/releases
- Angular standalone components: https://angular.dev/reference/migrations/standalone
- ASP.NET Core file upload security and limits: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
- PDFsharp licensing: https://docs.pdfsharp.net/General/License/License-FAQ.html
- PDFsharp drawing and image support: https://docs.pdfsharp.net/PDFsharp/Topics/Bitmap-Images/Drawing.html and https://docs.pdfsharp.net/PDFsharp/Overview/Features.html
- OWASP File Upload Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html
- PDF.js project: https://mozilla.github.io/pdf.js/

