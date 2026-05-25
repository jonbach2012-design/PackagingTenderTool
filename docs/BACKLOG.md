# Backlog — PackagingTenderTool

<!-- AUDIENCE: Developer / Architect | OWNER: docs/BACKLOG.md -->
<!-- Priority order for next sessions:
1. BACK-028 Deep-dive revision side-by-side (Part C)
2. BACK-019 POC visualization
3. BACK-016 multi-country benchmark
4. BACK-002 Azure deploy
-->

## How to use this

- **Status**: `idea` → `ready` → `in progress` → `done`
- **Score**: Value (1-5) + Priority (1-5) − Effort (1-5 inverted). Max = 10.
- **Rule**: Never start something not listed here. Never finish without updating status.

---

## Active Backlog

### 🟢 Score 10 — Do next

#### [BACK-031] Supplier Price Compare
- **Status**: `done` — 2026-05-25
- **Note**: `SupplierPriceCompareService`, DTOs, `LabelTenderPriceCompare.razor`, tab wired.
  NavBtnClass + GoTab alignment fixed. Price Compare filter bug fixed (sends filtered lines).
- **Category**: Frontend / Core / Architecture
- **Score**: 10
- **Depends on**: BACK-029 (done)
- **Description**: Side-by-side price and spend comparison across selected suppliers. No scoring, TCO, regulatory or compliance logic.

  **Summary view per supplier:**
  - Supplier name
  - Number of quoted lines
  - Total offered spend
  - Average unit price
  - Lowest quoted price count (lines where cheapest)
  - Highest quoted price count (lines where most expensive)
  - Price difference vs cheapest supplier (kr)
  - Percentage difference vs cheapest supplier (%)
  - Missing price / no-bid lines

  **Line drilldown:**
  - One row per tender line (ItemNo/surrogate key)
  - Columns: Label format, Site, Material + one price column per supplier
  - Cheapest price highlighted per line
  - Missing bids shown as "—"

  **Architecture requirements:**
  - Calculation logic in `SupplierPriceCompareService` (Core) — not in Razor
  - DTOs: `SupplierPriceCompareRow`, `SupplierPriceLineRow`, `SupplierPriceCompareResult`
  - Reuse existing `LabelLineItem` models
  - Separate Razor component: `LabelTenderPriceCompare.razor`
  - New tab: PRICE COMPARE in sidebar navigation
  - Unit tests for all calculation logic

  **DTO structure:**
  ```csharp
  SupplierPriceCompareRow {
      SupplierName, QuotedLineCount, MissingLineCount,
      TotalOfferedSpend, AverageUnitPrice,
      LowestPriceCount, HighestPriceCount,
      SpendVsCheapest, PctVsCheapest
  }

  SupplierPriceLineRow {
      ItemNo, LabelSize, Site, Material,
      Prices[] // SupplierName + Price + IsCheapest
  }

  SupplierPriceCompareResult {
      SupplierRows, LineRows
  }
  ```

  **Test cases:**
  - To leverandører, én linje → korrekt cheapest detection
  - Tre leverandører, fem linjer → korrekt LowestPriceCount per leverandør
  - Leverandør mangler bud på én linje → MissingLineCount = 1
  - Alle byder samme pris → PctVsCheapest = 0
  - SpendVsCheapest beregnes korrekt

- **Acceptance criteria**:
  - [ ] `SupplierPriceCompareService` beregner alle felter korrekt
  - [ ] Unit tests dækker alle 5 test cases
  - [ ] Summary tabel viser alle leverandører
  - [ ] Line drilldown viser priser per linje
  - [ ] Billigste pris per linje highlightet
  - [ ] PRICE COMPARE tab i sidebar
  - [ ] `dotnet build` green, existing tests pass

#### [BACK-028] Deep-dive revision side-by-side
- **Status**: `in progress` — Step 1 done 2026-05-25
- **Note**: Import dialog, dynamic header detection, accumulation with deduplication implemented.
  Remaining: sidebar grouping (Step 2), deep-dive side-by-side comparison (Step 3).
- **Category**: Frontend / UX
- **Score**: 10
- **Description**: Side-by-side sammenligning af revisioner per linje i deep-dive. `[LeverandørNavn] Rev[N]` konvention.

#### [BACK-019] POC Visualization & Navigation Design
- **Status**: `ready`
- **Category**: Frontend / UX
- **Score**: 10
- **Description**: POC approval requires three things: easy navigation, correct data, and overview with deep dive capability.

  **OVERVIEW LAYER** (what decision to make):
  - Dashboard: 4 KPI cards (Recommended, Lowest price, Best TCO, Risk exposure) + Spend overview chart. Already exists — refine only.
  - Regulatory Benchmark tab: heatmap grid suppliers × countries (BACK-016 output)
  - Single recommended supplier highlighted with clear rationale text

  **NAVIGATION LAYER** (how to move between views):
  - Sidebar: Import → Dashboard → Price Compare → Deep-dive → Audit Board → Settings. Clean, no clutter.
  - Breadcrumb or context bar: always shows current tender name, line count, supplier count
  - "Why this recommendation?" — clickable explanation on dashboard that links to Audit Board

  **DEEP DIVE LAYER** (prove the numbers):
  - Deep-dive: import issues (errors only), full line-item grid with search/filter.
  - Audit Board: supplier-by-supplier score breakdown. Commercial / Technical / Regulatory pillars visible. CalculationBreakdown text per supplier.
  - Price Compare: supplier × line item grid. Price per 1,000, quantity, spend.

  **INTERACTION PRINCIPLES**:
  - Every number must be traceable — click spend → see line items
  - No orphaned data — every view links to source or explanation
  - Filters persist across tab navigation
  - Mobile-readable (wide screen primary, but no horizontal scroll traps)
  - Replace native SVG tooltips with MudTooltip hover-cards showing CalculationBreakdown

  **IMMEDIATE ACTIONS** (before POC presentation):
  1. Add "Why recommended?" explanation card on Dashboard
  2. Ensure Audit Board shows pillar scores visually (bar or gauge)

- **Value**: Unblocks POC — stakeholders see a coherent story from KPI to evidence.

#### [BACK-016] Multi-country regulatory benchmark engine
- **Status**: `idea`
- **Category**: Architecture / Scoring
- **Score**: 7
- **Description**: Regulatory cost comparison across all Scandi Standard operating countries from a single tender import.

- **Technical**:
  - `RegulatoryProfile` record (CountryCode, EprFeeRate, PlasticTaxRate, PpwrMultiplier, RecyclabilityThreshold)
  - `TcoEngine.GetResultsAllCountries()` returns `Dictionary<string, List<TcoResult>>`
  - Triggered automatically on import — no extra user action required
  - 7 countries: DK, SE, NO, FI, IE, NL, LT

- **Data governance**:
  - Regulatory rates (EPR, plastic tax, PPWR multipliers) maintained by country specialists at Scandi Standard via central SharePoint folder (one file per country)
  - Rates validated before activation — unvalidated rates never applied to scoring
  - Integration with SharePoint RAG project already exists as data source

- **Value proposition**: Single tender file → instant regulatory cost comparison across all 7 Scandi Standard operating countries. Identifies cheapest supplier per country and highest regulatory risk exposure across portfolio.

---

### 🟠 Score 9 — High value, implement carefully

#### [BACK-017] Versioned TCO Models — Audit Shield
- **Status**: `ready`
- **Category**: Compliance / Architecture
- **Score**: 9
- **Description**: Every time a tender is "locked" by a user, serialize and store the complete decision state as JSON: TcoResult, RegulatoryProfile, ScoringWeights, SupplierSelection, timestamp, user. Creates an immutable audit trail for EU/PPWR compliance and internal governance.

- **Technical**:
  - `TcoArchiveEntry` record: Id (Guid), LockedAt (DateTime), LockedBy (string), SchemaVersion (int), TcoResultJson (string), RegulatoryProfileJson (string), ScoringWeightsJson (string), TenderName (string)
  - Phase 1: JSON file per tender in `/data/archive/`
  - Phase 2: Azure Blob Storage (aligns with BACK-002)
  - Phase 3: Structured SQL table when scale requires it
  - SchemaVersion mandatory — enables migration of old archives when TcoResult structure changes
  - "Lock tender" button in UI triggers archival. Locked tenders are read-only.

- **Safe-get requirement**: Deserialisation must handle missing fields gracefully using `JsonSerializerOptions` with default values — never crash on old schemas.

- **Value**: Legal defensibility of procurement decisions under PPWR/EU audit.

---

### 🔵 Score 9 — High value, plan carefully

#### [BACK-002] Deploy to Azure (Blazor cockpit)
- **Status**: `idea`
- **Category**: Infrastructure
- **Score**: 9
- **What**: Auto-deploy Blazor cockpit to Azure App Service on every green push to main.
- **Why**: Makes the tool accessible without running it locally. Required before stakeholder demos outside dev team.

---

### 🔵 Score 8 — Next sprint

#### [BACK-003] Code coverage reporting
- **Status**: `idea`
- **Score**: 8

#### [BACK-004] Trays packaging profile
- **Status**: `idea`
- **Score**: 8

#### [BACK-022] Generic profile architecture
- **Status**: `idea`
- **Score**: 8

---

### 🟡 Score 7 — Important, not urgent

#### [BACK-005] Scenario visualization in Blazor
- **Status**: `idea`
- **Score**: 7

#### [BACK-015] Export alignment via Cost Component Registry
- **Status**: `idea`
- **Score**: 7
- **Description**: Make CSV/XLSX exports dynamic based on CostComponentRegistry. Removes hardcoded column lists in export services. Ensures BI headers are stable and consistent across all export formats.
- **Depends on**: BACK-012a (done)

#### [BACK-021] Material-to-PPWR-grade mapping
- **Status**: `idea`
- **Score**: 7
- **Description**: Map material strings (PP top white, Thermo eco, Synthermal etc.) to PPWR recyclability grades A–E. Data source: sustainability team via SharePoint or supplier input. Required before PPWR effect moves from test to production.
- **Depends on**: BACK-016 (SharePoint / country-rate integration and sustainability data path)

#### [BACK-018] Constraint-Based Scenario Builder
- **Status**: `idea`
- **Score**: 7
- **Description**: Allow users to define procurement constraints before scoring. System calculates optimal solution within constraints AND shows "cost of constraints" — the price of supply security.

  Example output: "Applying 60% max volume share costs DKK 400,000/year vs. single-supplier optimum."

- **Technical**:
  - Phase 1: Three practical constraints Scandi actually uses:
    - `MaxVolumeSharePerSupplier` (decimal, default 1.0 = no limit)
    - `MinSuppliersPerRegion` (int, default 1)
    - `RequireLocalSupplier` (bool, default false)
  - Phase 2: Simple UI — 2–3 sliders/toggles in Settings view
  - Phase 3: Generic constraint engine only if Phase 1/2 prove insufficient

- **Critical**: Handle conflicting constraints explicitly — show user "These constraints cannot be satisfied simultaneously" rather than silent failure or wrong result.

- **Value**: "Price of security" argument for COO-level decisions. Quantifies supply risk in DKK — not just strategy.

#### [BACK-024] Filter drill-down
- **Status**: `idea`
- **Score**: 7
- **Description**: Når category manager vælger en specifik label size i Label Profile, vis automatisk hvilke specs der hører til: site, materiale, winding, varenummer, antal linjer.

  Eksempel: vælger man "80x110" → panel viser:
  - Varenummer: 540640
  - Site: Jæren
  - Materiale: PP top white
  - Antal linjer: 1

- **Value**: Reducerer kognitivt load for category manager ved komplekse tenders med mange label sizes.

---

### ⚪ Score 5 — Low urgency

#### [BACK-008] Knockout / exclusion rules (v2)
- **Status**: `idea`
- **Score**: 5

#### [BACK-010] Supplier risk scoring (M3 data)
- **Status**: `idea`
- **Score**: 5

#### [BACK-013] Solution og namespace rename til PTD-E
- **Status**: `idea`
- **Score**: 4

---

## Ideas (unscored)

| Idea | Notes |
|---|---|
| Plausibility checks for suspicious supplier inputs | Catches artificially low bids |
| Multi-tender comparison view | Compare two tender results side by side |
| PDF export of evaluation report | For stakeholder sign-off |
| TenderPriceAnalyze: læs valuta dynamisk fra kolonneheader | Frem for hardkodet DKK/NOK antagelse |

---

## Done

#### [BACK-031-followup] Dashboard + import fixes
- **Status**: `done` — 2026-05-25
- **Note**: Weighted average spend in BuildSuppliersFromImportedLines (Sum×Average bug).
  Flexoprint spend basis changed to priceInTarget×historicalVolume/1000.
  Price Matrix hidden (demo data). Price Compare filter wired correctly.

#### [BACK-030] Currency conversion service + currency selector UI
- **Status**: `done` — 2026-05-23
- **Note**: `CurrencyConverter` service, `TenderSettings.TargetCurrency` + `CurrencyRates`, MudButton toggles i Settings, editerbare kurser, `_targetCurrency` + `_currencyRates` sendes til import.

#### [BACK-029] TenderPriceAnalyze import service
- **Status**: `done` — 2026-05-23
- **Note**: Ny `TenderPriceAnalyzeImportService`, surrogate key `{DSH Site}|{Label format}`, dynamic column offset via `FindColumnOffset()`, Flexoprint DKK = CurrentContractPrice, revision suffix support, `SurfaceFinish` tilføjet til `LabelLineItem`.

#### [BACK-028] Deep-dive UI redesign (Part A + B)
- **Status**: `done` — 2026-05-23
- **Note**: Leverandør-header med badges + 3 KPI-kort, pris-analyse tabel per linje sorteret efter afvigelse, TCO breakdown. "Tilbage til dashboard" gendanner alle leverandørers selection. Sidebar-valg påvirker ikke deep-dive. Leverandør oversigt som sidebar-link.

#### [BACK-027] Dashboard UX overhaul
- **Status**: `done` — 2026-05-21

#### [BACK-026] Baseline price + current price deviation
- **Status**: `done` — 2026-05-21

#### [BACK-025] Migrate UI framework — hybrid architecture
- **Status**: `done` — 2026-05-21

#### [BACK-012a] Cost Component Registry
- **Status**: `done` — 2026-05-14

#### [BACK-012b] PPWR Risk Multiplier
- **Status**: `done` — 2026-05-14

#### [BACK-020] Pivot / multi-supplier label tender import
- **Status**: `done` — 2026-05-14

#### [BACK-001] Test results visible on GitHub
- **Status**: `done` — 2026-05-07

### Misc completions

| Item | Completed | Notes |
|---|---|---|
| Repo moved off OneDrive | 2026-05-07 | Now at C:\Dev |
| Documentation structure aligned | 2026-05-07 | _INDEX.md, ARCHITECTURE.md |
| CI/CD pipeline green | 2026-05-07 | dotnet.yml upgraded to @v5 |