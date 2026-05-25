# Session starter — PackagingTenderDecisionEngine

Quick context for humans and agents picking up the repo. For deep narrative and architecture lessons, read `docs/DEVELOPER_LOG.md`. For planned work, `docs/BACKLOG.md` is the source of truth (update statuses there when work completes).

---

## 1. Current project status

- **Three-panel layout**: green sidebar (240px) + Label profile filter panel (480px, collapsible) + main content
- **Bar chart dashboard (primary)**: Estimeret spend per leverandør, lodret `RadzenColumnSeries`, two dashed reference lines (best bid baseline red, current contract blue)
- **Dashboard**: Anbefaling card shows recommended supplier, narrative, pillar weight shares (stable/følsom chip, link to audit board)
- **Bubble chart (secondary)**: CTR vs spend, collapsible under bar chart
- **Score breakdown + Spend overview**: collapsible on dashboard (hidden by default)
- **Supplier selector**: checkbox list in sidebar with Alle/Ingen controls; revision suppliers grouped under base (indented, muted)
- **Multi-select filters**: all 6 dimensions wired and working
- **Deep-dive view**: single supplier detail — navigable via bar chart click or sidebar "Leverandør oversigt" link
  - Leverandør-header: badges + 3 KPI-kort (spend, vs best bid, vs nuværende kontrakt)
  - Pris-analyse tabel per linje sorteret efter afvigelse
  - TCO breakdown
  - Revisionsoversigt: line-by-line base vs revision price comparison (collapsible, color-coded % change)
  - Info-besked: sidebar-valg påvirker ikke deep-dive
  - "Tilbage til dashboard" gendanner alle leverandørers selection
- **Leverandør oversigt**: tilgås via sidebar under Leverandører-sektionen
- **Settings**: scoring weights + valuta-selector (NOK/DKK/SEK/EUR) med editerbare kurser
- **Currency**: `CurrencyConverter` service, `TenderSettings.TargetCurrency` + `CurrencyRates`, default NOK
- **Price Matrix**: hidden from navigation — contains synthetic demo data only
- **Price Compare**: wired to filtered line items (respects Label Profile filters)
- **Revision import**: second TenderPriceAnalyze import accumulates — supplier names from headers (e.g. "Grafiket Rev2"). Deduplication on SupplierName+ItemNo prevents doubling.
- **Audit Board**: Pillar score table with progress bars above clarification table

**Stack:** Blazor cockpit + `PackagingTenderTool.Core` + xUnit + WinForms verification shell
**Solution file:** `PackagingTenderTool.sln`

**UI layer (hybrid):**
| Layer | Responsibility |
|-------|----------------|
| MudBlazor | Shell, navigation, dialogs |
| Radzen 7.3.0 | Charts, data-heavy views |
| PTDE CSS | Brand identity |

**Import formats:**
- `TenderPriceAnalyzeImportService` — primær Labels format, detekteret via header-struktur
- `PivotLabelsExcelImportService` — legacy "All labels DSH" format
- `LabelsExcelImportService` — andre packaging profiler

---

## 2. Last 5 commits (newest first)

```
git log -5 --oneline
```

*Run anytime to refresh.*

---

## 3. Next priorities

1. **BACK-017**: Versioned TCO Models — Audit Shield
2. **BACK-002**: Azure deploy
3. **BACK-016**: Multi-country regulatory benchmark (deprioritized — status `idea`, score 7)

---

## 4. Known issues

- `Dense` warning på `MudTextField` i currency rates sektion — cosmetic
- Pivot test-fixtures bruger gamle kolonnenumre (11,14,17,20) → skal til (12,15,18,21)
- `decision-evidence-grid` CSS klasse ikke renamed
- `RadzenReferenceLine` ikke i Radzen 7.x → flat `RadzenLineSeries` workaround
- Dashboard spend: weighted average fix applied in BuildSuppliersFromImportedLines

---

## 5. Tech stack reminder

| Layer | Stack |
|-------|--------|
| Runtime | **.NET 10** |
| Web UI | **Blazor**, **MudBlazor** 7, **Radzen** 7.3.0 |
| Charts | `RadzenChart` / `RadzenColumnSeries` / `RadzenLineSeries` |
| Core | `PackagingTenderTool.Core` — models, import, `TcoEngineService`, cost registry, PPWR |
| Tests | **xUnit**, **ClosedXML**, **NSubstitute** |
| Data | Excel `.xlsx` via ClosedXML |

---

## 6. Key data model facts

- `LabelLineItem.CurrentContractPrice` — unit price per 1,000 labels fra Flexoprint (current supplier)
- `LabelLineItem.SurfaceFinish` — ny felt fra TenderPriceAnalyze format
- `BestBidBaseline` — `Dictionary<string, decimal>` (ItemNo → lowest unit price × quantity)
- `CurrentContractPriceBaseline` — samme struktur, fra current_price kolonne
- Surrogate key: `"{DSH Site}|{Label format}"` bruges som `ItemNo` i TenderPriceAnalyze format
- Revision konvention: `[LeverandørNavn] Rev[N]` behandles som separate leverandører

---

*End of session starter. Update this file or `docs/BACKLOG.md` when reality drifts.*