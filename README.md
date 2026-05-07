# 📦 Packaging Tender Decision Engine (PTD-E)

> Packaging tenders are still decided primarily on unit price. That is wrong — and expensive.

PTD-E replaces Excel-based tender evaluation with a structured, audit-ready, and deterministic decision engine. It is built for the demands packaging category managers face today: precise TCO, PPWR compliance, and robust handling of incomplete supplier data.

**Lowest price does not automatically win. Regulatory and technical factors can and should outweigh short-term savings.**

.NET CI/CD

---

## The Problem It Solves

A typical packaging tender evaluation in Excel:

- Compares unit prices — not real total cost of ownership.
- Misses EPR fees and PPWR penalties that materialise 12–24 months after contract.
- Depends on individual expertise that cannot be audited or repeated.
- Provides no traceability from decision back to data.

---

## Business Value

**Total Cost of Ownership — not unit price** The engine automatically includes EPR fees, CO₂ penalties, and logistics surcharges based on actual spend data. It makes the "cheap" supplier visible for what it actually costs.

**What-if simulation in real time** Interactive weight sliders let the user instantly see how supplier rankings shift if sustainability is prioritised over price — or vice versa.

**Deterministic handling of bad data** Incomplete bids do not slip through. Missing data triggers automatic maximum penalty. The buyer is protected from hidden compliance risk.

**Full traceability** Every score can be traced to the line-level data, the applied rules, and the active configuration version. No black boxes.

---

## Scoring Model

Evaluation runs across three dimensions:


| Dimension  | Default Weight | What it captures                          |
| ---------- | -------------- | ----------------------------------------- |
| Commercial | 30%            | Price, TCO, MOQ risk, switching cost      |
| Technical  | 30%            | Specification match, material fit         |
| Regulatory | 40%            | PPWR grade, EPR country fees, circularity |


Weights are configurable per tender and adjustable in real time via the cockpit sliders.

Regulatory carries the highest default weight because PPWR and EPR exposure creates financial risk for both supplier and buyer — not just a compliance checkbox.

---

## How Evaluation Works

1. Supplier data is imported from a structured Excel template.
2. Each line item is validated, cleaned, and normalised.
3. Scoring runs at line level across all three dimensions.
4. Line scores are aggregated to supplier level, weighted by spend.
5. Suppliers are classified: **Recommended**, **Conditional**, or **Manual Review**.
6. The cockpit surfaces rankings, TCO breakdown, and explainability tooltips.

Incomplete or inconsistent data triggers **Manual Review** — it does not automatically exclude a supplier. The buyer decides.

---

## Scenario Analysis — What-If

The engine supports scenario-based evaluation to answer forward-looking questions:

- What happens to rankings if rPET replaces virgin PET at 50%?
- What is the total cost impact if PPWR thresholds tighten next year?
- Which supplier is most exposed to EPR fee increases in Denmark and Sweden?

Each scenario recalculates scores and rankings in real time. Decisions become proactive, not reactive.

---

## Regulatory Coverage

EPR fee calculation covers: **DK, SE, NO, FI, IE**

PPWR grading: A (best) → E (worst), mapped to scoring impact.

High-risk materials (e.g. multi-laminates in high-EPR countries) trigger automatic **High Cost Risk** flags.

---

## Competitive Position


| Capability                  | Traditional e-Sourcing | Analytics Tools | PTD-E |
| --------------------------- | ---------------------- | --------------- | ----- |
| Tender process              | ✔                      | ✖               | ✔     |
| Line-level evaluation       | ✖                      | ✖               | ✔     |
| Packaging-specific logic    | ✖                      | ✖               | ✔     |
| Regulatory (PPWR / EPR)     | Limited                | Partial         | ✔     |
| Real-time scenario analysis | ✖                      | Limited         | ✔     |
| Dynamic price modelling     | ✖                      | ✖               | ✔     |
| Decision traceability       | Basic                  | ✖               | ✔     |


---

## Current Status

- ✅ Labels packaging profile — fully implemented
- ✅ Line-level evaluation with spend-weighted aggregation
- ✅ Configurable scoring (30/30/40 default, slider-adjustable)
- ✅ EPR fee matrix (DK, SE, NO, FI, IE)
- ✅ PPWR grading and risk flagging
- ✅ Blazor cockpit with real-time what-if sliders
- ✅ Full audit trail — every score explained
- 🔲 ERP / BI integration
- 🔲 Additional packaging profiles (trays, cardboard)

---

## For Technical Documentation

See `docs/ARCHITECTURE.md` for system architecture, service design, and development rules.

See `docs/spec.md` for the full specification including scoring formulas, EPR matrix, and domain model.