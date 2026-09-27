<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { ChevronRight, Plus } from 'lucide-vue-next'
import type { Id } from '@/types/models'
import { useTickieStore } from '@/stores/tickie'
import TaskRow from '@/components/TaskRow.vue'
import TaskPanel from '@/components/TaskPanel.vue'
import NewTaskDialog from '@/components/NewTaskDialog.vue'
import ChatWidget from '@/components/ChatWidget.vue'

const props = defineProps<{ id: string }>()
const store = useTickieStore()
const route = useRoute()
const router = useRouter()

const project = computed(() => store.projects.find((p) => p.id === Number(props.id)) ?? null)
const phases = computed(() => (project.value ? store.projectPhases(project.value.id) : []))
const currentPhase = computed(() => phases.value.find((item) => item.isCurrent) ?? null)

// The open task lives in the URL (?task=5), so a reload keeps the panel open
const selectedTask = computed(
  () =>
    phases.value
      .flatMap((item) => [...item.open, ...item.finished])
      .find((task) => task.id === Number(route.query.task)) ?? null,
)

const closePanel = () => router.replace({ query: {} })

const onKeydown = (event: KeyboardEvent) => {
  if (event.key === 'Escape' && selectedTask.value) closePanel()
}
onMounted(() => window.addEventListener('keydown', onKeydown))
onUnmounted(() => window.removeEventListener('keydown', onKeydown))

// The phase a new task is being added to; null while the dialog is closed
const addingToPhaseId = ref<Id | null>(null)
// Open the new task straight away so it's easy to find
const onTaskAdded = (id: Id) => router.replace({ query: { task: id } })

// Keep a folded section open when the selected task is in it (e.g. after a reload)
const holdsSelected = (tasks: { id: Id }[]) =>
  tasks.some((task) => task.id === selectedTask.value?.id)

// Phases that haven't started fold away; the current phase always stays open
const folds = (item: { isNotStarted: boolean; isCurrent: boolean; open: unknown[] }) =>
  item.isNotStarted && !item.isCurrent && item.open.length > 0
</script>

<template>
  <main :class="{ 'with-panel': selectedTask }">
    <div class="list">
      <RouterLink to="/" class="back">← All projects</RouterLink>

      <template v-if="project">
        <h1>{{ project.name }}</h1>
        <p class="muted">{{ project.description }}</p>
        <p v-if="currentPhase" class="now">
          Now in <strong>Phase {{ currentPhase.number }} · {{ currentPhase.phase.name }}</strong>
        </p>

        <!-- Phases that haven't started are folded into a <details>; the rest stay open -->
        <component
          :is="folds(item) ? 'details' : 'section'"
          v-for="item in phases"
          :key="item.phase.id"
          class="phase"
          :class="{ current: item.isCurrent, done: item.isDone }"
          :open="folds(item) && holdsSelected(item.open) ? true : undefined"
        >
          <component :is="folds(item) ? 'summary' : 'div'" class="phase-head">
            <ChevronRight v-if="folds(item)" class="chevron" :size="18" aria-hidden="true" />
            <h2>
              <span class="number">Phase {{ item.number }}</span>
              {{ item.phase.name }}
              <span v-if="item.isCurrent" class="badge current-badge">Current</span>
              <span v-else-if="item.isDone" class="badge done-badge">Done</span>
            </h2>
            <span v-if="folds(item)" class="muted summary-note">
              {{ item.open.length }} {{ item.open.length === 1 ? 'task' : 'tasks' }}, not started
            </span>
            <!-- Every phase takes new tasks, even a finished one (bugs or small features found later) -->
            <button
              type="button"
              class="add-task"
              :aria-label="`Add a task to Phase ${item.number}`"
              title="Add a task"
              @click.prevent.stop="addingToPhaseId = item.phase.id"
            >
              <Plus :size="16" aria-hidden="true" />
            </button>
          </component>

          <ul v-if="item.open.length" class="tasks">
            <li v-for="task in item.open" :key="task.id">
              <RouterLink
                :to="{ query: { task: task.id } }"
                replace
                class="row-link"
                :class="{ selected: task.id === selectedTask?.id }"
              >
                <TaskRow :task="task" />
              </RouterLink>
            </li>
          </ul>

          <details
            v-if="item.finished.length"
            class="finished"
            :open="holdsSelected(item.finished) || undefined"
          >
            <summary>Finished ({{ item.finished.length }})</summary>
            <ul class="tasks">
              <li v-for="task in item.finished" :key="task.id">
                <RouterLink
                  :to="{ query: { task: task.id } }"
                  replace
                  class="row-link"
                  :class="{ selected: task.id === selectedTask?.id }"
                >
                  <TaskRow :task="task" />
                </RouterLink>
              </li>
            </ul>
          </details>

          <p v-if="!item.open.length && !item.finished.length" class="muted">No tasks yet.</p>
        </component>

        <p v-if="!phases.length" class="empty muted">No phases yet.</p>
      </template>
      <p v-else>Project not found.</p>
    </div>

    <NewTaskDialog
      :phase-id="addingToPhaseId"
      @close="addingToPhaseId = null"
      @added="onTaskAdded"
    />
    <ChatWidget v-if="project" :project-id="project.id" />
    <TaskPanel v-if="selectedTask" :key="selectedTask.id" :task="selectedTask" @close="closePanel" />
  </main>
</template>

<style scoped>
/* Wide windows: list and panel side by side */
main.with-panel {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 26rem;
  align-items: start;
  gap: 1.5rem;
}

main.with-panel :deep(.panel) {
  position: sticky;
  top: 1rem;
  max-height: calc(100vh - 2rem);
  overflow-y: auto;
}

/* Narrow windows: the panel slides over the list from the right */
@media (max-width: 1000px) {
  main.with-panel {
    display: block;
  }

  main.with-panel :deep(.panel) {
    position: fixed;
    top: 0;
    right: 0;
    bottom: 0;
    width: min(26rem, 100vw);
    max-height: none;
    border-radius: 0;
    box-shadow: -8px 0 24px rgba(0, 0, 0, 0.15);
  }
}

.back {
  color: var(--color-text-muted);
  font-size: 0.9rem;
  text-decoration: none;
}

h1 {
  margin-top: 0.5rem;
  font-size: 1.8rem;
  font-weight: 600;
}

.muted {
  color: var(--color-text-muted);
}

.now {
  margin-top: 0.75rem;
}

.phase {
  margin-top: 2rem;
}

.phase-head {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 0.6rem;
}

summary.phase-head {
  list-style: none;
  cursor: pointer;
}

summary.phase-head::-webkit-details-marker {
  display: none;
}

.chevron {
  flex: none;
  margin-left: -0.2rem;
  color: var(--color-text-muted);
  transition: transform 0.15s;
}

details[open] > summary .chevron {
  transform: rotate(90deg);
}

details.phase:not([open]) > .phase-head {
  margin-bottom: 0;
}

.add-task {
  display: grid;
  place-items: center;
  width: 1.75rem;
  height: 1.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text-muted);
  cursor: pointer;
}

.add-task:hover {
  border-color: var(--color-border-strong);
  color: var(--color-text);
}

.summary-note {
  font-size: 0.85rem;
}

.phase h2 {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 1.1rem;
  font-weight: 700;
}

.number {
  color: var(--color-text-muted);
  font-weight: 600;
}

.phase.current .number {
  color: var(--color-accent);
}

.phase.done h2 {
  color: var(--color-text-muted);
}

.badge {
  padding: 0.05rem 0.5rem;
  border-radius: 999px;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.current-badge {
  background: var(--color-accent);
  color: #fff;
}

.done-badge {
  background: var(--color-band);
  color: var(--color-text-muted);
}

.tasks {
  container-type: inline-size;
  padding: 0;
  list-style: none;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  overflow: hidden;
}

.phase.current > .tasks {
  border-color: var(--color-accent);
}

.tasks li + li {
  border-top: 1px solid var(--color-border);
}

.row-link {
  display: block;
  color: inherit;
  text-decoration: none;
}

.row-link:hover {
  background: var(--color-background-soft);
}

.row-link.selected {
  outline: 2px solid var(--color-accent);
  outline-offset: -2px;
}

.empty {
  margin-top: 1.25rem;
}

.finished {
  margin-top: 0.6rem;
}

.finished summary {
  color: var(--color-text-muted);
  font-size: 0.9rem;
  cursor: pointer;
}

.finished .tasks {
  margin-top: 0.5rem;
  opacity: 0.75;
}
</style>
