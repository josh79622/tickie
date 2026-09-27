import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import * as mock from '@/mock/data'
import { currentPhase, currentTask, isFinished, isPhaseDone, tasksInTimeOrder } from '@/lib/derive'
import type {
  ChatAgent,
  ChatMessage,
  ChatSession,
  Id,
  IsoDateTime,
  QaTask,
  Task,
} from '@/types/models'

// One shared whiteboard for every screen. Later the refs get filled from the C# API instead of mock data.
export const useTickieStore = defineStore('tickie', () => {
  const projects = ref(structuredClone(mock.projects))
  const phases = ref(structuredClone(mock.phases))
  const tasks = ref(structuredClone(mock.tasks))
  const taskDependencies = ref(structuredClone(mock.taskDependencies))
  const testCases = ref(structuredClone(mock.testCases))
  const statusHistory = ref(structuredClone(mock.statusHistory))
  const agentRuns = ref(structuredClone(mock.agentRuns))
  const folders = ref(structuredClone(mock.folders))
  const chatSessions = ref<ChatSession[]>([])
  const chatMessages = ref<ChatMessage[]>([])

  const projectList = computed(() =>
    projects.value
      .filter((project) => project.removedAt === null)
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((project) => {
        const task = currentTask(project.id, phases.value, tasks.value)
        return {
          project,
          currentTask: task,
          // The phase of the task shown, which is the current phase unless that phase has nothing left
          currentPhase: phases.value.find((phase) => phase.id === task?.phaseId) ?? null,
          hasTasks: phases.value.some(
            (phase) =>
              phase.projectId === project.id && tasks.value.some((t) => t.phaseId === phase.id),
          ),
        }
      }),
  )

  // A project's phases in order, each with its tasks in time order split into open and finished
  const projectPhases = (projectId: Id) => {
    const current = currentPhase(projectId, phases.value, tasks.value)
    return phases.value
      .filter((phase) => phase.projectId === projectId)
      .sort((a, b) => a.order - b.order)
      .map((phase, index) => {
        const phaseTasks = tasksInTimeOrder(projectId, phases.value, tasks.value).filter(
          (task) => task.phaseId === phase.id,
        )
        return {
          phase,
          number: index + 1,
          isCurrent: current?.id === phase.id,
          isDone: isPhaseDone(phase, tasks.value),
          // Nothing in it has moved past Todo yet
          isNotStarted: phaseTasks.every((task) => task.status === 'Todo'),
          open: phaseTasks.filter((task) => !isFinished(task)),
          finished: phaseTasks.filter((task) => isFinished(task)),
        }
      })
  }

  // Opens a task by hand (Build, Design or Debug; QA tasks are never opened by hand)
  const addTask = (input: {
    phaseId: Id
    type: 'Build' | 'Design' | 'Debug'
    title: string
    tagline: string
    assignedAgent: string
    plannedStart: IsoDateTime
    estimatedHours: number
    prerequisiteIds: Id[]
    sourceTaskId: Id | null
  }) => {
    const phase = phases.value.find((p) => p.id === input.phaseId)
    const project = projects.value.find((p) => p.id === phase?.projectId)
    if (!phase || !project) return
    const id = Math.max(0, ...tasks.value.map((t) => t.id)) + 1
    const base = {
      id,
      phaseId: phase.id,
      order: Math.max(0, ...tasks.value.filter((t) => t.phaseId === phase.id).map((t) => t.order)) + 1,
      title: input.title,
      tagline: input.tagline,
      assignedAgent: input.assignedAgent,
      plannedStart: input.plannedStart,
      estimatedHours: input.estimatedHours,
      // Anything added after the baseline was approved is unplanned
      isUnplanned: project.baselineFrozenAt !== null,
    }
    const task: Task =
      input.type === 'Debug'
        ? { ...base, type: 'Debug', status: 'Todo', sourceTaskId: input.sourceTaskId! }
        : { ...base, type: input.type, status: 'Todo' }
    tasks.value.push(task)
    taskDependencies.value.push(
      ...input.prerequisiteIds.map((prerequisiteTaskId) => ({ taskId: id, prerequisiteTaskId })),
    )
    const now = new Date().toISOString()
    const nextHistoryId = () => Math.max(0, ...statusHistory.value.map((entry) => entry.id)) + 1
    statusHistory.value.push({
      id: nextHistoryId(),
      taskId: id,
      fromStatus: null,
      toStatus: 'Todo',
      at: now,
      note: null,
    })
    // A new task in a finished phase reopens it: its QA goes back to waiting for the new work
    const qa = tasks.value.find(
      (t): t is QaTask => t.phaseId === phase.id && t.type === 'QA' && t.status === 'Done',
    )
    if (qa) {
      qa.status = 'WaitingForDev'
      statusHistory.value.push({
        id: nextHistoryId(),
        taskId: qa.id,
        fromStatus: 'Done',
        toStatus: 'WaitingForDev',
        at: now,
        note: `New task added: ${task.title}`,
      })
    }
    return id
  }

  // A project's sessions with one agent, oldest first; the last one is the current session
  const chatSessionsFor = (projectId: Id, agent: ChatAgent) =>
    chatSessions.value
      .filter((session) => session.projectId === projectId && session.agent === agent)
      .sort((a, b) => a.startedAt.localeCompare(b.startedAt) || a.id - b.id)

  const startChatSession = (projectId: Id, agent: ChatAgent): Id => {
    const id = chatSessions.value.length + 1
    chatSessions.value.push({ id, projectId, agent, startedAt: new Date().toISOString() })
    return id
  }

  const chatMessagesIn = (sessionId: Id) =>
    chatMessages.value.filter((message) => message.sessionId === sessionId)

  const addChatMessage = (sessionId: Id, fromMe: boolean, text: string) => {
    chatMessages.value.push({
      id: chatMessages.value.length + 1,
      sessionId,
      fromMe,
      text,
      at: new Date().toISOString(),
    })
  }

  // Rewrites sortOrder as 1, 2, 3… in the given order
  const renumber = (orderedIds: Id[]) => {
    orderedIds.forEach((id, index) => {
      const project = projects.value.find((p) => p.id === id)
      if (project) project.sortOrder = index + 1
    })
  }

  const projectInFolder = (folderPath: string) =>
    projects.value.find((p) => p.folderPath === folderPath) ?? null

  // Brings a removed project back to the top of the home page, with everything under it
  const restoreProject = (id: Id) => {
    const project = projects.value.find((p) => p.id === id)
    if (!project) return
    const shown = projectList.value.map((item) => item.project.id)
    project.removedAt = null
    renumber([id, ...shown])
  }

  const addProject = (name: string, description: string, folderPath: string) => {
    const shown = projectList.value.map((item) => item.project.id)
    const id = Math.max(0, ...projects.value.map((p) => p.id)) + 1
    projects.value.push({
      id,
      name,
      description,
      folderPath,
      sortOrder: 0,
      baselineFrozenAt: null,
      removedAt: null,
    })
    // New projects go to the top
    renumber([id, ...shown])
  }

  // Stands in for Tauri creating a real folder on disk
  const createFolder = (folderPath: string) => {
    if (!folders.value.includes(folderPath)) folders.value.push(folderPath)
  }

  const moveProject = (id: Id, toIndex: number) => {
    const ids = projectList.value.map((item) => item.project.id).filter((other) => other !== id)
    ids.splice(toIndex, 0, id)
    renumber(ids)
  }

  // Only hides the project from the home page; its phases, tasks and history stay
  const removeProject = (id: Id) => {
    const project = projects.value.find((p) => p.id === id)
    if (!project) return
    project.removedAt = new Date().toISOString()
    renumber(projectList.value.map((item) => item.project.id))
  }

  return {
    projects,
    phases,
    tasks,
    taskDependencies,
    testCases,
    statusHistory,
    agentRuns,
    folders,
    chatSessions,
    chatMessages,
    projectList,
    projectPhases,
    addTask,
    chatSessionsFor,
    startChatSession,
    chatMessagesIn,
    addChatMessage,
    projectInFolder,
    restoreProject,
    addProject,
    createFolder,
    moveProject,
    removeProject,
  }
})
