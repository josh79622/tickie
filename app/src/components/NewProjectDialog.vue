<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useTickieStore } from '@/stores/tickie'
import { folderName, folderSlug, nameFromFolder } from '@/lib/folders'
import BaseDialog from '@/components/BaseDialog.vue'
import FolderPicker from '@/components/FolderPicker.vue'

const props = defineProps<{ open: boolean }>()
const emit = defineEmits<{ close: [] }>()

const store = useTickieStore()

type Step = 'start' | 'pickProject' | 'restore' | 'existingDetails' | 'newDetails' | 'pickLocation'

const step = ref<Step>('start')
const name = ref('')
const description = ref('')
// Existing folder: the project folder itself. New folder: where the new folder goes.
const folder = ref('')
const location = ref('~/Projects')

watch(
  () => props.open,
  (open) => {
    if (!open) return
    step.value = 'start'
    name.value = ''
    description.value = ''
    folder.value = ''
    location.value = '~/Projects'
  },
)

// autofocus only works when the dialog first opens, so focus Name by hand on each form step
const nameInput = ref<HTMLInputElement | null>(null)
watch(step, async (value) => {
  if (value !== 'existingDetails' && value !== 'newDetails') return
  await nextTick()
  nameInput.value?.focus()
})

const titles: Record<Step, string> = {
  start: 'Add a project',
  pickProject: 'Choose the project folder',
  restore: 'Restore project?',
  existingDetails: 'Add existing folder',
  newDetails: 'New empty folder',
  pickLocation: 'Where should the folder go?',
}

const removedProject = computed(() => store.projectInFolder(folder.value))

const onProjectFolderChosen = (path: string) => {
  folder.value = path
  if (store.projectInFolder(path)) {
    step.value = 'restore'
  } else {
    name.value = nameFromFolder(path)
    step.value = 'existingDetails'
  }
}

const restore = () => {
  if (removedProject.value) store.restoreProject(removedProject.value.id)
  emit('close')
}

const addExisting = () => {
  if (!name.value.trim()) return
  store.addProject(name.value.trim(), description.value.trim(), folder.value)
  emit('close')
}

const newFolderPath = computed(() => {
  const slug = folderSlug(name.value)
  return slug ? `${location.value}/${slug}` : null
})
const folderTaken = computed(
  () => newFolderPath.value !== null && store.folders.includes(newFolderPath.value),
)

const addNew = () => {
  if (!newFolderPath.value || folderTaken.value) return
  store.createFolder(newFolderPath.value)
  store.addProject(name.value.trim(), description.value.trim(), newFolderPath.value)
  emit('close')
}
</script>

<template>
  <BaseDialog :open="open" :title="titles[step]" @close="emit('close')">
    <div v-if="step === 'start'" class="choices">
      <button type="button" class="choice" @click="step = 'pickProject'">
        <strong>Open existing folder</strong>
        <span>A folder already on your Mac, with or without code in it</span>
      </button>
      <button type="button" class="choice" @click="step = 'newDetails'">
        <strong>New empty folder</strong>
        <span>Start from nothing; Tickie creates the folder for you</span>
      </button>
      <div class="actions">
        <button type="button" @click="emit('close')">Cancel</button>
      </div>
    </div>

    <FolderPicker
      v-else-if="step === 'pickProject'"
      purpose="project"
      @choose="onProjectFolderChosen"
      @cancel="step = 'start'"
    />

    <div v-else-if="step === 'restore'">
      <p>
        <strong>{{ removedProject?.name }}</strong> was removed from the home page earlier. Restore
        it with all its phases, tasks and history?
      </p>
      <div class="actions">
        <button type="button" @click="step = 'pickProject'">Back</button>
        <button type="button" class="primary" @click="restore">Restore</button>
      </div>
    </div>

    <form v-else-if="step === 'existingDetails'" class="form" @submit.prevent="addExisting">
      <p class="hint">📁 {{ folder }}</p>
      <label>
        Name
        <input ref="nameInput" v-model="name" required />
      </label>
      <label>
        Description
        <textarea v-model="description" rows="3" />
      </label>
      <div class="actions">
        <button type="button" @click="step = 'pickProject'">Back</button>
        <button type="submit" class="primary" :disabled="!name.trim()">Add project</button>
      </div>
    </form>

    <form v-else-if="step === 'newDetails'" class="form" @submit.prevent="addNew">
      <label>
        Name
        <input ref="nameInput" v-model="name" required />
      </label>
      <label>
        Description
        <textarea v-model="description" rows="3" />
      </label>
      <div class="location">
        <span>Location</span>
        <button type="button" class="location-button" @click="step = 'pickLocation'">
          📁 {{ folderName(location) }}
          <span class="muted">{{ location }}</span>
        </button>
      </div>
      <p v-if="folderTaken" class="hint error">
        A folder named “{{ folderSlug(name) }}” already exists there.
      </p>
      <p v-else-if="newFolderPath" class="hint">Creates {{ newFolderPath }}</p>
      <div class="actions">
        <button type="button" @click="step = 'start'">Back</button>
        <button type="submit" class="primary" :disabled="!newFolderPath || folderTaken">
          Create project
        </button>
      </div>
    </form>

    <FolderPicker
      v-else-if="step === 'pickLocation'"
      purpose="location"
      :start-in="location"
      @choose="(path) => ((location = path), (step = 'newDetails'))"
      @cancel="step = 'newDetails'"
    />
  </BaseDialog>
</template>

<style scoped>
.choices {
  display: grid;
  gap: 0.5rem;
}

.choice {
  display: grid;
  gap: 0.15rem;
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-border-strong);
  border-radius: 8px;
  background: var(--color-background);
  color: var(--color-text);
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.choice:hover {
  border-color: var(--color-accent);
}

.choice span {
  font-size: 0.85rem;
  color: var(--color-text-muted);
}

.location {
  display: grid;
  gap: 0.25rem;
  font-size: 0.9rem;
}

.location-button {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  padding: 0.5rem;
  border: 1px solid var(--color-border-strong);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.muted {
  font-size: 0.8rem;
  color: var(--color-text-muted);
}
</style>
