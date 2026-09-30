# Decisions

Design decisions worth explaining, with the reasoning behind them. The product rules themselves live in `brief.md`; this file records *why* the code is shaped the way it is.

Each entry: the question, what was decided, why, what was traded away, and what would make us revisit it.

---

## 1. No create endpoint for phases (yet)

*Date: 2026-09-30 · Area: manager API (application programming interface) · Related: `brief.md` rules 11, 13 and 15*

### Question

The `Phase` entity, its table and `GET /projects/{projectId}/phases` are built. Why isn't there a `POST` to create a phase?

### Decision

Phase creation waits until the `Task` entity exists. It will then be built as **one piece of manager code that creates a phase together with its QA (quality assurance) task**. Any endpoint that creates phases will call that code: the plan-approval endpoint first, and a manual "create phase" endpoint if we ever add one.

Until then, test phases are inserted by hand with `sqlite3`.

### Why

1. **A phase is never valid on its own.** Rule 11 says every phase has exactly one QA task, and a phase can't be Done without it. A `POST /phases` written today could only create a phase with no QA task, because `Task` doesn't exist yet. The endpoint would produce data the rules don't allow.
2. **The real creator creates phases in bulk.** Phases come from the planning agent's plan. When I approve a plan, the manager saves every phase, every task and the baseline (`BaselineFrozenAt`) together. That's an "approve plan" endpoint, not a one-phase-at-a-time `POST`. A single-phase endpoint built now would be the wrong shape and would need rewriting.
3. **Safety comes from the manager, not from who's calling.** An AI agent is not "safer" than a human. Both go through the same API, and the manager is the only program that writes to the database. If the manager enforces the rules, every caller is safe; if it doesn't, none are. So the rule "a phase always comes with its QA task" belongs in shared manager code, not in any one endpoint.
4. **Adding manual creation later is cheap.** The table, the foreign key to `Project` and the unique `(ProjectId, Order)` index already accept phases from any source. A manual create endpoint would be new code only: no migration, no rework.

Analogy: one kitchen, several doors. The plan-approval endpoint and a possible "create phase" button are two doors (dine-in and takeaway), but they share one recipe: phase plus QA task.

### Trade-offs

- **Test data is less realistic.** Rows inserted with `sqlite3` skip the manager's checks. The database's foreign key and unique index still apply, as long as `PRAGMA foreign_keys = ON;` runs first, but any business rule in the manager's C# code does not.
- **No write practice on phases yet.** Accepted, because `Task` will need a real create endpoint anyway: the brief's **+** button opens tasks by hand.

### Revisit when

- **The `Task` entity exists.** Then build "create phase + QA task" as shared manager code.
- **The planning agent exists.** Then build the approve-plan endpoint on top of it.
- **I want users to add phases by hand.** That changes `brief.md` (which currently has no manual phase creation), so update the brief first. Also decide whether a phase added after approval is marked "unplanned", the way tasks are under rule 15.

### Rejected alternatives

- **A plain `POST /phases` now, for future use.** It would break rule 11 and be the wrong shape for plan approval.
- **A temporary `POST` just for testing.** It adds code that will be deleted, and it teaches nothing that the `Task` create endpoint won't.
