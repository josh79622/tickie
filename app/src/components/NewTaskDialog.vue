<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { Id } from '@/types/models'
import { statusLabel } from '@/lib/labels'
import { useTickieStore } from '@/stores/tickie'
import BaseDialog from '@/components/BaseDialog.vue'
import TaskTypeBadge from '@/components/TaskTypeBadge.vue'

const props = defineProps<{ phaseId: Id | null }>()
const emit = defineEmits<{ close: []; added: [id: Id] }>()
const store = useTickieStore()

// QA tasks are never opened by hand, so only these three can be picked
const types = ['Build', 'Design', 'Debug'] as const
const agents = ['Claude Code', 'Codex']

const type = ref<(typeof types)[number]>('Build')
const title = ref('')
const tagline = ref('')
const agent = ref(agents[0]!)
const plannedStart = ref('')
const estimatedHours = ref(4)
const prerequisiteIds = ref<Id[]>([])
const sourceTaskId = ref<Id | null>(null)

// <input type="datetime-local"> wants local time as "YYYY-MM-DDTHH:mm"
const toLocalInput = (date: Date) => {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

watch(
  () => props.phaseId,
  (phaseId) => {
    if (phaseId === null) return
    type.value = 'Build'
    title.value = ''
    tagline.value = ''
    agent.value = agents[0]!
    // Defaults to the next full hour
    const next = new Date()
    next.setHours(next.getHours() + 1, 0, 0, 0)
    plannedStart.value = toLocalInput(next)
    estimatedHours.value = 4
    prerequisiteIds.value = []
    sourceTaskId.value = null
  },
)

const phase = computed(() => store.phases.find((p) => p.id === props.phaseId) ?? null)
const phaseNumber = computed(() => {
  if (!phase.value) return 0
  return store.projectPhases(phase.value.projectId).find((item) => item.phase.id === phase.value!.id)!
    .number
})

// Every task in the same project can be a prerequisite or the task a bug was found in
const projectTasks = computed(() =>
  phase.value
    ? store
        .projectPhases(phase.value.projectId)
        .flatMap((item) =>
          [...item.open, ...item.finished].map((task) => ({ task, phaseNumber: item.number })),
        )
    : [],
)

const canAdd = computed(
  () =>
    title.value.trim() !== '' &&
    plannedStart.value !== '' &&
    estimatedHours.value > 0 &&
    (type.value !== 'Debug' || sourceTaskId.value !== null),
)

const add = () => {
  if (!canAdd.value || props.phaseId === null) return
  const id = store.addTask({
    phaseId: props.phaseId,
    type: type.value,
    title: title.value.trim(),
    tagline: tagline.value.trim(),
    assignedAgent: agent.value,
    plannedStart: new Date(plannedStart.value).toISOString(),
    estimatedHours: estimatedHours.value,
    prerequisiteIds: prerequisiteIds.value,
    sourceTaskId: type.value === 'Debug' ? sourceTaskId.value : null,
  })
  if (id !== undefined) emit('added', id)
  emit('close')
}
</script>

<template>
  <BaseDialog
    :open="phaseId !== null"
    :title="`New task in Phase ${phaseNumber} · ${phase?.name ?? ''}`"
    @close="emit('close')"
  >
    <form class="form" @submit.prevent="add">
      <fieldset class="types">
        <legend>Type</legend>
        <label v-for="option in types" :key="option" :class="{ picked: type === option }">
          <input v-model="type" type="radio" name="type" :value="option" />
          <TaskTypeBadge :type="option" />
        </label>
      </fieldset>

      <label>
        Title
        <input v-model="title" type="text" required />
      </label>
      <label>
        Tagline <span class="hint">optional, one line</span>
        <input v-model="tagline" type="text" />
      </label>

      <label v-if="type === 'Debug'">
        Found in
        <select v-model="sourceTaskId" required>
          <option :value="null" disabled>Pick the task where the problem showed up</option>
          <option v-for="item in projectTasks" :key="item.task.id" :value="item.task.id">
            Phase {{ item.phaseNumber }} · {{ item.task.title }}
          </option>
        </select>
      </label>

      <div class="pair">
        <label>
          Agent
          <select v-model="agent">
            <option v-for="option in agents" :key="option">{{ option }}</option>
          </select>
        </label>
        <label>
          Estimated hours
          <input v-model.number="estimatedHours" type="number" min="0.5" step="0.5" required />
        </label>
      </div>
      <label>
        Planned start
        <input v-model="plannedStart" type="datetime-local" required />
      </label>

      <fieldset>
        <legend>Prerequisites <span class="hint">must be finished before this can start</span></legend>
        <div class="prerequisites">
          <label v-for="item in projectTasks" :key="item.task.id" class="check">
            <input v-model="prerequisiteIds" type="checkbox" :value="item.task.id" />
            <span>Phase {{ item.phaseNumber }} · {{ item.task.title }}</span>
            <span class="hint">{{ statusLabel[item.task.status] }}</span>
          </label>
          <p v-if="!projectTasks.length" class="hint">No other tasks yet.</p>
        </div>
      </fieldset>

      <div class="actions">
        <button type="button" @click="emit('close')">Cancel</button>
        <button type="submit" class="primary" :disabled="!canAdd">Add task</button>
      </div>
    </form>
  </BaseDialog>
</template>

<style scoped>
fieldset {
  display: grid;
  gap: 0.35rem;
  padding: 0;
  border: none;
  font-size: 0.9rem;
}

legend {
  margin-bottom: 0.25rem;
}

.types {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.types label {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.3rem 0.6rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  cursor: pointer;
}

.types label.picked {
  border-color: var(--color-accent);
  box-shadow: 0 0 0 1px var(--color-accent);
}

/* The radio stays focusable for the keyboard, but only the badge shows */
.types input {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}

.types label:has(input:focus-visible) {
  outline: 2px solid var(--color-accent);
  outline-offset: 2px;
}

.pair {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

select {
  padding: 0.5rem;
  border: 1px solid var(--color-border-strong);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  font: inherit;
}

.prerequisites {
  display: grid;
  gap: 0.25rem;
  max-height: 10rem;
  overflow-y: auto;
  padding: 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
}

.form .check {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
}

.check .hint {
  margin-left: auto;
  white-space: nowrap;
}
</style>
