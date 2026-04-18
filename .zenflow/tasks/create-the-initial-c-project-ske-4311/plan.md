# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: beba1749-a242-47e4-b453-8631aaa397a3 -->

Assess the task's difficulty, as underestimating it leads to poor outcomes.
- easy: Straightforward implementation, trivial bug fix or feature
- medium: Moderate complexity, some edge cases or caveats to consider
- hard: Complex logic, many caveats, architectural considerations, or high-risk changes

Create a technical specification for the task that is appropriate for the complexity level:
- Review the existing codebase architecture and identify reusable components.
- Define the implementation approach based on established patterns in the project.
- Identify all source code files that will be created or modified.
- Define any necessary data model, API, or interface changes.
- Describe verification steps using the project's test and lint commands.

Save the output to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach
- Source code structure changes
- Data model / API / interface changes
- Verification approach

If the task is complex enough, create a detailed implementation plan based on `{@artifacts_path}/spec.md`:
- Break down the work into concrete tasks (incrementable, testable milestones)
- Each task should reference relevant contracts and include verification steps
- Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint, write tests for a module). Avoid steps that are too granular (single function).

Important: unit tests must be part of each implementation task, not separate tasks. Each task should implement the code and its tests together, if relevant.

Save to `{@artifacts_path}/plan.md`. If the feature is trivial and doesn't warrant this breakdown, keep the Implementation step below as is.

---

### [x] Step: Create Solution and Project Structure
<!-- chat-id: 40a2b79d-a355-4c2a-b40a-bfdfce48c0ba -->

Implement the runnable skeleton from `{@artifacts_path}/spec.md`.

- Create `PackagingTenderTool.sln`.
- Create `src/PackagingTenderTool.App` as a thin runnable console host.
- Create `src/PackagingTenderTool.Core` as the domain/core class library.
- Create `tests/PackagingTenderTool.Core.Tests` for focused domain tests.
- Add project references so the app and tests reference the core library.
- Prepare folders for `Models`, `Services`, `Import`, and `UI`.
- Verify with `dotnet restore PackagingTenderTool.sln` and `dotnet build PackagingTenderTool.sln`.

### [ ] Step: Add Initial Domain Models

Implement only the initial Labels profile v1 domain contracts described in `{@artifacts_path}/spec.md`.

- Add `Tender`, `TenderSettings`, `PackagingProfile`, `LabelLineItem`, `Supplier`, `LineEvaluation`, `SupplierEvaluation`, `ScoreBreakdown`, and `ManualReviewFlag`.
- Keep currency at tender level with default `EUR`.
- Keep supplier grouping based on supplier name for version 1.
- Keep missing or invalid source data representable for manual review handling.
- Do not implement final thresholds, exclusion rules, advanced plausibility checks, Excel import, or full UI.
- Add focused tests for defaults, relationships, nullable imported values, and manual-review-capable evaluation objects.
- Verify with `dotnet test PackagingTenderTool.sln`.

### [ ] Step: Verify and Report

Run final verification and document the implementation result.

- Run `dotnet restore PackagingTenderTool.sln`.
- Run `dotnet build PackagingTenderTool.sln`.
- Run `dotnet test PackagingTenderTool.sln`.
- Run `dotnet run --project src/PackagingTenderTool.App/PackagingTenderTool.App.csproj`.
- Write `{@artifacts_path}/report.md` describing what was implemented, how it was tested, and any issues or challenges encountered.
