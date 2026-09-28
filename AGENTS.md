# Tickie

A fully local Mac desktop app for opening tickets and dispatching work to AI coding agents on your machine from a PM's (Project Manager's) point of view, with every task gated by locked test cases.
Where the name comes from: a ticket is only done once every item on its acceptance checklist has been ticked.

The full product rules, status flow, data structures and technical decisions live in **`docs/brief.md`**. Read the relevant sections before making changes.

## Current status

- The design phase is complete. The **home page** and **project page** demos are built (Vue 3 with mock data): phases with their tasks, a task detail side panel, opening tasks by hand, and a floating chat with the planning agent / IT supervisor (demo replies only).
- **The demo is finished**: a Build or Debug task can be clicked from Todo to Done in its side panel, including the Needs decision branch (agent steps are simulated with dashed "Simulate agent" buttons). Design and QA have no status buttons. Visual polish is deliberately left for real frontend work.
- The C# manager has started (see "How we work together"). `manager/` is an ASP.NET Core Web API (Controllers, .NET 10) with EF Core SQLite installed. The first entity, `Entities/Project.cs`, is written and builds cleanly; the template's WeatherForecast sample is kept as a reference until the first real controller exists.
  - Decisions Josh made on `Project`: `int` Id (single database, single writer, matches the frontend's `Id = number`); `Id` and `CreatedAt` use `private init`; `CreatedAt` defaults to `DateTime.UtcNow` (all timestamps are stored in UTC); `Name`, `FolderPath` and `SortOrder` are `required`; entities live in `Entities/`, separate from future request/response DTOs.
- Next: the `DbContext` (register it in `Program.cs`, point it at a SQLite file), then the first migration.
- Known leftovers for real frontend work (not bugs in the design):
  - Mock task 3 ("Project page") is a Done Build task with no test cases, which the rules don't allow; add some.
  - Long task titles wrap in narrow windows because of the wider type column.
  - The side panel's status badge isn't colored like the list's; the panel, chat and new-task dialog were built quickly and need a design pass.
  - Status buttons for Design and QA tasks, and QA moving from Waiting for dev to Running automatically, aren't built.
- Folder structure (one repo for everything):
  - `docs/`: design documents (`brief.md`)
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

This is Josh's learning project and job-hunting portfolio piece. He needs to be able to explain every detail in an interview.

- **Josh writes the backend (C#, ASP.NET Core, EF Core, SignalR) himself, step by step.** Act as a tutor: explain one concept at a time, guide his thinking with questions first, and don't hand over a complete implementation unless Josh explicitly asks for one.
- **Frontend (Vue 3)**: you can help more here, but Josh reviews every change.
- The whole system uses English: UI text, code, database values and documentation.
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
