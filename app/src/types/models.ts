// Shapes of the data the C# manager will return. Mirrors "Data model" in docs/brief.md.
// Derived values (progress, actual times, current task, waiting on me) are not here on purpose.

export type Id = number

// JSON has no date type, so timestamps travel as ISO 8601 strings.
export type IsoDateTime = string

export interface Project {
  id: Id
  name: string
  description: string
  baselineFrozenAt: IsoDateTime | null
}

export interface Phase {
  id: Id
  projectId: Id
  name: string
  order: number
}

export type BuildStatus =
  | 'Todo'
  | 'TestCases'
  | 'WritingTests'
  | 'Working'
  | 'Testing'
  | 'Fixing'
  | 'NeedsDecision'
  | 'AwaitingConfirmation'
  | 'Done'
  | 'Cancelled'

export type DesignStatus = 'Todo' | 'Demo' | 'Adopted' | 'Rejected'

export type QaStatus =
  'Todo' | 'TestCases' | 'WritingTests' | 'WaitingForDev' | 'Running' | 'Analysing' | 'Done'

export type TaskStatus = BuildStatus | DesignStatus | QaStatus

interface TaskBase {
  id: Id
  phaseId: Id
  order: number
  title: string
  tagline: string
  assignedAgent: string
  plannedStart: IsoDateTime
  estimatedHours: number
  isUnplanned: boolean
}

export interface BuildTask extends TaskBase {
  type: 'Build'
  status: BuildStatus
}

export interface DebugTask extends TaskBase {
  type: 'Debug'
  status: BuildStatus
  sourceTaskId: Id
}

export interface DesignTask extends TaskBase {
  type: 'Design'
  status: DesignStatus
}

export interface QaTask extends TaskBase {
  type: 'QA'
  status: QaStatus
}

export type Task = BuildTask | DebugTask | DesignTask | QaTask

export type TaskType = Task['type']

export interface TestCase {
  id: Id
  taskId: Id
  content: string
  isLocked: boolean
  // null until the tests have run at least once
  passedOnLastRun: boolean | null
}

export interface StatusHistoryEntry {
  id: Id
  taskId: Id
  // null for the entry created when the task is opened
  fromStatus: TaskStatus | null
  toStatus: TaskStatus
  at: IsoDateTime
  // e.g. "Resumed from commit a1b2c3d", or my note on Retry with note / Fix
  note: string | null
}

export interface AgentRun {
  id: Id
  taskId: Id
  agent: string
  startedAt: IsoDateTime
  // null while the agent is still running
  endedAt: IsoDateTime | null
}

export interface TaskDependency {
  taskId: Id
  prerequisiteTaskId: Id
}
