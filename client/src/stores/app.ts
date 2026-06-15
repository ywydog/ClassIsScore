import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { AppState } from '@/types'
import { useScoreStore } from './score'
import { settingsApi } from '@/services/settings'

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

      // 从后端检查是否已完成引导
      try {
        const response = await settingsApi.getSettings()
        const settings = response.data.data as Record<string, unknown>
        appState.value.isOnboardingCompleted = settings.onboardingCompleted === 'true'
          || localStorage.getItem('onboardingCompleted') === 'true'
      } catch {
        // 后端不可用时检查 localStorage
        appState.value.isOnboardingCompleted = localStorage.getItem('onboardingCompleted') === 'true'
      }

      initialized.value = true
    } catch (err) {
      showToast(err instanceof Error ? err.message : '初始化失败', 'error')
    } finally {
      loading.value = false
    }
  }

  async function completeOnboarding() {
    appState.value.isOnboardingCompleted = true
    localStorage.setItem('onboardingCompleted', 'true')
    try {
      await settingsApi.updateSettings({ onboardingCompleted: true } as any)
    } catch {
      // 静默失败
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
    completeOnboarding,
    cleanup,
    showToast,
    removeToast,
  }
})
