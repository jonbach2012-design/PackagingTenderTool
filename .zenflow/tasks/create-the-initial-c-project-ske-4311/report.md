# GUI Update

## Implementation Summary

PackagingTenderTool now has a minimal Windows desktop interface for the Labels v1 import and evaluation flow.

The app host was changed from console output to a WinForms executable:

- `src/PackagingTenderTool.App/PackagingTenderTool.App.csproj` now targets `net10.0-windows`
- `UseWindowsForms` is enabled
- `Program.cs` starts `MainForm`
- `MainForm` provides the file picker, evaluation action, supplier result table, and status/error messaging

The GUI reuses the existing core import, evaluation, aggregation, scoring, classification, and Manual Review behavior. It does not redesign the business logic.

## GUI Flow

1. Start the app:

```powershell
dotnet run --project src/PackagingTenderTool.App/PackagingTenderTool.App.csproj
```

2. Click `Browse...` and select a Labels v1 Excel workbook.
3. Click `Import and evaluate`.
4. Review supplier-level results in the table:

- Supplier name
- Total spend
- Commercial score
- Technical score
- Regulatory score
- Total score
- Classification
- Manual review required
- Manual review flag count

The status line reports successful imports, supplier counts, manual-review supplier counts, and import/evaluation errors.

## Supporting Core Change

Added `LabelsTenderEvaluationService` as a small workflow service that runs the existing sequence:

- import Labels Excel data into a `Tender`
- evaluate line items
- aggregate supplier evaluations by supplier name
- apply supplier classification

Added `TenderEvaluationResult` to carry the tender, line evaluations, and supplier evaluations back to the app.

## Verification

Commands run from the repository root:

```powershell
dotnet build PackagingTenderTool.sln
```

Result: passed. Build succeeded with 0 warnings and 0 errors.

```powershell
dotnet test PackagingTenderTool.sln
```

Result: passed. 42 tests passed, 0 failed, 0 skipped.

GUI smoke check:

```powershell
Start-Process src\PackagingTenderTool.App\bin\Debug\net10.0-windows\PackagingTenderTool.App.exe
```

Result: passed. The WinForms app launched successfully and was closed after startup verification.

## Scope Notes

The UI is intentionally minimal and practical.

No advanced charts or visualizations were added.

No tender-rule editing was added.

Manual Review remains non-blocking and is surfaced in the supplier result table.
