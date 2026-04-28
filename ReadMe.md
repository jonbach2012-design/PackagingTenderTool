# PackagingTenderTool

## Overview

PackagingTenderTool is a configurable, line-level evaluation engine for packaging tenders.

The solution replaces manual, Excel-based evaluations with a structured, traceable and reusable decision model, where supplier selection is based on price, technical fit and regulatory impact.

---

## Business Context

Packaging tenders involve:

- multiple suppliers  
- multiple specifications and sites  
- varying data quality  
- increasing regulatory pressure (e.g. PPWR and EPR)

Traditional Excel-based evaluation leads to:

- inconsistent decisions  
- limited transparency  
- dependency on individual knowledge  
- weak handling of long-term cost drivers

Regulatory factors such as recyclability, material composition and EPR fees are becoming direct economic drivers, not just compliance requirements.

---

## Solution

The engine operates as a structured evaluation pipeline:

### Input

- standardized Excel templates for supplier submissions

### Processing

- validation of structure and data  
- cleaning and normalization  
- line-level evaluation  
- aggregation to supplier level (spend-weighted)

### Output

- comparable supplier scores  
- structured decision support  
- frontend-ready data models  
- traceable evaluation results

---

## Scoring Model

Evaluation is based on three dimensions:

- **Commercial** – price and total cost  
- **Technical** – match against specifications  
- **Regulatory** – compliance and future cost/risk (PPWR, EPR)

### Weighting (example)

- Commercial: 30%  
- Technical: 30%  
- Regulatory: 40%

Weighting is configurable per tender and packaging type.

### Key Principle

Lowest price does not automatically win.  
Regulatory and technical factors can outweigh short-term savings.

---

## Evaluation Logic

- evaluation starts at line level  
- results are aggregated to supplier level  
- aggregation is spend-weighted  
- incomplete or inconsistent data triggers **Manual Review**

---

## Scenario Builder (What-if Analysis)

The engine supports scenario-based evaluation.

### Scenarios simulate:

- material composition (e.g. virgin vs rPET)  
- regulatory assumptions (EPR fees, PPWR thresholds)  
- scoring weights  
- supplier inputs

### Example

Replacing virgin PET with 50% rPET:

- recalculates line-level scores  
- updates supplier rankings  
- shows total cost impact

### Value

- future-aware decision support  
- proactive decision-making  
- clearer trade-offs

---

## Traceability and Versioned Configuration

Each evaluation uses a specific configuration profile and version:

- scoring model  
- criteria  
- regulatory assumptions  
- review rules

### Enables:

- traceability (result → config → input)  
- comparison across tenders  
- explanation of score differences

Each result can be traced to:

- line-level data  
- applied rules  
- configuration version  
- scenario assumptions

---

## Architecture

### Layers

**1. Raw Import Layer**

- Excel input  
- structural validation

**2. Normalized Domain Layer**

- cleaned and standardized data

**3. Evaluation & Analytics Layer**

- scoring  
- aggregation  
- analytics

**4. Scenario Layer**

- applies alternative assumptions  
- recalculates results

**5. View Models**

- frontend-ready structures

### Principles

- separation of concerns  
- UI-independent core  
- reusable services  
- extensibility

---

## Strategy-Based Evaluation Extensions

- **IndexationStrategy** – dynamic price modelling  
- **RiskStrategy** – supplier dependency and risk  
- **CircularityStrategy** – PPWR/EPR scoring

---

## Advanced Evaluation Libraries

### Dynamic Indexation Library

- decomposes price into components  
- links to indices (PIX, ICIS, Platts)  
- simulates price development

### Supplier Risk & Resilience Library

- financial health  
- geographic exposure  
- spend share

### Design for Circularity Maturity Index

- material composition  
- adhesives  
- color detectability  
- separability

---

## Strategic Extension: Requirement-Based Substitution Logic

Future capability:

- target requirements  
- acceptable ranges  
- penalty logic  
- hard constraints

### Enables:

- alternative material evaluation  
- better trade-off decisions

---

## Soft Factors and Governance

Handled as:

- separate overlays or flags  
- documented assessments  
- traceable inputs

---

## Frontend Strategy

### Current

- WinForms (verification only)

### Future

- Blazor frontend  
- component-based UI (Radzen)

---

## Competitive Positioning


| Capability               | Traditional e-Sourcing | Analytics Tools | PackagingTenderTool |
| ------------------------ | ---------------------- | --------------- | ------------------- |
| Tender process           | ✔                      | ✖               | ✔                   |
| Spend analytics          | Limited                | ✔               | ✔                   |
| Line-level evaluation    | ✖                      | ✖               | ✔                   |
| Packaging-specific logic | ✖                      | ✖               | ✔                   |
| Regulatory (PPWR/EPR)    | Limited                | Partial         | ✔                   |
| Scenario analysis        | ✖                      | Limited         | ✔                   |
| Dynamic price modelling  | ✖                      | ✖               | ✔                   |
| Supplier risk modelling  | ✖                      | Limited         | ✔                   |
| Circularity scoring      | ✖                      | Partial         | ✔                   |
| Decision model           | Basic                  | ✖               | ✔                   |


---

## Development Approach

- Spec-Driven Development (SDD)  
- Zenflow for planning and iteration  
- iterative refinement

---

## Status

Implemented prototype with:

- line-level evaluation  
- configurable scoring  
- scenario capability  
- extensible architecture

---

## Next Steps

- Blazor frontend  
- scenario visualization  
- filtering  
- integration (ERP / BI)

---

## Summary

PackagingTenderTool is a **decision engine**.

From:

- manual Excel evaluation

To:

- structured, traceable and future-aware decision-making