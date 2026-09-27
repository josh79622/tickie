<script setup lang="ts">
import { Bug, Hammer, Palette, ShieldCheck } from 'lucide-vue-next'
import type { TaskType } from '@/types/models'

defineProps<{ type: TaskType }>()

// One icon per task type so the four kinds can be told apart at a glance
const icons = { Build: Hammer, Debug: Bug, Design: Palette, QA: ShieldCheck }
</script>

<template>
  <span class="type">
    <component :is="icons[type]" :size="14" :stroke-width="2.25" aria-hidden="true" />
    {{ type }}
  </span>
</template>

<style scoped>
.type {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  justify-self: start;
  padding: 0.05rem 0.45rem;
  border-radius: 4px;
  background: var(--color-band);
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}
</style>
