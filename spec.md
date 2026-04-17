# Packaging Tender Evaluation Tool - Specification

## 1. Purpose
The purpose of this application is to support structured comparison and evaluation of packaging suppliers during a tender process.

The tool shall function as an internal decision-support and recommendation model. It must help the user compare multiple suppliers consistently, calculate a weighted total score, classify suppliers, and identify which suppliers best support Scandi Standard’s packaging strategy.

The application is both:
- an exam project
- a business-relevant prototype

---

## 2. Problem Statement
Supplier evaluation in packaging tenders can become inconsistent if criteria, weighting, exclusion rules, and strategic considerations are handled manually or differently from project to project.

A structured solution is needed to:
- compare suppliers within the same packaging type
- apply tender-specific weighting
- support transparent decision-making
- identify suppliers that fit both current business needs and future packaging strategy
- take into account future requirements related to PPWR and reduced EPR fees

The model must also support that certain conditions automatically exclude a supplier, regardless of the total score.

---

## 3. Business Context
The system is intended for packaging tenders where suppliers are compared within the same packaging category.

Version 1 supports:
- Trays
- Labels
- Cardboard

Suppliers must only be compared within the same packaging type in the same tender.

The Excel files used in the tender process have a locked structure for each packaging type. The columns may differ between packaging types, but for a given packaging type the structure is fixed.

The model should support not only commercial comparison, but also strategic supplier selection in line with Scandi Standard’s packaging direction.

---

## 4. Users
Primary users:
- Procurement
- Category Manager
- Packaging Specialist
- Cross-functional tender team

The tool should be usable by business users without requiring Visual Studio Code.

---

## 5. Scope for Version 1
Version 1 shall support:
- one tender at a time
- one selected packaging type per tender
- comparison of multiple suppliers within the same packaging type
- import of supplier data from Excel
- fixed Excel templates per packaging type
- tender-specific weighting of criteria
- weighted total score from 1-100
- supplier classification
- automatic exclusion based on mandatory criteria
- result overview with ranking
- visual comparison, including radar chart

Version 1 does not need:
- ERP integration
- database backend
- multi-user support
- advanced authentication
- full cloud/web deployment
- full historical tender archive

---

## 6. Packaging Profiles
The system uses packaging-specific evaluation profiles.

Each packaging profile defines:
- packaging type
- expected Excel structure
- relevant evaluation criteria
- default weights
- exclusion rules
- score mapping rules

Version 1 includes the following packaging profiles:
- Labels
- Trays
- Cardboard

This means the system uses one common evaluation engine, but different rules and templates depending on the selected packaging type.

---

## 7. Input Model

### 7.1 User input
The user provides:
- tender name
- packaging type
- criteria weights
- manual ratings where needed
- optional comments

### 7.2 Excel input
The user imports supplier data from Excel.

For each packaging type, the Excel template is fixed in structure and must contain the expected columns for that type.

The imported Excel file may contain:
- direct evaluation fields
- supporting raw data fields
- contextual fields
- fields used for validation
- fields used only for reference

Not all columns are necessarily scored directly.

Some imported fields can be transformed directly into scores.  
Some fields require manual evaluation or interpretation by the user.  
Some fields may trigger automatic exclusion.

### 7.3 Types of imported data
Imported data may include:
- raw technical data
- commercial data
- sustainability / strategic data
- qualification / compliance data

Examples of possible Excel fields:
- SupplierName
- SupplierCountry
- PackagingType
- Price
- MOQ
- LeadTime
- Certifications
- MaterialType
- RecyclabilityData
- CO2Data
- ComplianceIndicators
- Technical specification fields depending on packaging type

---

## 8. Evaluation Criteria
The tool should support evaluation criteria grouped into the following main categories:

### 8.1 Commercial
Examples:
- price competitiveness
- payment terms
- MOQ / batch economics
- total cost impact

### 8.2 Technical Fit
Examples:
- specification fit
- material suitability
- production compatibility
- print / application / sealing / format fit depending on packaging type

### 8.3 Supply & Service
Examples:
- lead time
- delivery reliability
- logistics setup
- support / responsiveness
- flexibility

### 8.4 Compliance
Examples:
- required certifications
- food safety / legal compliance
- traceability
- documentation quality

### 8.5 Sustainability & Strategy
Examples:
- recyclability
- material simplicity
- future readiness for PPWR
- lower expected EPR impact
- sustainability maturity
- innovation capability aligned with Scandi Standard strategy

Some criteria are common across packaging types.  
Some criteria are packaging-specific and defined in the packaging profile.

---

## 9. Scoring Logic
The final evaluation combines:
1. imported objective data
2. user-entered ratings
3. packaging-profile-specific scoring rules
4. tender-specific weights

The model must calculate:
- criterion score
- weighted total score
- classification
- exclusion status

The final total score shall be shown on a scale from 1-100.

Weights must be configurable for each tender.  
The sum of all active weights must equal 100.

The exact scoring scale for each criterion can be finalized later, but version 1 should be designed so that scoring logic can be adjusted without redesigning the whole application.

---

## 10. Exclusion Logic
Certain conditions automatically exclude a supplier from recommendation, regardless of total score.

Examples may include:
- missing mandatory certification
- technical incompatibility with packaging requirements
- wrong packaging type
- failure to meet critical compliance requirements
- failure on minimum threshold for a mandatory criterion
- inability to support required strategic or regulatory direction

Excluded suppliers may still be visible in the result overview, but they must be clearly marked as excluded.

---

## 11. Strategic Alignment
The model must not only compare suppliers on current commercial and technical fit.

It must also support strategic supplier selection by rewarding suppliers that align with Scandi Standard’s packaging direction, including:
- improved recyclability
- reduced material complexity
- future readiness for PPWR
- lower expected EPR fee exposure
- stronger sustainability profile
- stronger traceability and compliance readiness

This is important because the model is intended as a decision-support tool, not only a price comparison tool.

---

## 12. Output / Results
The application must produce:
- supplier name
- packaging type
- score per criterion
- weighted total score from 1-100
- classification
- exclusion flag if applicable
- recommendation summary
- supplier ranking
- visual comparison, including radar chart

Possible classifications:
- Recommended
- Conditional
- Not Recommended
- Excluded

The output should be understandable for non-technical users.

---

## 13. Business Rules
The system must follow these rules:

### 13.1 Packaging type rule
Only suppliers within the same packaging type may be compared in the same tender.

### 13.2 Weighting rule
Criteria weights must be configurable per tender.  
The sum of weights must equal 100.

### 13.3 Excel structure rule
Excel files are fixed in structure for each packaging type.  
The system must validate that the imported file matches the selected packaging profile.

### 13.4 Exclusion rule
Mandatory exclusion criteria override total score.

### 13.5 Strategic rule
The model should support supplier selection in line with packaging strategy, not only lowest short-term cost.

---

## 14. Data Model / Classes
Expected core classes in version 1:

### `Tender`
Contains:
- tender name
- packaging type
- selected criteria
- criteria weights
- supplier list

### `Supplier`
Contains:
- supplier name
- country
- packaging type
- imported values
- manual scores
- comments

### `Criterion`
Contains:
- criterion name
- description
- weight
- minimum threshold
- isMandatory
- category group

### `PackagingProfile`
Contains:
- packaging type
- expected Excel columns
- relevant criteria
- default weights
- exclusion rules
- score mapping rules

### `EvaluationResult`
Contains:
- supplier
- criterion scores
- weighted total score
- classification
- excluded flag
- explanation summary

### `ScoreCalculator`
Responsible for:
- calculating weighted score
- transforming criterion values into total score

### `ClassificationEngine`
Responsible for:
- assigning Recommended / Conditional / Not Recommended / Excluded

### `ExcelImporter`
Responsible for:
- reading supplier data from Excel
- validating template structure
- mapping imported columns to system fields

---

## 15. User Flow

### Step 1
Create a new tender

### Step 2
Enter tender name

### Step 3
Select packaging type

### Step 4
Load matching packaging profile and criteria

### Step 5
Adjust tender-specific weights

### Step 6
Import supplier Excel file

### Step 7
Review imported suppliers and complete manual scoring where required

### Step 8
Run evaluation

### Step 9
View ranking, classifications, exclusions, total scores, and radar chart

---

## 16. Validation Rules
The system must validate:
- tender name is entered
- packaging type is selected
- imported Excel file matches expected structure
- imported rows match selected packaging type
- required fields are present
- weights sum to 100
- mandatory criteria are evaluated
- excluded suppliers are clearly marked

---

## 17. Non-Functional Requirements
The application should:
- run locally on a normal user machine
- be usable without VS Code
- have a clear GUI
- be understandable for non-technical users
- be modular and maintainable
- support future extension without major redesign

---

## 18. Future Improvements
Possible future improvements:
- save/load tenders
- export results to Excel or PDF
- database support
- audit trail / change history
- Power BI integration
- AI-assisted explanation of recommendation
- additional packaging categories
- more advanced scenario analysis
- cloud deployment

---

## 19. Open Questions / Assumptions

### Current assumptions
- version 1 compares suppliers only within one packaging type at a time
- version 1 supports trays, labels, and cardboard
- Excel template is fixed and controlled externally
- some scores are imported, while others are entered manually
- exclusion rules are defined per packaging profile
- the tool supports decision-making, but does not replace human judgment

### Open questions
- which criteria are common across all packaging types?
- which criteria are packaging-specific for trays, labels, and cardboard?
- which exact exclusion rules must be active in version 1?
- which fields should be scored automatically vs manually?
- what scoring scale should be used per criterion?