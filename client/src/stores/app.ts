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
      // 从后端读取引导完成状态
      const response = await settingsApi.getSettings()
      const settings = response.data.data as unknown as Record<string, string>
      appState.value.isOnboardingCompleted = settings['onboardingCompleted'] === 'true'

      // 设置 Tauri 事件监听（积分实时更新）
      const scoreStore = useScoreStore()
      scoreStore.setupWebSocket()

      initialized.value = true
    } catch (err) {
      showToast(err instanceof Error ? err.message : '初始化失败', 'error')
      // 即使读取设置失败，也标记为已初始化，避免卡死
      initialized.value = true
    } finally {
      loading.value = false
    }
  }

  async function completeOnboarding() {
    try {
      await settingsApi.updateSettings({ onboardingCompleted: 'true' } as Record<string, unknown>)
      appState.value.isOnboardingCompleted = true
    } catch {
      // 降级：写入 localStorage
      localStorage.setItem('onboardingCompleted', 'true')
      appState.value.isOnboardingCompleted = true
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
