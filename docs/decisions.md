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

---

## 4. A unit of work is called a Ticket

*Date: 2026-09-30 · Area: naming (brief, manager, later the UI) · Related: `brief.md` "Data model"*

### Question

The brief called a unit of work a "task". C# already has a built-in `System.Threading.Tasks.Task`, which every `async` controller method returns. Naming the entity `Task` would make the two names clash. What should it be called?

### Decision

Call it **Ticket**, everywhere, not just in C#. The brief and the manager use it now. The frontend (`models.ts`, mock data, UI text) switches when it moves to the real API, because that code is being rewritten anyway.

### Why

- **It's the word IT teams already use.** Jira, helpdesks and bug trackers call a tracked unit of work with a status flow a "ticket".
- **It matches the product.** The app is called Tickie, and a ticket is done once its checklist is ticked.
- **No clash with `System.Threading.Tasks.Task`,** so no full names or aliases in the code.

### Trade-offs

- A rename across the brief and, later, the frontend, and until then the frontend still says "task".

### Rejected alternatives

- **`Task`**: clashes with the built-in type.
- **`TaskItem` / `ProjectTask`**: fixes the clash, but then the code and the product use different words for the same thing.
- **Mission / Work / Job**: "mission" isn't used for software work, "work" is vague and can't be counted, and "job" usually means a background process in back-end code.

---

## 5. Tickets have a description instead of a tagline, plus optional labels

*Date: 2026-09-30 · Area: data model · Related: `brief.md` "Data model → Ticket"*

### Question

The brief gave a ticket a title and a one-line tagline. Is that enough to explain a ticket, and can the tagline also be used to group tickets?

### Decision

- Replace the tagline with a **description**. The title is the short summary, and the description holds the details.
- Add optional **labels** (for example `backend`, `ui`) for grouping and filtering. They get their own table, built after the ticket's plain fields.

### Why

- **A tagline and a title do the same job.** A short title is already a one-line summary, and a tagline leaves no room for details.
- **A tagline can't do the job of labels.** It's free text, one per ticket. Labels need to be shared between tickets, and one ticket can have several, so you can filter by them.

### Trade-offs

- The frontend's `tagline` field and mock data have to change when the UI moves to the real API.
- Labels add a table (and a link table) to the model.

### Rejected alternatives

- **Title + tagline + description**: three text fields where two do the job, and one more field for the planning agent to fill in.

---

## 6. Enums are saved as text, not numbers

*Date: 2026-09-30 · Area: database · Related: `Entities/TicketType.cs`*

### Question

`TicketType` (Design, Build, Debug, QA) is a C# enum, which SQLite doesn't understand. By default EF Core saves an enum as its position number (Design = 0, Build = 1…). Keep that, or save the name?

### Decision

Save the name as text (`"Build"`), using `.HasConversion<string>()` in `OnModelCreating`.

### Why

- **Adding a value can't break old rows.** With numbers, inserting a new value in the middle (say `Research` after `Design`) shifts every later number. A Build ticket saved as `1` would silently turn into Research, with no error.
- **Readable in the database.** Checking rows by hand with `sqlite3` shows `Build`, not `1`.

### Trade-offs

- A few extra bytes per row, which doesn't matter at this scale.
- Renaming an enum value now breaks old rows instead, since they hold the old name. That's much rarer than adding a value, and it's easy to spot.

### Rejected alternatives

- **Numbers, pinned in code** (`Build = 1`): just as safe, but only while everyone remembers never to change a pinned number, and rows are still unreadable by hand.

---

## 7. Ticket order is unique within a phase

*Date: 2026-09-30 · Area: database · Related: `brief.md` rules 1 and 2*

### Question

Tickets are shown by planned start time. `Order` only breaks ties between tickets that start at the same time. Does it still need to be unique within a phase?

### Decision

Yes. A unique index on `(PhaseId, Order)`, the same shape as Phase's `(ProjectId, Order)`.

### Why

- **A tie-breaker has to break the tie.** If two tickets shared both start time and order, the database could return them in any order. The list could flip between reloads, and the "current ticket" on the home page could change even though nothing happened.
- Like a photo finish: two runners can finish in the same second, but they never share a place.

### Trade-offs

- Reordering tickets will need the same two-pass trick as phases (entry 3).

---

## 8. Hand-opened tickets have no planned start or estimate

*Date: 2026-09-30 · Area: data model · Related: `brief.md` rules 2 and 13*

### Question

The planning agent estimates each ticket's planned start and hours. A ticket I open by hand (the **+** under a phase) has had no estimate. Must I fill them in?

### Decision

No. `PlannedStart` (`DateTime?`) and `EstimatedHours` (`double?`) can be empty. In a phase's list, tickets with no planned start go last.

### Why

- **Nobody estimated it, so any number would be fake.** A made-up estimate would also mislead the planning agent later, when it calibrates against real times.
- **The `?` matters.** Without it, C# fills in defaults: 0 hours and 1 January 0001. That date sorts first, so an unestimated ticket would jump to the top of the list, the opposite of what's wanted.

### Trade-offs

- Every query that sorts by planned start must place empty values last on purpose.

---

## 9. The assigned agent is a string, checked against the tools found on the machine

*Date: 2026-09-30 · Area: data model · Related: `brief.md` "Data model → Ticket"*

### Question

A ticket's assigned agent is the AI tool that does the work (Claude Code, Codex…). Should it be an enum, like `TicketType`, or a string?

### Decision

A `string`. The manager finds out which agent tools are installed (by running command-line checks) and only accepts a name from that list.

### Why

- **The list comes from the machine, not from the code.** Which tools are installed changes over time and differs between Macs. An enum would freeze the list at compile time.
- **The typo risk is handled by checking, not by the type.** The manager rejects any name that isn't in the list of detected tools, so `"claude code"` vs `"Claude Code"` can't slip in.

### Trade-offs

- A typo is caught when the request arrives, not when the code compiles.
- The manager still needs to know how to launch each tool. Unless launching can be made generic (a command per tool, kept as data), adding a new tool still needs a code change.

### Rejected alternatives

- **Enum**: the compiler catches typos, but the list would be fixed in code and couldn't reflect what's actually installed.

---

## 10. One status enum for every ticket type, starting at Todo

*Date: 2026-10-01 · Area: data model · Related: `brief.md` "Ticket status flows"*

### Question

Build/Debug, Design and QA tickets each have their own set of statuses, but a ticket has only one `Status` column. One enum or one per type? And must whoever creates a ticket set its status?

### Decision

- One `TicketStatus` enum with all 16 statuses. Whether a status fits a ticket's type is a rule the manager checks.
- `Status` defaults to `Todo` instead of being `required`. Saved as text, like `TicketType` (entry 6).

### Why

- **One column can hold only one C# type.** Three enums would need three columns or awkward conversions.
- **Same pattern as the assigned agent (entry 9):** the type allows it, the manager checks it.
- **Every ticket type starts at Todo,** including Debug tickets proposed by the IT supervisor: they follow the Build flow (rule 12), so their draft test cases still go through Test cases for review. A default means callers can't start a ticket in the wrong place.

### Trade-offs

- The compiler doesn't stop a Design ticket from being set to `Fixing`; the manager has to.

### Rejected alternatives

- **One enum per type**: each is cleaner on its own, but they don't fit in one column.
- **`required` status**: every caller would have to write `Todo` by hand, and could get it wrong.

---

## 11. "Unplanned" is worked out from the ticket's creation time

*Date: 2026-10-01 · Area: data model · Related: `brief.md` rule 15*

### Question

A ticket added after the plan was approved is marked "unplanned". Store an `IsUnplanned` flag, or work it out?

### Decision

Work it out. Ticket gets a `CreatedAt` (set automatically, in UTC). A ticket is unplanned when its `CreatedAt` is later than its project's `BaselineFrozenAt`.

### Why

- **Derived data is never stored separately** (the brief's core principle). A stored flag could disagree with the two times it's based on.
- **The baseline never moves.** The plan can change later, but `BaselineFrozenAt` is set once at approval (rule 15), so the comparison always gives the same answer.
- `CreatedAt` is useful on its own too, for example for the history of a phase.

### Trade-offs

- Reading "unplanned" needs the project's `BaselineFrozenAt`, which means going Ticket → Phase → Project.

---

## 12. No source ticket on Debug tickets (for now)

*Date: 2026-10-01 · Area: data model*

### Question

The brief gave Debug tickets a "source ticket" pointing to where the problem was found (usually the phase's QA ticket). Keep it?

### Decision

Drop it.

### Why

- **Nothing uses it.** No rule in the status flow reads it. QA goes back to Waiting for dev when *any* ticket is added to the phase, not because of this link.
- **It doesn't always have a value.** A Debug ticket I open by hand for a bug I found myself has no source ticket.
- Build what a feature needs, when it needs it.

### Trade-offs

- A Debug ticket doesn't record which ticket it came from, so "how many bugs did this ticket produce" can't be counted yet.

### Revisit when

- Tracing is needed. Then links between tickets (possibly several per ticket, e.g. "referenced tickets") would get their own link table, like the dependency table, since one column can't hold a list of Ids.
