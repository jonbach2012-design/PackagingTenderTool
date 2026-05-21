# Documentation Index — PackagingTenderDecisionEngine

<!-- AUDIENCE: Everyone | OWNER: docs/_INDEX.md -->
<!-- This is the single source of truth for documentation ownership.
     Before creating any new markdown file, check here first.
     If a file is not listed here, it should not exist.
     If a file is retired or deleted, it must be recorded here with the reason. -->

---

## Rule

One file per purpose. No duplicates. No root-level narrative docs except `README.md` and `SESSION_STARTER.md`.

Before creating a new documentation file:
1. Check this index first.
2. Add to an existing file if the content fits.
3. If a new file is genuinely needed, add it here before creating it.
4. Never create documentation files at repo root except `README.md` and `SESSION_STARTER.md`.
5. Never create `README2.md`, `overview.md`, `notes.md`, or similar shadow files.
6. All ADRs extend `docs/decisions/decisions.md` — never create separate ADR files.

---

## Active Files

### Repo root

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `README.md` | Business / Category Manager | What the tool is, why it exists, how it creates value. Competitive positioning, scoring model summary, current status. | English | ✅ Active |
| `SESSION_STARTER.md` | Developer / AI agent | Quick context for humans and agents picking up the repo. Current status, next priorities, known issues, key data model facts. Update when reality drifts. | English | ✅ Active — updated 2026-05-21 |
| `.cursorrules` | AI agent (Cursor) | Project constitution. Roles, guardrails, architecture rules, doc ownership, feedback structure, audit gates, completion rules. Read by Cursor on every session. | English | ✅ Active |
| `.gitignore` | Developer | Git ignore rules. Standard .NET ignores. | — | ✅ Active |
| `NuGet.config` | Developer | NuGet package source configuration. | — | ✅ Active |
| `PackagingTenderDecisionEngine.sln` | Developer | Visual Studio solution file. Entry point for build and test. | — | ✅ Active |
| `run-blazor.ps1` | Developer | PowerShell launch script for Blazor cockpit (active frontend). | PowerShell | ✅ Active |
| `run-winforms.ps1` | Developer | PowerShell launch script for WinForms shell (verification only). | PowerShell | ✅ Active |

---

### /docs

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `docs/_INDEX.md` | Everyone | This file. Single source of truth for documentation ownership. | English | ✅ Active |
| `docs/ARCHITECTURE.md` | Developer / AI agent | System orientation: layers, named services, data flow, invariants, repo structure, how to run. Summary level — detail lives in spec.md. | English | ✅ Active — updated 2026-05-21 |
| `docs/BACKLOG.md` | Developer / Architect | Prioritised improvement backlog. Scored by value, priority, effort. Single source of truth for what is planned, in progress, and done. | English | ✅ Active — updated 2026-05-21 |
| `docs/spec.md` | Developer / AI agent | Full system specification. Scoring formulas, EPR matrix, domain model, strategy pattern, TCO calculation, baseline/imputation logic, import formats. Canonical technical reference. | English | ✅ Active — updated 2026-05-21 |
| `docs/DEVELOPER_LOG.md` | Developer | Refactoring journal and engineering decisions. Root cause analyses, architecture sanitation records, lessons learned. Narrative format — not a reference doc. | English / Danish | ✅ Active — moved from root 2026-05-07 |
| `docs/plan_HISTORICAL.md` | Developer | Historical baseline plan from early project phase. Records original scope, direction decisions, and implementation strategy. Read-only — do not update. | English | 📦 Historical — do not update |

---

### /docs/decisions

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `docs/decisions/decisions.md` | Developer / AI agent | All Architecture Decision Records (ADRs). One file, in order. Extend this — never create separate ADR files. Current records: ADR-001 through ADR-007 + ADR-PPWR. | English | ✅ Active — updated 2026-05-21 |

---

### /docs/learning

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `docs/learning/SL_LearningGoals.md` | Personal | Education learning goals for SmartLearning programme. Uses codebase as a sandbox to visualise and verify understanding. Not project documentation. | Danish / English | ✅ Active — moved from root 2026-05-07 |

---

### /.github/workflows

| File | Audience | Purpose | Language | Status |
|---|---|---|---|---|
| `.github/workflows/dotnet.yml` | Developer / CI | GitHub Actions pipeline. Triggers on push to main. Steps: checkout → setup .NET 10 → restore → build Release → test Release. | YAML | ✅ Active — updated 2026-05-07 |

---

## Retired / Deleted Files

These files no longer exist in the repo. Recorded here for audit trail and to prevent recreation.

| File | Removed | Reason | Replaced by |
|---|---|---|---|
| `ReadMe.md` (root) | 2026-05-07 | Casing conflict with `README.md`. Content redistributed. | `README.md` + `docs/ARCHITECTURE.md` |
| `DEVELOPER_LOG.md` (root) | 2026-05-07 | Moved to `docs/` — belongs with developer documentation. Git history preserved. | `docs/DEVELOPER_LOG.md` |
| `SL_LearningGoals.md` (root) | 2026-05-07 | Personal education content — wrong location. Git history preserved. | `docs/learning/SL_LearningGoals.md` |
| `docs/report.md` | 2026-05-07 | Stale WinForms session report. Content superseded by ARCHITECTURE.md and spec.md. | Deleted |
| `decisions.md` (root) | 2026-05-21 | Moved to `docs/decisions/` — belongs with documentation, not at repo root. | `docs/decisions/decisions.md` |
| `decisions/` (root folder) | 2026-05-21 | Duplicate of `docs/decisions/` — consolidated into docs. | `docs/decisions/` |
| `run-app.ps1` (root) | 2026-05-21 | Replaced by separate `run-blazor.ps1` and `run-winforms.ps1` for clarity. | `run-blazor.ps1` + `run-winforms.ps1` |

---

## Forbidden Patterns

The following patterns have caused documentation drift in this project and must not recur:

| Pattern | Why forbidden |
|---|---|
| Creating `README2.md`, `overview.md`, `notes.md` at root | Splits the single README contract |
| Putting architecture content in `README.md` | README is business-facing only |
| Putting business narrative in `docs/ARCHITECTURE.md` | ARCHITECTURE.md is developer/AI-facing only |
| Duplicating content between `spec.md` and `ARCHITECTURE.md` | Spec owns detail. Architecture owns orientation. |
| Creating new ADR files | All ADRs extend `docs/decisions/decisions.md` |
| Storing personal notes at repo root | Personal content lives in `docs/learning/` |
| Using AI chat to create doc files without registering here first | Causes invisible sprawl |
| Editing docs in OneDrive-synced path | OneDrive creates phantom git modifications — always work from `C:\Dev\` |
| Putting `decisions.md` at repo root | It lives in `docs/decisions/decisions.md` |

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
| 2026-05-21 | Moved `decisions.md` → `docs/decisions/decisions.md` | Architect |
| 2026-05-21 | Deleted root `decisions/` folder — consolidated into `docs/decisions/` | Architect |
| 2026-05-21 | Added `SESSION_STARTER.md` to root — quick context for agents and developers | Architect |
| 2026-05-21 | Added ADR-006 (hybrid UI), ADR-007 (pivot import) to `docs/decisions/decisions.md` | Architect |
| 2026-05-21 | Updated `docs/spec.md` — pivot format, baselines, imputed spend, revision convention | Architect |
| 2026-05-21 | Updated `docs/ARCHITECTURE.md` — hybrid UI, new services, new DTOs, new invariants | Architect |
| 2026-05-21 | Updated `docs/BACKLOG.md` — BACK-025/026/027 done, BACK-028 added | Architect |
| 2026-05-21 | Renamed `run-app.ps1` → `run-blazor.ps1` + `run-winforms.ps1` | Architect |
| 2026-05-21 | Radzen 7.3.0 installed, hybrid UI architecture delivered (BACK-025) | Architect |
| 2026-05-21 | Bar chart dashboard, supplier selector, deep-dive navigation delivered (BACK-027) | Architect |
| 2026-05-21 | Baseline + current price import pipeline delivered (BACK-026) | Architect |