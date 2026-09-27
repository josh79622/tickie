<script setup lang="ts">
import { ref, watch } from 'vue'

// Wraps the native <dialog> element: focus trapping and Esc-to-close come for free.
const props = defineProps<{ open: boolean; title: string }>()
const emit = defineEmits<{ close: [] }>()

const dialog = ref<HTMLDialogElement | null>(null)

watch(
  () => props.open,
  (open) => {
    if (open) dialog.value?.showModal()
    else dialog.value?.close()
  },
)
</script>

<template>
  <dialog ref="dialog" class="dialog" @close="emit('close')">
    <h2>{{ title }}</h2>
    <slot />
  </dialog>
</template>

<style scoped>
.dialog {
  /* base.css resets margin to 0, which would pin the dialog to the top-left */
  margin: auto;
  width: min(28rem, calc(100vw - 2rem));
  padding: 1.25rem;
  border: 1px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-background);
  color: var(--color-text);
}

.dialog::backdrop {
  background: rgba(0, 0, 0, 0.35);
}

h2 {
  font-size: 1.1rem;
  font-weight: 700;
  margin-bottom: 0.75rem;
}
</style>
