import { ref, watch } from 'vue'

export type Theme = 'light' | 'dark'

const storageKey = 'tickie-theme'

const readSaved = (): Theme => {
  try {
    return localStorage.getItem(storageKey) === 'dark' ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}

export const theme = ref<Theme>(readSaved())

watch(
  theme,
  (value) => {
    document.documentElement.dataset.theme = value
    try {
      localStorage.setItem(storageKey, value)
    } catch {
      // Storage can be blocked; the theme still applies for this session
    }
  },
  { immediate: true },
)

export const toggleTheme = () => {
  theme.value = theme.value === 'light' ? 'dark' : 'light'
}
