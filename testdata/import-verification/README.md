# Import recovery — manual verification

Regenerate fixtures (from repo root):

```powershell
dotnet run --project tools/ImportVerificationFixtures/ImportVerificationFixtures.csproj
```

Run the app (HTTP example):

```powershell
dotnet watch run --project src/PackagingTenderTool.Blazor/PackagingTenderTool.Blazor.csproj --urls "http://127.0.0.1:5055"
```

Open `http://127.0.0.1:5055/labeltender`, choose **Labels**.

---

## Fixture reference

| File | Purpose | Expected sidebar/snackbar text (starts with) | Expected session / data | Importing / spinner |
|------|---------|-----------------------------------------------|---------------------------|------------------------|
| `min-valid-labels.xlsx` | Minimal valid Labels tender (one line, Item no + Supplier name + …). | Success line: `Imported … rows • Flags: …` | Tender replaces with **Fixture Supplier** data; filters/KPIs refresh. | Brief spinner while import runs; then file control returns. |
| `bad-missing-required-supplier.xlsx` | **Recognizable** header (has **Item no**) but **no Supplier name** column → validation fails. | **`Import failed:`** + body contains **`Missing required column`** and **`Supplier name`**. | **Snapshot restore**: previous tender unchanged. | Brief spinner; not stuck on **Importing…**. |
| `bad-unrecognizable-labels-header.xlsx` | Valid `.xlsx`, but no header row maps to **Item no** (only Supplier/Site/Qty). | **`Import failed:`** + *recognizable Labels tender header* (or equivalent from `FormatImportFailureMessage`). | **Snapshot restore**. | Brief spinner; not stuck. |
| `not-excel.txt` | Plain text, not a workbook. | **`Import failed:`** + unsupported / could not read `.xlsx` style message. | **Snapshot restore** (after a prior valid import). | Brief spinner; use file picker **All files (*.*)** if `.txt` is hidden by `accept=".xlsx"`. |

**Note:** Earlier builds used `bad-missing-itemno.xlsx` for the unrecognizable-header case. That name was **misleading**; the generator now writes **`bad-unrecognizable-labels-header.xlsx`** and deletes the legacy file when you re-run the tool.

---

## Checklist (manual)

| # | Scenario | Pass criteria |
|---|----------|---------------|
| 1 | **Valid import** | After `min-valid-labels.xlsx`: success status, KPI/table show **Fixture Supplier** (or updated totals), no endless importing. |
| 2 | **Bad recognizable template, missing required column** | After `bad-missing-required-supplier.xlsx`: message contains **`Import failed:`** and **`Missing required column`**; prior tender still shown. |
| 3 | **Unrecognizable Excel template** | After `bad-unrecognizable-labels-header.xlsx`: **`Import failed:`** + header recognition failure; prior tender intact. |
| 4 | **Non-xlsx file** | After `not-excel.txt`: **`Import failed:`**; UI ready; prior tender intact. |
| 5 | **Valid import after failure** | After 2–4, upload `min-valid-labels.xlsx` again: import succeeds; status shows rows imported. |

Suggested order: **1 → 2 → 5**, then **1 → 3 → 5**, then **1 → 4 → 5**.

---

## Absolute paths

Replace with your clone root, e.g.:

`C:\Users\…\PackagingTenderDecisionEngine\testdata\import-verification\`
