// Values the brief says are derived, never stored. Pure functions so they can move to C# later unchanged.

import type {
  AgentRun,
  Id,
  IsoDateTime,
  Phase,
  StatusHistoryEntry,
  Task,
  TaskStatus,
  TestCase,
} from '@/types/models'

const finishedStatuses: TaskStatus[] = ['Done', 'Cancelled', 'Adopted', 'Rejected']
const waitingOnOtherTasks: TaskStatus[] = ['Todo', 'WaitingForDev']

export const isFinished = (task: Task): boolean => finishedStatuses.includes(task.status)

// In progress: started (past Todo) but not finished yet
export const isInProgress = (task: Task): boolean => task.status !== 'Todo' && !isFinished(task)

export const isAgentRunning = (task: Task, runs: AgentRun[]): boolean =>
  runs.some((run) => run.taskId === task.id && run.endedAt === null)

export const isWaitingOnMe = (task: Task, runs: AgentRun[]): boolean =>
  !isAgentRunning(task, runs) && !isFinished(task) && !waitingOnOtherTasks.includes(task.status)

// Tasks of a project in time order: earliest planned start first; ties go to phase order, then task order.
export const tasksInTimeOrder = (projectId: Id, phases: Phase[], tasks: Task[]): Task[] => {
  const phaseOrder = new Map(
    phases.filter((phase) => phase.projectId === projectId).map((phase) => [phase.id, phase.order]),
  )
  return tasks
    .filter((task) => phaseOrder.has(task.phaseId))
    .sort(
      (a, b) =>
        a.plannedStart.localeCompare(b.plannedStart) ||
        phaseOrder.get(a.phaseId)! - phaseOrder.get(b.phaseId)! ||
        a.order - b.order,
    )
}

// A phase is done when its QA task is Done
export const isPhaseDone = (phase: Phase, tasks: Task[]): boolean =>
  tasks.some((task) => task.phaseId === phase.id && task.type === 'QA' && task.status === 'Done')

// A phase has started once any of its tasks has moved past Todo
const hasStarted = (phase: Phase, tasks: Task[]): boolean =>
  tasks.some((task) => task.phaseId === phase.id && task.status !== 'Todo')

// How far the project has got: the last phase that has started, or the one after it once it's done.
// Reopening an earlier phase (a task added after its QA was Done) doesn't move this back.
export const currentPhase = (projectId: Id, phases: Phase[], tasks: Task[]): Phase | null => {
  const ordered = phases
    .filter((phase) => phase.projectId === projectId)
    .sort((a, b) => a.order - b.order)
  let lastStarted = -1
  ordered.forEach((phase, index) => {
    if (hasStarted(phase, tasks)) lastStarted = index
  })
  if (lastStarted === -1) return ordered[0] ?? null
  const phase = ordered[lastStarted]!
  return isPhaseDone(phase, tasks) ? (ordered[lastStarted + 1] ?? null) : phase
}

// The first unfinished task, in time order, of the current phase. If that phase has nothing
// left (or every phase is done), fall back to the earliest unfinished task anywhere, e.g. in a reopened phase.
export const currentTask = (projectId: Id, phases: Phase[], tasks: Task[]): Task | null => {
  const unfinished = tasksInTimeOrder(projectId, phases, tasks).filter((task) => !isFinished(task))
  const phase = currentPhase(projectId, phases, tasks)
  return unfinished.find((task) => task.phaseId === phase?.id) ?? unfinished[0] ?? null
}

export const progress = (task: Task, testCases: TestCase[]): { passed: number; total: number } => {
  const own = testCases.filter((testCase) => testCase.taskId === task.id)
  return {
    passed: own.filter((testCase) => testCase.passedOnLastRun === true).length,
    total: own.length,
  }
}

// Actual start is the first move out of Todo; actual end is reaching a finished status.
export const actualTimes = (
  task: Task,
  history: StatusHistoryEntry[],
): { start: IsoDateTime | null; end: IsoDateTime | null } => {
  const own = history.filter((entry) => entry.taskId === task.id).sort((a, b) => a.at.localeCompare(b.at))
  return {
    start: own.find((entry) => entry.fromStatus === 'Todo')?.at ?? null,
    end: isFinished(task) ? ([...own].reverse().find((entry) => finishedStatuses.includes(entry.toStatus))?.at ?? null) : null,
  }
}

// Coding stops and waits for my decision after this many failures in a row
export const failureLimit = 3

// Failures in a row since the last decision I made in Needs decision (every decision resets the count)
export const consecutiveFailures = (task: Task, history: StatusHistoryEntry[]): number => {
  const own = history.filter((entry) => entry.taskId === task.id).sort((a, b) => a.at.localeCompare(b.at) || a.id - b.id)
  let count = 0
  for (const entry of own) {
    if (entry.fromStatus === 'NeedsDecision') count = 0
    else if (entry.fromStatus === 'Testing' && entry.toStatus === 'Fixing') count++
  }
  return count
}
