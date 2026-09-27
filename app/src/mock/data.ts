// Mock data in the same shape the C# manager will return.
// "Now" in this data is around 2026-09-27 10:00 UTC.

import type {
  AgentRun,
  Id,
  IsoDateTime,
  Phase,
  Project,
  StatusHistoryEntry,
  Task,
  TaskDependency,
  TaskStatus,
  TestCase,
} from '@/types/models'

const at = (day: number, hour: number): IsoDateTime =>
  `2026-09-${String(day).padStart(2, '0')}T${String(hour).padStart(2, '0')}:00:00Z`

export const projects: Project[] = [
  {
    id: 1,
    name: 'Tickie',
    description: 'Dispatch work to local AI coding agents and gate every task with locked tests.',
    baselineFrozenAt: at(20, 9),
  },
  {
    id: 2,
    name: 'Recipe Box',
    description: 'Save family recipes and search them by ingredient.',
    baselineFrozenAt: at(15, 9),
  },
  {
    id: 3,
    name: 'Budget Buddy',
    description: 'Import bank CSVs and see where the money went each month.',
    baselineFrozenAt: at(10, 9),
  },
  {
    id: 4,
    name: 'Portfolio Site',
    description: 'A personal site listing projects for job applications.',
    baselineFrozenAt: at(5, 9),
  },
  {
    id: 5,
    name: 'Habit Tracker',
    description: 'Track daily habits with streaks and a weekly summary.',
    baselineFrozenAt: null,
  },
]

export const phases: Phase[] = [
  { id: 1, projectId: 1, name: 'UI demo', order: 1 },
  { id: 2, projectId: 1, name: 'Manager API', order: 2 },
  { id: 3, projectId: 1, name: 'Agent integration', order: 3 },
  { id: 4, projectId: 2, name: 'Core recipes', order: 1 },
  { id: 5, projectId: 3, name: 'CSV import', order: 1 },
  { id: 6, projectId: 4, name: 'Launch', order: 1 },
  { id: 7, projectId: 5, name: 'Planning', order: 1 },
]

const base = { isUnplanned: false, estimatedHours: 4 }

export const tasks: Task[] = [
  // Tickie, phase 1: finished
  {
    ...base,
    id: 1,
    phaseId: 1,
    order: 1,
    type: 'Design',
    status: 'Adopted',
    title: 'Home and project page demo',
    tagline: 'Clickable mockups of both screens',
    assignedAgent: 'Claude Code',
    plannedStart: at(20, 10),
  },
  {
    ...base,
    id: 2,
    phaseId: 1,
    order: 2,
    type: 'Build',
    status: 'Done',
    title: 'Home page',
    tagline: 'Five most recent projects with current phase and task',
    assignedAgent: 'Claude Code',
    plannedStart: at(21, 9),
  },
  {
    ...base,
    id: 3,
    phaseId: 1,
    order: 3,
    type: 'Build',
    status: 'Done',
    title: 'Project page',
    tagline: 'All tasks of a project, sorted by time',
    assignedAgent: 'Claude Code',
    plannedStart: at(21, 14),
  },
  {
    ...base,
    id: 4,
    phaseId: 1,
    order: 4,
    type: 'QA',
    status: 'Done',
    title: 'UI demo E2E',
    tagline: 'Click a task from Todo to Done',
    assignedAgent: 'Codex',
    plannedStart: at(22, 9),
  },

  // Tickie, phase 2: in progress, covers most statuses
  {
    ...base,
    id: 5,
    phaseId: 2,
    order: 1,
    type: 'Build',
    status: 'Working',
    title: 'Project and task endpoints',
    tagline: 'GET/POST for projects, phases and tasks',
    assignedAgent: 'Claude Code',
    plannedStart: at(24, 9),
  },
  {
    ...base,
    id: 6,
    phaseId: 2,
    order: 2,
    type: 'Build',
    status: 'TestCases',
    title: 'Status history recording',
    tagline: 'One entry per status change',
    assignedAgent: 'Claude Code',
    plannedStart: at(25, 9),
  },
  {
    ...base,
    id: 7,
    phaseId: 2,
    order: 3,
    type: 'Build',
    status: 'AwaitingConfirmation',
    title: 'Resume after sleep',
    tagline: 'Continue from the latest commit when the laptop wakes',
    assignedAgent: 'Codex',
    plannedStart: at(25, 14),
  },
  {
    ...base,
    id: 8,
    phaseId: 2,
    order: 4,
    type: 'Build',
    status: 'Todo',
    title: 'SignalR task updates',
    tagline: 'Push task changes to the UI',
    assignedAgent: 'Claude Code',
    plannedStart: at(27, 9),
  },
  {
    ...base,
    id: 9,
    phaseId: 2,
    order: 5,
    type: 'Debug',
    status: 'NeedsDecision',
    isUnplanned: true,
    sourceTaskId: 3,
    title: 'Task order lost after reload',
    tagline: 'Project page shows tasks in creation order',
    assignedAgent: 'Claude Code',
    plannedStart: at(26, 9),
    estimatedHours: 2,
  },
  {
    ...base,
    id: 10,
    phaseId: 2,
    order: 6,
    type: 'QA',
    status: 'WritingTests',
    title: 'Manager API E2E',
    tagline: 'UI talks to the real manager end to end',
    assignedAgent: 'Codex',
    plannedStart: at(28, 9),
  },

  // Tickie, phase 3: not started
  {
    ...base,
    id: 11,
    phaseId: 3,
    order: 1,
    type: 'Build',
    status: 'Todo',
    title: 'Launch coding agent',
    tagline: 'Start an agent CLI in the project folder',
    assignedAgent: 'Claude Code',
    plannedStart: at(29, 9),
  },
  {
    ...base,
    id: 12,
    phaseId: 3,
    order: 2,
    type: 'QA',
    status: 'Todo',
    title: 'Agent integration E2E',
    tagline: 'A real agent completes a task',
    assignedAgent: 'Codex',
    plannedStart: at(30, 9),
  },

  // Other projects
  {
    ...base,
    id: 13,
    phaseId: 4,
    order: 1,
    type: 'Design',
    status: 'Demo',
    title: 'Recipe card layout',
    tagline: 'How a single recipe looks',
    assignedAgent: 'Claude Code',
    plannedStart: at(26, 9),
  },
  {
    ...base,
    id: 14,
    phaseId: 4,
    order: 2,
    type: 'QA',
    status: 'TestCases',
    title: 'Core recipes E2E',
    tagline: 'Add, edit and search a recipe',
    assignedAgent: 'Codex',
    plannedStart: at(29, 9),
  },
  {
    ...base,
    id: 15,
    phaseId: 5,
    order: 1,
    type: 'Build',
    status: 'Done',
    title: 'Parse CSV',
    tagline: 'Read transactions from a bank export',
    assignedAgent: 'Gemini CLI',
    plannedStart: at(11, 9),
  },
  {
    ...base,
    id: 16,
    phaseId: 5,
    order: 2,
    type: 'QA',
    status: 'Running',
    title: 'CSV import E2E',
    tagline: 'Import three real bank exports',
    assignedAgent: 'Codex',
    plannedStart: at(14, 9),
  },
  {
    ...base,
    id: 17,
    phaseId: 6,
    order: 1,
    type: 'Build',
    status: 'Cancelled',
    title: 'Blog section',
    tagline: 'Markdown posts',
    assignedAgent: 'Claude Code',
    plannedStart: at(6, 9),
  },
  {
    ...base,
    id: 18,
    phaseId: 6,
    order: 2,
    type: 'Build',
    status: 'Done',
    title: 'Project list',
    tagline: 'Cards linking to each repo',
    assignedAgent: 'Claude Code',
    plannedStart: at(6, 14),
  },
  {
    ...base,
    id: 19,
    phaseId: 6,
    order: 3,
    type: 'QA',
    status: 'Done',
    title: 'Launch E2E',
    tagline: 'Every page loads and links work',
    assignedAgent: 'Codex',
    plannedStart: at(8, 9),
  },
]

export const taskDependencies: TaskDependency[] = [
  { taskId: 2, prerequisiteTaskId: 1 },
  { taskId: 3, prerequisiteTaskId: 1 },
  { taskId: 6, prerequisiteTaskId: 5 },
  { taskId: 8, prerequisiteTaskId: 5 },
  { taskId: 11, prerequisiteTaskId: 8 },
]

export const testCases: TestCase[] = [
  // Home page (done)
  {
    id: 1,
    taskId: 2,
    content: 'Shows at most five projects',
    isLocked: true,
    passedOnLastRun: true,
  },
  {
    id: 2,
    taskId: 2,
    content: 'Most recently active project is first',
    isLocked: true,
    passedOnLastRun: true,
  },
  {
    id: 3,
    taskId: 2,
    content: 'Each project shows its current phase and task',
    isLocked: true,
    passedOnLastRun: true,
  },
  // Project and task endpoints (working)
  {
    id: 4,
    taskId: 5,
    content: 'GET /projects returns every project',
    isLocked: true,
    passedOnLastRun: null,
  },
  {
    id: 5,
    taskId: 5,
    content: 'POST /tasks rejects a circular dependency',
    isLocked: true,
    passedOnLastRun: null,
  },
  {
    id: 6,
    taskId: 5,
    content: 'GET /projects/{id}/tasks returns tasks in planned order',
    isLocked: true,
    passedOnLastRun: null,
  },
  // Status history recording (drafts waiting for my review)
  {
    id: 7,
    taskId: 6,
    content: 'Changing a status adds exactly one history entry',
    isLocked: false,
    passedOnLastRun: null,
  },
  {
    id: 8,
    taskId: 6,
    content: 'A resume after sleep adds an entry with a note',
    isLocked: false,
    passedOnLastRun: null,
  },
  // Resume after sleep (all passing, waiting for confirmation)
  {
    id: 9,
    taskId: 7,
    content: 'Agent resumes from the latest commit after wake',
    isLocked: true,
    passedOnLastRun: true,
  },
  {
    id: 10,
    taskId: 7,
    content: 'Closing the window does not stop the agent',
    isLocked: true,
    passedOnLastRun: true,
  },
  // Task order lost after reload (stuck)
  {
    id: 11,
    taskId: 9,
    content: 'Tasks keep planned order after a page reload',
    isLocked: true,
    passedOnLastRun: false,
  },
]

// Status history is generated from each task's path so the data stays readable.
let historyId = 0
const walk = (
  taskId: Id,
  path: TaskStatus[],
  startDay: number,
  startHour: number,
  notes: Partial<Record<number, string>> = {},
): StatusHistoryEntry[] =>
  path.map((toStatus, i) => ({
    id: ++historyId,
    taskId,
    fromStatus: i === 0 ? null : path[i - 1]!,
    toStatus,
    at: at(startDay + Math.floor((startHour + i) / 24), (startHour + i) % 24),
    note: notes[i] ?? null,
  }))

const buildToDone: TaskStatus[] = [
  'Todo',
  'TestCases',
  'WritingTests',
  'Working',
  'Testing',
  'AwaitingConfirmation',
  'Done',
]
const qaToDone: TaskStatus[] = [
  'Todo',
  'TestCases',
  'WritingTests',
  'WaitingForDev',
  'Running',
  'Done',
]
const allTasksCreated = (ids: Id[], day: number): StatusHistoryEntry[] =>
  ids.flatMap((id) => walk(id, ['Todo'], day, 9))

export const statusHistory: StatusHistoryEntry[] = [
  ...walk(1, ['Todo', 'Demo', 'Demo', 'Adopted'], 20, 10, {
    2: 'Request changes: bigger status lights',
  }),
  ...walk(2, buildToDone, 21, 9),
  ...walk(3, buildToDone, 21, 14),
  ...walk(4, qaToDone, 22, 9),
  ...walk(5, ['Todo', 'TestCases', 'WritingTests', 'Working'], 24, 9),
  ...walk(6, ['Todo', 'TestCases'], 26, 15),
  // A resume is not a status change, so it shows up as Working -> Working with a note
  ...walk(
    7,
    ['Todo', 'TestCases', 'WritingTests', 'Working', 'Working', 'Testing', 'AwaitingConfirmation'],
    25,
    14,
    { 4: 'Resumed from commit 3f9c2ab' },
  ),
  ...allTasksCreated([8, 11, 12], 20),
  ...walk(
    9,
    [
      'Todo',
      'TestCases',
      'WritingTests',
      'Working',
      'Testing',
      'Fixing',
      'Testing',
      'Fixing',
      'Testing',
      'Fixing',
      'NeedsDecision',
    ],
    26,
    9,
  ),
  ...walk(10, ['Todo', 'TestCases', 'WritingTests'], 24, 9),
  ...walk(13, ['Todo', 'Demo'], 26, 9),
  ...walk(14, ['Todo', 'TestCases'], 26, 9),
  ...walk(15, buildToDone, 11, 9),
  ...walk(16, ['Todo', 'TestCases', 'WritingTests', 'WaitingForDev', 'Running'], 14, 9),
  ...walk(17, ['Todo', 'TestCases', 'Cancelled'], 6, 9),
  ...walk(18, buildToDone, 6, 14),
  ...walk(19, qaToDone, 8, 9),
]

export const agentRuns: AgentRun[] = [
  { id: 1, taskId: 5, agent: 'Claude Code', startedAt: at(27, 8), endedAt: null },
  { id: 2, taskId: 10, agent: 'Codex', startedAt: at(27, 9), endedAt: null },
  { id: 3, taskId: 16, agent: 'Codex', startedAt: at(27, 9), endedAt: null },
  { id: 4, taskId: 9, agent: 'Claude Code', startedAt: at(26, 12), endedAt: at(26, 19) },
]
