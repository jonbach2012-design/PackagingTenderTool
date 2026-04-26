# Decision Log

## ADR 001 - SRP & Decoupling
- Decision: Moved TCO math to `TcoEngineService.cs`.
- Reason: Separation of concerns. Makes logic testable without UI and protects against rendering side-effects.

## ADR 002 - Deterministic SVG
- Decision: Forced `InvariantCulture` (`.`) via `FmtSvg` logic.
- Reason: Prevents "Empty Bar" syndrome caused by Danish comma-decimals in SVG attributes.

## ADR 004 - 80/20 Audit Strategy
- Decision: The hard “8-principles audit” is mandatory for TCO logic, calculation services, and data contracts (the 20% core). UI tweaks and cosmetic changes (the 80%) are fast-tracked without full audit.
- Reason: Optimizes development time without compromising system integrity and decision validity.

