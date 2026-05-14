# Session starter — PackagingTenderDecisionEngine

Quick context for humans and agents picking up the repo. For deep narrative and architecture lessons, read `docs/DEVELOPER_LOG.md`. For planned work, `docs/BACKLOG.md` is the source of truth (update statuses there when work completes).

---

## 1. Current project status (what works today)

- **Blazor cockpit (`PackagingTenderTool.Blazor`)**: Main layout with app bar, drawer, **Label Tender** flow (`/labeltender`) — profile selection, Excel import (standard + pivot “All labels DSH”), filters, dashboard KPIs, tabs (Dashboard, Price matrix, Evidence, Audit, Settings), sidebar shell (import, navigation, scenarios including PPWR effect test toggle wired to session).
- **Core engine (`PackagingTenderTool.Core`)**: Label line items, suppliers, **TcoEngineService** (label TCO / `LabelTenderDashboardDto`, filters, decision insight), **CostComponentRegistry** + **`TcoResult`** in Core, **PpwrRiskEvaluator** / PPWR static grade penalty (Option A) integrated where `TcoResult` is built (e.g. `TcoCalculator`, tender cockpit paths).
- **Tests (`PackagingTenderTool.Core.Tests`)**: Broad suite (import, pivot import, golden cases, registry, PPWR tests, etc.); CI can show results in GitHub (BACK-001 done per backlog).
- **WinForms (`PackagingTenderTool.App`)**: Present in solution; not the primary focus of recent cockpit work.
- **Architecture direction** (from `DEVELOPER_LOG.md`): Prefer **engine → DTO → Razor**; avoid business math in markup; invariant formatting for SVG/numeric output where applicable; deterministic, testable paths.

---

## 2. Last 5 commits (newest first)

| Commit   | Summary |
|----------|---------|
| `b1994e8` | **feat:** front-page colors, topbar title, logo sizing attempts, filter cleanup |
| `a8339d6` | **feat:** PPWR effect toggle, scenarios section in shell sidebar, filter bar cleanup, topbar title adjustments |
| `2036700` | **feat:** implement **BACK-012b** PPWR Risk Multiplier (Option A) |
| `d130d2c` | **feat:** implement **BACK-012a** Cost Component Registry |
| `a68d29d` | **feat:** topbar title, filter improvements, color updates, logo size |

*Run `git log -5 --oneline` anytime to refresh.*

---

## 3. Next priorities (from `docs/BACKLOG.md`)

Backlog header lists this **session order** (adjust after updating item statuses in `BACKLOG.md`):

1. **BACK-019** — POC visualization & navigation (ready; unblocks stakeholder story KPI → evidence).
2. **BACK-012a** — Cost component registry (still marked `ready` in file; **code exists** from `d130d2c` — reconcile backlog status vs repo).
3. **BACK-017** — Versioned TCO / audit shield (ready; compliance).
4. **BACK-016** — Multi-country regulatory benchmark (ready; depends on 012a/012b per file).
5. **BACK-012b** — PPWR risk multiplier ADR/scoring (still marked `ready` with ADR checkbox; **Option A implementation** landed in `2036700` — reconcile backlog vs ADR acceptance).
6. **BACK-018** — Constraint builder (idea).

Also in file: **BACK-002** Azure deploy (idea), **BACK-003** coverage (idea), **BACK-004** Trays profile (idea), etc.

---

## 4. Known issues (track in UI / polish)

- **Logo whitespace**: App bar logo (`scandi-standard-logo.png`) + `.pt-app-bar` / `MudAppBar` / `.mud-toolbar` height rules have been tuned several times; extra vertical whitespace or clipping may still need a pass (asset aspect ratio vs CSS `height`/`min-height`/`Dense`).
- **Profile landing cards layout**: Horizontal profile cards (`sm="4"`), max-width grid, faded vs Labels card — spacing/contrast on different viewports may need refinement after recent color experiments (white vs green landing backgrounds).

---

## 5. Tech stack reminder

| Layer | Stack |
|-------|--------|
| Runtime | **.NET 10** |
| Web UI | **Blazor** (Server/Web per project config), **MudBlazor** 7, Radzen in older cockpit pages |
| Core | **PackagingTenderTool.Core** — models, import (`LabelsExcelImportService`, `PivotLabelsExcelImportService`), scoring helpers, `TcoResult`, cost registry, PPWR evaluator |
| Tests | **xUnit**, **NSubstitute**, **ClosedXML**; test project references Core (and Blazor where needed) |
| Data | Excel **.xlsx** via ClosedXML; validation reports on import |
| Docs | `docs/spec.md`, `docs/BACKLOG.md`, `docs/DEVELOPER_LOG.md`, `docs/decisions/` (e.g. PPWR ADR) |

---

*End of session starter. Do not delete existing docs; update this file or the backlog when reality drifts from the summaries above.*
