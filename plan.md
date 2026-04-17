# Packaging Tender Evaluation Tool - Development Plan

## 1. Goal
Build a first working version of a desktop-based tender evaluation tool for packaging suppliers.

The tool must:
- import supplier data from Excel
- support trays, labels, and cardboard
- apply packaging-specific evaluation profiles
- allow tender-specific weights
- calculate total score from 1-100
- classify suppliers
- automatically exclude suppliers where required
- show results visually, including radar chart

---

## 2. Development Strategy
The solution should be developed in phases, starting with the core business logic before building the full GUI.

Main principle:
1. define the model clearly
2. build the logic
3. build the import
4. build the interface
5. validate and test

---

## 3. Phase 1 - Clarify Core Model
Tasks:
- finalize packaging types for version 1
- define common evaluation groups
- define initial criteria
- identify likely exclusion criteria
- define assumptions for Excel structure
- confirm overall output requirements

Deliverable:
- stable `spec.md`

Status:
- started

---

## 4. Phase 2 - Define Packaging Profiles
Tasks:
- create first profile for Labels
- define expected Excel columns for Labels
- define relevant criteria for Labels
- define score mapping logic for Labels
- define exclusion rules for Labels

Then repeat for:
- Trays
- Cardboard

Deliverable:
- packaging profile definitions for all 3 version 1 types

---

## 5. Phase 3 - Define Scoring Model
Tasks:
- define weight model
- define how total score is calculated
- define classification thresholds
- define how exclusion overrides total score
- define which inputs are automatic vs manual

Deliverable:
- clear scoring and classification logic

---

## 6. Phase 4 - Design Data Model / Classes
Tasks:
- design `Tender`
- design `Supplier`
- design `Criterion`
- design `PackagingProfile`
- design `EvaluationResult`
- design `ScoreCalculator`
- design `ClassificationEngine`
- design `ExcelImporter`

Deliverable:
- agreed class structure for implementation

---

## 7. Phase 5 - Build Core Logic
Tasks:
- implement scoring logic
- implement classification logic
- implement exclusion logic
- test with manual sample data before Excel import

Deliverable:
- working evaluation engine without full GUI dependency

---

## 8. Phase 6 - Build Excel Import
Tasks:
- read Excel input
- validate template structure
- map imported data into supplier objects
- handle mismatched packaging type
- handle missing required columns

Deliverable:
- working Excel import for at least one profile first, then all three

Recommended order:
1. Labels
2. Trays
3. Cardboard

---

## 9. Phase 7 - Build GUI
Suggested GUI sections:
- Start / Main menu
- Create Tender screen
- Weight setup screen
- Import screen
- Supplier review screen
- Results screen

GUI must allow user to:
- create tender
- choose packaging type
- adjust weights
- import Excel
- review supplier data
- run evaluation
- view ranking and charts

Deliverable:
- working desktop interface for non-technical users

---

## 10. Phase 8 - Visual Results
Tasks:
- show total score
- show classification
- show exclusion status
- show score breakdown by criterion
- show radar chart
- show ranking table
- show short recommendation summary

Deliverable:
- understandable results view

---

## 11. Phase 9 - Validation and Testing
Tasks:
- test with valid Excel files
- test with wrong packaging type
- test with missing columns
- test with invalid weights
- test with excluded suppliers
- test classification thresholds
- test UI flow from start to result

Deliverable:
- stable version 1 candidate

---

## 12. Phase 10 - Documentation
Tasks:
- update README
- explain project purpose
- explain structure
- explain business relevance
- explain SDD approach
- explain limitations and future improvements

Deliverable:
- exam-ready project documentation

---

## 13. MVP Definition
A version 1 MVP is complete when:
- one tender can be created
- packaging type can be selected
- Excel can be imported
- multiple suppliers can be evaluated
- weights can be adjusted
- total score is calculated
- classification is shown
- exclusions are applied
- results are displayed clearly

---

## 14. Recommended Implementation Order
1. spec.md
2. label profile
3. scoring logic
4. exclusion logic
5. core classes
6. import logic
7. basic GUI
8. results screen
9. chart
10. testing and cleanup

---

## 15. Risks / Challenges
Known risks:
- too many criteria too early
- unclear boundary between imported data and manual scoring
- packaging-specific logic becoming too complex
- Excel template variations
- overbuilding version 1

Mitigation:
- start with one packaging profile first
- keep version 1 narrow
- separate common logic from packaging-specific logic
- validate Excel strictly
- treat advanced features as later scope

---

## 16. Immediate Next Steps
Next concrete steps:
1. identify common criteria groups
2. define first Labels profile
3. list likely exclusion rules for Labels
4. decide initial scoring approach
5. start class model draft