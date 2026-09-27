<script setup lang="ts">
import { computed } from 'vue'
import type { Task } from '@/types/models'
import { isAgentRunning, isWaitingOnMe } from '@/lib/derive'
import { useTickieStore } from '@/stores/tickie'

const props = defineProps<{ task: Task }>()
const store = useTickieStore()

const running = computed(() => isAgentRunning(props.task, store.agentRuns))
const waiting = computed(() => isWaitingOnMe(props.task, store.agentRuns))
</script>

<template>
  <span v-if="running" class="running" title="An agent is working on this task">
    <span class="light" />
    Agent running
  </span>
  <span v-else-if="waiting" class="waiting">Your turn</span>
</template>

<style scoped>
.running,
.waiting {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.8rem;
  white-space: nowrap;
}

.running {
  color: var(--color-running);
}

.light {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 50%;
  background: var(--color-running);
  animation: pulse 1.4s ease-in-out infinite;
}

.waiting {
  padding: 0.05rem 0.5rem;
  border-radius: 999px;
  background: var(--color-waiting-bg);
  color: var(--color-waiting);
  font-weight: 600;
}

@keyframes pulse {
  50% {
    opacity: 0.3;
    transform: scale(0.8);
  }
}
</style>
