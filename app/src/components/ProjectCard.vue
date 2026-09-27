<script setup lang="ts">
import { RouterLink } from 'vue-router'
import type { Phase, Project, Task } from '@/types/models'
import { statusLabel } from '@/lib/labels'
import TurnIndicator from '@/components/TurnIndicator.vue'

defineProps<{
  project: Project
  currentPhase: Phase | null
  currentTask: Task | null
  hasTasks: boolean
}>()

const emit = defineEmits<{ remove: [] }>()
</script>

<template>
  <!-- draggable=false on the link so the whole card drags, not the link's URL -->
  <RouterLink
    :to="{ name: 'project', params: { id: project.id } }"
    class="project"
    draggable="false"
  >
    <div class="head">
      <div>
        <h2>{{ project.name }}</h2>
        <p class="muted">{{ project.description }}</p>
      </div>
      <button
        type="button"
        class="remove"
        :aria-label="`Remove ${project.name} from home`"
        title="Remove from home"
        @click.prevent.stop="emit('remove')"
      >
        ×
      </button>
    </div>

    <div v-if="currentTask" class="current">
      <span class="label">Now</span>
      <span class="muted">{{ currentPhase?.name }}</span>
      <span class="muted">/</span>
      <span class="task">{{ currentTask.title }}</span>
      <span class="status">{{ statusLabel[currentTask.status] }}</span>
      <TurnIndicator :task="currentTask" />
    </div>
    <p v-else-if="hasTasks" class="current muted">All phases done</p>
    <p v-else class="current muted">No tasks yet</p>
  </RouterLink>
</template>

<style scoped>
.project {
  display: block;
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  color: inherit;
  text-decoration: none;
}

.project:hover {
  border-color: var(--color-border-strong);
}

.head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  padding: 1rem 1.25rem;
}

h2 {
  font-size: 1.3rem;
  font-weight: 700;
  line-height: 1.3;
}

.head p {
  margin-top: 0.2rem;
}

.muted {
  color: var(--color-text-muted);
}

.remove {
  flex: none;
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

.remove:hover {
  background: var(--color-danger-bg);
  color: var(--color-danger);
}

.current {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.6rem;
  padding: 0.6rem 1.25rem;
  border-top: 1px solid var(--color-border);
  background: var(--color-band);
  font-size: 0.9rem;
}

.label {
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--color-text-muted);
}

.task {
  font-weight: 600;
}

.status {
  padding: 0.05rem 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  font-size: 0.8rem;
}
</style>
