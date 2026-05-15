# Backlog — PackagingTenderTool

<!-- AUDIENCE: Developer / Architect | OWNER: docs/BACKLOG.md -->
<!-- This is the single source of truth for planned improvements.
     Before starting any new feature or improvement, check here first.
     Before adding a new idea, add it here — not in chat, not in a comment, not in your head. -->

<!-- Priority order for next sessions:
1. BACK-019 visualization (POC blocker)
2. BACK-025 hybrid UI architecture (MudBlazor + Radzen + PTDE CSS)
3. BACK-016 multi-country benchmark
4. BACK-017 audit shield
5. BACK-018 constraint builder
6. BACK-021 material PPWR grade mapping
-->

## How to use this

- **Status**: `idea` → `ready` → `in progress` → `done`
- **Score**: Value (1-5) + Priority (1-5) − Effort (1-5 inverted). Max = 10.
- **Rule**: Never start something not listed here. Never finish without updating status.

---

## Active Backlog

### 🟢 Score 10 — Do next

#### [BACK-019] POC Visualization & Navigation Design
- **Status**: `ready`
- **Category**: Frontend / UX
- **Score**: 10
- **Depends on**: nothing — can start immediately
- **Description**: POC approval requires three things: easy navigation, correct data, and overview with deep dive capability.

  Design and implement a coherent visualization layer across all views:

  **OVERVIEW LAYER** (what decision to make):
  - Dashboard: 4 KPI cards (Recommended, Lowest price, Best TCO, Risk exposure) + Spend overview chart. Already exists — refine only.
  - Regulatory Benchmark tab: heatmap grid suppliers × countries (BACK-016 output)
  - Single recommended supplier highlighted with clear rationale text

  **NAVIGATION LAYER** (how to move between views):
  - Sidebar: Import → Dashboard → Price Matrix → Evidence → Audit Board → Settings. Clean, no clutter. Already improved.
  - Breadcrumb or context bar: always shows current tender name, line count, supplier count
  - "Why this recommendation?" — clickable explanation on dashboard that links to Audit Board

  **DEEP DIVE LAYER** (prove the numbers):
  - Evidence Table: import issues (errors only), full line-item grid with search/filter. Already exists.
  - Audit Board: supplier-by-supplier score breakdown. Commercial / Technical / Regulatory pillars visible. CalculationBreakdown text per supplier.
  - Price Matrix: supplier × item number grid. Price per 1,000, quantity, spend. Pivot-style.

  **INTERACTION PRINCIPLES**:
  - Every number must be traceable — click spend → see line items
  - No orphaned data — every view links to source or explanation
  - Filters persist across tab navigation
  - Mobile-readable (wide screen primary, but no horizontal scroll traps)
  - Replace native SVG `<title>` tooltips with designed hover-cards showing CalculationBreakdown in a readable MudTooltip format. Native browser tooltips removed for POC presentation.

  **IMMEDIATE ACTIONS** (before POC presentation):
  1. Remove duplicate KPI boxes from Evidence Table (already identified)
  2. Add "Why recommended?" explanation card on Dashboard
  3. Ensure Audit Board shows pillar scores visually (bar or gauge)
  4. Price Matrix must show real imported data, not placeholder

- **Value**: Unblocks POC — stakeholders see a coherent story from KPI to evidence.

#### [BACK-016] Multi-country regulatory benchmark engine
- **Status**: `ready`
- **Category**: Architecture / Scoring
- **Score**: 10
- **Depends on**: BACK-012a, BACK-012b — **delivered** (see Done — 2026-05-14)
- **Description**: After Excel import, automatically run all suppliers through 7 regulatory profiles (DK, SE, NO, FI, IE, NL, LT) using CountryRegulatoryRegistry. Add "Regulatory Benchmark" tab to dashboard showing heatmap grid: suppliers as rows, countries as columns, TCO cost as values. Green = lowest exposure, Red = highest.
- **Technical**:
  - RegulatoryProfile record (CountryCode, EprFeeRate, PlasticTaxRate, PpwrMultiplier, RecyclabilityThreshold).
  - TcoEngine.GetResultsAllCountries() returns Dictionary<string, List<TcoResult>>.
  - Triggered automatically on import — no extra user action required.
- **Data governance**:
  - Regulatory rates (EPR, plastic tax, PPWR multipliers) are maintained by country specialists at Scandi Standard via a central SharePoint folder (one file per country).
  - Rates are validated before activation.
  - System reads from validated rate files only — unvalidated rates are never applied to scoring.
  - Integration with SharePoint RAG project already exists as data source.
- **Value proposition**: Single tender file → instant regulatory cost comparison across all 7 Scandi Standard operating countries. Identifies cheapest supplier per country and highest regulatory risk exposure across portfolio.

---

### 🔴 High priority — blocks future packaging profiles

#### [BACK-025] Migrate UI framework — MudBlazor to hybrid architecture
- **Status**: `planned`
- **Category**: Frontend / Architecture
- **Priority**: High (blocks all future packaging profiles)
- **Prerequisite**: POC delivery in MudBlazor complete
- **Reason**: MudBlazor is too rigid for dynamic dashboards, responsive chart layouts, and data-heavy visualizations.
- **Target architecture**:
  - **MudBlazor**: app shell, navigation, snackbars, dialogs, polish
  - **Radzen**: grids, filters, data-heavy cockpit views
  - **Custom PTDE CSS**: brand identity, KPI-cards, colors, spacing
- **Scope**:
  - `MainLayout.razor`
  - `LabelTender.razor`
  - `LabelTenderFilterPanel.razor`
  - `LabelTenderShellSidebar.razor`
  - `LabelTenderDrawerPanel.razor`

---

### 🟠 Score 9 — High value, implement carefully

#### [BACK-017] Versioned TCO Models — Audit Shield
- **Status**: `ready`
- **Category**: Compliance / Architecture
- **Score**: 9
- **Depends on**: BACK-012a, BACK-012b — **delivered** (see Done — 2026-05-14)
- **Description**: Every time a tender is "locked" by a user, serialize and store the complete decision state as JSON: TcoResult, RegulatoryProfile, ScoringWeights, SupplierSelection, timestamp, user.

  This creates an immutable audit trail for EU/PPWR compliance and internal governance. Auditors and regulators can reconstruct exactly why a supplier was chosen at a given point in time.

- **Technical**:
  - `TcoArchiveEntry` record with fields: Id (Guid), LockedAt (DateTime), LockedBy (string), SchemaVersion (int), TcoResultJson (string), RegulatoryProfileJson (string), ScoringWeightsJson (string), TenderName (string).
  - Phase 1: JSON file per tender stored locally in `/data/archive/`
  - Phase 2: Azure Blob Storage (aligns with BACK-002)
  - Phase 3: Structured SQL table when scale requires it
  - SchemaVersion is mandatory — enables migration of old archives when TcoResult structure changes.
  - "Lock tender" button in UI triggers archival. Locked tenders are read-only. Audit view shows full decision trail.

- **Safe-get requirement**: Deserialisation must handle missing fields gracefully using `JsonSerializerOptions` with default values — never crash on old schemas.

- **Value**: Legal defensibility of procurement decisions under PPWR/EU audit.

---

### 🔵 Score 9 — High value, plan carefully

#### [BACK-002] Deploy to Azure (Blazor cockpit)
- **Status**: `idea`
- **Category**: Infrastructure
- **Value**: 5 | **Priority**: 4 | **Effort**: 1–2 days
- **Score**: 9
- **What**: Auto-deploy Blazor cockpit to Azure App Service on every green push to main.
- **Why**: Makes the tool accessible without running it locally. Stakeholder demo without a laptop.
- **Acceptance criteria**:
  - [ ] Azure App Service created and configured
  - [ ] `dotnet.yml` extended with deploy step
  - [ ] Blazor cockpit accessible via public URL after green push
  - [ ] Secrets stored in GitHub — not in code

---

### 🔵 Score 8 — Next sprint

#### [BACK-003] Code coverage reporting
- **Status**: `idea`
- **Category**: CI/CD
- **Value**: 4 | **Priority**: 4 | **Effort**: 3–4h
- **Score**: 8
- **What**: Coverlet + GitHub Actions summary. Percentage of scoring logic covered by tests.
- **Why**: Required discipline before adding new packaging profiles. Proves the engine is trustworthy.
- **Acceptance criteria**:
  - [ ] Coverage % visible in GitHub Actions summary after each push
  - [ ] Minimum threshold defined (suggest 80% for Core)
  - [ ] No coverage for WinForms shell — excluded explicitly

#### [BACK-004] Trays packaging profile
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 5 | **Priority**: 3 | **Effort**: 2–3 days
- **Score**: 8
- **What**: Second Strategy Pattern implementation after Labels. Trays-specific scoring logic.
- **Why**: Proves the engine is extensible. Labels was profile 1 — trays is the proof it scales.
- **Acceptance criteria**:
  - [ ] New profile implements strategy interface — zero changes to TcoEngineService
  - [ ] ADR written before implementation starts
  - [ ] Tests cover trays golden cases (same 5 as Labels)
  - [ ] spec.md updated with trays profile section

#### [BACK-022] Generic profile architecture — reusable sidebar, filter panel, bridge
- **Status**: `idea`
- **Category**: Architecture
- **Score**: 8
- **Depends on**: BACK-019 (UI structure settled)
- **Description**: Abstract LabelTenderSidebarBridge, LabelTenderShellSidebar,
  and LabelTenderFilterPanel into generic base components that any packaging
  profile can inherit or configure.

  Current state: everything is hardcoded to Labels profile.
  Target state: new profile (Trays, Cartons, Films) = new config, not new architecture.

- **Technical**:
  - IProfileSidebarBridge interface with common properties (HasImportedData,
    DrawerSuppliers, FilterPanelOpen, OnToggleFilterPanel etc.)
  - Generic FilterPanelBase.razor with configurable filter group definitions
  - Profile-specific config passed as parameters, not hardcoded
  - LabelTenderSidebarBridge implements IProfileSidebarBridge
  - New profiles get a config class, not a full razor rewrite

- **Value**: Profile nr. 2 (Trays) takes days not weeks.
  UI consistency guaranteed across all profiles by default.
  No copy-paste architecture debt.

---

### 🟡 Score 7 — Important, not urgent

#### [BACK-005] Scenario visualization in Blazor
- **Status**: `idea`
- **Category**: Frontend
- **Value**: 5 | **Priority**: 3 | **Effort**: 3–5 days
- **Score**: 7
- **What**: Visual what-if comparison — show ranking shift when weights or EPR assumptions change.
- **Why**: The slider logic exists. The visual storytelling doesn't. Highest stakeholder impact feature.
- **Acceptance criteria**:
  - [ ] Side-by-side or animated ranking shift when sliders move
  - [ ] No calculation logic in Razor — all through TcoEngineService
  - [ ] Works with existing session/tender data — no hardcoded demo data

#### [BACK-015] Export alignment via Cost Component Registry
- **Status**: `idea`
- **Category**: Architecture
- **Score**: 7
- **Depends on**: BACK-012a — **delivered** (CostComponentRegistry in Core; see Done — 2026-05-14)
- **Description**: Make CSV/XLSX exports dynamic based on CostComponentRegistry. Removes hardcoded column lists in `MainForm.cs`, `ExportService.cs` and `TenderDashboardCsvExporter.cs`. Ensures BI headers are stable and consistent across all export formats.

#### [BACK-021] Material-to-PPWR-grade mapping
- **Status**: `idea`
- **Category**: Architecture / Data
- **Score**: 7
- **Depends on**: BACK-016 (SharePoint / country-rate integration and sustainability data path)
- **Description**: Map material strings (PP top white, Thermo eco, etc.) to PPWR recyclability grades A–E. Data source: sustainability team via SharePoint or leverandør input. Required before PPWR effect moves from test to production.

#### [BACK-018] Constraint-Based Scenario Builder
- **Status**: `idea`
- **Category**: Architecture / Scoring
- **Score**: 7
- **Depends on**: BACK-016
- **Description**: Allow users to define procurement constraints before scoring. System calculates optimal solution within constraints AND shows "cost of constraints" — the price of supply security.

  Example output: "Applying 60% max volume share costs DKK 400,000/year vs. single-supplier optimum."

- **Technical**:
  - Phase 1: Three practical constraints Scandi actually uses:
    - MaxVolumeSharePerSupplier (decimal, default 1.0 = no limit)
    - MinSuppliersPerRegion (int, default 1)
    - RequireLocalSupplier (bool, default false)
  - Phase 2: Simple UI — 2–3 sliders/toggles in Settings view
  - Phase 3: Generic constraint engine only if Phase 1/2 prove insufficient

- **Critical**: Handle conflicting constraints explicitly — show user "These constraints cannot be satisfied simultaneously" rather than silent failure or wrong result.

- **Value**: "Price of security" argument for COO-level decisions. Quantifies supply risk in DKK — not just strategy.

#### [BACK-024] Filter drill-down — vis specs ved label size valg
- **Status**: `idea`
- **Category**: UX
- **Score**: 7
- **Depends on**: multi-select filters (done)
- **Description**: Når category manager vælger en specifik label size 
  i Label Profile, vis automatisk hvilke specs der hører til:
  site, materiale, winding, varenummer, antal linjer.
  
  Eksempel: vælger man "80x110" → panel viser:
  - Varenummer: 540640
  - Site: Jæren
  - Materiale: PP top white
  - Winding: OUT Bottom first
  - Antal linjer: 1
  
  Formål: hurtig kontekst uden at skulle krydstjekke i Evidence table.

- **Value**: Reducerer kognitivt load for category manager ved 
  komplekse tenders med mange label sizes.

---

### ⚪ Score 6 — Backlog

#### [BACK-006] Automatic semantic versioning
- **Status**: `idea`
- **Category**: CI/CD
- **Value**: 3 | **Priority**: 3 | **Effort**: 4–6h
- **Score**: 6
- **What**: Tag releases automatically on green push to main. Semantic versioning (1.0.0, 1.0.1).
- **Why**: Makes rollback, audit trail, and release history explicit. Required before stakeholder demos.
- **Acceptance criteria**:
  - [ ] Version tag created automatically after green build
  - [ ] Tag visible on GitHub releases page
  - [ ] Version convention documented in decisions.md

#### [BACK-007] ERP / BI integration stub
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 4 | **Priority**: 2 | **Effort**: 1–2 days
- **Score**: 6
- **What**: Export-ready API contract for M3 or Power BI. Interface only — no full integration.
- **Why**: Defines the boundary now so integration is additive later, not a redesign.
- **Implementation note**: Blocked by BACK-015 — wait for export alignment before BI stub
- **Acceptance criteria**:
  - [ ] DTO contract defined and documented
  - [ ] CSV export working and tested
  - [ ] ADR written for integration approach

---

### ⚪ Score 5 — Low urgency

#### [BACK-008] Knockout / exclusion rules (v2)
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 4 | **Priority**: 2 | **Effort**: 3–4 days
- **Score**: 5
- **What**: Hard constraints that auto-exclude non-compliant suppliers. ADR required before touching engine.
- **Why**: v1 uses Manual Review as safety net. v2 needs explicit exclusion for PPWR hard failures.
- **Acceptance criteria**:
  - [ ] ADR written and approved before any code
  - [ ] Golden cases updated to cover exclusion scenarios
  - [ ] Manual Review remains non-blocking for soft failures

#### [BACK-009] Fix ADR-003 gap in decisions.md
- **Status**: `ready`
- **Category**: Docs
- **Value**: 2 | **Priority**: 3 | **Effort**: 30min
- **Score**: 5
- **What**: Document or explicitly close the missing ADR-003. Traceability hole in the decision log.
- **Why**: decisions.md goes 001 → 002 → 004. Either ADR-003 was deleted, renamed, or never written.
- **Acceptance criteria**:
  - [ ] ADR-003 either written with correct decision or marked as voided with explanation

#### [BACK-010] Supplier risk scoring (M3 data)
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 5 | **Priority**: 1 | **Effort**: 1–2 weeks
- **Score**: 5
- **What**: Financial health + geographic exposure scoring using M3 supplier master data.
- **Why**: Completes the risk dimension. Blocked on M3 integration maturity.
- **Acceptance criteria**:
  - [ ] M3 integration approach decided (ADR first)
  - [ ] Risk score integrated into TcoEngineService via Strategy Pattern
  - [ ] Does not break existing Labels scoring


#### [BACK-013] Solution og namespace rename til PTD-E
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 3 | **Priority**: 2 | **Effort**: 1 day
- **Score**: 4
- **What**: Rename solution, projects og namespaces fra `PackagingTenderTool` til `PackagingTenderDecisionEngine` eller `PTDE`. GUI titel er allerede korrekt.
- **Why**: "PackagingTenderTool" underdriver produktet. PTD-E kommunikerer et seriøst beslutningssystem.
- **Risk**: Rammer 50+ filer. Kræver fuld build + 135 tests grønne bagefter.
- **Acceptance criteria**:
  - [ ] Alle `.sln` og `.csproj` filer renamed
  - [ ] Alle namespaces opdateret
  - [ ] `dotnet build` — 0 errors
  - [ ] `dotnet test` — 135 tests grønne
  - [ ] CI badge stadig grøn
  - [ ] Docs opdateret
  
---

## Ideas (unscored — not ready for backlog)

| Idea | Notes |
|---|---|
| Plausibility checks for suspicious supplier inputs | Catches artificially low bids |
| Multi-tender comparison view | Compare two tender results side by side |
| PDF export of evaluation report | For stakeholder sign-off |
| Supplier master data via M3 ID | Replaces name-based grouping in v2 |

---

## Done

### Backlog items (completed)

#### [BACK-012a] Cost Component Registry (blocks BACK-012b — satisfied)
- **Status**: `done` — 2026-05-14
- **Category**: Architecture / Scoring
- **Score**: 10 (at delivery)
- **Note**: `TcoResult` moved to Core layer. `CostComponentRegistry` implemented with five cost components plus `ppwr_risk`; reflection in `TcoDashboardViewModel` replaced with explicit registry. Startup validation on `GetValue` delegates.

#### [BACK-012b] PPWR Risk Multiplier — time-bounded market access scoring
- **Status**: `done` — 2026-05-14
- **Category**: Architecture / Scoring
- **Score**: 9 (at delivery)
- **Note**: PPWR Risk Multiplier **Option A** (static grade lookup). Grade **C = 5%**, **D = 15%**, **E = 25%** penalty on commercial spend; A/B = 0%. `MarketAccessRisk2030` / `MarketAccessRiskNow` flags implemented. `PpwrEffectTest` toggle in Scenarios section (with PPWR scenario coupling) for POC demonstration. **No real material-to-grade mapping yet** — grades default to A unless set in session. Real grades require sustainability team input or SharePoint integration (BACK-016 dependency). See `docs/decisions/ADR-ppwr-risk-multiplier.md`.

#### [BACK-020] Pivot / multi-supplier label tender import
- **Status**: `done` — 2026-05-14
- **Category**: Import
- **Note**: `PivotLabelsExcelImportService` handles multi-supplier pivot workbook format. Auto-detected by worksheet name **"All labels DSH"**; pipeline feeds `LabelsExcelImportService` for unified tender model.

#### [BACK-001] Test results visible on GitHub
- **Status**: `done` — 2026-05-07
- **Category**: CI/CD
- **Value**: 5 | **Priority**: 5 | **Effort**: 2h
- **Score**: 10
- **What**: Add TRX test logger + GitHub test reporter action. Inline test failure details in Actions.
- **Result**: Tests passing, visible by name and suite in GitHub Actions summary.

### Misc completions

| Item | Completed | Notes |
|---|---|---|
| Repo moved off OneDrive | 2026-05-07 | Now at C:\Dev — no more phantom file changes |
| Documentation structure aligned | 2026-05-07 | _INDEX.md, ARCHITECTURE.md, single README |
| CI/CD pipeline green | 2026-05-07 | dotnet.yml upgraded to @v5, Release config fixed |
| Mermaid diagrams fixed | 2026-05-07 | spec.md diagram rendering on GitHub |
| .cursorrules doc ownership section | 2026-05-07 | Section 0 prevents future sprawl |
