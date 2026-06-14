import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import type { AppSettings } from '@/types'
import { DisplayMode } from '@/types'
import { settingsApi } from '@/services/settings'

export const useSettingsStore = defineStore('settings', () => {
  const settings = ref<AppSettings>({
    theme: 'light',
    fontSize: 14,
    displayMode: DisplayMode.Card,
    themeMode: 'default',
  })

  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchSettings() {
    loading.value = true
    error.value = null
    try {
      const response = await settingsApi.getSettings()
      settings.value = { ...settings.value, ...response.data.data }
      applyTheme(settings.value.theme)
      applyFontSize(settings.value.fontSize)
      applyThemeMode(settings.value.themeMode || 'default')
    } catch (err) {
      error.value = err instanceof Error ? err.message : '获取设置失败'
    } finally {
      loading.value = false
    }
  }

  async function updateSettings(newSettings: Partial<AppSettings>) {
    loading.value = true
    error.value = null
    try {
      const response = await settingsApi.updateSettings(newSettings)
      settings.value = { ...settings.value, ...response.data.data }
      if (newSettings.theme) {
        applyTheme(settings.value.theme)
      }
      if (newSettings.fontSize) {
        applyFontSize(settings.value.fontSize)
      }
      if (newSettings.themeMode) {
        applyThemeMode(settings.value.themeMode || 'default')
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : '更新设置失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  function applyTheme(theme: 'light' | 'dark' | 'system') {
    const root = document.documentElement
    root.removeAttribute('data-theme')

    if (theme === 'system') {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      root.setAttribute('data-theme', prefersDark ? 'dark' : 'light')
    } else {
      root.setAttribute('data-theme', theme)
    }
  }

  function applyFontSize(size: number) {
    document.documentElement.style.setProperty('--cis-font-size-base', `${size}px`)
  }

  function applyThemeMode(mode: 'default' | 'xianxia') {
    const root = document.documentElement
    root.removeAttribute('data-theme-mode')
    if (mode === 'xianxia') {
      root.setAttribute('data-theme-mode', 'xianxia')
    }
  }

  watch(
    () => settings.value.theme,
    (newTheme) => {
      applyTheme(newTheme)
    }
  )

  watch(
    () => settings.value.themeMode,
    (newMode) => {
      applyThemeMode(newMode || 'default')
    }
  )

  return {
    settings,
    loading,
    error,
    fetchSettings,
    updateSettings,
    applyTheme,
    applyFontSize,
    applyThemeMode,
  }
})
