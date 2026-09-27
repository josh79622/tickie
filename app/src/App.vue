<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import ThemeToggle from '@/components/ThemeToggle.vue'
</script>

<template>
  <header class="top-bar">
    <RouterLink to="/" class="brand">Tickie</RouterLink>
    <ThemeToggle />
  </header>
  <RouterView />
</template>

<style scoped>
.top-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.brand {
  font-weight: 700;
  color: inherit;
  text-decoration: none;
}
</style>
