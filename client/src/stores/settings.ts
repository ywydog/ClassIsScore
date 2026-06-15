import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import type { AppSettings } from '@/types'
import { DisplayMode, PetModeBehavior } from '@/types'
import { settingsApi } from '@/services/settings'
import { invoke } from '@/services/tauri'

export const useSettingsStore = defineStore('settings', () => {
  const settings = ref<AppSettings>({
    theme: 'light',
    fontSize: 14,
    displayMode: DisplayMode.Card,
    petModeBehavior: PetModeBehavior.Replace,
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
      applyFontFamily(settings.value.fontFamily)
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
      if (newSettings.fontFamily !== undefined) {
        applyFontFamily(newSettings.fontFamily)
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

  function applyFontFamily(fontFamily?: string) {
    if (fontFamily) {
      document.documentElement.style.setProperty('--cis-font-family', fontFamily)
    } else {
      document.documentElement.style.removeProperty('--cis-font-family')
    }
  }

  function applyThemeMode(mode: 'default' | 'xianxia') {
    const root = document.documentElement
    root.removeAttribute('data-theme-mode')
    if (mode === 'xianxia') {
      root.setAttribute('data-theme-mode', 'xianxia')
    }
  }

  async function exportSettings() {
    const result = await invoke('settings_export', {})
    return result as string
  }

  async function importSettings(data: string) {
    const parsed = typeof data === 'string' ? JSON.parse(data) : data
    await invoke('settings_import', { data: parsed })
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
    applyFontFamily,
    applyThemeMode,
    exportSettings,
    importSettings,
  }
})
