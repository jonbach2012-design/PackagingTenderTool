# Architecture — PackagingTenderTool

 <!-- AUDIENCE: Developer / AI agent | OWNER: docs/ARCHITECTURE.md --> <!-- Source of truth for system orientation. Detail lives in docs/spec.md. --> 

## What This Is

PackagingTenderTool is a **decision engine**, not a reporting tool. It takes raw supplier tender data, applies configurable scoring logic across commercial, technical, and regulatory dimensions, and produces ranked, explainable, audit-ready output.

Tech stack: **C# / .NET 10 / Blazor / MudBlazor / Radzen**

---

## System Dataflow

```
Excel Input
    └── ImportLayer (raw rows, structural validation)
            └── NormalisedDomain (cleaned, validated LabelLineItems)
                    └── TcoEngineService (scoring, aggregation, breakdown)
                            └── DTOs (LabelTenderDashboardDto, TcoDecisionOutput, ...)
                                    └── Blazor Cockpit (Decision Cockpit UI)

```

Each layer has a single responsibility. Logic does not leak across boundaries.

---

## Layers

### 1. Raw Import Layer

- Reads Excel input via structured import pipeline
- Validates required columns, data types, row types (detail vs summary)
- Produces raw import models — no transformation yet
- Reports import issues clearly (rows imported / valid / invalid / skipped)

### 2. Normalised Domain Layer

- Cleans and standardises raw data: label sizes, materials, spend, currency
- Normalisation is conservative — the system does not invent interpretations
- Outputs `LabelLineItem` domain objects safe for scoring

### 3. Evaluation Layer — `TcoEngineService`

- **This is the core engine. All calculation logic lives here. Nothing else.**
- Implements the Strategy Pattern — each packaging profile provides its own scoring implementation
- Calculates line-level scores across three dimensions (Commercial / Technical / Regulatory)
- Aggregates to supplier level using spend-weighted averaging
- Produces `CalculationBreakdown` for every supplier — explains every penalty and assumption
- Enforces `InvariantCulture` (`FmtSvg`) for all SVG/numeric output — prevents Danish decimal comma corruption
- Protects the ±5% weight perturbation sensitivity model — do not regress this

### 4. Scenario Layer

- Applies alternative weight assumptions over existing evaluation results
- Recalculates rankings without re-running the full import pipeline
- Supports: material composition scenarios, EPR fee scenarios, weight rebalancing

### 5. View Models (DTOs)

- Frontend-ready output structures — decoupled from both domain logic and UI framework
- Key types: `LabelTenderDashboardDto`, `TcoDecisionOutput`, `CalculationBreakdown`
- DTO contracts are stable — breaking changes require explicit ADR and full UI impact analysis

---

## Key Services


| Service                         | Responsibility                                                   |
| ------------------------------- | ---------------------------------------------------------------- |
| `TcoEngineService`              | Core scoring, aggregation, breakdown generation                  |
| `LabelsTenderEvaluationService` | Workflow orchestration: import → evaluate → aggregate → classify |
| `EprFeeService`                 | Country-specific EPR rate lookup (DK, SE, NO, FI, IE)            |
| Import pipeline                 | Excel read, validation, raw model production                     |


---

## Strategy Pattern — Mandatory

Every packaging profile implements evaluation through the Strategy Pattern. `Labels` is the first profile. Adding a new profile (trays, cardboard) means implementing the strategy interface — not modifying the engine.

Evaluation logic **must not** live in Razor components, pages, or view models.

---

## Scoring — Quick Reference

Line score formula:

```
LS = (Score_Commercial × W_Commercial) + (Score_Technical × W_Technical) + (Score_Regulatory × W_Regulatory)

```

Supplier aggregation (spend-weighted):

```
S_total = Σ(LS_i × Spend_i) / Σ(Spend_i)

```

TCO components: `Commercial + EPR + Switching + MOQ`

Price score: `clamp(TCO_min / TCO_total × 100, 0, 100)`

CTR (final weighted score): `clamp((PriceScore × W_Comm + TechScore × W_Tech + RegScore × W_Reg) / 100, 0, 100)`

Visual opacity: `max(0.25, CTR / 100)` — strategic mismatch fades visually.

Full formula detail: `docs/spec.md` sections 13–14.

---

## Blazor UI — Decision Cockpit

Current frontend: **Blazor with MudBlazor and Radzen components.**

Brand: Scandi Standard Green `#91A363`. No default MudBlazor blue in the cockpit shell.

UI rules:

- Every element must drive a decision (Selection / Risk / TCO). Remove visual noise.
- UI code must **never** contain calculation logic. All math goes through `TcoEngineService`.
- After async data updates (e.g. Excel import), always call `InvokeAsync(StateHasChanged)`.
- A UI task is complete only when the rendered browser output visually matches the acceptance criteria. Build success alone is not enough.

WinForms exists as a minimal verification shell only. It is not a development target.

---

## Audit Gates — Mandatory

The following changes **always** require full audit before merging:

- Scoring formulas
- Dimension weights
- DTO / data contracts
- Filtering or aggregation logic

UI cosmetic changes are fast-tracked without audit.

Golden cases that must always be verified:

1. Zero volume
2. Missing data / grades
3. Extreme scaling
4. PPWR toggles
5. Ranking stability

---

## Repo Structure

```
/
├── README.md                    ← Business overview (Category Manager)
├── decisions.md                 ← Architecture Decision Records
├── .cursorrules                 ← AI agent project constitution
├── PackagingTenderTool.sln
├── epr-settings.json            ← EPR fee configuration (runtime)
├── run-app.ps1                  ← Launch script
│
├── src/
│   ├── PackagingTenderTool.Core/        ← Domain, services, DTOs
│   ├── PackagingTenderTool.App/         ← WinForms shell (verification only)
│   └── PackagingTenderTool.Blazor/      ← Blazor cockpit (active frontend)
│
├── tests/
│   └── PackagingTenderTool.Core.Tests/  ← All automated tests incl. GoldenCaseTests
│
└── docs/
    ├── _INDEX.md                ← Documentation ownership map
    ├── ARCHITECTURE.md          ← This file
    ├── spec.md                  ← Full specification (canonical technical reference)
    ├── plan.md                  ← Historical baseline (context only)
    ├── DEVELOPER_LOG.md         ← Refactoring journal
    └── learning/
        └── SL_LearningGoals.md ← Personal education (not project documentation)

```

---

## How to Run

```powershell
# Build
dotnet build PackagingTenderTool.sln

# Test
dotnet test PackagingTenderTool.sln

# Run Blazor cockpit
dotnet run --project src/PackagingTenderTool.Blazor/PackagingTenderTool.Blazor.csproj

# Or use the launch script
./run-app.ps1

```

---

## Invariants — Do Not Break


| Rule                                      | Why                                                        |
| ----------------------------------------- | ---------------------------------------------------------- |
| No calculation logic in Razor / UI        | SoC — testability and auditability                         |
| `InvariantCulture` for all SVG output     | Prevents decimal comma corruption in browser               |
| Strategy Pattern for all scoring          | Extensibility — new profiles must not touch the engine     |
| DTO contracts are stable                  | UI binding breaks silently on contract changes             |
| ±5% weight perturbation model intact      | Decision sensitivity logic — regression breaks the cockpit |
| Small commits for core logic (<200 lines) | Safe rollback discipline                                   |


---

## Reference

- Full specification: `docs/spec.md`
- Decision log: `decisions.md`
- AI agent rules: `.cursorrules`

