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
- **Status**: `ready` (Part A+B done, revision comparison pending)
- **Category**: Frontend / UX
- **Score**: 10
- **Description**: Side-by-side sammenligning af revisioner per linje i deep-dive. `[LeverandørNavn] Rev[N]` konvention.

#### [BACK-019] POC Visualization & Navigation Design
- **Status**: `ready`
- **Category**: Frontend / UX
- **Score**: 10

#### [BACK-016] Multi-country regulatory benchmark engine
- **Status**: `ready`
- **Category**: Architecture / Scoring
- **Score**: 10

---

### 🟠 Score 9 — High value, implement carefully

#### [BACK-017] Versioned TCO Models — Audit Shield
- **Status**: `ready`
- **Category**: Compliance / Architecture
- **Score**: 9

---

### 🔵 Score 9 — High value, plan carefully

#### [BACK-002] Deploy to Azure (Blazor cockpit)
- **Status**: `idea`
- **Category**: Infrastructure
- **Score**: 9

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

#### [BACK-021] Material-to-PPWR-grade mapping
- **Status**: `idea`
- **Score**: 7

#### [BACK-018] Constraint-Based Scenario Builder
- **Status**: `idea`
- **Score**: 7

#### [BACK-024] Filter drill-down
- **Status**: `idea`
- **Score**: 7

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