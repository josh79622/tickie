<script setup lang="ts">
import { computed, ref } from 'vue'
import { useTickieStore } from '@/stores/tickie'
import type { Id } from '@/types/models'
import ProjectCard from '@/components/ProjectCard.vue'
import BaseDialog from '@/components/BaseDialog.vue'
import NewProjectDialog from '@/components/NewProjectDialog.vue'

const store = useTickieStore()

const creating = ref(false)

// Remove from the home page (nothing is deleted)
const removingId = ref<Id | null>(null)
const removingProject = computed(
  () => store.projects.find((project) => project.id === removingId.value) ?? null,
)

const confirmRemove = () => {
  if (removingId.value !== null) store.removeProject(removingId.value)
  removingId.value = null
}

// Drag to reorder
const draggingId = ref<Id | null>(null)
const dropIndex = ref<number | null>(null)

const onDragStart = (event: DragEvent, id: Id) => {
  draggingId.value = id
  event.dataTransfer?.setData('text/plain', String(id))
  if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move'
}

const onDragOver = (event: DragEvent, index: number) => {
  if (draggingId.value === null) return
  event.preventDefault()
  const target = event.currentTarget as HTMLElement
  const { top, height } = target.getBoundingClientRect()
  // Upper half of a card drops before it, lower half after it
  dropIndex.value = event.clientY < top + height / 2 ? index : index + 1
}

const onDrop = () => {
  if (draggingId.value === null || dropIndex.value === null) return
  const fromIndex = store.projectList.findIndex((item) => item.project.id === draggingId.value)
  // Removing the dragged card first shifts everything after it up by one
  const toIndex = dropIndex.value > fromIndex ? dropIndex.value - 1 : dropIndex.value
  store.moveProject(draggingId.value, toIndex)
  onDragEnd()
}

const onDragEnd = () => {
  draggingId.value = null
  dropIndex.value = null
}
</script>

<template>
  <main>
    <div class="title-row">
      <h1>Projects</h1>
      <button
        type="button"
        class="add"
        title="New project"
        aria-label="New project"
        @click="creating = true"
      >
        +
      </button>
    </div>

    <ul class="projects" @dragover.prevent @drop.prevent="onDrop">
      <li
        v-for="(item, index) in store.projectList"
        :key="item.project.id"
        draggable="true"
        :class="{
          dragging: draggingId === item.project.id,
          'drop-before': draggingId !== null && dropIndex === index,
          'drop-after':
            draggingId !== null &&
            dropIndex === index + 1 &&
            index === store.projectList.length - 1,
        }"
        @dragstart="onDragStart($event, item.project.id)"
        @dragover="onDragOver($event, index)"
        @dragend="onDragEnd"
      >
        <ProjectCard v-bind="item" @remove="removingId = item.project.id" />
      </li>
    </ul>
    <p v-if="store.projectList.length === 0" class="muted">No projects yet. Press + to add one.</p>

    <NewProjectDialog :open="creating" @close="creating = false" />

    <BaseDialog
      :open="removingProject !== null"
      title="Remove from home?"
      @close="removingId = null"
    >
      <p>
        <strong>{{ removingProject?.name }}</strong> will be hidden from the home page. Its phases,
        tasks and history are kept. To bring it back, add its folder again with +.
      </p>
      <div class="actions">
        <button type="button" @click="removingId = null">Cancel</button>
        <button type="button" class="danger" @click="confirmRemove">Remove</button>
      </div>
    </BaseDialog>
  </main>
</template>

<style scoped>
.title-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

h1 {
  font-size: 1.8rem;
  font-weight: 600;
}

.add {
  width: 2.25rem;
  height: 2.25rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 1.4rem;
  line-height: 1;
  cursor: pointer;
}

.add:hover {
  border-color: var(--color-border-strong);
}

.muted {
  color: var(--color-text-muted);
}

.projects {
  list-style: none;
  padding: 0;
  margin-top: 1.5rem;
  display: grid;
  gap: 0.75rem;
}

.projects li {
  position: relative;
  cursor: grab;
}

.projects li.dragging {
  opacity: 0.4;
}

/* A line showing where the dragged card will land */
.projects li.drop-before::before,
.projects li.drop-after::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  height: 3px;
  border-radius: 2px;
  background: var(--color-accent);
}

.projects li.drop-before::before {
  top: -0.45rem;
}

.projects li.drop-after::after {
  bottom: -0.45rem;
}
</style>
