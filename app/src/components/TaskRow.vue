<script setup lang="ts">
import { computed } from 'vue'
import type { Task } from '@/types/models'
import { isInProgress, progress } from '@/lib/derive'
import { formatDateTime } from '@/lib/format'
import { statusHue, statusLabel } from '@/lib/labels'
import { useTickieStore } from '@/stores/tickie'
import TaskTypeBadge from '@/components/TaskTypeBadge.vue'
import TurnIndicator from '@/components/TurnIndicator.vue'

const props = defineProps<{ task: Task }>()
const store = useTickieStore()

// Tasks without test cases (every Design task, and Build tasks before drafting) show no progress
const testProgress = computed(() => {
  const { passed, total } = progress(props.task, store.testCases)
  return total === 0 ? '' : `${passed}/${total}`
})

const plannedStart = computed(() => formatDateTime(props.task.plannedStart))

const hue = computed(() => statusHue[props.task.status])
</script>

<template>
  <div class="row" :class="{ 'in-progress': isInProgress(task) }">
    <TaskTypeBadge :type="task.type" />
    <div class="main">
      <span class="title">{{ task.title }}</span>
      <span v-if="task.isUnplanned" class="unplanned">Unplanned</span>
    </div>
    <div class="details">
      <span
        class="status"
        :class="{ neutral: hue === null }"
        :style="hue === null ? undefined : { '--hue': hue }"
        >{{ statusLabel[task.status] }}</span
      >
      <span class="turn"><TurnIndicator :task="task" /></span>
      <span class="muted progress">{{ testProgress }}</span>
      <span class="muted start">{{ plannedStart }}</span>
    </div>
  </div>
</template>

<style scoped>
.row {
  display: grid;
  grid-template-columns: 5.5rem minmax(0, 1fr) 11rem 7.5rem 3rem 7.5rem;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 1rem;
  font-size: 0.9rem;
}

.main {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  column-gap: 0.5rem;
  min-width: 0;
}

.title {
  font-weight: 600;
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

/* Started but not finished: tinted so it stands out from Todo and finished tasks */
.row.in-progress {
  background: var(--color-active-bg);
  box-shadow: inset 3px 0 var(--color-accent);
}

.row.in-progress .title {
  color: var(--color-active-text);
}

/* Each status has its own hue (set inline as --hue); light and dark tints come from base.css */
.status {
  justify-self: start;
  padding: 0.05rem 0.5rem;
  border: 1px solid hsl(var(--hue) var(--status-border));
  border-radius: 4px;
  background: hsl(var(--hue) var(--status-bg));
  color: hsl(var(--hue) var(--status-text));
  font-size: 0.8rem;
  font-weight: 600;
  white-space: nowrap;
}

.status.neutral {
  border-color: var(--color-border);
  background: var(--color-band);
  color: var(--color-text-muted);
}

/* On wide windows the details sit in the row's own grid columns */
.details {
  display: contents;
}

.progress,
.start {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

/* Narrow list (small window, or the task panel is open): stack the details under the title */
@container (max-width: 720px) {
  .row {
    grid-template-columns: 5.5rem minmax(0, 1fr);
  }

  .details {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.6rem;
    grid-column: 2;
  }

  .turn:empty,
  .progress:empty {
    display: none;
  }
}
</style>
