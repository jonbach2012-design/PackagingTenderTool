# Decision Log

<!-- All architectural decisions live here. One file, in order.
     Do not create separate ADR files. -->

## ADR-001 — SRP & Decoupling

- **Status**: Done
- **Context**: Calculation logic was leaking into UI components, making it untestable and fragile.
- **Decision**: Moved all TCO math to `TcoEngineService.cs`. UI code must never contain calculation logic.
- **Consequence**: Logic is testable without UI. Rendering side-effects are isolated. Enforced by `.cursorrules`.

---

## ADR-002 — Deterministic SVG Output

- **Status**: Done
- **Context**: SVG attributes were corrupted by Danish decimal comma format, causing empty bars and diagonal text in the dashboard.
- **Decision**: Forced `InvariantCulture` (`.`) for all SVG/numeric output via `FmtSvg` helper.
- **Consequence**: Prevents "Empty Bar" syndrome. All SVG output is culture-independent and deterministic.

---

## ADR-003 — Migration to Blazor + hybrid UI layer

- **Status**: Done
- **Context**: WinForms could not support interactive SVG, real-time sliders, or browser-based stakeholder access.
- **Decision**: Blazor is the primary UI direction for the tender cockpit. WinForms retained as a minimal verification shell only.
- **Update (2026-05-21)**: Radzen 7.3.0 added as chart and data-grid layer alongside MudBlazor. See ADR-006.
- **Consequence**: Enhanced UX, deterministic web rendering, stakeholder access without local install.

---

## ADR-004 — Configuration Isolation

- **Status**: Partially implemented — under review
- **Context**: `epr-settings.json` at repo root creates ambiguity between runtime config and source code.
- **Decision**: Move `epr-settings.json` into `/config` folder for clear config boundary.
- **Note**: File is currently still at repo root. Migration to `/config` is pending — see BACKLOG.md BACK-011.

---

## ADR-005 — 80/20 Audit Strategy

- **Status**: Active
- **Context**: Full audit on every change was slowing development without proportional benefit.
- **Decision**: Mandatory audit applies only to the 20% core — formulas, weights, DTO/data contracts, filtering/aggregation. UI cosmetic changes are fast-tracked.

### Audit triggers — mandatory

Audit required when changing: formulas, weighting, DTO/data contracts, filtering/aggregation.

### Golden cases — always verify

1. Zero volume
2. Missing data / grades
3. Extreme scaling
4. PPWR toggles
5. Ranking stability

### Rollback discipline

Core-logic changes must be shipped as small commits (<200 lines) to keep rollback safe and fast.

---

## ADR-006 — Hybrid UI Architecture (MudBlazor + Radzen + PTDE CSS)

- **Status**: Done — 2026-05-21
- **Context**: MudBlazor 7 is too rigid for dynamic dashboards and data-heavy visualizations.
- **Decision**: Three-way split:

| Layer | Responsibility |
|-------|----------------|
| **MudBlazor** | App shell, navigation, snackbars, dialogs, polish |
| **Radzen 7.3.0** | Charts, grids, data-heavy cockpit views |
| **Custom PTDE CSS** | Brand identity, KPI-cards, colors, spacing |

- **Rules**:
  - `Variant` and `AlignItems` aliased to MudBlazor in `_Imports.razor` — do not remove.
  - `RadzenReferenceLine` not available in Radzen 7.x — use flat `RadzenLineSeries` as workaround.
- **Consequence**: Incremental migration. MudBlazor rigidity solved for charts without destabilising the shell.

---

## ADR-007 — Pivot Import Column Layout (Hardcoded Indices)

- **Status**: Done — 2026-05-21
- **Context**: `PivotLabelsExcelImportService` reads a fixed-format "All labels DSH" workbook.
- **Decision**: Hardcoded column indices — simpler, faster, easier to audit than fuzzy header matching.
- **Current layout** (as of 2026-05-21):

| Column | Field |
|--------|-------|
| A (1) | Item no |
| B (2) | Item name |
| C (3) | DSH Site |
| D (4) | Quantity |
| E (5) | Label size |
| F (6) | Winding direction |
| G (7) | Material |
| H (8) | Reel diameter / pcs per roll |
| I (9) | No. of colors |
| J (10) | Suggested MOQ |
| K (11) | current_price |
| L–N (12–14) | Flexoprint (price, MOQ, comment) |
| O–Q (15–17) | Norsk Etikett (price, MOQ, comment) |
| R–T (18–20) | Grafiket (price, MOQ, comment) |
| U–W (21–23) | Ettiketto (price, MOQ, comment) |

- **Risk**: Column mismatch → silent data errors. Any format change must update both `PivotLabelsExcelImportService.cs` AND this ADR.
- **Technical gæld**: Pivot test-fixtures still reference old column indices (11, 14, 17, 20).

---

## ADR-008 — TenderPriceAnalyze Import Format + Currency Architecture

- **Status**: Ready — 2026-05-23
- **Context**: A new consolidated Excel format ("Tender Price Analyze") replaces the previous pivot format as the primary Labels tender input. The new format:
  - Has one row per label format per site (not one row per supplier per item)
  - Uses `DSH Site + Label format` as surrogate key (no `Item no` column)
  - Has supplier price blocks in columns (like pivot) but with DKK and NOK mixed
  - Has Flexoprint as the current supplier — Flexoprint price = `CurrentContractPrice`
  - Sheet name is irrelevant and must not be used for format detection

  Additionally, multi-currency comparison is required: all supplier prices must be normalised to a single user-selected currency (DKK, NOK, SEK, EUR) for meaningful spend comparison.

### Import format detection

Format detected by column header structure — sheet name is unreliable (users rename sheets):

| Headers present | Format |
|-----------------|--------|
| `Label format` + `Flexoprint` in header row | TenderPriceAnalyze → `TenderPriceAnalyzeImportService` |
| Sheet name = "All labels DSH" | Legacy pivot → `PivotLabelsExcelImportService` |
| Otherwise | Standard → `LabelsExcelImportService` |

### Column layout (TenderPriceAnalyze format)

**Anchor columns (1–12, hardcoded + column offset):**

| Column | Field | Currency | Notes |
|--------|-------|----------|-------|
| A (1) | DSH Site | — | |
| B (2) | Label format | — | → `LabelSize` + part of surrogate `ItemNo` |
| C (3) | Label material | — | → `Material` |
| D (4) | No. of colors | — | → `NumberOfColors` |
| E (5) | Surface finish | — | → `SurfaceFinish` |
| F (6) | Labels per roll | — | → `ReelDiameterOrPcsPerRoll` |
| G (7) | Historical yearly volume | — | → `Quantity` (basis for spend) |
| H (8) | Number of designs | — | → `Comment` |
| I (9) | Suggested production volume | — | imported, not used for spend |
| J (10) | Stock article | — | → `Comment` |
| K (11) | Flexoprint price | DKK | → `CurrentContractPrice` + Flexoprint `PricePerThousand` |
| L (12) | Flexoprint spend | NOK | anchor only; Flexoprint tender `Spend` computed as price × volume / 1000 |

**Supplier columns (13+, header-detected at runtime):**

- `DetectSupplierBlocks()` reads the header row and builds one block per price column after column 12.
- Supports arbitrary supplier count and revision price columns (e.g. `Grafiket price rev 2 (DKK)` → supplier name `Grafiket Rev2`).
- Supplier name extracted from price column header via regex (`ExtractSupplierNameFromPriceHeader`).
- Currency detected from `(DKK)` / `(NOK)` suffix in the price header (default NOK if absent).
- Spend, MOQ, and comment columns matched to the base supplier name via `ExtractSupplierFromHeader` on spend/MOQ/comment headers.
- Typical four-supplier layout (offset 0): Norsk Etikett (13–16), Grafiket (17–20), Ettiketto (21–24) — exact indices vary if columns are inserted or reordered.

### Surrogate key

```
ItemNo = "{DSH Site}|{Label format}"
```

Example: `"Jæren|90x219"`

This is stable as long as `DSH Site + Label format` is unique per row — which is the business invariant for this format.

### Currency architecture

**Problem:** Flexoprint and Grafiket prices are in DKK. Norsk Etikett and Ettiketto are in NOK. Comparison requires a single target currency.

**Decision:** All prices converted to `TargetCurrency` at import time via `CurrencyConverter` service.

```csharp
// CurrencyConverter interface
decimal Convert(decimal amount, string fromCurrency, string toCurrency);
```

- Rates stored in `TenderSettings.CurrencyRates` as `Dictionary<string, decimal>` keyed `"DKK:NOK"`, `"NOK:DKK"` etc.
- Inverse rate computed automatically if reverse key not present: `rate("NOK:DKK") = 1 / rate("DKK:NOK")`
- Same currency → pass through unchanged
- Default rates (2026-05-23): DKK→NOK = 1.4403
- Rate overridable in Settings UI per tender

**TargetCurrency options:** DKK, NOK, SEK, EUR

**Flexoprint DKK→NOK conversion for `CurrentContractPrice`:**
```
CurrentContractPrice = FlexoprintPriceDKK × rate("DKK:TargetCurrency")
```

### Consequences

- `LabelLineItem` gains `SurfaceFinish` (string?) field.
- `TenderSettings` gains `TargetCurrency` (string, default "NOK") and `CurrencyRates` (Dictionary).
- `CurrencyConverter` is a new injectable service in Core.
- `TenderPriceAnalyzeImportService` depends on `CurrencyConverter`.
- Supplier price blocks detected dynamically from headers (`DetectSupplierBlocks`); columns 1–12 remain hardcoded anchors.
- `PivotLabelsExcelImportService` retained for legacy "All labels DSH" format — no changes.
- `LabelsExcelImportService` retained for other packaging profiles — no changes.
- Re-import required when `TargetCurrency` changes (no in-memory re-conversion).

---

## ADR-PPWR — PPWR Risk Multiplier — Static Grade Penalty (Option A)

- **Status**: Accepted — static penalty model for POC
- **Context**: Category managers need PPWR recyclability grades reflected in TCO as a deterministic penalty on commercial spend, plus clear market-access risk flags for grades D and E.
- **Decision**:
  - **Formula:** `PpwrRiskPenalty = CommercialSpend × PenaltyRate(grade)`
  - **Penalty rates:**

| Grade | Penalty rate |
|-------|-------------|
| A | 0% |
| B | 0% |
| C | 5% |
| D | 15% |
| E | 25% |

  - `MarketAccessRisk2030` = true when grade is D
  - `MarketAccessRiskNow` = true when grade is E
- **Consequences**: Deterministic, explainable. Does not model time decay — replaceable without changing `TcoResult` shape.