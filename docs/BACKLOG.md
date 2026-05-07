# Backlog — PackagingTenderTool

<!-- AUDIENCE: Developer / Architect | OWNER: docs/BACKLOG.md -->
<!-- This is the single source of truth for planned improvements.
     Before starting any new feature or improvement, check here first.
     Before adding a new idea, add it here — not in chat, not in a comment, not in your head. -->

## How to use this

- **Status**: `idea` → `ready` → `in progress` → `done`
- **Score**: Value (1-5) + Priority (1-5) − Effort (1-5 inverted). Max = 10.
- **Rule**: Never start something not listed here. Never finish without updating status.

---

## Active Backlog

### 🟢 Score 10 — Do next

#### [BACK-001] Test results visible on GitHub
- **Status**: `done` — 2026-05-07
- **Category**: CI/CD
- **Value**: 5 | **Priority**: 5 | **Effort**: 2h
- **Score**: 10
- **What**: Add TRX test logger + GitHub test reporter action. Inline test failure details in Actions.
- **Result**: 135 tests passing, visible by name and suite in GitHub Actions summary.

---

### 🟠 Score 9 — High value, implement carefully

#### [BACK-012] PPWR Risk Multiplier — time-bounded market access scoring
- **Status**: `ready`
- **Category**: Architecture / Scoring
- **Value**: 5 | **Priority**: 5 | **Effort**: 1-2 days
- **Score**: 9
- **What**: Enhance regulatory scoring with a PPWR Risk Multiplier that reflects time-bounded market access consequences — not just a static A-E point mapping.
- **Why**: The current model (D=25pts, E=0pts) is a static snapshot. Grade D suppliers face market access restrictions from 2030. Grade E are effectively unviable beyond 2029. A category manager making a 3-year contract decision needs this risk made visible.
- **Business logic**:
  - Grade A (>=95% recyclable): no penalty — future-proof
  - Grade B (>=80%): no penalty — compliant through 2038
  - Grade C (>=70%): soft warning — compliant through 2035, review by 2033
  - Grade D (50-70%): Market Access Risk 2030 flag — phase-out in 4 years
  - Grade E (<50%): Market Access Risk NOW flag — non-viable beyond 2029
- **Scoring approach** (ADR required before implementation):
  - Option A: Static penalty added to regulatory score based on grade (simple, deterministic)
  - Option B: Time-decay multiplier — penalty increases as 2030 deadline approaches (dynamic, powerful)
  - Option C: Hard flag only — no score impact, visible warning in cockpit (conservative)
- **Spec reference**: docs/spec.md section 14.3.3
- **Acceptance criteria**:
  - [ ] ADR written and approved before any code
  - [ ] Grade D triggers Market Access Risk 2030 flag in dashboard
  - [ ] Grade E triggers Market Access Risk NOW flag in dashboard
  - [ ] Score change visible in CalculationBreakdown — fully explainable
  - [ ] Existing golden cases still pass
  - [ ] No changes to TcoEngineService interface — only scoring strategy implementation


---

### 🔵 Score 9 — High value, plan carefully

#### [BACK-002] Deploy to Azure (Blazor cockpit)
- **Status**: `idea`
- **Category**: Infrastructure
- **Value**: 5 | **Priority**: 4 | **Effort**: 1–2 days
- **Score**: 9
- **What**: Auto-deploy Blazor cockpit to Azure App Service on every green push to main.
- **Why**: Makes the tool accessible without running it locally. Stakeholder demo without a laptop.
- **Acceptance criteria**:
  - [ ] Azure App Service created and configured
  - [ ] `dotnet.yml` extended with deploy step
  - [ ] Blazor cockpit accessible via public URL after green push
  - [ ] Secrets stored in GitHub — not in code

---

### 🔵 Score 8 — Next sprint

#### [BACK-003] Code coverage reporting
- **Status**: `idea`
- **Category**: CI/CD
- **Value**: 4 | **Priority**: 4 | **Effort**: 3–4h
- **Score**: 8
- **What**: Coverlet + GitHub Actions summary. Percentage of scoring logic covered by tests.
- **Why**: Required discipline before adding new packaging profiles. Proves the engine is trustworthy.
- **Acceptance criteria**:
  - [ ] Coverage % visible in GitHub Actions summary after each push
  - [ ] Minimum threshold defined (suggest 80% for Core)
  - [ ] No coverage for WinForms shell — excluded explicitly

#### [BACK-004] Trays packaging profile
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 5 | **Priority**: 3 | **Effort**: 2–3 days
- **Score**: 8
- **What**: Second Strategy Pattern implementation after Labels. Trays-specific scoring logic.
- **Why**: Proves the engine is extensible. Labels was profile 1 — trays is the proof it scales.
- **Acceptance criteria**:
  - [ ] New profile implements strategy interface — zero changes to TcoEngineService
  - [ ] ADR written before implementation starts
  - [ ] Tests cover trays golden cases (same 5 as Labels)
  - [ ] spec.md updated with trays profile section

---

### 🟡 Score 7 — Important, not urgent

#### [BACK-005] Scenario visualization in Blazor
- **Status**: `idea`
- **Category**: Frontend
- **Value**: 5 | **Priority**: 3 | **Effort**: 3–5 days
- **Score**: 7
- **What**: Visual what-if comparison — show ranking shift when weights or EPR assumptions change.
- **Why**: The slider logic exists. The visual storytelling doesn't. Highest stakeholder impact feature.
- **Acceptance criteria**:
  - [ ] Side-by-side or animated ranking shift when sliders move
  - [ ] No calculation logic in Razor — all through TcoEngineService
  - [ ] Works with existing session/tender data — no hardcoded demo data

---

### ⚪ Score 6 — Backlog

#### [BACK-006] Automatic semantic versioning
- **Status**: `idea`
- **Category**: CI/CD
- **Value**: 3 | **Priority**: 3 | **Effort**: 4–6h
- **Score**: 6
- **What**: Tag releases automatically on green push to main. Semantic versioning (1.0.0, 1.0.1).
- **Why**: Makes rollback, audit trail, and release history explicit. Required before stakeholder demos.
- **Acceptance criteria**:
  - [ ] Version tag created automatically after green build
  - [ ] Tag visible on GitHub releases page
  - [ ] Version convention documented in decisions.md

#### [BACK-007] ERP / BI integration stub
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 4 | **Priority**: 2 | **Effort**: 1–2 days
- **Score**: 6
- **What**: Export-ready API contract for M3 or Power BI. Interface only — no full integration.
- **Why**: Defines the boundary now so integration is additive later, not a redesign.
- **Acceptance criteria**:
  - [ ] DTO contract defined and documented
  - [ ] CSV export working and tested
  - [ ] ADR written for integration approach

---

### ⚪ Score 5 — Low urgency

#### [BACK-008] Knockout / exclusion rules (v2)
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 4 | **Priority**: 2 | **Effort**: 3–4 days
- **Score**: 5
- **What**: Hard constraints that auto-exclude non-compliant suppliers. ADR required before touching engine.
- **Why**: v1 uses Manual Review as safety net. v2 needs explicit exclusion for PPWR hard failures.
- **Acceptance criteria**:
  - [ ] ADR written and approved before any code
  - [ ] Golden cases updated to cover exclusion scenarios
  - [ ] Manual Review remains non-blocking for soft failures

#### [BACK-009] Fix ADR-003 gap in decisions.md
- **Status**: `ready`
- **Category**: Docs
- **Value**: 2 | **Priority**: 3 | **Effort**: 30min
- **Score**: 5
- **What**: Document or explicitly close the missing ADR-003. Traceability hole in the decision log.
- **Why**: decisions.md goes 001 → 002 → 004. Either ADR-003 was deleted, renamed, or never written.
- **Acceptance criteria**:
  - [ ] ADR-003 either written with correct decision or marked as voided with explanation

#### [BACK-010] Supplier risk scoring (M3 data)
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 5 | **Priority**: 1 | **Effort**: 1–2 weeks
- **Score**: 5
- **What**: Financial health + geographic exposure scoring using M3 supplier master data.
- **Why**: Completes the risk dimension. Blocked on M3 integration maturity.
- **Acceptance criteria**:
  - [ ] M3 integration approach decided (ADR first)
  - [ ] Risk score integrated into TcoEngineService via Strategy Pattern
  - [ ] Does not break existing Labels scoring


#### [BACK-013] Solution og namespace rename til PTD-E
- **Status**: `idea`
- **Category**: Architecture
- **Value**: 3 | **Priority**: 2 | **Effort**: 1 day
- **Score**: 4
- **What**: Rename solution, projects og namespaces fra `PackagingTenderTool` til `PackagingTenderDecisionEngine` eller `PTDE`. GUI titel er allerede korrekt.
- **Why**: "PackagingTenderTool" underdriver produktet. PTD-E kommunikerer et seriøst beslutningssystem.
- **Risk**: Rammer 50+ filer. Kræver fuld build + 135 tests grønne bagefter.
- **Acceptance criteria**:
  - [ ] Alle `.sln` og `.csproj` filer renamed
  - [ ] Alle namespaces opdateret
  - [ ] `dotnet build` — 0 errors
  - [ ] `dotnet test` — 135 tests grønne
  - [ ] CI badge stadig grøn
  - [ ] Docs opdateret
  
---

## Ideas (unscored — not ready for backlog)

| Idea | Notes |
|---|---|
| Plausibility checks for suspicious supplier inputs | Catches artificially low bids |
| Multi-tender comparison view | Compare two tender results side by side |
| PDF export of evaluation report | For stakeholder sign-off |
| Supplier master data via M3 ID | Replaces name-based grouping in v2 |

---

## Done

| Item | Completed | Notes |
|---|---|---|
| Repo moved off OneDrive | 2026-05-07 | Now at C:\Dev — no more phantom file changes |
| Documentation structure aligned | 2026-05-07 | _INDEX.md, ARCHITECTURE.md, single README |
| CI/CD pipeline green | 2026-05-07 | dotnet.yml upgraded to @v5, Release config fixed |
| Mermaid diagrams fixed | 2026-05-07 | spec.md diagram rendering on GitHub |
| .cursorrules doc ownership section | 2026-05-07 | Section 0 prevents future sprawl |