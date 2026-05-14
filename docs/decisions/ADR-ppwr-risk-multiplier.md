# ADR: PPWR Risk Multiplier — Static Grade Penalty (Option A)

## Status

Accepted — static penalty model for POC.

## Context

Category managers need PPWR recyclability grades reflected in TCO as a deterministic penalty on commercial spend, plus clear market-access risk flags for grades D and E.

## Decision

- **Formula:** `PpwrRiskPenalty = CommercialSpend × PenaltyRate(grade)`
- **CommercialSpend:** the same rounded commercial amount used as `TcoResult.Commercial` (DKK spend basis for the line).
- **Penalty rates (by grade):**

| Grade | Penalty rate |
|-------|----------------|
| A     | 0%            |
| B     | 0%            |
| C     | 5%            |
| D     | 15%           |
| E     | 25%           |

- **Total TCO:** `PpwrRiskPenalty` is included in `TcoResult.Total` (`IncludeInTotal: true` in the cost component registry).
- **Market access flags:**
  - `MarketAccessRisk2030` = true when grade is **D**
  - `MarketAccessRiskNow` = true when grade is **E**
- **Breakdown text (for UI / audit):**  
  `Grade {grade} — penalty {penaltyRate:P0} of commercial spend`

## Consequences

- Deterministic, easy to explain in procurement and audit.
- Does not model time decay (contrast Option B in backlog discussion); can be replaced later without changing the `TcoResult` shape.
