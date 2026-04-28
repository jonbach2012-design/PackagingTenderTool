# Decision Log

## ADR 001 - SRP & Decoupling

- Decision: Moved TCO math to `TcoEngineService.cs`.
- Reason: Separation of concerns. Makes logic testable without UI and protects against rendering side-effects.

## ADR 003 - Migration to Blazor

- Context: Need for interactive SVG and real-time What-if analysis.
- Decision: Blazor is the primary UI direction for the tender cockpit experience.
- Consequence: Enhanced UX (live sliders + SVG updates) and deterministic web rendering (stable DOM + tooltips).

## ADR 002 - Deterministic SVG

- Decision: Forced `InvariantCulture` (`.`) via `FmtSvg` logic.
- Reason: Prevents "Empty Bar" syndrome caused by Danish comma-decimals in SVG attributes.

## ADR 004 - Configuration Isolation

- Context: Clean architecture and safer configuration handling.
- Decision: Move `epr-settings.json` into `/config`.
- Consequence: Improved security posture (clear config boundary) and better project structure.

## ADR 005 - 80/20 Audit Strategy

- Decision: The hard “8-principles audit” is mandatory for TCO logic, calculation services, and data contracts (the 20% core). UI tweaks and cosmetic changes (the 80%) are fast-tracked without full audit.
- Reason: Optimizes development time without compromising system integrity and decision validity.

### Audit Triggers (Mandatory)

- Audit required when changing: **formulas**, **weighting**, **DTO/data contracts**, **filtering/aggregation**.

### Golden Cases (Always verify)

1. **Zero Volume**
2. **Missing Data / Grades**
3. **Extreme Scaling**
4. **PPWR Toggles**
5. **Ranking Stability**

### Reversibility (Rollback discipline)

- Core-logic changes must be shipped as **small commits (<200 lines)** to keep rollback safe and fast.