// Values the brief says are derived, never stored. Pure functions so they can move to C# later unchanged.

import type { AgentRun, Id, Phase, Task, TaskStatus, TestCase } from '@/types/models'

const finishedStatuses: TaskStatus[] = ['Done', 'Cancelled', 'Adopted', 'Rejected']
const waitingOnOtherTasks: TaskStatus[] = ['Todo', 'WaitingForDev']

export const isFinished = (task: Task): boolean => finishedStatuses.includes(task.status)

export const isAgentRunning = (task: Task, runs: AgentRun[]): boolean =>
  runs.some((run) => run.taskId === task.id && run.endedAt === null)

export const isWaitingOnMe = (task: Task, runs: AgentRun[]): boolean =>
  !isAgentRunning(task, runs) && !isFinished(task) && !waitingOnOtherTasks.includes(task.status)

// Tasks of a project in planned order: phase order first, then task order within the phase.
export const tasksInPlannedOrder = (projectId: Id, phases: Phase[], tasks: Task[]): Task[] => {
  const projectPhases = phases
    .filter((phase) => phase.projectId === projectId)
    .sort((a, b) => a.order - b.order)
  return projectPhases.flatMap((phase) =>
    tasks.filter((task) => task.phaseId === phase.id).sort((a, b) => a.order - b.order),
  )
}

// The first unfinished task in planned order; its phase is the current phase.
export const currentTask = (projectId: Id, phases: Phase[], tasks: Task[]): Task | null =>
  tasksInPlannedOrder(projectId, phases, tasks).find((task) => !isFinished(task)) ?? null

export const progress = (task: Task, testCases: TestCase[]): { passed: number; total: number } => {
  const own = testCases.filter((testCase) => testCase.taskId === task.id)
  return {
    passed: own.filter((testCase) => testCase.passedOnLastRun === true).length,
    total: own.length,
  }
}
