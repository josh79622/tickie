<script setup lang="ts">
import { theme, toggleTheme } from '@/lib/theme'
</script>

<template>
  <button type="button" class="toggle" @click="toggleTheme">
    {{ theme === 'light' ? 'Dark mode' : 'Light mode' }}
  </button>
</template>

<style scoped>
.toggle {
  padding: 0.3rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  font: inherit;
  font-size: 0.85rem;
  cursor: pointer;
}

.toggle:hover {
  background: var(--color-background-soft);
}
</style>
