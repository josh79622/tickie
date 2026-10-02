# Tickie

A fully local Mac desktop app for opening tickets and dispatching work to AI coding agents on your machine from a PM's (Project Manager's) point of view, with every task gated by locked test cases.
Where the name comes from: a ticket is only done once every item on its acceptance checklist has been ticked.

The full product rules, status flow, data structures and technical decisions live in **`docs/brief.md`**. Read the relevant sections before making changes.

## Current status

- The design phase is complete. The **home page** and **project page** demos are built (Vue 3 with mock data): phases with their tasks, a task detail side panel, opening tasks by hand, and a floating chat with the planning agent / IT supervisor (demo replies only).
- **The demo is finished**: a Build or Debug task can be clicked from Todo to Done in its side panel, including the Needs decision branch (agent steps are simulated with dashed "Simulate agent" buttons). Design and QA have no status buttons. Visual polish is deliberately left for real frontend work.
- The C# manager has started (see "How we work together"). `manager/` is an ASP.NET Core Web API (Controllers, .NET 10) with EF Core SQLite installed. The first entity, `Entities/Project.cs`, is written and builds cleanly; the template's WeatherForecast sample has been removed now that `ProjectsController` exists.
  - Decisions Josh made on `Project`: `int` Id (single database, single writer, matches the frontend's `Id = number`); `Id` and `CreatedAt` use `private init`; `CreatedAt` defaults to `DateTime.UtcNow` (all timestamps are stored in UTC); `Name`, `FolderPath` and `SortOrder` are `required`; entities live in `Entities/`, separate from future request/response DTOs.
- `Data/TickieDbContext.cs` has one `DbSet<Project>` and takes its options through the constructor. `Program.cs` registers it with `UseSqlite`, reading the `Tickie` connection string from `appsettings.json` before `AddDbContext` so a missing value fails at startup. SQLite files (`*.db`, `*.db-shm`, `*.db-wal`) are git-ignored: they hold private data and are rebuilt from migrations.
- `TickieDbContext.OnModelCreating` holds all database rules (Fluent API, chosen over attributes so rules stay in one place): so far a unique index on `Project.FolderPath`, since the folder identifies a project. `dotnet-ef` is a local tool (`manager/dotnet-tools.json`, run `dotnet tool restore`); the `InitialCreate` migration is applied with `dotnet ef database update`.
- `Controllers/ProjectsController.cs` finishes Project as a vertical slice (database → entity → API) before any other entity. It gets `TickieDbContext` through constructor injection. Request/response shapes are `record` DTOs in `Dtos/`: `CreateProjectRequest` (only `Name`, `Description`, `FolderPath`, which blocks overposting), `ProjectResponse` (no `CreatedAt`, which the frontend doesn't use, and no `RemovedAt`, which would always be null), and `ReorderProjectsRequest`. A private `ToResponse` helper does the entity → DTO mapping. Endpoints and Josh's decisions:
  - `GET /projects`: projects not removed, ordered by `SortOrder`.
  - `POST /projects`: a new project goes to the top with `SortOrder` = current smallest − 1 (only one row is written; numbers may go to 0 or below). A folder that belongs to a project on the home page returns 409 Conflict, so the user knows why nothing appeared. A folder that belongs to a removed project restores it (original name kept, moved to the top) and returns 200; a new project returns 201.
  - `PATCH /projects/{id}/remove`: soft removal, sets `RemovedAt`; 204, or 404 if missing or already removed. PATCH rather than DELETE because the row only changes.
  - `PATCH /projects/reorder`: the frontend sends every home-page project Id in the new order and the manager renumbers them 1, 2, 3…; 400 unless the list has each project exactly once. Addresses use verbs (`remove`, `reorder`) consistently.
  - Removed projects keep a stale `SortOrder`; it's harmless because every query filters them out and restoring always assigns a fresh one.
  - Errors are returned as JSON (`{ "message": ... }`). Test by hand with `curl` or `manager/Tickie.Manager.http`, and restart the manager (or use `dotnet watch`) after every C# change.
- `Phase` is the second vertical slice. `Entities/Phase.cs` has `Id`, `ProjectId`, `Name` and `Order` (all but `Id` `required`); "is done" isn't stored because it's derived from the phase's QA task. Josh's decisions:
  - Phases are created by an AI agent through the manager's API; there is no create-phase button. `POST /projects/{projectId}/phases` takes only `Name` (`CreatePhaseRequest`); the manager assigns `Order` = highest + 1 (the first phase gets 1). 404 for a missing or removed project, 409 for a name already used in that project, 201 otherwise.
  - It doesn't create the phase's QA ticket yet, because `Ticket` doesn't exist; once it does, a phase and its QA ticket must be saved together (brief rule 11).
  - The foreign key is set in `OnModelCreating` with `HasOne<Project>().WithMany().HasForeignKey(ph => ph.ProjectId)` (no navigation properties). EF Core's default cascade delete never fires because projects are only soft-removed.
  - `(ProjectId, Order)` and `(ProjectId, Name)` each have their own unique index (two rules, so two indexes; one three-column index would only block exact duplicates). The `(ProjectId, Order)` index also replaces EF Core's automatic `ProjectId` index because `ProjectId` comes first. The manager checks names too, so callers get a clear 409; the index is the safety net.
  - `PATCH /projects/{projectId}/phases/reorder` takes every phase Id of the project in the new order (`ReorderPhasesRequest`), 400 unless each appears exactly once. To get past the `(ProjectId, Order)` index it renumbers in two passes (temporary negative orders, then 1, 2, 3…) inside one transaction, so a failure rolls everything back.
  - `Controllers/PhasesController.cs` has its own controller, since it's about phases rather than projects, routed at `projects/{projectId}/[controller]`. `GET /projects/{projectId}/phases` returns `PhaseResponse` DTOs ordered by `Order`, or 404 if the project is missing or removed.
  - Until the planning agent exists, test phases are inserted by hand with `sqlite3 tickie.db` (run `PRAGMA foreign_keys = ON;` first, since the command-line tool doesn't enforce foreign keys by default).
- The manager listens on `http://localhost:5051` (`Properties/launchSettings.json`). Port 5000 on a Mac belongs to the AirPlay Receiver, which answers with an empty 403.
- A unit of work is now called a **Ticket** (decisions.md entry 4); the brief and C# use it, the frontend still says "task". The whole project uses American spelling (`Canceled`, `Analyzing`).
- `Ticket` is the third entity (plain fields done, migration `AddTicket` applied; decisions.md entries 5–12). `Entities/Ticket.cs` has `Id`, `PhaseId`, `Title`, `Description`, `Type`, `Order`, `PlannedStart`, `EstimatedHours`, `AssignedAgent`, `Status`, `CreatedAt`. Josh's decisions:
  - `Description` replaces the brief's tagline and is required. Optional labels come later in their own table.
  - `Type` (`TicketType`) and `Status` (`TicketStatus`, one enum with all 16 statuses) are enums saved as text with `HasConversion<string>()`. `Status` defaults to `Todo`; the manager must check that a status fits the ticket's type.
  - `PlannedStart` and `EstimatedHours` are nullable: tickets opened by hand have no estimate and go last in a phase's list.
  - `AssignedAgent` is a required string; the manager will accept only agent tools it detects on the machine.
  - `(PhaseId, Order)` is unique, with a foreign key to `Phase`. "Is unplanned" isn't stored: it's `CreatedAt` > the project's `BaselineFrozenAt`. The brief's "source ticket" was dropped.
- Default agents (decisions.md entries 13–17): a ticket's agent can be **Manual** (I do the work myself, e.g. when out of AI quota; switchable at any time). `Entities/UserSettings.cs` holds the global `DefaultAgent` in one row with `Id = 1` (`ValueGeneratedNever()`); `Program.cs` creates that row at startup inside a `CreateScope()` block if it's missing, currently always with `"Manual"`.
- Next, in order: `Project.DefaultAgent` (required, copied from `UserSettings` when a project is added); creating a phase also creates its QA ticket in the same transaction (title filled in by the manager, optional `QaDescription` on `CreatePhaseRequest`, agent = the project's default); then the Ticket endpoints. Later: replace `"Manual"` at startup with detection of installed agent tools.
- Frontend leftovers once the UI uses the real API: rename task → ticket, `tagline` → `description`, statuses to American spelling, drop `sourceTaskId`, and drop `removedAt` from the `Project` type.
- Known leftovers for real frontend work (not bugs in the design):
  - Mock task 3 ("Project page") is a Done Build task with no test cases, which the rules don't allow; add some.
  - Long task titles wrap in narrow windows because of the wider type column.
  - The side panel's status badge isn't colored like the list's; the panel, chat and new-task dialog were built quickly and need a design pass.
  - Status buttons for Design and QA tasks, and QA moving from Waiting for dev to Running automatically, aren't built.
- Folder structure (one repo for everything):
  - `docs/`: design documents (`brief.md` for the product rules, `decisions.md` for why the code is shaped the way it is; Josh uses it to prepare for interviews, so add an entry whenever a design choice is worth explaining)
  - `app/`: the UI (Vue 3 + TypeScript). Tauri will be added later as `app/src-tauri/`.
  - `manager/`: the background manager (C#, project `Tickie.Manager`). Run `dotnet build` inside it to check changes.
- Where things live in `app/src/`:
  - `types/models.ts`: TypeScript types mirroring the brief's "Data model"; the contract with the future C# API.
  - `mock/data.ts`: mock data in that shape (5 projects; "Tickie" covers most task types and statuses), plus a pretend folder list for the folder picker.
  - `lib/derive.ts`: pure functions for every derived value (current phase and task, waiting on me, agent running, progress, actual times).
  - `stores/tickie.ts`: the Pinia store all screens share.
  - `components/FolderPicker.vue`: a pretend Finder, to be replaced by Tauri's native folder dialog.
- Mock data resets on every page reload; nothing persists until the C# manager and SQLite exist.
- Run the UI with `npm --prefix app run dev` (also set up in `.claude/launch.json`). Check changes with `npm run type-check` and `npm run lint` inside `app/`.

## Tech stack

| Layer | Choice |
| --- | --- |
| Desktop shell | Tauri (Mac build only for now) |
| UI | Vue 3 + TypeScript |
| Background manager | C# + ASP.NET Core (Controllers) |
| Data access | EF Core (Entity Framework Core) |
| Database | SQLite |
| Real-time updates | SignalR |

The UI and the C# manager are two separate programs that talk through a local API (Application Programming Interface).

## How we work together (important)

This is Josh's learning project. He needs to be able to explain every detail of it.

- **Josh writes the backend (C#, ASP.NET Core, EF Core, SignalR) himself, step by step.** Act as a tutor: explain one concept at a time, guide his thinking with questions first, and don't hand over a complete implementation unless Josh explicitly asks for one.
- **Frontend (Vue 3)**: you can help more here, but Josh reviews every change.
- The whole system uses American English: UI text, code, database values and documentation (e.g. Canceled, Analyzing).
- Spell out an abbreviation in parentheses the first time it appears.
- Use everyday analogies instead of jargon.
- Be direct and honest; no reassurance. Point out problems when you see them.
- Replies may be in Traditional Chinese; keep technical terms in English.
- Ask Josh **one decision at a time**, and end every reply with a short recap of exactly what he needs to answer.

## Rules for design decisions

- Check with Josh before changing any rule already settled in `docs/brief.md`; once confirmed, update `docs/brief.md` to match.
- A few core principles that are easy to miss:
  - Data that can be derived from system behavior (progress, actual start/end times, current task) is never entered by hand and never stored separately.
  - Test cases are locked once finalized; the coding agent cannot change them.
  - The baseline never changes once approved; tasks added afterward are marked "unplanned".
