<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { ClipboardList, LifeBuoy, MessageCircle, Plus, Send, X } from 'lucide-vue-next'
import type { ChatAgent, Id } from '@/types/models'
import { formatDateTime } from '@/lib/format'
import { useTickieStore } from '@/stores/tickie'

const props = defineProps<{ projectId: Id }>()
const store = useTickieStore()

const open = ref(false)
const agent = ref<ChatAgent>('Planning')
const draft = ref('')
const replying = ref(false)

const agents = [
  { value: 'Planning', label: 'Planning agent', icon: ClipboardList },
  { value: 'ITSupervisor', label: 'IT supervisor', icon: LifeBuoy },
] as const

const intro: Record<ChatAgent, string> = {
  Planning: 'Talk about direction and the plan. Nothing changes until you approve.',
  ITSupervisor: 'Talk through a bug or a hard problem. It only proposes; you approve.',
}

// Sessions with the chosen agent, oldest first; the last one is current and the only one sent to the agent
const sessions = computed(() => store.chatSessionsFor(props.projectId, agent.value))
const currentSession = computed(() => sessions.value.at(-1) ?? null)

// A past session picked for reading; null means the current one
const viewedId = ref<Id | null>(null)
watch(agent, () => (viewedId.value = null))
const viewedSession = computed(
  () => sessions.value.find((session) => session.id === viewedId.value) ?? currentSession.value,
)
const isPast = computed(
  () => viewedSession.value !== null && viewedSession.value.id !== currentSession.value?.id,
)

const messages = computed(() =>
  viewedSession.value ? store.chatMessagesIn(viewedSession.value.id) : [],
)

// An empty current session is reused rather than stacking up blank ones
const canStartNew = computed(
  () => currentSession.value !== null && store.chatMessagesIn(currentSession.value.id).length > 0,
)
const startNew = () => {
  store.startChatSession(props.projectId, agent.value)
  viewedId.value = null
}

// Keep the newest message in view
const list = ref<HTMLElement | null>(null)
const scrollToEnd = async () => {
  await nextTick()
  list.value?.scrollTo({ top: list.value.scrollHeight })
}
watch([() => messages.value.length, agent, open], scrollToEnd)

const input = ref<HTMLTextAreaElement | null>(null)
watch([open, agent], async () => {
  await nextTick()
  input.value?.focus()
})

const send = () => {
  const text = draft.value.trim()
  if (!text || replying.value) return
  if (isPast.value) return
  const to = agent.value
  const sessionId = currentSession.value?.id ?? store.startChatSession(props.projectId, to)
  store.addChatMessage(sessionId, true, text)
  draft.value = ''
  // Stand-in until the C# manager connects a real agent
  replying.value = true
  setTimeout(() => {
    store.addChatMessage(
      sessionId,
      false,
      `(Demo) The ${to === 'Planning' ? 'planning agent' : 'IT supervisor'} will reply here once the manager is connected.`,
    )
    replying.value = false
  }, 700)
}

// Enter sends, Shift+Enter adds a new line
const onKeydown = (event: KeyboardEvent) => {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
    event.preventDefault()
    send()
  }
}
</script>

<template>
  <div class="chat">
    <section v-if="open" class="window" aria-label="Chat">
      <header>
        <div class="tabs" role="tablist">
          <button
            v-for="option in agents"
            :key="option.value"
            type="button"
            role="tab"
            :aria-selected="agent === option.value"
            :class="{ active: agent === option.value }"
            @click="agent = option.value"
          >
            <component :is="option.icon" :size="15" aria-hidden="true" />
            {{ option.label }}
          </button>
        </div>
        <button type="button" class="icon" aria-label="Close chat" @click="open = false">
          <X :size="18" aria-hidden="true" />
        </button>
      </header>

      <div v-if="sessions.length" class="sessions">
        <select v-model="viewedId" aria-label="Session">
          <option
            v-for="(session, index) in sessions"
            :key="session.id"
            :value="index === sessions.length - 1 ? null : session.id"
          >
            Session {{ index + 1 }} · {{ formatDateTime(session.startedAt) }}
            {{ index === sessions.length - 1 ? '(current)' : '' }}
          </option>
        </select>
        <button type="button" class="new" :disabled="!canStartNew" @click="startNew">
          <Plus :size="14" aria-hidden="true" />
          New session
        </button>
      </div>

      <div ref="list" class="messages">
        <p class="intro">{{ intro[agent] }}</p>
        <p v-if="!isPast && messages.length === 0 && sessions.length > 1" class="intro">
          New session. The agent starts from the project's current brief and tasks, not from
          earlier sessions.
        </p>
        <p
          v-for="message in messages"
          :key="message.id"
          class="bubble"
          :class="{ mine: message.fromMe }"
        >
          {{ message.text }}
        </p>
        <p v-if="replying" class="bubble typing" aria-label="Agent is typing">
          <span /><span /><span />
        </p>
      </div>

      <p v-if="isPast" class="past">
        Past session, read only. The agent no longer sees it.
        <button type="button" class="link" @click="viewedId = null">Back to current</button>
      </p>
      <form v-else class="composer" @submit.prevent="send">
        <textarea
          ref="input"
          v-model="draft"
          rows="2"
          :placeholder="`Message the ${agent === 'Planning' ? 'planning agent' : 'IT supervisor'}`"
          @keydown="onKeydown"
        />
        <button type="submit" class="icon send" aria-label="Send" :disabled="!draft.trim() || replying">
          <Send :size="18" aria-hidden="true" />
        </button>
      </form>
    </section>

    <button
      type="button"
      class="launcher"
      :aria-label="open ? 'Close chat' : 'Open chat'"
      :aria-expanded="open"
      @click="open = !open"
    >
      <X v-if="open" :size="24" aria-hidden="true" />
      <MessageCircle v-else :size="24" aria-hidden="true" />
    </button>
  </div>
</template>

<style scoped>
.chat {
  position: fixed;
  right: 1.5rem;
  bottom: 1.5rem;
  z-index: 20;
  display: grid;
  justify-items: end;
  gap: 0.75rem;
}

.launcher {
  display: grid;
  place-items: center;
  width: 3.25rem;
  height: 3.25rem;
  border: none;
  border-radius: 50%;
  background: var(--color-accent);
  color: #fff;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.2);
  cursor: pointer;
}

.window {
  display: flex;
  flex-direction: column;
  width: min(24rem, calc(100vw - 2rem));
  height: min(32rem, calc(100vh - 7rem));
  border: 1px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-background);
  box-shadow: 0 8px 28px rgba(0, 0, 0, 0.18);
  overflow: hidden;
}

header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 0.6rem 0.6rem 0.75rem;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-band);
}

.tabs {
  display: flex;
  gap: 0.25rem;
}

.tabs button {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.3rem 0.65rem;
  border: 1px solid transparent;
  border-radius: 6px;
  background: none;
  color: var(--color-text-muted);
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
}

.tabs button.active {
  border-color: var(--color-border);
  background: var(--color-background);
  color: var(--color-text);
}

.icon {
  display: grid;
  place-items: center;
  width: 2rem;
  height: 2rem;
  border: none;
  border-radius: 6px;
  background: none;
  color: var(--color-text-muted);
  cursor: pointer;
}

.icon:hover:not(:disabled) {
  background: var(--color-background-soft);
  color: var(--color-text);
}

.sessions {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.45rem 0.6rem;
  border-bottom: 1px solid var(--color-border);
  font-size: 0.8rem;
}

.sessions select {
  flex: 1;
  min-width: 0;
  padding: 0.25rem 0.4rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  font: inherit;
}

.new {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.55rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  font: inherit;
  white-space: nowrap;
  cursor: pointer;
}

.new:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.past {
  padding: 0.7rem;
  border-top: 1px solid var(--color-border);
  background: var(--color-band);
  color: var(--color-text-muted);
  font-size: 0.8rem;
  text-align: center;
}

.link {
  border: none;
  background: none;
  color: var(--color-accent);
  font: inherit;
  cursor: pointer;
}

.messages {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.75rem;
  overflow-y: auto;
  font-size: 0.9rem;
}

.intro {
  align-self: center;
  max-width: 90%;
  margin-bottom: 0.25rem;
  color: var(--color-text-muted);
  font-size: 0.8rem;
  text-align: center;
}

.bubble {
  align-self: flex-start;
  max-width: 85%;
  padding: 0.45rem 0.7rem;
  border-radius: 12px 12px 12px 4px;
  background: var(--color-band);
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.bubble.mine {
  align-self: flex-end;
  border-radius: 12px 12px 4px 12px;
  background: var(--color-accent);
  color: #fff;
}

.typing {
  display: flex;
  gap: 0.25rem;
  padding: 0.7rem;
}

.typing span {
  width: 0.4rem;
  height: 0.4rem;
  border-radius: 50%;
  background: var(--color-text-muted);
  animation: blink 1s infinite;
}

.typing span:nth-child(2) {
  animation-delay: 0.2s;
}

.typing span:nth-child(3) {
  animation-delay: 0.4s;
}

@keyframes blink {
  50% {
    opacity: 0.25;
  }
}

.composer {
  display: flex;
  align-items: flex-end;
  gap: 0.4rem;
  padding: 0.6rem;
  border-top: 1px solid var(--color-border);
}

.composer textarea {
  flex: 1;
  resize: none;
  font-size: 0.9rem;
}

.send {
  color: var(--color-accent);
}

.send:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
</style>
