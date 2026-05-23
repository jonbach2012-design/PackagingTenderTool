# Backlog — PackagingTenderTool

<!-- AUDIENCE: Developer / Architect | OWNER: docs/BACKLOG.md -->
<!-- This is the single source of truth for planned improvements.
     Before starting any new feature or improvement, check here first.
     Before adding a new idea, add it here — not in chat, not in a comment, not in your head. -->

<!-- Priority order for next sessions:
1. BACK-030 currency conversion service (BACK-029 depends on it)
2. BACK-029 TenderPriceAnalyze import service
3. BACK-028 deep-dive UI redesign + revision handling
4. BACK-019 visualization (POC blocker)
5. BACK-016 multi-country benchmark
6. BACK-017 audit shield
7. BACK-018 constraint builder
8. BACK-021 material PPWR grade mapping
-->

## How to use this

- **Status**: `idea` → `ready` → `in progress` → `done`
- **Score**: Value (1-5) + Priority (1-5) − Effort (1-5 inverted). Max = 10.
- **Rule**: Never start something not listed here. Never finish without updating status.

---

## Active Backlog

### 🟢 Score 10 — Do next

#### [BACK-030] Currency conversion service + currency selector UI
- **Status**: `ready`
- **Category**: Core / Frontend
- **Score**: 10
- **Depends on**: nothing — implement first, BACK-029 depends on it
- **Description**: Unified currency handling. All prices normalised to user-selected target currency. See ADR-008.

  **`CurrencyConverter` service (Core):**
  - `Convert(decimal amount, string fromCurrency, string toCurrency) → decimal`
  - Rates in `TenderSettings.CurrencyRates` as `Dictionary<string, decimal>` keyed `"DKK:NOK"` etc.
  - Inverse rate auto-computed: `rate("NOK:DKK") = 1 / rate("DKK:NOK")`
  - Same currency → return unchanged
  - Default rates (2026-05-23): DKK→NOK = 1.4403
  - Registered as scoped service in `Program.cs`

  **`TenderSettings` changes:**
  - Add `TargetCurrency` (string, default `"NOK"`)
  - Add `CurrencyRates` (Dictionary<string, decimal>)

  **UI — Settings view:**
  - Dropdown: DKK / NOK / SEK / EUR
  - Current rate displayed: "DKK → NOK: 1.4403"
  - Rate editable inline for manual override
  - Changing currency triggers re-import

- **Acceptance criteria**:
  - [ ] `CurrencyConverter` converts between DKK, NOK, SEK, EUR
  - [ ] Inverse rate computed automatically
  - [ ] `TenderSettings.TargetCurrency` and `CurrencyRates` added
  - [ ] Currency selector in Settings view
  - [ ] Rate editable per tender
  - [ ] Re-import triggered on currency change
  - [ ] `dotnet build` green, existing tests pass

#### [BACK-029] TenderPriceAnalyze import service — new Labels format
- **Status**: `ready`
- **Category**: Import / Architecture
- **Score**: 10
- **Depends on**: BACK-030 (CurrencyConverter must exist first)
- **Description**: New `TenderPriceAnalyzeImportService` for the consolidated Labels tender format. Replaces pivot as primary Labels import path. See ADR-008 for full spec.

  **Key rules:**
  - Detection: `Label format` + `Flexoprint` in header row — sheet name irrelevant
  - Surrogate key: `"{DSH Site}|{Label format}"` → stored as `ItemNo`
  - Quantity: `Historical yearly volume` (column G)
  - CurrentContractPrice: Flexoprint DKK price (column K) converted to TargetCurrency
  - Per input row → 4 `LabelLineItem` rows (one per supplier)
  - DKK prices (Flexoprint K, Grafiket Q) → convert to TargetCurrency
  - NOK prices (Norsk Etikett M, Ettiketto U) → convert to TargetCurrency
  - New field: `LabelLineItem.SurfaceFinish` (string?) from column E

  **Model change:**
  - Add `SurfaceFinish` (string?) to `LabelLineItem`

  **Auto-detection order in `LabelTender.razor`:**
  ```
  1. Headers match TenderPriceAnalyze → TenderPriceAnalyzeImportService
  2. Sheet "All labels DSH" exists  → PivotLabelsExcelImportService (legacy)
  3. Otherwise                       → LabelsExcelImportService
  ```

- **Acceptance criteria**:
  - [ ] Reads all 4 supplier blocks per row
  - [ ] `{DSH Site}|{Label format}` as surrogate `ItemNo`
  - [ ] `Historical yearly volume` as Quantity
  - [ ] Flexoprint DKK price = `CurrentContractPrice` (converted)
  - [ ] All prices converted to `TargetCurrency`
  - [ ] `SurfaceFinish` imported to new field
  - [ ] Detection by header structure, not sheet name
  - [ ] `dotnet build` green, existing tests pass

#### [BACK-028] Deep-dive UI redesign + revision handling
- **Status**: `ready`
- **Category**: Frontend / UX / Import
- **Score**: 10
- **Depends on**: BACK-027 (done)
- **Description**: Three-section deep-dive layout + revision support.

  *Section 1 — Leverandør-header:*
  - "Tilbage til dashboard" + leverandørnavn + badges + 3 KPI-kort

  *Section 2 — Pris-analyse per linje:*
  - Varenr., Label size, Winding, Materiale, Pris, Best bid, Nuv. pris, Afvigelse %
  - Sorteret efter afvigelse, farvekodet

  *Section 3 — TCO breakdown*

  **Revision handling:** `[LeverandørNavn] Rev[N]` konvention.

- **Technical gæld**: Pivot test-fixtures bruger gamle kolonnenumre.
- **Acceptance criteria**:
  - [ ] Deep-dive header med KPI-kort
  - [ ] Pris-analyse tabel med afvigelse sorteret
  - [ ] Revision-gruppering i supplier selector
  - [ ] `dotnet build` green, existing tests pass

#### [BACK-019] POC Visualization & Navigation Design
- **Status**: `ready`
- **Category**: Frontend / UX
- **Score**: 10
- **Description**: Coherent visualization layer across all views. Overview → navigation → deep dive.
- **Immediate actions**:
  1. Remove duplicate KPI boxes from Deep-dive
  2. Add "Why recommended?" explanation card on Dashboard
  3. Audit Board shows pillar scores visually
  4. Price Matrix shows real imported data

#### [BACK-016] Multi-country regulatory benchmark engine
- **Status**: `ready`
- **Category**: Architecture / Scoring
- **Score**: 10
- **Depends on**: BACK-012a, BACK-012b (done)
- **Description**: Run all suppliers through 7 regulatory profiles (DK, SE, NO, FI, IE, NL, LT). Heatmap grid on dashboard.

---

### 🟠 Score 9 — High value, implement carefully

#### [BACK-017] Versioned TCO Models — Audit Shield
- **Status**: `ready`
- **Category**: Compliance / Architecture
- **Score**: 9
- **Depends on**: BACK-012a, BACK-012b (done)

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
- **Category**: CI/CD
- **Score**: 8

#### [BACK-004] Trays packaging profile
- **Status**: `idea`
- **Category**: Architecture
- **Score**: 8

#### [BACK-022] Generic profile architecture
- **Status**: `idea`
- **Category**: Architecture
- **Score**: 8
- **Depends on**: BACK-019

---

### 🟡 Score 7 — Important, not urgent

#### [BACK-005] Scenario visualization in Blazor
- **Status**: `idea`
- **Category**: Frontend
- **Score**: 7

#### [BACK-015] Export alignment via Cost Component Registry
- **Status**: `idea`
- **Category**: Architecture
- **Score**: 7

#### [BACK-021] Material-to-PPWR-grade mapping
- **Status**: `idea`
- **Category**: Architecture / Data
- **Score**: 7

#### [BACK-018] Constraint-Based Scenario Builder
- **Status**: `idea`
- **Category**: Architecture / Scoring
- **Score**: 7

#### [BACK-024] Filter drill-down — vis specs ved label size valg
- **Status**: `idea`
- **Category**: UX
- **Score**: 7

---

### ⚪ Score 6 — Backlog

#### [BACK-006] Automatic semantic versioning
- **Status**: `idea`
- **Category**: CI/CD
- **Score**: 6

#### [BACK-007] ERP / BI integration stub
- **Status**: `idea`
- **Category**: Architecture
- **Score**: 6

---

### ⚪ Score 5 — Low urgency

#### [BACK-008] Knockout / exclusion rules (v2)
- **Status**: `idea`
- **Score**: 5

#### [BACK-009] Fix ADR-003 gap in decisions.md
- **Status**: `ready`
- **Score**: 5

#### [BACK-010] Supplier risk scoring (M3 data)
- **Status**: `idea`
- **Score**: 5

#### [BACK-013] Solution og namespace rename til PTD-E
- **Status**: `idea`
- **Score**: 4

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
| Mermaid diagrams fixed | 2026-05-07 | spec.md diagram rendering on GitHub |
| .cursorrules doc ownership section | 2026-05-07 | Section 0 prevents future sprawl |