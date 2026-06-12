import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { AppState } from '@/types'
import { useScoreStore } from './score'

export const useAppStore = defineStore('app', () => {
  const appState = ref<AppState>({
    isOnboardingCompleted: false,
  })

  const initialized = ref(false)
  const loading = ref(false)
  const toasts = ref<Array<{ id: number; message: string; type: 'success' | 'error' | 'info' | 'warning' }>>([])
  let toastId = 0

  async function initialize() {
    if (initialized.value) return
    loading.value = true

    try {
      const scoreStore = useScoreStore()
      scoreStore.setupWebSocket()

      appState.value.isOnboardingCompleted = true
      initialized.value = true
    } catch (err) {
      showToast(err instanceof Error ? err.message : '初始化失败', 'error')
    } finally {
      loading.value = false
    }
  }

  function cleanup() {
    const scoreStore = useScoreStore()
    scoreStore.teardownWebSocket()
  }

  function showToast(message: string, type: 'success' | 'error' | 'info' | 'warning' = 'info') {
    const id = ++toastId
    toasts.value.push({ id, message, type })
    setTimeout(() => {
      toasts.value = toasts.value.filter((t) => t.id !== id)
    }, 3000)
  }

  function removeToast(id: number) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  return {
    appState,
    initialized,
    loading,
    toasts,
    initialize,
    cleanup,
    showToast,
    removeToast,
  }
})
