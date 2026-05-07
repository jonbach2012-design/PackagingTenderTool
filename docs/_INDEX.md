# Documentation Index — PackagingTenderTool

## Rule

One file per purpose. No duplicates. No root-level narrative docs except README.md.

If you need to add documentation:

1. Check this index first.
2. Add to an existing file if the content fits.
3. If a new file is genuinely needed, add it here before creating it.
4. Never create documentation files at repo root (except README.md and decisions.md).

---

## File Map

### Root (repo root)


| File           | Audience                    | Purpose                                               | Language |
| -------------- | --------------------------- | ----------------------------------------------------- | -------- |
| `README.md`    | Business / Category Manager | What the tool is, why it exists, how it creates value | English  |
| `decisions.md` | Developer / AI agent        | Architecture Decision Records (ADRs)                  | English  |
| `.cursorrules` | AI agent (Cursor)           | Project constitution — rules, guardrails, patterns    | English  |


### /docs


| File                    | Audience             | Purpose                                                         | Language       |
| ----------------------- | -------------------- | --------------------------------------------------------------- | -------------- |
| `docs/_INDEX.md`        | Everyone             | This file. Doc ownership map.                                   | English        |
| `docs/ARCHITECTURE.md`  | Developer / AI agent | Technical architecture, layers, named services, patterns        | English        |
| `docs/spec.md`          | Developer / AI agent | Full system specification — canonical technical reference       | English        |
| `docs/plan.md`          | Developer            | Historical baseline plan — context only, not current state      | English        |
| `docs/DEVELOPER_LOG.md` | Developer            | Refactoring journal, root cause analyses, engineering decisions | English/Danish |


### /docs/learning


| File                                | Audience | Purpose                                             | Language       |
| ----------------------------------- | -------- | --------------------------------------------------- | -------------- |
| `docs/learning/SL_LearningGoals.md` | Personal | Education learning goals — uses codebase as sandbox | Danish/English |


---

## Deleted / Retired Files


| File                                   | Reason                                           |
| -------------------------------------- | ------------------------------------------------ |
| `ReadMe.md` (root, old English sprawl) | Replaced by `README.md` + `docs/ARCHITECTURE.md` |


---

## Forbidden Patterns

- Do not create `README2.md`, `README_old.md`, `overview.md` or similar at root.
- Do not put architecture content in `README.md`.
- Do not put business narrative in `docs/ARCHITECTURE.md`.
- Do not put personal learning content outside `docs/learning/`.
- Do not create new ADR files — extend `decisions.md`.
- Do not duplicate content between `spec.md` and `ARCHITECTURE.md`. Spec owns the detail. Architecture owns the summary and orientation.