# Documentation Index — PackagingTenderTool

<!-- AUDIENCE: Everyone | OWNER: docs/_INDEX.md -->
<!-- This is the single source of truth for documentation ownership.
     Before creating any new markdown file, check here first.
     If a file is not listed here, it should not exist.
     If a file is retired or deleted, it must be recorded here with the reason. -->

---

## Rule

One file per purpose. No duplicates. No root-level narrative docs except `README.md` and `decisions.md`.

Before creating a new documentation file:
1. Check this index first.
2. Add to an existing file if the content fits.
3. If a new file is genuinely needed, add it here before creating it.
4. Never create documentation files at repo root except `README.md` and `decisions.md`.
5. Never create `README2.md`, `overview.md`, `notes.md`, or similar shadow files.

---

## Active Files

### Repo root

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `README.md` | Business / Category Manager | What the tool is, why it exists, how it creates value. Competitive positioning, scoring model summary, current status. | English | ✅ Active |
| `decisions.md` | Developer / AI agent | Architecture Decision Records (ADRs). One record per architectural decision. Extend this — do not create new ADR files. | English | ✅ Active |
| `.cursorrules` | AI agent (Cursor) | Project constitution. Roles, guardrails, architecture rules, doc ownership, feedback structure, audit gates, completion rules. Read by Cursor on every session. | English | ✅ Active |
| `.gitignore` | Developer | Git ignore rules. Standard .NET ignores. | — | ✅ Active |
| `NuGet.config` | Developer | NuGet package source configuration. | — | ✅ Active |
| `PackagingTenderTool.sln` | Developer | Visual Studio solution file. Entry point for build and test. | — | ✅ Active |
| `epr-settings.json` | Developer / System | Runtime EPR fee configuration. Country-specific rates consumed by EprFeeService. Not a demo file — this is live config. | JSON | ✅ Active |
| `run-app.ps1` | Developer | PowerShell launch script for local development. Starts the Blazor cockpit. | PowerShell | ✅ Active |

---

### /docs

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `docs/_INDEX.md` | Everyone | This file. Single source of truth for documentation ownership. | English | ✅ Active |
| `docs/ARCHITECTURE.md` | Developer / AI agent | System orientation: layers, named services, data flow, invariants, repo structure, how to run. Summary level — detail lives in spec.md. | English | ✅ Active — created 2026-05-07 |
| `docs/BACKLOG.md` | Developer / Architect | Prioritised improvement backlog. Scored by value, priority, effort. Single source of truth for what is planned, in progress, and done. | English | ✅ Active — created 2026-05-07 |
| `docs/spec.md` | Developer / AI agent | Full system specification. Scoring formulas, EPR matrix, domain model, strategy pattern, TCO calculation, normalization rules. Canonical technical reference. | English | ✅ Active — updated 2026-05-07 (Blazor marked current, WinForms retired) |
| `docs/DEVELOPER_LOG.md` | Developer | Refactoring journal and engineering decisions. Root cause analyses, architecture sanitation records, lessons learned. Narrative format — not a reference doc. | English / Danish | ✅ Active — moved from root 2026-05-07 |
| `docs/plan_HISTORICAL.md` | Developer | Historical baseline plan from early project phase. Records original scope, direction decisions, and implementation strategy. Read-only — do not update. | English | 📦 Historical — do not update |
| `docs/report.md` | Developer | Status report snapshot. Captures implemented state at a point in time. | English / Danish | 📦 Historical — review for archiving |

---

### /docs/learning

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `docs/learning/SL_LearningGoals.md` | Personal | Education learning goals for SmartLearning programme. Uses codebase as a sandbox to visualise and verify understanding. Not project documentation. | Danish / English | ✅ Active — moved from root 2026-05-07 |

---

### /.github/workflows

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `.github/workflows/dotnet.yml` | Developer / CI | GitHub Actions pipeline. Triggers on push to main. Steps: checkout → setup .NET 10 → restore → build Release → test Release. Upgraded to actions@v5 on 2026-05-07. | YAML | ✅ Active — updated 2026-05-07 |

---

## Retired / Deleted Files

These files no longer exist in the repo. Recorded here for audit trail and to prevent recreation.

| File | Removed | Reason | Replaced by |
|---|---|---|---|
| `ReadMe.md` (root) | 2026-05-07 | Casing conflict with `README.md` on case-insensitive filesystems. Content was a mixed English sprawl of architecture + business + roadmap — doing all jobs poorly. | `README.md` (business) + `docs/ARCHITECTURE.md` (technical) |
| `DEVELOPER_LOG.md` (root) | 2026-05-07 | Moved to `docs/` — belongs with other developer documentation, not at repo root. Git history preserved via `git mv`. | `docs/DEVELOPER_LOG.md` |
| `SL_LearningGoals.md` (root) | 2026-05-07 | Personal education content — wrong location at repo root. Git history preserved via `git mv`. | `docs/learning/SL_LearningGoals.md` |
| `docs/report.md` | 2026-05-07 | Stale WinForms session report. 42-test count wrong (135 now). Content superseded by ARCHITECTURE.md, spec.md, and DEVELOPER_LOG.md. | Deleted |

---

## Forbidden Patterns

The following patterns have caused documentation drift in this project and must not recur:

| Pattern | Why forbidden |
|---|---|
| Creating `README2.md`, `overview.md`, `notes.md` at root | Splits the single README contract |
| Putting architecture content in `README.md` | README is business-facing only |
| Putting business narrative in `docs/ARCHITECTURE.md` | ARCHITECTURE.md is developer/AI-facing only |
| Duplicating content between `spec.md` and `ARCHITECTURE.md` | Spec owns detail. Architecture owns orientation. |
| Creating new ADR files | All ADRs extend `decisions.md` |
| Storing personal notes at repo root | Personal content lives in `docs/learning/` |
| Using AI chat to create doc files without registering here first | Causes invisible sprawl |
| Editing docs in OneDrive-synced path | OneDrive creates phantom git modifications — always work from `C:\Dev\` |

---

## Change Log

| Date | Change | Author |
|---|---|---|
| 2026-05-07 | Created `docs/_INDEX.md` as anti-sprawl lock | Architect |
| 2026-05-07 | Created `docs/ARCHITECTURE.md` — technical orientation doc | Architect |
| 2026-05-07 | Created `docs/BACKLOG.md` — improvement backlog | Architect |
| 2026-05-07 | Updated `docs/spec.md` — Blazor marked current, WinForms retired | Architect |
| 2026-05-07 | Updated `README.md` — English, business audience, aligned with current reality | Architect |
| 2026-05-07 | Updated `.cursorrules` — added Section 0 doc ownership rules | Architect |
| 2026-05-07 | Moved `DEVELOPER_LOG.md` → `docs/DEVELOPER_LOG.md` | Architect |
| 2026-05-07 | Moved `SL_LearningGoals.md` → `docs/learning/SL_LearningGoals.md` | Architect |
| 2026-05-07 | Deleted `ReadMe.md` — casing conflict, content redistributed | Architect |
| 2026-05-07 | Repo moved from OneDrive path to `C:\Dev\` — eliminates sync conflicts | Architect |
| 2026-05-07 | CI pipeline fixed — upgraded to actions@v5, aligned test to Release config | Architect |
| 2026-05-07 | CI badge added to README | Architect |
| 2026-05-07 | Mermaid diagram fixed in `docs/spec.md` | Architect |