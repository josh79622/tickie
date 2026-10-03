# Decisions

Design decisions worth explaining, and why we made them. The product rules live in `brief.md`. This file explains *why* the code looks the way it does.

Each entry has: the question, what we decided, why, the downside, and the other options we said no to.

## Where I questioned the design myself

I built this with an AI tutor, but I didn't just accept what it suggested. These are the places where my own question or idea changed the design:

- **Entry 5:** I wanted a real description on each ticket, not just a one-line tagline.
- **Entry 12:** I asked "why do we need this field?" about the source ticket. Nothing used it, so we removed it.
- **Entry 14:** I didn't want the default agent in `appsettings.json`. It's a user choice, so it belongs in the database.
- **Entry 15:** I pointed out that the "existing projects" we were worrying about were only test rows. That changed the question to "what does a brand-new install need?"
- **Entry 16:** I came up with the **Manual** option, so work can go on when I'm out of AI quota.
- **Entry 18:** I noticed the QA ticket would get `Order = 1` even though QA runs last. Following that up showed `Order` didn't do anything useful, so we removed it.

In one sentence: *"I used AI to move faster, but I kept asking why each piece existed, and several fields and rules changed or disappeared because of that."*

---

## 1. Creating a phase: the manager picks its number

*Date: 2026-09-30 · Area: manager API (Application Programming Interface) · Related: `brief.md` rules 11 and 13*

### Question

How is a phase created, and who decides its number (`Order`)?

### Decision

`POST /projects/{projectId}/phases` takes only a `Name`. The manager gives the new phase the highest `Order` in the project + 1, so it always goes at the end. The first phase gets 1.

- 404 if the project doesn't exist or was removed.
- 409 if the project already has a phase with that name.

An AI agent creates phases through this endpoint. There is no "create phase" button.

### Why

- **The agent still goes through the manager.** An agent isn't "safer" than a person. The manager is the only program that writes to the database, so the rules are checked there, no matter who calls.
- **The caller can't pick a bad number.** If the caller sent `Order`, it would need to know which numbers are taken. Two callers could pick the same number (409), or leave gaps (1, 2, 7). The manager already knows the highest number, so it can't get it wrong. `POST /projects` does the same with `SortOrder`.
- **The request only has what the caller may decide.** `ProjectId` comes from the address and `Order` comes from the manager, so neither is in the request body. This is called overposting protection: the caller can't sneak in values it shouldn't set.

### Downside

- **A new phase always goes at the end.** To put it in the middle, you need a second call to reorder (entry 3).
- **The QA ticket isn't created yet.** Rule 11 says every phase has one QA ticket. That's added later, in the same transaction (entry 13).

### Other options I said no to

- **Wait for the plan-approval flow before building any create endpoint.** The agent needs this endpoint anyway, and adding the QA ticket later is a small addition, not a rewrite.
- **The caller sends `Order`.** No, because of the clashes and gaps above.

---

## 2. Phase names are unique inside a project, checked twice

*Date: 2026-09-30 · Area: database + manager API*

### Question

Can two phases have the same name? If not, who checks: the manager, the database, or both?

### Decision

Names are unique **inside one project**. Two projects can each have a "Polish" phase, but one project can't have two.

Checked twice:

1. The manager checks first, so the caller gets a clear 409 message.
2. A unique database index on `(ProjectId, Name)` is the safety net.

### Why

- **Names are how people and agents talk about phases.** "Move Polish before Core flow" must mean exactly one phase. Numbers are for the computer; names are for people. Two phases with the same name would only confuse.
- **Per project, not across all projects.** Names like "Polish" are common and repeat across projects. That's fine, because you never see phases from different projects side by side.
- **Each check has its own job.**
  - The **index** catches writes that skip the manager's check, like rows I add by hand with `sqlite3`, or a future endpoint that forgets to check.
  - The **manager's check** turns a database error (which would be an unhelpful 500) into a clear 409 message.
  - Same pairing as `(ProjectId, Order)`.

### Why two indexes, not one with three columns

A unique index only blocks a row when **all** its columns match. One index on `(ProjectId, Order, Name)` would still allow:

- two "phase 3"s with different names, and
- two "Polish" phases with different numbers.

Two rules, so two indexes: `(ProjectId, Order)` and `(ProjectId, Name)`.

### Downside

- **Capital letters count.** "Polish" and "polish" are different names. Fix it later if it becomes a problem.

---

## 3. Reordering phases even though order must be unique

*Date: 2026-09-30 · Area: database + manager API*

### Question

`(ProjectId, Order)` must be unique, but I want to reorder phases (me or an agent). To swap phase 1 and phase 2, for a moment two phases have the same number, and SQLite rejects it right away. Remove the index, or find a way around it?

### Decision

Keep the index. `PATCH /projects/{projectId}/phases/reorder` takes every phase Id of the project, in the new order. The manager renumbers them in **two steps, inside one transaction**:

1. Give every phase a temporary negative number (−1, −2, −3…) and save. Negative numbers can't clash with real ones, which are always 1 or more.
2. Give them the real numbers (1, 2, 3…) and save.
3. Commit (make it final).

If anything fails before step 3, everything is undone, and the database goes back to how it was. Callers never see the negative numbers.

The request gets 400 unless it lists every phase of the project exactly once (same rule as `PATCH /projects/reorder`).

Like moving furniture between two rooms: first put everything in the hallway, then move it into place.

### Why

- **Same reason as entry 2:** a rule that only lives in the code is easy to break later without noticing.
- **The extra work stays in one endpoint.** Every other write still gets the index's protection for free.

### Downside

- Two saves instead of one. That doesn't matter with only a few phases.
- The trick only works if real numbers are always positive. `Create` makes sure of that: it uses highest + 1, starting at 1.

### Other options I said no to

- **Remove the `(ProjectId, Order)` index** and trust the code to keep numbers unique. Simpler, but that's the "rule only in code" problem from entry 2.

### Still open

- Should I be able to reorder a phase that has already started or finished? The "current phase" on the home page depends on order (`brief.md` rule 1), so moving a finished phase after an unstarted one could look confusing.

---

## 4. A unit of work is called a Ticket

*Date: 2026-09-30 · Area: naming · Related: `brief.md` "Data model"*

### Question

The brief called a unit of work a "task". But C# already has a type called `Task`. Every `async` controller method uses it (`async Task<ActionResult>`). If our entity is also called `Task`, the two names clash. What name do we use?

### Decision

**Ticket**, everywhere: the brief, the C# code, and later the UI.

The brief and C# use it now. The frontend changes later, when it moves to the real API, because that code gets rewritten anyway.

### Why

- **IT people already say "ticket".** Jira, helpdesks and bug trackers all call a piece of work with a status a "ticket".
- **It fits the app.** The app is called Tickie, and a ticket is done when its checklist is ticked.
- **No clash with C#'s `Task`.** We don't need long names like `System.Threading.Tasks.Task` in the code.

### Downside

- Until the frontend is updated, the frontend says "task" and the backend says "ticket".

### Other options I said no to

- **`Task`**: clashes with C#.
- **`TaskItem` / `ProjectTask`**: no clash, but then the code and the app use different words for the same thing.
- **Mission / Work / Job**: nobody says "mission" for software work. "Work" is too vague and you can't count it ("one work"?). "Job" usually means a background process in backend code, which is confusing because our agents run in the background.

---

## 5. Tickets have a description, not a tagline, plus optional labels

*Date: 2026-09-30 · Area: data model · Related: `brief.md` "Data model → Ticket"*

### Question

The brief gave a ticket a title and a tagline (a one-line summary). Is that enough? And can the tagline be used to group tickets?

### Decision

- Remove the tagline. Add a **description** instead. It is required.
- Add optional **labels** (like `backend` or `ui`) to group and filter tickets. Labels get their own table, built later.

### Why

- **A title and a tagline do the same job.** A short title is already a one-line summary. A tagline is too short for real details.
- **Every ticket needs details.** The coding agent needs something to work from.
- **A tagline can't work as labels.** A tagline is one piece of free text per ticket. Labels are shared: one ticket can have many labels, and one label is used by many tickets. That needs its own table.

### Downside

- The frontend's `tagline` has to change later.
- Labels add more tables.

### Other options I said no to

- **Title + tagline + description**: three text fields where two are enough, and one more thing for the planning agent to fill in.

---

## 6. Enums are saved as text, not numbers

*Date: 2026-09-30 · Area: database · Related: `Entities/TicketType.cs`*

### Question

`TicketType` is an enum: Design, Build, Debug, QA. SQLite doesn't know about C# enums. By default, EF Core saves an enum as a number based on its position (Design = 0, Build = 1, Debug = 2, QA = 3). Keep the number, or save the name?

### Decision

Save the name as text (`"Build"`). In `OnModelCreating`: `.HasConversion<string>()`.

### Why

- **Adding a new value can't break old rows.** Example: a Build ticket is saved as `1`. Later I add `Research` after `Design`. Now `Research` is 1 and `Build` is 2. The old row still says `1`, so it silently becomes a Research ticket. No error. With text, the row says `"Build"` and stays Build.
- **I can read it.** When I look in the database with `sqlite3`, I see `Build`, not `1`.
- **Same in JSON.** Saving as text in the database doesn't change what the API sends. By default ASP.NET Core writes enums as numbers in JSON (`"type":3`). So `Program.cs` adds `JsonStringEnumConverter` to `AddControllers().AddJsonOptions(...)`, and the API sends `"type":"QA"` too. It works both ways: requests can send `"QA"` as well.

### Downside

- Text takes a few more bytes than a number. That doesn't matter here.
- If I **rename** an enum value, old rows still have the old name and break. But renaming is rare, and the error is easy to see.

### Other options I said no to

- **Numbers, written in the code** (`Build = 1`): also safe, but only if nobody ever changes those numbers. And I still can't read the rows.

---

## 7. Ticket order is unique inside a phase (replaced by entry 18)

*Date: 2026-09-30 · Area: database · Related: `brief.md` rules 1 and 2*

### Question

Tickets are listed by planned start time. `Order` is only used when two tickets start at the same time. Does `Order` still need to be unique inside a phase?

### Decision

Yes. A unique index on `(PhaseId, Order)`, the same as Phase's `(ProjectId, Order)`.

### Why

- **A tie-breaker must break the tie.** If two tickets had the same start time **and** the same order, nothing decides which comes first. The database could return them in any order, so the list could flip when I reload. The "current ticket" on the home page could also change even though nothing happened.
- Like runners who finish in the same second: a photo finish decides, so no two runners share a place.

### Downside

- Reordering tickets will need the same two-step trick as phases (entry 3).

---

## 8. Tickets I open by hand have no planned start or estimate

*Date: 2026-09-30 · Area: data model · Related: `brief.md` rules 2 and 13*

### Question

The planning agent estimates each ticket's planned start and hours. But when I open a ticket by hand (the **+** under a phase), nobody estimated it. Do I have to fill them in?

### Decision

No. `PlannedStart` (`DateTime?`) and `EstimatedHours` (`double?`) can be empty. In a phase's list, tickets with no planned start go **last**.

### Why

- **Nobody estimated it, so any number would be fake.** A fake number would also confuse the planning agent later, when it compares its estimates with real times.
- **The `?` is important.** Without it, C# fills in a default: 0 hours, and the date 1 January 0001. That date is earlier than everything, so the ticket would jump to the **top** of the list, the opposite of what I want.

### Downside

- Every list sorted by planned start must put empty values last on purpose.

---

## 9. The assigned agent is a string, checked against the tools on my Mac

*Date: 2026-09-30 · Area: data model · Related: `brief.md` "Data model → Ticket"*

### Question

The assigned agent is the AI tool that does the work (Claude Code, Codex…). Should it be an enum, like `TicketType`, or a string?

### Decision

A `string`. The manager checks which tools are installed (with command-line checks) and only accepts a name from that list.

Note: this is the **tool** (Claude Code, Codex). The **model** (Opus, Sonnet) runs inside the tool. Choosing a model would be a different field.

### Why

- **The list comes from my Mac, not from the code.** Which tools are installed changes over time. An enum would fix the list in the code.
- **Typos are still caught.** The manager rejects any name that isn't in the list, so `"claude code"` instead of `"Claude Code"` can't get in.

### Downside

- A typo is caught when the request arrives, not when the code compiles.
- The manager still needs to know **how to start** each tool. Unless that can be stored as data (a command per tool), a new tool still needs a code change.

### Other options I said no to

- **Enum**: the compiler catches typos, but the list would be fixed in the code and wouldn't match what's really installed.

---

## 10. One status enum for all ticket types, starting at Todo

*Date: 2026-10-01 · Area: data model · Related: `brief.md` "Ticket status flows"*

### Question

Each ticket type has its own statuses:

- Build / Debug: Todo, TestCases, WritingTests, Working, Testing, Fixing, NeedsDecision, AwaitingConfirmation, Done, Canceled
- Design: Todo, Demo, Adopted, Rejected
- QA: Todo, TestCases, WritingTests, WaitingForDev, Running, Analyzing, Done

But a ticket has only **one** `Status` column. One enum or three? And must the creator set the status?

### Decision

- One `TicketStatus` enum with all 16 statuses (each listed once). Saved as text, like `TicketType` (entry 6).
- The manager checks that a status fits the ticket's type.
- `Status` starts at `Todo` by default (`= TicketStatus.Todo`). It is **not** `required`.

### Why

- **One column can only hold one C# type.** Three enums would need three columns or messy conversions.
- **Same idea as the agent name (entry 9):** the type allows it, the manager checks it.
- **Every ticket starts at Todo.** Even a Debug ticket from the IT supervisor starts at Todo, because Debug follows the Build flow (rule 12). Its draft test cases still need my review in Test cases. It can't start at Fixing: Fixing means "tests failed and the agent is retrying", but a new ticket has no tests yet.
- **`required` and a default don't mix.** `required` forces the caller to set the value, so the default would never be used.

### Downside

- The compiler won't stop a Design ticket from being set to `Fixing`. The manager must check.

### Other options I said no to

- **One enum per type**: cleaner on their own, but they don't fit in one column.
- **`required` status**: every caller would write `Todo` by hand, and could get it wrong.

---

## 11. "Unplanned" is worked out from when the ticket was created

*Date: 2026-10-01 · Area: data model · Related: `brief.md` rule 15*

### Question

A ticket added after the plan was approved is "unplanned". Should we store an `IsUnplanned` true/false column, or work it out?

### Decision

Work it out. Ticket gets a `CreatedAt` time (set automatically, in UTC (Coordinated Universal Time)). A ticket is unplanned if its `CreatedAt` is later than its project's `BaselineFrozenAt` (the moment the plan was approved).

### Why

- **We never store what we can work out** (the brief's main rule). A stored true/false could disagree with the two times.
- **The baseline never moves.** I can change the plan later, but the baseline is a snapshot taken once at approval, like a photo of the plan on that day (rule 15). So the comparison always gives the same answer.
- `CreatedAt` is useful for other things too, like a phase's history.

### Downside

- To know if a ticket is unplanned, we need the project's `BaselineFrozenAt`, so we go Ticket → Phase → Project.

---

## 12. No "source ticket" on Debug tickets (for now)

*Date: 2026-10-01 · Area: data model*

### Question

The brief gave Debug tickets a "source ticket": the ticket where the problem was found (usually the phase's QA ticket). Keep it?

### Decision

Remove it.

### Why

- **Nothing uses it.** No rule in the status flow reads it. QA goes back to Waiting for dev when **any** ticket is added to the phase, not because of this link.
- **It's not always there.** If I find a bug myself and open a Debug ticket by hand, there's no source ticket.
- Build a field when a feature needs it, not before.

### Downside

- A Debug ticket doesn't remember where it came from, so I can't count "how many bugs did this ticket cause" yet.

### When to come back to this

- When I need tracing. Then links between tickets get their own **link table** (like the prerequisite table), because one column holds one value, not a list of Ids.

---

## 13. A phase's QA ticket is created in the same call

*Date: 2026-10-01 · Area: manager API · Related: `brief.md` rule 11, entry 1*

### Question

Creating a phase must also create its QA ticket (rule 11). The request only sends the phase `Name`. Where do the QA ticket's title and description come from?

### Decision

- The manager makes the title automatically.
- `CreatePhaseRequest` gets an **optional** `QaDescription`. If it's sent, use it. If not, the manager uses a default description.
- The phase and its QA ticket are saved in **one transaction** (all or nothing).

### Why

- **No half-finished state.** The other way was two calls: create the phase with a placeholder description, then update it. If the second call is forgotten or fails, the placeholder stays forever. It looks like a real value, so nobody notices.
- **Optional**, so a caller with nothing to add still gets a working phase.

### Other options I said no to

- **Two calls (create, then update the description)**: simpler endpoint, but the caller must remember the second step.

---

## 14. The default agent is saved in the database, not in appsettings.json

*Date: 2026-10-01 · Area: settings vs. user data*

### Question

The QA ticket made with a phase needs an `AssignedAgent`, but the request doesn't send one. So the manager needs a default agent. Where do we keep it?

### Decision

In the database, not in `appsettings.json`.

### Why

- **`appsettings.json` is for the developer.** It holds things like the database connection string. You change it by editing a file and restarting the program.
- **The default agent is the user's choice.** The user should change it inside the app, while it's running. User choices (like project order) already live in the database, and the UI reaches the database through the API.

In one sentence: *"`appsettings.json` is for settings the developer changes by editing a file; a default agent is a choice the user makes in the app, so it goes in the database."*

### Other options I said no to

- **`appsettings.json`**: easy to read at startup, but the user would have to edit a file and restart.
- **The caller sends the agent every time**: every caller has to do extra work, and different callers could disagree.

---

## 15. A global default agent, copied into each new project

*Date: 2026-10-01 · Area: data model · Related: entry 14*

### Question

Entry 14 put the default agent in the database. One value for everything, or one per project? And how does a new project get its value?

### Decision

- Each project has its own `DefaultAgent` (required), so different projects can use different tools.
- There is also one **global** default agent.
- A new project **copies** the global value when it's created.
- If I change the global default later, only **new** projects get the new value. Old projects keep theirs.

### Why

- **Per project:** one project might suit Codex, another Claude Code.
- **Copy, not fall back:** a project's agent should only change when I change it for that project. Example: I switch the global default from Claude Code to Codex. With copying, my running projects stay on Claude Code. Nothing switches tools behind my back in the middle of work.
- **Always has a value:** every project copies a value, so `DefaultAgent` can be required, and a new phase can always give its QA ticket an agent.

### Real data vs. test data (worth telling in an interview)

At first we said "existing projects need a value for the new column". But I pointed out that my database only had **test rows I typed in by hand**. I can delete the database and rebuild it from the migrations any time.

So the real question was different: on a **brand-new, empty** database, the very first project copies the global default. So the global default must have a value from day one. Thinking about test data would have solved the wrong problem.

In one sentence: *"I noticed we were designing around test rows we could throw away, so I asked what the very first project on a fresh install needs instead."*

### Other options I said no to

- **Fall back** (empty project value means "use the global one"): stores less, but changing the global default would quietly change every project that didn't set its own.
- **Only a global default**: every project would use the same tool.
- **Only per project, no global**: every new project starts empty, and creating a phase fails until I set one.

---

## 16. "Manual": I can do a ticket myself, and it's the fallback default

*Date: 2026-10-02 · Area: product rules + data model · Related: entries 9 and 15, `brief.md` rule 6*

### Question

1. On a brand-new install, the global default agent needs a value before the first project is added. Where does it come from?
2. What if no AI tool is installed?

### Decision

- **First launch:** the manager looks for installed agent tools (the same check as entry 9) and uses the first one it finds as the global default.
- **No tool found:** the default becomes **Manual**.
- **Manual is a real choice for any ticket,** not just a fallback. It means "I do this ticket myself". I do **all three** AI jobs: drafting the test cases, writing the test code, and writing the code.
- **I can switch a ticket between Manual and an AI tool at any time.** If an agent is working when I switch, it **stops right away**. The other side continues from the latest git commit. The switch is written in the status history (same from and to status, like Working → Working, with a note).

### Why

- **AI quota runs out.** If I'm out of quota, the work shouldn't stop. I switch the ticket to Manual and keep going, then switch back when the quota returns.
- **Detect instead of a fixed value:** the default matches what's really on my Mac, instead of naming a tool that may not be installed.
- **Manual instead of empty or an error:** `AssignedAgent` always has a value, and Tickie still works on a Mac with no AI tool.
- **Stop right away when switching:** I usually switch *because* the agent can't go on, so waiting makes no sense. It's also how Cancel works.
- **It already fits "waiting on me".** A Manual ticket never has an agent running, so whenever it's past Todo and not finished, it shows as waiting on me. That's correct: it's my turn.

### Downside

- The brief needs new rules for how a Manual ticket moves through steps an agent normally finishes (e.g. Working → Testing). That's still open.
- The agent-name check (entry 9) must accept "Manual" even though it isn't an installed tool.

### Other options I said no to

- **Start the global default as a fixed `"Claude Code"`:** simpler, but it can name a tool that isn't installed.
- **Manual only as a fallback** (tickets can't actually run as Manual): smaller, but doesn't help when I'm out of quota.
- **Let the agent finish its current step before switching:** pointless if it's out of quota, and slower.

---

## 17. App-wide settings: one row with Id 1, created when the manager starts

*Date: 2026-10-02 · Area: database + startup · Related: entries 14–16*

### Question

The global default agent needs a home in the database. It's one value for the whole app, not one per project. How do we store it, and who creates it?

### Decision

- A `UserSettings` table with **one row**, always `Id = 1`. The code only ever reads and updates row 1. It never adds another row.
- `Id` is set by our code, not by the database: `ValueGeneratedNever()` in `OnModelCreating`, and a normal `{ get; set; }` (not `private init`).
- **When the manager starts**, it checks: "Does row 1 exist?" If not, it creates it. For now the value is `"Manual"`; tool detection comes later (entry 16).

### Why

- **One row means one answer.** With two rows, nobody would know which default is the real one. Like a house with one mailbox.
- **Named `UserSettings`, not `Settings`:** `appsettings.json` already means "settings" for the developer. These are **my** choices in the app (entry 14).
- **Created at startup, not "when first needed":** every other part of the code can assume row 1 is always there, and no code has to check for it.
- **A scope at startup:** controllers get a `TickieDbContext` per request. At startup there is no request, so the code makes a scope with `CreateScope()` and gets a `TickieDbContext` from it. Like borrowing a library book and returning it at the end of the block.

### Downside

- The "one row" rule lives in the code, not the database. A hand-written `sqlite3` insert could still add row 2.

### Other options I said no to

- **Create the row the first time a project needs it:** then every place that reads it must handle "not there yet".

---

## 18. Tickets have no Order field

*Date: 2026-10-03 · Area: data model · Replaces: entry 7*

### How it came up

I noticed this myself. While writing the QA ticket, I pointed out that QA runs last, so `Order = 1` looked wrong. Following that up showed `Order` didn't matter at all.

### Question

While creating the QA ticket, I asked: should QA get `Order = 1` or the last number? That showed that `Order` barely does anything. Do we need it?

### Decision

Remove `Order` from Ticket, along with its unique `(PhaseId, Order)` index. A phase's tickets are listed by planned start. If two start at the same time, the one with the **smaller `Id`** (created first) comes first.

### Why

- **Planned start already decides the list.** QA comes last because it has no planned start yet (entry 8), and later because the planning agent plans it last. `Order` doesn't change that.
- **`Id` already breaks ties.** It's unique, and it grows as tickets are created. So "same time → older ticket first" works without an extra field.
- **`Order` only costs work.** Someone has to fill it in, there's a unique index to keep, and reordering would need the two-step trick (entry 3).
- **There's no ticket dragging** in the brief, so nobody would ever set `Order` by hand.

### Downside

- I can't manually choose which of two same-time tickets comes first. If I add ticket dragging one day, `Order` comes back.

### Other options I said no to

- **Keep `Order` and give QA the last number:** extra work for a field that doesn't change what I see.

---

## 19. Ticket has no ProjectId; find a project's tickets through its phases

*Date: 2026-10-03 · Area: data model + queries · Related: `GET /projects/{projectId}/tickets`*

### Question

To list all tickets in a project, the code has to go Ticket → Phase → Project, because a ticket only stores `PhaseId`. Should Ticket also store `ProjectId` directly, to make this easier?

### Decision

No. Get the project's tickets in two queries:

1. Get the Ids of all phases in the project (e.g. `[4, 5, 6]`).
2. Get all tickets whose `PhaseId` is in that list (SQL `IN (...)`).

### Why

- **One fact, one place.** The phase already says which project it's in. If Ticket stored `ProjectId` too, the two could disagree (ticket says project 2, its phase is in project 1), and nothing would stop it. Like writing your city on every page of a notebook when it's already on the cover.
- **It's always two queries, not one per phase.** Step 1 returns all phase Ids at once, and step 2 checks the whole list at once. 1 phase or 100 phases, still two queries.
- **Tiny data.** A local SQLite database with hundreds of tickets: a few milliseconds.

### What real projects usually do

- Start **normalized** (no copies). Only add a copy (**denormalize**) when a query is measured to be too slow, and then add code or rules to keep the copies in sync.
- Exception: Jira issues store their project directly, because an issue can exist **without** a sprint (backlog). There, project isn't a copy; it's the only link. If Tickie ever allows tickets with no phase, `ProjectId` would become a real field.

### Downside

- Listing a project's tickets needs a step through `Phases`.

### Other options I said no to

- **Store `ProjectId` on Ticket:** simpler query, but two copies of the same fact that can disagree.
