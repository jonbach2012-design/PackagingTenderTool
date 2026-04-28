# PackagingTenderTool Plan

## 1. Objective

The objective of PackagingTenderTool is to create a structured packaging tender evaluation tool that is easier to reuse, explain, and improve than a spreadsheet-only process.

The solution should support a more systematic tender workflow through:
- import of tender data
- validation and data quality handling
- cleaning and normalization
- line-level and supplier-level evaluation
- analytics and decision support
- reusable output models for a future frontend

Version 1 should establish:
- a stable core domain model
- a clear Labels packaging profile
- line-level evaluation
- supplier-level aggregation
- manual review handling
- a usable scoring and ranking workflow
- a reusable architecture that can later support a Blazor frontend with Radzen

---

## 2. Current Status

The repository is now aligned to one local path and one Git repository.

Confirmed repository setup:
- local working path is the PackagingTenderTool repository
- Git is connected and working
- `main` is the active branch
- build is green
- tests are green
- the solution currently contains 56 automated tests

Confirmed implementation status:
- solution and project structure exist
- core domain code exists
- Excel import exists
- validation and import reporting exist
- data cleaning / normalization exist
- analytics services exist
- frontend-ready dashboard / view-model contracts exist
- WinForms prototype exists as a temporary demo shell

The project is therefore no longer in early definition only. It has moved into an implemented prototype stage with a stronger architecture direction.

---

## 3. Confirmed Product Direction

The following decisions are confirmed for version 1:

### 3.1 Scope
- version 1 handles one tender at a time
- version 1 uses one packaging profile per tender
- Labels is the first packaging profile

### 3.2 Evaluation structure
- evaluation starts at line level
- line results are aggregated to supplier level
- supplier aggregation is weighted by Spend

### 3.3 Supplier identity
- supplier grouping is based on Supplier name in version 1
- Supplier ID may be introduced later when M3 integration is more mature

### 3.4 Currency handling
- one currency applies per tender
- currency does not vary by line
- default currency may be configured per tender
- tender setup may allow currencies such as EUR or NOK

### 3.5 Data quality handling
- missing or invalid data should trigger Manual Review
- this applies broadly in version 1 where practical
- missing or invalid data should not automatically exclude a supplier in version 1

### 3.6 Scoring structure
- Commercial: 30%
- Technical: 30%
- Regulatory: 40%

### 3.7 Commercial direction
- price must influence evaluation
- lowest price should generally result in the highest commercial score
- theoretical spend is important but not sufficient on its own

### 3.8 Regulatory direction
Regulatory has the highest weight because PPWR and EPR related issues may create significant risk not only for the supplier, but also for the buying company.

Important focus areas include:
- lower weight
- mono-material design
- easy separation
- reusable or recyclable material direction
- traceability

Regulatory criteria should be able to both:
- increase score
- reduce score

Some regulatory criteria may later become knockout rules, but not in version 1.

---

## 4. Strategic Direction Change

The project is no longer prioritizing further heavy WinForms GUI polishing.

### WinForms role now
WinForms is retained only as:
- a temporary prototype shell
- a quick demonstration surface
- a way to verify that the engine works

### Future frontend direction
The preferred future GUI direction is:
- **Blazor**
- **Radzen**

Reason:
- better fit for data-heavy applications
- stronger component support for dashboards, tables, filters, and drill-down
- less manual GUI fine-tuning than WinForms
- clearer path toward enterprise-style presentation of analytics

This means new development should primarily strengthen:
- Core
- Import
- Analytics
- frontend-ready view models
- filtering
- export-ready outputs

---

## 5. Remaining Open Decisions

The following items are not yet fully defined:

- detailed price scoring method
- detailed material scoring logic
- detailed technical scoring logic
- classification thresholds
- future exclusion / knockout rules
- future plausibility checks for suspicious supplier inputs
- possible use of Supplier ID through M3 integration
- exact future Blazor screen composition and navigation flow

These are important, but they should not block stable continued implementation.

---

## 6. Architecture Direction

The solution should continue to evolve with clear separation of concerns.

### 6.1 Raw import data
Raw data read directly from Excel.

### 6.2 Cleaned / normalized domain data
Validated and normalized data that can safely be used for scoring and analytics.

### 6.3 Analytics / summary results
Aggregated outputs such as spend, breakdowns, outliers, flags, and candidate summaries.

### 6.4 Frontend-ready view models
Reusable output models that can later be consumed by a Blazor/Radzen frontend.

This architecture direction is preferred over expanding WinForms-specific logic.

---

## 7. Implementation Strategy

The recommended implementation direction is now:

1. keep the current specification baseline
2. continue strengthening the core domain and services
3. improve import and validation reliability
4. improve cleaning and normalization
5. expand analytics and summary outputs
6. create and refine frontend-ready view models
7. add reusable filtering models
8. support export of useful outputs
9. keep WinForms only as a minimal verification shell
10. prepare for a future Blazor + Radzen frontend

This keeps architecture ahead of interface and avoids over-investment in the current temporary GUI.

---

## 8. Priority Areas

### High priority
1. **Import pipeline**
   - reliable Excel import
   - required-column validation
   - datatype validation
   - distinction between detail rows and summary rows
   - clear import reporting

2. **Data cleaning / normalization**
   - label size normalization
   - material normalization
   - color field normalization
   - number / spend normalization
   - conservative handling of inconsistent data

3. **Analytics**
   - spend by supplier
   - spend by country
   - spend by site
   - spend by material
   - spend by size
   - top spend items
   - outlier candidates
   - consolidation / standardization candidates
   - flags/issues summary

4. **Frontend-ready view models**
   - reusable output models for later UI consumption

5. **Filtering model**
   Support filters such as:
   - supplier
   - country
   - site
   - material
   - size
   - flagged only
   - outliers only

6. **Export**
   - cleaned data
   - analytics summary
   - flags/issues report

---

## 9. Planned Data Surfaces / Future Screens

The following surfaces should form the basis for a later Blazor/Radzen frontend:

- Import summary
- Supplier overview
- Country breakdown
- Site breakdown
- Material breakdown
- Item/detail table
- Flags/issues table

These should be supported by reusable models and services, not hidden inside WinForms code.

---

## 10. Demo Data Direction

If more suppliers are needed for demonstration purposes, synthetic suppliers may be used.

Naming:
- `Fiktiv1`
- `Fiktiv2`
- `Fiktiv3`

Rules:
- they must be clearly synthetic
- they must be based on realistic transformations of imported tender data
- they should not be random or detached from real data patterns

---

## 11. Working Principles

The project should continue to follow these principles:

- start small and stable
- architecture before interface
- line-level logic before supplier-level summary visuals
- manual review before automatic exclusion
- extensibility for future profiles
- clear business explanation for each scoring dimension
- reusable services before UI-specific implementation
- frontend preparation should not pollute domain logic

This remains important because the tool must be understandable not only technically, but also commercially and operationally.

---

## 12. Immediate Next Step

The next documentation and planning step is:

- update `spec.md` so it matches the current implemented and architectural direction

---

## 12.1 Execution Steps (ADR-driven)

- **Step 1: ADR 001 Compliance** — **DONE (100%)**
- **Step 2: Interactive Sliders** — **DONE (100%)**
- **Step 4: Infrastructure & CI/CD** — **DONE (100%)**

The next implementation focus should continue around:
- import robustness
- cleaning / normalization
- analytics outputs
- frontend-ready models
- filtering
- export readiness

---

## 13. Suggested Commit / Development Sequence

A sensible next sequence is:

1. refine plan and specification alignment
2. strengthen import and validation rules
3. strengthen cleaning and normalization
4. expand analytics outputs
5. introduce or refine filtering model
6. improve export-ready outputs
7. keep WinForms verification minimal
8. prepare future Blazor/Radzen frontend work

This gives a clean history and makes the process easier to explain later.

---

## 14. Summary

PackagingTenderTool has moved from definition into implemented prototype form.

The most important version 1 baseline decisions for Labels remain:
- one tender
- one packaging profile
- line-level scoring
- spend-weighted supplier aggregation
- manual review instead of early exclusion
- 30/30/40 dimension model
- regulatory weighted highest because compliance risk affects both supplier and buyer

At the same time, the project direction has evolved:
- WinForms is now only a temporary prototype shell
- the core value is moving into reusable architecture
- the future frontend direction is Blazor + Radzen
- development should prioritize import, cleaning, analytics, filters, export, and frontend-ready models over GUI cosmetics