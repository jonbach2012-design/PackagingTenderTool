# Session starter — PackagingTenderDecisionEngine

Quick context for humans and agents picking up the repo. For deep narrative and architecture lessons, read `docs/DEVELOPER_LOG.md`. For planned work, `docs/BACKLOG.md` is the source of truth (update statuses there when work completes).

---

## 1. Current project status

- **Three-panel layout**: green sidebar (240px) + Label profile filter panel (480px, collapsible) + main content
- **Bubble chart dashboard**: CTR vs spend, bubble size = line count, green color ramp
- **Multi-select filters**: all 6 dimensions (countries, sites, materials, label sizes, winding, colors) wired and working
- **Score breakdown table** under bubble chart
- **CTR info box** on right with definitions and thresholds
- **Spend overview** below score breakdown
- **Gardin toggle** on green sidebar

**Stack (unchanged):** Blazor cockpit + `PackagingTenderTool.Core` (TcoEngine, import, PPWR/cost registry) + xUnit tests + WinForms app in solution. Prefer engine → DTO → Razor; see `docs/spec.md` and `.cursorrules`.

---

## 2. Last 5 commits (newest first)

```
a60fbc0 docs: add BACK-024 filter drill-down label size specs
0a63a5f feat: multi-select filter infrastructure, sites filter, bubble chart improvements
42c5e09 feat: bubble chart x-axis values, CTR info box, spend overview aligned, score breakdown
60d978c feat: bubble chart dashboard, remove tabs, green bubbles, responsive chart, sidebar scroll
c832979 docs: add BACK-022 generic profile architecture
```

*Run `git log -5 --oneline` anytime to refresh.*

---

## 3. Next priorities

1. Fix bubble chart full-width layout
2. **BACK-002** Azure deploy
3. **BACK-017** Audit Shield
4. **BACK-023** Manual T-score per supplier

See `docs/BACKLOG.md` for full scored backlog (e.g. **BACK-019** POC visualization, **BACK-016** multi-country benchmark, **BACK-024** filter drill-down on label size).

---

## 4. Known issues

- Bubble chart does not fill full available width on large screens
- Logo position drifts after Cursor edits
- Gardin toggle visibility inconsistent

---

## 5. Tech stack reminder

| Layer | Stack |
|-------|--------|
| Runtime | **.NET 10** |
| Web UI | **Blazor**, **MudBlazor** 7 |
| Core | **PackagingTenderTool.Core** — models, import, `TcoEngineService`, `TcoResult`, cost registry, PPWR |
| Tests | **xUnit**, **ClosedXML** |
| Data | Excel **.xlsx** via ClosedXML |
| Docs | `docs/spec.md`, `docs/BACKLOG.md`, `docs/DEVELOPER_LOG.md`, `docs/decisions/` |

---

*End of session starter. Update this file or `docs/BACKLOG.md` when reality drifts.*
