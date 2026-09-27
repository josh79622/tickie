<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { Task } from '@/types/models'
import { actualTimes, consecutiveFailures, failureLimit, isFinished } from '@/lib/derive'
import { formatDateTime } from '@/lib/format'
import { statusLabel } from '@/lib/labels'
import { useTickieStore } from '@/stores/tickie'
import TaskTypeBadge from '@/components/TaskTypeBadge.vue'
import TurnIndicator from '@/components/TurnIndicator.vue'

const props = defineProps<{ task: Task }>()
const emit = defineEmits<{ close: [] }>()
const store = useTickieStore()

const phase = computed(() => store.phases.find((p) => p.id === props.task.phaseId) ?? null)
const actual = computed(() => actualTimes(props.task, store.statusHistory))

const prerequisites = computed(() =>
  store.taskDependencies
    .filter((dependency) => dependency.taskId === props.task.id)
    .map((dependency) => store.tasks.find((t) => t.id === dependency.prerequisiteTaskId))
    .filter((t) => t !== undefined),
)

// Debug tasks only: the task where the problem was found
const sourceTask = computed(() => {
  const task = props.task
  return task.type === 'Debug' ? (store.tasks.find((t) => t.id === task.sourceTaskId) ?? null) : null
})

const testCases = computed(() => store.testCases.filter((testCase) => testCase.taskId === props.task.id))

// Newest first, so the latest change is at the top
const history = computed(() =>
  store.statusHistory
    .filter((entry) => entry.taskId === props.task.id)
    .sort((a, b) => b.at.localeCompare(a.at) || b.id - a.id),
)

// Status buttons, Build and Debug only (the demo's finish line). Agent steps are simulated.
interface Action {
  label: string
  run: () => void
  byAgent?: boolean
  danger?: boolean
}

const prerequisitesFinished = computed(() => prerequisites.value.every((t) => isFinished(t)))
const failures = computed(() => consecutiveFailures(props.task, store.statusHistory))

const actions = computed((): Action[] => {
  const task = props.task
  if (task.type !== 'Build' && task.type !== 'Debug') return []
  const id = task.id
  const move = store.moveTask
  const list: Action[] = []
  switch (task.status) {
    case 'Todo':
      if (prerequisitesFinished.value)
        list.push({
          label: 'Start: draft test cases',
          run: () => {
            store.draftTestCases(id)
            move(id, 'TestCases')
          },
        })
      break
    case 'TestCases':
      list.push({
        label: 'Lock test cases',
        run: () => {
          store.lockTestCases(id)
          move(id, 'WritingTests')
        },
      })
      break
    case 'WritingTests':
      list.push({ label: 'Lock test code', run: () => move(id, 'Working') })
      break
    case 'Working':
      list.push({ label: 'Code written, run tests', byAgent: true, run: () => move(id, 'Testing') })
      break
    case 'Testing':
      list.push(
        {
          label: 'Tests pass',
          byAgent: true,
          run: () => {
            store.recordTestRun(id, true)
            move(id, 'AwaitingConfirmation')
          },
        },
        {
          label: 'Tests fail',
          byAgent: true,
          run: () => {
            store.recordTestRun(id, false)
            move(id, 'Fixing')
            if (consecutiveFailures(task, store.statusHistory) >= failureLimit)
              move(id, 'NeedsDecision', `Failed ${failureLimit} times in a row`)
          },
        },
      )
      break
    case 'Fixing':
      list.push({ label: 'Fix tried, run tests again', byAgent: true, run: () => move(id, 'Testing') })
      break
    case 'NeedsDecision':
      list.push(
        { label: 'Edit test cases', run: () => move(id, 'TestCases') },
        { label: 'Edit test code', run: () => move(id, 'WritingTests') },
        {
          label: 'Retry with note',
          run: () => {
            const note = window.prompt('What should the agent try?')
            if (note?.trim()) move(id, 'Working', note.trim())
          },
        },
        { label: 'Run tests (I fixed it)', run: () => move(id, 'Testing') },
      )
      break
    case 'AwaitingConfirmation':
      list.push({ label: 'Confirm done', run: () => move(id, 'Done') })
      break
  }
  if (!isFinished(task))
    list.push({
      label: 'Cancel task',
      danger: true,
      run: () => {
        if (window.confirm(`Cancel "${task.title}"? Any agent on it stops.`)) move(id, 'Cancelled')
      },
    })
  return list
})

const resultLabel = (passed: boolean | null) =>
  passed === null ? 'Not run' : passed ? 'Passed' : 'Failed'
</script>

<template>
  <aside class="panel" aria-label="Task details">
    <header>
      <div>
        <TaskTypeBadge :type="task.type" />
        <span v-if="task.isUnplanned" class="unplanned">Unplanned</span>
      </div>
      <button type="button" class="close" aria-label="Close task details" @click="emit('close')">
        ×
      </button>
    </header>

    <h2>{{ task.title }}</h2>
    <p class="muted">{{ task.tagline }}</p>

    <div class="state">
      <span class="status">{{ statusLabel[task.status] }}</span>
      <TurnIndicator :task="task" />
    </div>

    <section v-if="actions.length || (task.status === 'Todo' && !prerequisitesFinished)">
      <h3>
        Actions
        <span v-if="failures > 0 && !isFinished(task)" class="muted count">
          · {{ failures }}/{{ failureLimit }} failures in a row
        </span>
      </h3>
      <p v-if="task.status === 'Todo' && !prerequisitesFinished" class="muted">
        Can't start until every prerequisite is finished.
      </p>
      <div class="actions-list">
        <button
          v-for="action in actions"
          :key="action.label"
          type="button"
          :class="{ agent: action.byAgent, danger: action.danger }"
          @click="action.run"
        >
          <span v-if="action.byAgent" class="sim">Simulate agent</span>
          {{ action.label }}
        </button>
      </div>
    </section>

    <dl class="facts">
      <dt>Phase</dt>
      <dd>{{ phase?.name }}</dd>
      <dt>Agent</dt>
      <dd>{{ task.assignedAgent }}</dd>
      <dt>Planned start</dt>
      <dd>{{ formatDateTime(task.plannedStart) }}</dd>
      <dt>Estimated</dt>
      <dd>{{ task.estimatedHours }} h</dd>
      <dt>Actual start</dt>
      <dd>{{ actual.start ? formatDateTime(actual.start) : '—' }}</dd>
      <dt>Actual end</dt>
      <dd>{{ actual.end ? formatDateTime(actual.end) : '—' }}</dd>
      <template v-if="sourceTask">
        <dt>Found in</dt>
        <dd>
          <RouterLink :to="{ query: { task: sourceTask.id } }" replace>{{
            sourceTask.title
          }}</RouterLink>
        </dd>
      </template>
    </dl>

    <section>
      <h3>Prerequisites</h3>
      <ul v-if="prerequisites.length" class="plain">
        <li v-for="prerequisite in prerequisites" :key="prerequisite.id">
          <RouterLink :to="{ query: { task: prerequisite.id } }" replace>{{
            prerequisite.title
          }}</RouterLink>
          <span class="muted"> · {{ statusLabel[prerequisite.status] }}</span>
        </li>
      </ul>
      <p v-else class="muted">None</p>
    </section>

    <section v-if="task.type !== 'Design'">
      <h3>Test cases</h3>
      <ul v-if="testCases.length" class="plain tests">
        <li v-for="testCase in testCases" :key="testCase.id">
          <span
            class="result"
            :data-result="String(testCase.passedOnLastRun)"
            :title="resultLabel(testCase.passedOnLastRun)"
          />
          <span>{{ testCase.content }}</span>
          <span v-if="testCase.isLocked" class="muted locked">Locked</span>
        </li>
      </ul>
      <p v-else class="muted">Not drafted yet</p>
    </section>

    <section>
      <h3>History</h3>
      <ol class="plain history">
        <li v-for="entry in history" :key="entry.id">
          <span class="muted when">{{ formatDateTime(entry.at) }}</span>
          <span>
            {{
              entry.fromStatus === null
                ? `Opened as ${statusLabel[entry.toStatus]}`
                : entry.fromStatus === entry.toStatus
                  ? statusLabel[entry.toStatus]
                  : `${statusLabel[entry.fromStatus]} → ${statusLabel[entry.toStatus]}`
            }}
            <span v-if="entry.note" class="note">{{ entry.note }}</span>
          </span>
        </li>
      </ol>
    </section>
  </aside>
</template>

<style scoped>
.panel {
  display: grid;
  align-content: start;
  gap: 1rem;
  padding: 1.25rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  font-size: 0.9rem;
}

header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

header div {
  display: flex;
  gap: 0.5rem;
}

h2 {
  font-size: 1.3rem;
  font-weight: 700;
  line-height: 1.3;
}

h2 + p {
  margin-top: -0.75rem;
}

h3 {
  margin-bottom: 0.35rem;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--color-text-muted);
}

.muted {
  color: var(--color-text-muted);
}


.unplanned {
  padding: 0 0.4rem;
  border: 1px dashed var(--color-waiting);
  border-radius: 4px;
  color: var(--color-waiting);
  font-size: 0.7rem;
}

.close {
  width: 1.75rem;
  height: 1.75rem;
  border: none;
  border-radius: 6px;
  background: none;
  color: var(--color-text-muted);
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
}

.close:hover {
  background: var(--color-band);
}

.state {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.status {
  padding: 0.05rem 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  font-size: 0.8rem;
}

.count {
  text-transform: none;
  letter-spacing: 0;
}

.actions-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.actions-list button {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.35rem 0.7rem;
  border: 1px solid var(--color-accent);
  border-radius: 6px;
  background: var(--color-accent);
  color: #fff;
  font: inherit;
  font-size: 0.85rem;
  cursor: pointer;
}

/* Simulated agent steps look different so they're not mistaken for my own decisions */
.actions-list button.agent {
  border-style: dashed;
  background: var(--color-background);
  color: var(--color-accent);
}

.actions-list button.danger {
  border-color: var(--color-danger);
  background: var(--color-background);
  color: var(--color-danger);
}

.sim {
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  opacity: 0.8;
}

.facts {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 0.25rem 1rem;
}

.facts dt {
  color: var(--color-text-muted);
}

.facts dd {
  font-variant-numeric: tabular-nums;
}

.plain {
  display: grid;
  gap: 0.35rem;
  padding: 0;
  list-style: none;
}

a {
  color: var(--color-accent);
}

.tests li {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
}

.result {
  flex: none;
  width: 0.55rem;
  height: 0.55rem;
  border: 1px solid var(--color-border-strong);
  border-radius: 50%;
}

.result[data-result='true'] {
  border-color: var(--color-running);
  background: var(--color-running);
}

.result[data-result='false'] {
  border-color: var(--color-danger);
  background: var(--color-danger);
}

.locked {
  margin-left: auto;
  font-size: 0.75rem;
}

.history li {
  display: grid;
  grid-template-columns: 6.5rem 1fr;
  gap: 0.5rem;
}

.when {
  font-variant-numeric: tabular-nums;
}

.note {
  display: block;
  color: var(--color-text-muted);
  font-style: italic;
}
</style>
