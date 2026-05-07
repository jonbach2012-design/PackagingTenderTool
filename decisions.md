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

## ADR-003 — Migration to Blazor

- **Status**: Done
- **Context**: WinForms could not support interactive SVG, real-time sliders, or browser-based stakeholder access. What-if analysis required live DOM updates.
- **Decision**: Blazor is the primary UI direction for the tender cockpit. WinForms retained as a minimal verification shell only — no new development targets it.
- **Consequence**: Enhanced UX (live weight sliders, SVG updates, tooltips). Deterministic web rendering. Stakeholder access without local install.

---

## ADR-004 — Configuration Isolation

- **Status**: Partially implemented — under review
- **Context**: `epr-settings.json` at repo root creates ambiguity between runtime config and source code.
- **Decision**: Move `epr-settings.json` into `/config` folder for clear config boundary.
- **Consequence**: Improved security posture and cleaner project structure.
- **Note**: File is currently still at repo root. Migration to `/config` is pending — see BACKLOG.md BACK-011.

---

## ADR-005 — 80/20 Audit Strategy

- **Status**: Active
- **Context**: Full audit on every change was slowing development without proportional benefit.
- **Decision**: Mandatory audit applies only to the 20% core — formulas, weights, DTO/data contracts, filtering/aggregation. UI cosmetic changes are fast-tracked.
- **Consequence**: Development speed preserved without compromising calculation integrity or frontend contracts.

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