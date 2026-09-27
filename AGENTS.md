# Tickie

A fully local Mac desktop app for opening tickets and dispatching work to AI coding agents on your machine from a PM's (Project Manager's) point of view, with every task gated by locked test cases.
Where the name comes from: a ticket is only done once every item on its acceptance checklist has been ticked.

The full product rules, status flow, data structures and technical decisions live in **`docs/brief.md`**. Read the relevant sections before making changes.

## Current status

- The design phase is complete. `app/` has been scaffolded and the TypeScript types for the data model are in `app/src/types/models.ts`; no screens yet.
- Folder structure (one repo for everything):
  - `docs/`: design documents (`brief.md`)
  - `app/`: the UI (Vue 3 + TypeScript). Mock data lives in `app/src/mock/`. Tauri will be added later as `app/src-tauri/`.
  - `manager/`: the background manager (C#). Not created yet; Josh sets it up himself when backend work starts.
- Next step: build a demo with Vue 3 and mock data (home page and project page). The mock data follows the "Data model" section of `docs/brief.md` and will later become the API response format.
- The demo is finished when a task can be clicked through its whole flow, from Todo to Done. Stop there.

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

## Rules for design decisions

- Check with Josh before changing any rule already settled in `docs/brief.md`; once confirmed, update `docs/brief.md` to match.
- A few core principles that are easy to miss:
  - Data that can be derived from system behavior (progress, actual start/end times, current task) is never entered by hand and never stored separately.
  - Test cases are locked once finalized; the coding agent cannot change them.
  - The baseline never changes once approved; tasks added afterward are marked "unplanned".
