# Decisions

Design decisions worth explaining, with the reasoning behind them. The product rules themselves live in `brief.md`; this file records *why* the code is shaped the way it is.

Each entry: the question, what was decided, why, what was traded away, and what would make us revisit it.

---

## 1. Creating phases: the manager picks the order

*Date: 2026-09-30 · Area: manager API (application programming interface) · Related: `brief.md` rules 11 and 13*

### Question

How does a phase get created, and who decides its number (`Order`)?

### Decision

`POST /projects/{projectId}/phases` takes only a `Name`. The manager gives the new phase the project's highest `Order` + 1, so it always goes at the end. The first phase gets 1. The endpoint returns 404 for a missing or removed project and 409 for a name the project already uses.

Phases are created by an AI agent through this endpoint. There is no create-phase button.

### Why

- **An agent creates phases, but it still goes through the manager like everything else.** An agent isn't "safer" than a person. The manager is the only program that writes to the database, so that's where the rules are enforced, whoever calls it.
- **The caller can't pick a clashing number.** If callers sent `Order`, they'd have to know which numbers are taken, collisions would return 409, and gaps (1, 2, 7) would be possible. The manager already knows the highest number, so it can't get it wrong. `POST /projects` does the same with `SortOrder`.
- **The request accepts only what the caller may decide.** `ProjectId` comes from the address and `Order` from the manager, so neither is in the request body (overposting protection).

### Trade-offs

- **A new phase can only go at the end.** Putting it in the middle takes a second call to reorder (see entry 3).
- **The endpoint doesn't create the phase's QA task yet.** Rule 11 says every phase has exactly one QA task, but `Task` doesn't exist yet. Once it does, the phase and its QA task will be saved together in one transaction.

### Rejected alternatives

- **Wait for the plan-approval flow before building any create endpoint.** The agent needs an endpoint either way, and adding the QA task later is an extension, not a rewrite.
- **Caller sends `Order`.** Rejected for the collision and gap problems above.

---

## 2. Phase names are unique within a project, enforced twice

*Date: 2026-09-30 · Area: database + manager API*

### Question

Can two phases share a name? And if not, should the manager check it, the database, or both?

### Decision

Names are unique **within a project**: two projects can each have a "Polish" phase, but one project can't have two. The manager checks it first so callers get a clear 409. A unique database index on `(ProjectId, Name)` is the safety net underneath.

### Why

- **Names are how people and agents point at a phase.** "Move Polish before Core flow" or "current phase: Polish" has to mean exactly one phase. Numbers tell phases apart for the computer; names do that for humans. Two phases with the same name would be confusing and wouldn't help anyone.
- **Per project, not global.** Common phase names like "Polish" or "Foundations" naturally repeat across projects, and phases never appear side by side across projects.
- **Both layers, each with its own job.** The index catches writes that skip the check, such as `sqlite3` test inserts, or a future rename or plan-approval endpoint that forgets to check. The manager's check turns a database error (which would be a 500) into a helpful 409 message. It's the same pairing as `(ProjectId, Order)`.

### Why two indexes, not one three-column index

A unique index rejects a row only when **every** listed column matches. `(ProjectId, Order, Name)` would therefore let through both "two phase 3s with different names" and "two 'Polish' phases with different numbers". Two rules need two indexes: `(ProjectId, Order)` and `(ProjectId, Name)`.

### Trade-offs

- **Names are compared exactly,** so "Polish" and "polish" count as different. Decide separately if this becomes a problem.

---

## 3. Reordering phases despite a unique order index

*Date: 2026-09-30 · Area: database + manager API*

### Question

`(ProjectId, Order)` is unique, but I want to be able to reorder phases (me or an agent). Swapping phase 1 and phase 2 briefly makes two phases share an `Order`, and SQLite rejects that immediately. Drop the index, or work around it?

### Decision

Keep the index. `PATCH /projects/{projectId}/phases/reorder` takes every phase Id of the project in the new order and renumbers them in **two passes inside one transaction**:

1. Set every phase to a temporary negative order (−1, −2, −3…) and save. Negatives can't clash with real orders, which are all 1 or more.
2. Set the real orders (1, 2, 3…) and save.
3. Commit.

If anything fails before the commit, the transaction is thrown away and the database rolls back to how it was. Callers never see the negative numbers. The request is rejected with 400 unless it contains each of the project's phases exactly once, the same rule as `PATCH /projects/reorder`.

Analogy: moving furniture between two rooms by first parking everything in the hallway.

### Why

- **Consistent with entry 2:** a rule that only exists in the code is easy to bypass later without noticing.
- **The extra complexity stays inside one endpoint.** Every other write still gets the index's protection for free.

### Trade-offs

- Two saves instead of one, which doesn't matter at this scale (a handful of phases).
- The trick relies on real orders always being positive. `Create` guarantees that: it uses highest + 1, starting from 1.

### Rejected alternatives

- **Drop the `(ProjectId, Order)` index** and trust `Create` and `Reorder` to keep numbers unique. Simpler, but it's the "rule only in code" weakness rejected in entry 2.

### Open questions

- Should reordering be allowed for a phase that has already started or finished? The "current phase" rule (`brief.md` rule 1) depends on order, so moving a finished phase after an unstarted one could make the display confusing.
