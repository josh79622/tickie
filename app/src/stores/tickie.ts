import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import * as mock from '@/mock/data'
import { currentTask } from '@/lib/derive'
import type { Id } from '@/types/models'

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

  const projectList = computed(() =>
    projects.value
      .filter((project) => project.removedAt === null)
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((project) => {
        const task = currentTask(project.id, phases.value, tasks.value)
        return {
          project,
          currentTask: task,
          currentPhase: phases.value.find((phase) => phase.id === task?.phaseId) ?? null,
          hasTasks: phases.value.some(
            (phase) =>
              phase.projectId === project.id && tasks.value.some((t) => t.phaseId === phase.id),
          ),
        }
      }),
  )

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
    projectList,
    projectInFolder,
    restoreProject,
    addProject,
    createFolder,
    moveProject,
    removeProject,
  }
})
