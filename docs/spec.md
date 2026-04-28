# PackagingTenderTool Specification

## 1. Purpose

PackagingTenderTool is intended to support structured evaluation of packaging tenders in a way that is more reusable, transparent, and explainable than a spreadsheet-only process.

The solution should help transform tender input data into:

- validated and normalized line data
- structured supplier evaluation
- analytics and summary outputs
- reusable frontend-ready models for later UI presentation

Version 1 focuses on one packaging profile at a time, with **Labels** as the first supported profile.

---

## 2. System Arkitektur & Dataflow

For at sikre fuld gennemsigtighed i beslutningsprocessen følger systemet et lineært dataflow, hvor brugerens strategiske vægtning (Weights) og de faktuelle leverandørdata (TCO) smeltes sammen i beregningsmotoren.

```mermaid
graph TD
    subgraph Input_Layer [Data Input]
        A[Supplier Excel/JSON] --> B[Raw Supplier Data]
        C[User Sliders] --> D[Session Weights %]
    end

    subgraph Processing_Layer [TCO Engine Service]
        B --> E{Calculation Engine}
        D --> E
        E --> F[TCO Math: Spend + Penalties]
        E --> G[Scoring Strategy: Relative to Best]
        E --> H[Explainability: Breakdown Generator]
    end

    subgraph Output_Layer [Blazor Dashboard]
        F --> I[SVG Bar: Financials]
        G --> J[SVG Opacity: Strategic Match]
        H --> K[Interactive Tooltips: Why?]
    end

    I & J & K --> L((Decision Maker))
    
    style E fill:#f9f,stroke:#333,stroke-width:2px
    style L fill:#91a363,stroke:#333,stroke-width:2px
```



Dette flow sikrer, at hver visuel ændring i dashboardet kan spores direkte tilbage til enten en ændring i input-data eller en justering af den strategiske prioritering.

---

## 3. Version 1 Scope

Version 1 includes:

- one tender at a time
- one packaging profile per tender
- Labels as first packaging profile
- line-level evaluation
- supplier-level aggregation
- spend-weighted supplier comparison
- manual review handling
- Excel import for tender input
- validation and cleaning of imported data
- analytics outputs based on imported and cleaned data
- reusable output models for future frontend use

Version 1 does **not** require:

- multiple packaging profiles in the same tender
- final advanced scoring logic for all dimensions
- knockout/exclusion rules
- M3-based supplier identity
- a completed modern frontend

---

## 4. Current Implementation Direction

The current implementation direction is:

- keep WinForms only as a temporary prototype/demo shell
- move core value into reusable architecture
- prepare the system for a future **Blazor frontend with Radzen**
- prioritize business logic, import, analytics, filters, export, and frontend-ready models over GUI cosmetics

This means the specification should not assume that WinForms is the long-term UI target.

---

## 5. Main Use Case

A user should be able to:

1. create or open a tender context
2. import Labels tender data from Excel
3. validate and parse the data
4. identify invalid, missing, or suspicious data
5. normalize the imported values
6. evaluate tender data at line level
7. aggregate results at supplier level
8. calculate analytics and summary outputs
9. expose results in a form that can later be presented in a richer frontend

---

## 6. Core Business Direction

The business direction remains:

- supplier evaluation starts at line level
- line-level results roll up to supplier level
- spend is important in aggregation
- missing or invalid data should trigger Manual Review rather than early automatic exclusion
- scoring should remain explainable
- decision support must be understandable both technically and commercially

---

## 7. Packaging Profile

### 6.1 Version 1 Packaging Profile

Version 1 supports:

- **Labels**

Additional packaging profiles may be introduced later, such as:

- trays
- cardboard
- other packaging formats

### 6.2 Packaging Profile Role

A packaging profile defines:

- relevant input fields
- validation rules
- scoring logic direction
- interpretation of technical and regulatory criteria

---

## 8. Input Data

## 8.1 Input Source

Version 1 uses Excel input as the main source for tender data.

The uploaded tender file should be treated as the primary real-world data reference for the current development direction.

## 8.2 Expected Input Characteristics

The system should support structured tender rows with fields such as:

- item number
- item name
- supplier name
- site / country / business location where relevant
- quantity
- spend
- price / theoretical spend related values
- label size
- material
- reel / roll information where relevant
- color-related fields
- free-text comments where useful

Exact column names may vary and should be validated explicitly by the import layer.

## 8.3 Detail Rows vs Summary Rows

The import process must distinguish between:

- detailed tender rows
- summary or report rows inside the same file

Summary blocks must not be treated as normal evaluation lines unless explicitly used for validation or comparison purposes.

---

## 9. Import and Validation

## 9.1 Import Goals

The import layer should:

- read tender rows from Excel
- validate required columns
- validate field formats and datatypes
- parse rows into raw import models
- report issues clearly
- support a path from raw rows to cleaned domain rows

## 9.2 Import Result Requirements

The import result should support reporting of:

- rows imported
- valid rows
- invalid rows
- skipped rows
- supplier count
- site count
- size count
- material count
- total spend where relevant

## 9.3 Data Quality Handling

Missing or invalid data should:

- trigger Manual Review where appropriate
- be captured as import issues
- not automatically exclude a supplier in version 1 unless a later rule explicitly requires that

## 9.4 Manual Review

Manual Review should be used for:

- missing required values
- invalid values
- uncertain interpretation
- suspicious but non-blocking data patterns

Manual Review is intended as a safety mechanism, not as a final decision on supplier exclusion.

---

## 10. Data Layers

The solution should keep the following data layers distinct.

## 10.1 Raw Import Data

Represents rows as read from the source file with minimal transformation.

Purpose:

- preserve imported structure
- support diagnostics
- isolate parsing concerns

## 10.2 Cleaned / Normalized Domain Data

Represents validated and normalized business data used for evaluation.

Purpose:

- standardize values
- reduce noise from import format differences
- provide consistent input to scoring and analytics

## 10.3 Analytics / Summary Results

Represents aggregated outputs and decision-support metrics.

Purpose:

- support ranking, comparison, and insight generation
- support later export and presentation

## 10.4 Frontend-ready View Models

Represents reusable output structures that later can be bound to a Blazor/Radzen UI.

Purpose:

- avoid coupling analytics directly to WinForms
- support later dashboard, table, and filter views

---

## 11. Normalization Rules

Version 1 should normalize where practical:

- label size values
- material names
- color-related values
- number formats
- spend and monetary fields
- site/country naming where useful

Normalization should be:

- conservative
- explainable
- testable

The system should not aggressively invent interpretations when source data is unclear.

---

## 12. Domain Model Direction

The domain model should support at least the following concepts:

- Tender
- TenderSettings
- PackagingProfile
- LabelLineItem
- Supplier
- LineEvaluation
- SupplierEvaluation
- ScoreBreakdown
- ManualReviewFlag
- TenderEvaluationResult

Supporting or adjacent models may include:

- raw import row models
- cleaned line item models
- import summary / issue models
- analytics summary models
- dashboard / output view models

The exact class design may evolve, but the responsibility boundaries should remain clear.

---

## 13. Evaluation Structure & Strategy

### 12.1 Strategy Pattern Enforcement

Evaluation must be implemented using the **Strategy Pattern**. Each packaging profile (starting with Labels) must provide its own implementation of evaluation logic.

- **Explainability:** Every score must be accompanied by a logic-container that explains the deduction or bonus.
- **GUI Readiness:** Calculation weights must be read from `TenderSettings` to support real-time slider adjustments (1-100) in the frontend.

### 12.2 Manual Review & Robustness

The engine must be resilient to missing data to avoid "all-or-nothing" results:

- If a line lacks critical data (e.g., price or material info), the engine must **not** return a 0 score for the entire dimension.
- Instead, the specific line is flagged with `ManualReviewFlag = True`.
- The supplier's aggregated result is marked with `Status: Conditional`, allowing users to drill down and identify missing data points.

---

## 14. Scoring Logic & Formula

### 13.1 Dynamic Weighting (Slider-Ready)

The total score is a weighted sum of three dimensions: Commercial, Technical, and Regulatory. Weights ($W$) are adjustable via the GUI but must always be normalized.

**Line Score Formula:**
For each line ($i$), a $LineScore$ ($LS$) is calculated:
$$LS_i = (Score_{Comm,i} \cdot W_{Comm}) + (Score_{Tech,i} \cdot W_{Tech}) + (Score_{Reg,i} \cdot W_{Reg})$$

### 13.1.1 Implemented What-If Weight Normalization (Deterministic)

In the current Labels cockpit implementation, weights are treated as **linked sliders** (Commercial/Technical/Regulatory) and are maintained to always sum to **100%** by deterministically distributing the remainder across the two non-primary pillars (proportional to their prior ratio).

Effective invariant:

- $W_{Comm} + W_{Tech} + W_{Reg} = 100$

### 13.2 Spend-Weighted Supplier Aggregation

To determine a supplier's total score ($S_{total}$), each line score is weighted by its relative $Spend$:
$$S_{total} = \frac{\sum_{i=1}^{n} (LS_i \cdot Spend_i)}{\sum_{i=1}^{n} Spend_i}$$

### 13.3 Regulatory Dimension (PPWR & EPR)

Regulatory criteria are weighted highest by default ($W_{Reg} = 40$).

- **PPWR (A-E):** Linear mapping from grade to points:
  - **Grade A:** 100 pts | **Grade B:** 75 pts | **Grade C:** 50 pts | **Grade D:** 25 pts | **Grade E:** 0 pts.
- **EPR (Scandi Focus):** Calculation must validate against country-specific rates for **DK, SE, NO, FI, IE**.
- **Risk Flagging:** High-risk materials (e.g., multi-laminates in high-EPR fee countries) must trigger a "High Cost Risk" flag.

### 13.4 Commercial Dimension

- **Price Benchmarking:** The lowest price in the tender for a specific line item sets the benchmark (100 pts).
- **Relative Scoring:** Other prices are scored relative to the benchmark:
$$Score_{Comm,i} = \left( \frac{Price_{min,i}}{Price_{current,i}} \right) \cdot 100$$

### 13.6 Implemented TCO Formulas (Labels cockpit)

The Labels dashboard uses supplier-level TCO components and a derived price-score.

**Base quantities**

- $V$ = volume (labels), derived from supplier quantity (if $V \le 0$, treated as 0)
- $P$ = price per label

**TCO components**

- **Commercial**: $Commercial = V \cdot P$
- **Regulatory (EPR)**:
  - If PPWR scenario is OFF: $EPR = 0$
  - If PPWR scenario is ON: $EPR = EPRBase(country, weight, V) \cdot GradeFactor$
    - Grade factors used: A=1.0, B=1.3, C=1.8, D=2.4, E=3.0 (fallback defaults to C-like 1.8)
- **Switching**:
  - If supplier is incumbent: $Switching = 0$
  - Else: $Switching = StartupCost + (MonthlySupportCost \cdot 12)$
- **MOQ risk**:
  - $MOQ = Commercial \cdot \frac{MOQPenaltyPct}{100}$

**Total**
$$TCO_{total} = Commercial + EPR + Switching + MOQ$$

**Price score (relative to best total)**
Let $TCO_{min}$ be the minimum $TCO_{total}$ among current suppliers (with a defensive floor of 1 to avoid divide-by-zero).

$$PriceScore = clamp\left(\frac{TCO_{min}}{TCO_{total}} \cdot 100,\ 0,\ 100\right)$$

**Final CTR score (weighted)**
Weights are session-driven ($W_{Comm}, W_{Tech}, W_{Reg}$) and sum to 100:

$$CTR = clamp\left(\frac{(PriceScore \cdot W_{Comm}) + (TechScore \cdot W_{Tech}) + (RegScore \cdot W_{Reg})}{100},\ 0,\ 100\right)$$

**Visual cue mapping**

- Opacity is proportional to CTR match with a lower bound:
  - $Opacity = max(0.25,\ CTR/100)$

### 13.7 Explainability (CalculationBreakdown)

Each supplier row contains a `CalculationBreakdown` string that is intended to answer:

- **What penalty?** (PPWR/EPR, Switching, MOQ)
- **What assumption?** (e.g., zero-volume cases)
- **What weights were active?** (Commercial/Technical/Regulatory % from the session)

The UI surfaces this explainability via native SVG tooltips (`<title>`) on each supplier bar group.

### 13.5 EPR Country Matrix & Fee Calculation

The system must support country-specific EPR fees to calculate the Total Cost of Ownership (TCO) impact.

**Supported Countries:** DK, SE, NO, FI, IE.
**Core Categories:**

- **Labels**
- **Cardboard**
- **Trays**
- **Packaging Mixed**
- **Flexibles**

**Calculation Logic:**
Fees are calculated based on material category and weight:
$$EPR_{cost} = Weight_{kg} \times Rate_{category, country}$$

The `EprFeeService` provides rate lookups. If a rate is missing for a specific country/category combination, the system must trigger a `ManualReviewFlag`.

## 15. Classification Direction

Supplier classification should remain explainable and may include states such as:

- Recommended
- Conditional
- Manual Review

The final thresholds and classification logic are still open for further refinement.

Classification should never hide the reasons behind the outcome.

---

## 16. Analytics Outputs

The system should support analytics such as:

- spend by supplier
- spend by country
- spend by site
- spend by material
- spend by size
- top spend items
- outlier candidates
- consolidation / standardization candidates
- import issue summary
- manual review / flags summary

These outputs are part of the product direction and should not be considered optional decoration.

---

## 17. Planned Data Surfaces / Future Screens

The following output surfaces should be supportable by reusable models:

- Import summary
- Supplier overview
- Country breakdown
- Site breakdown
- Material breakdown
- Item/detail table
- Flags/issues table

These are future-facing UI/data surfaces and should be supported by services and view models even if the current WinForms shell only exposes them partially.

---

## 18. Filtering Direction

A reusable filtering model should support future filtering by:

- supplier
- country
- site
- material
- size
- flagged only
- outliers only

Filtering should be implemented in a way that is reusable by a future Blazor/Radzen frontend.

---

## 19. Export Direction

The solution should support export-ready outputs for:

- cleaned data
- analytics summary
- flags/issues report

CSV is sufficient as an initial practical direction.

Export logic should be reusable and not tightly coupled to the current WinForms shell.

---

## 20. Demo / Synthetic Data

If synthetic suppliers are required for demonstration:

- they may be named `Fiktiv1`, `Fiktiv2`, `Fiktiv3`
- they must be clearly synthetic
- they should be based on realistic transformations of actual imported data patterns
- they should not be random filler disconnected from the real structure

---

## 21. Non-Functional Requirements

The solution should be:

- understandable
- testable
- explainable
- reusable
- extensible for future packaging profiles
- robust enough for inconsistent tender input
- suitable for further UI evolution

The architecture should prioritize:

- separation of concerns
- reusable services
- controlled data flow
- limited UI coupling

---

## 22. Testing Direction

Automated tests should continue to cover:

- domain model behavior
- import and validation
- cleaning and normalization
- evaluation logic
- analytics outputs
- frontend-ready view-model creation

Testing should remain focused on business logic and reusable outputs rather than fragile UI-specific behavior.

---

## 23. Open Decisions

The following remain open:

- detailed price scoring formula
- detailed technical scoring logic
- detailed material scoring logic
- classification thresholds
- knockout / exclusion rules
- plausibility checks for suspicious inputs
- exact supplier master-data identity strategy
- exact future Blazor navigation/layout composition

These should be documented clearly and refined incrementally.

---

## 24. Summary

PackagingTenderTool version 1 is a Labels-focused tender evaluation solution built around:

- one tender at a time
- one packaging profile at a time
- line-level evaluation
- spend-weighted supplier aggregation
- manual review instead of early exclusion
- 30/30/40 scoring direction
- import, validation, cleaning, and analytics
- reusable models for future Blazor + Radzen presentation

The specification should continue to support implementation decisions that strengthen business value, explainability, reuse, and frontend readiness rather than further investment in temporary GUI cosmetics.