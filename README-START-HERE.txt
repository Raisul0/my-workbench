MY WORKBENCH — COPILOT STARTER BUNDLE
Specification version: 1.1

WHAT THIS PROJECT IS
My Workbench is a personal work operating system built as a modular monolith: one .NET 10 API host, one Angular frontend project, and separate .NET projects for implemented business modules.

THE MVP
The first implementation is the core shell/authentication/module registry plus the Invoices module (upload a PDF/image invoice design, position field bindings, save template versions, enter invoice data, render a PDF, and view invoice history).

FUTURE ROADMAP (NOT PART OF THE MVP)
- Work & Client Projects
- Freelance Opportunities (Upwork, Freelancer, referrals, other sources; manual entry first)
- Career & Job Applications (LinkedIn and other sources; manual entry first)
- Automation Center with approvals, allow-listed actions, execution history, and secret handling
- Cross-module dashboard summaries and future finance extensions

HOW TO USE WITH GITHUB COPILOT
1. Copy docs/specification.md into your repository at docs/specification.md.
2. Copy .github/copilot-instructions.md into your repository at .github/copilot-instructions.md.
3. Keep the specification available in the repository and open it in VS Code.
4. Start GitHub Copilot Chat in Agent mode and run Phase 0 — Inspect and plan.
5. Follow Phases 0-6 one at a time for the invoice MVP. Do not start Phases 7+ until the MVP is stable and you explicitly decide to implement the relevant future module.
6. Require a clean build, relevant tests, honest command results, and an updated README at the end of each phase.

KEY ARCHITECTURE RULES
- Exactly one Angular frontend project: my-workbench-web.
- Each implemented business module has its own backend .NET project and an Angular feature folder inside that one frontend.
- The initial functional module is MyWorkbench.Modules.Invoices.
- No microservices or second frontend by default.
- Do not implement scraping, auto-apply/auto-submit, or arbitrary-code automations by default.
- Keep credentials and user files private; do not commit them.

The DOCX in this bundle is the formatted human-readable version of docs/specification.md.
