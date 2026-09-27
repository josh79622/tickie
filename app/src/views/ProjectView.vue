<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { useTickieStore } from '@/stores/tickie'

const props = defineProps<{ id: string }>()
const store = useTickieStore()

const project = computed(() => store.projects.find((p) => p.id === Number(props.id)) ?? null)
</script>

<template>
  <main>
    <RouterLink to="/">← All projects</RouterLink>
    <h1 v-if="project">{{ project.name }}</h1>
    <p v-else>Project not found.</p>
    <p>The project page comes next.</p>
  </main>
</template>
