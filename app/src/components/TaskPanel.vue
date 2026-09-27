<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { Task } from '@/types/models'
import { actualTimes } from '@/lib/derive'
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
    .sort((a, b) => b.at.localeCompare(a.at)),
)

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
