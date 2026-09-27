<script setup lang="ts">
import { computed, ref } from 'vue'
import { useTickieStore } from '@/stores/tickie'
import { childFolders, folderName, parentFolder } from '@/lib/folders'

// A pretend Finder. In the real app this whole component is replaced by Tauri's native folder dialog.
const props = defineProps<{
  // 'project': pick a project folder. 'location': pick where a new folder goes.
  purpose: 'project' | 'location'
  startIn?: string
}>()

const emit = defineEmits<{ choose: [path: string]; cancel: [] }>()

const store = useTickieStore()

const current = ref(props.startIn ?? '~/Projects')
const selected = ref<string | null>(null)

const children = computed(() => childFolders(store.folders, current.value))
const parent = computed(() => parentFolder(current.value))

const projectStatus = (path: string): 'shown' | 'removed' | null => {
  const project = store.projectInFolder(path)
  if (!project) return null
  return project.removedAt === null ? 'shown' : 'removed'
}

const open = (path: string) => {
  current.value = path
  selected.value = null
}

// Picking a location with nothing selected means "put it in the folder I'm looking at"
const choice = computed(() =>
  props.purpose === 'location' ? (selected.value ?? current.value) : selected.value,
)

const canChoose = computed(
  () =>
    choice.value !== null &&
    (props.purpose === 'location' || projectStatus(choice.value) !== 'shown'),
)
</script>

<template>
  <div class="picker">
    <div class="path-bar">
      <button
        type="button"
        class="up"
        :disabled="!parent"
        title="Up one folder"
        @click="parent && open(parent)"
      >
        ‹
      </button>
      <span class="path">{{ current }}</span>
    </div>

    <ul class="list">
      <li v-for="folder in children" :key="folder">
        <button
          type="button"
          class="row"
          :class="{ selected: selected === folder }"
          @click="selected = folder"
          @dblclick="open(folder)"
        >
          <span class="icon">📁</span>
          <span class="name">{{ folderName(folder) }}</span>
          <span v-if="purpose === 'project' && projectStatus(folder) === 'shown'" class="tag">
            On home
          </span>
          <span
            v-else-if="purpose === 'project' && projectStatus(folder) === 'removed'"
            class="tag"
          >
            Removed
          </span>
        </button>
      </li>
      <li v-if="children.length === 0" class="empty">This folder is empty</li>
    </ul>
    <p class="tip">Double-click a folder to open it.</p>

    <div class="actions">
      <button type="button" @click="emit('cancel')">Back</button>
      <button
        type="button"
        class="primary"
        :disabled="!canChoose"
        @click="choice && emit('choose', choice)"
      >
        {{ purpose === 'location' ? `Put it in “${folderName(choice ?? current)}”` : 'Open' }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.path-bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.up {
  width: 1.75rem;
  height: 1.75rem;
  border: 1px solid var(--color-border-strong);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 1.1rem;
  line-height: 1;
  cursor: pointer;
}

.up:disabled {
  opacity: 0.4;
  cursor: default;
}

.path {
  font-size: 0.85rem;
  color: var(--color-text-muted);
}

.list {
  list-style: none;
  padding: 0.25rem;
  height: 14rem;
  overflow-y: auto;
  border: 1px solid var(--color-border-strong);
  border-radius: 6px;
}

.row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.3rem 0.5rem;
  border: none;
  border-radius: 4px;
  background: none;
  color: var(--color-text);
  font: inherit;
  text-align: left;
  cursor: default;
  user-select: none;
}

.row:hover {
  background: var(--color-background-soft);
}

.row.selected {
  background: var(--color-accent);
  color: #fff;
}

.name {
  flex: 1;
}

.tag {
  font-size: 0.75rem;
  opacity: 0.75;
}

.empty {
  padding: 0.5rem;
  color: var(--color-text-muted);
  font-size: 0.9rem;
}

.tip {
  margin-top: 0.35rem;
  font-size: 0.75rem;
  color: var(--color-text-muted);
}
</style>
