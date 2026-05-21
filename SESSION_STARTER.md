# Session starter — PackagingTenderDecisionEngine

Quick context for humans and agents picking up the repo. For deep narrative and architecture lessons, read `docs/DEVELOPER_LOG.md`. For planned work, `docs/BACKLOG.md` is the source of truth (update statuses there when work completes).

---

## 1. Current project status

- **Three-panel layout**: green sidebar (240px) + Label profile filter panel (480px, collapsible) + main content
- **Bar chart dashboard (primary)**: Estimeret spend per leverandør, lodret `RadzenColumnSeries`, two dashed reference lines (best bid baseline red, current contract blue)
- **Bubble chart (secondary)**: CTR vs spend, collapsible under bar chart, bubble size = line count, green color ramp
- **Supplier selector**: checkbox list in sidebar with Alle/Ingen controls, filters all views
- **Multi-select filters**: all 6 dimensions (countries, sites, materials, label sizes, winding, colors) wired and working
- **Score breakdown table** under bar chart
- **CTR info box** on right with definitions and thresholds
- **Spend overview** below score breakdown
- **Deep-dive view**: renamed from Evidence, navigable via bar chart click (pre-filtered to clicked supplier)
- **Gardin toggle** on green sidebar

**Stack (unchanged):** Blazor cockpit + `PackagingTenderTool.Core` (TcoEngine, import, PPWR/cost registry) + xUnit tests + WinForms app in solution. Prefer engine → DTO → Razor; see `docs/spec.md` and `.cursorrules`.

**UI layer (hybrid):** MudBlazor (shell, navigation, dialogs) + Radzen 7.3.0 (charts, data-heavy views) + PTDE CSS (brand). `AlignItems` and `Variant` aliased to MudBlazor in `_Imports.razor` to resolve CS0104 ambiguity.

**Import (pivot format):** `PivotLabelsExcelImportService` reads "All labels DSH" sheet. Column layout: A–J = item fields, K = `current_price`, L–N = Flexoprint (12,13,14), O–Q = Norsk Etikett (15,16,17), R–T = Grafiket (18,19,20), U–W = Ettiketto (21,22,23).

---

## 2. Last 5 commits (newest first)

```
git log -5 --oneline
```

*Run `git log -5 --oneline` anytime to refresh.*

---

## 3. Next priorities

1. **BACK-028**: Deep-dive UI redesign + revision handling (`Rev[N]` convention)
2. **BACK-019**: POC visualization & navigation
3. **BACK-016**: Multi-country regulatory benchmark
4. **BACK-017**: Audit Shield
5. **BACK-002**: Azure deploy

See `docs/BACKLOG.md` for full scored backlog.

---

## 4. Known issues

- Pivot test-fixtures use old column indices (11, 14, 17, 20) — update to (12, 15, 18, 21) when regenerating fixtures
- `decision-evidence-grid` CSS class not yet renamed to `decision-deep-dive-grid` (cosmetic, low priority)
- Logo position drifts after Cursor edits
- Gardin toggle visibility inconsistent
- `RadzenReferenceLine` not available in Radzen 7.x — reference lines implemented via flat `RadzenLineSeries` as workaround

---

## 5. Tech stack reminder

| Layer | Stack |
|-------|--------|
| Runtime | **.NET 10** |
| Web UI | **Blazor**, **MudBlazor** 7, **Radzen** 7.3.0 |
| Charts | **Radzen** `RadzenChart` / `RadzenColumnSeries` / `RadzenLineSeries` |
| Core | **PackagingTenderTool.Core** — models, import, `TcoEngineService`, `TcoResult`, cost registry, PPWR |
| Tests | **xUnit**, **ClosedXML**, **NSubstitute** |
| Data | Excel **.xlsx** via ClosedXML — standard + pivot format ("All labels DSH") |
| Docs | `docs/spec.md`, `docs/BACKLOG.md`, `docs/DEVELOPER_LOG.md`, `docs/decisions/` |

---

## 6. Key data model facts

- `LabelLineItem.CurrentContractPrice` — unit price per 1,000 labels from `current_price` Excel column. Null if absent.
- `BestBidBaseline` — `Dictionary<string, decimal>` (ItemNo → lowest unit price × quantity). Computed in `LabelsTenderEvaluationService` and `LabelTender.razor`.
- `CurrentContractPriceBaseline` — same structure, from `current_price` column. Unit price / 1000 × quantity.
- `ImputedSupplierSpend` — spend for suppliers with incomplete bids: missing lines use `CurrentContractPrice` fallback, then `BestBidBaseline` fallback. Line excluded if neither available.
- Revision convention: `[LeverandørNavn] Rev[N]` (e.g. `Ettiketto Rev2`) — treated as separate suppliers, grouped in sidebar.

---

*End of session starter. Update this file or `docs/BACKLOG.md` when reality drifts.*