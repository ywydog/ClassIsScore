import { invoke } from './tauri'
import type { AppSettings, AdminSettings } from '@/types'

interface RustSetting {
  id: number
  setting_key: string
  setting_value: string | null
  updated_at: string
}

export const settingsApi = {
  async getSettings() {
    const settings = await invoke<RustSetting[]>('settings_get_all', {})
    const result: Record<string, string> = {}
    for (const s of settings) {
      if (s.setting_value) {
        result[s.setting_key] = s.setting_value
      }
    }
    return { data: { data: result as unknown as AppSettings } }
  },

  async updateSettings(settings: Partial<AppSettings>) {
    for (const [key, value] of Object.entries(settings)) {
      await invoke('settings_set', { key, value: String(value) })
    }
    return this.getSettings()
  },

  async getAdminSettings() {
    const settings = await invoke<RustSetting[]>('settings_get_all', {})
    const result: Record<string, string> = {}
    for (const s of settings) {
      if (s.setting_value) {
        result[s.setting_key] = s.setting_value
      }
    }
    return {
      data: {
        data: {
          ...result,
          hasPassword: !!result['admin_password'],
        } as unknown as AdminSettings & { hasPassword?: boolean }
      }
    }
  },

  async updateAdminSettings(settings: Partial<AdminSettings> & { usbDeviceId?: string }) {
    for (const [key, value] of Object.entries(settings)) {
      if (value !== undefined) {
        await invoke('settings_set', { key, value: String(value) })
      }
    }
    return { data: { data: undefined } }
  },

  async verifyAdmin(method: string, credential: string) {
    if (method === 'password') {
      const result = await invoke<{ success: boolean; message: string }>('auth_verify', { password: credential })
      return { data: { data: result.success } }
    }
    return { data: { data: false } }
  },

  async setPassword(password: string) {
    await invoke('auth_set_passwords', { adminPassword: password })
    return { data: { data: undefined } }
  },

  async resetAll() {
    await invoke('student_reset_scores')
    return { data: { data: undefined } }
  },

  async getFloatingSettings() {
    const settings = await invoke<RustSetting[]>('settings_get_all', {})
    const result: Record<string, unknown> = {}
    for (const s of settings) {
      if (s.setting_value && s.setting_key.startsWith('floating.')) {
        result[s.setting_key] = s.setting_value
      }
    }
    return { data: { data: result } }
  },

  async updateFloatingSettings(settings: Record<string, unknown>) {
    for (const [key, value] of Object.entries(settings)) {
      if (value !== undefined) {
        await invoke('settings_set', { key, value: String(value) })
      }
    }
    return { data: { data: undefined } }
  },

  async getDataPath() {
    try {
      const { appDataDir } = await import('@tauri-apps/api/path')
      const path = await appDataDir()
      return { data: { data: { path } } }
    } catch {
      return { data: { data: { path: '' } } }
    }
  },

  async exportSettings() {
    const data = await invoke<RustSettingsExportData>('settings_export')
    return data
  },

  async importSettings(data: SettingsImportData) {
    const result = await invoke<string>('settings_import', { data })
    return result
  },
}

// Rust 端导出数据类型
interface RustSettingsExportData {
  version: string
  exported_at: string
  settings: RustSetting[]
  evaluation_items: {
    id: number
    name: string
    score_change: number
    category: string | null
    is_quick_access: boolean
    created_at: string
  }[]
  auto_evaluation_configs: {
    id: number
    name: string
    trigger_type: string
    trigger_time: string | null
    day_of_week: number | null
    day_of_month: number | null
    evaluation_item_id: number | null
    score_change: number | null
    reason: string | null
    target_type: string | null
    target_group_id: number | null
    target_student_id: number | null
    is_enabled: boolean
    last_executed_at: string | null
    created_at: string
  }[]
}

// 导入数据类型（不含 id 和时间戳）
export interface SettingsImportData {
  settings: { setting_key: string; setting_value: string | null }[]
  evaluation_items: { name: string; score_change: number; category: string | null; is_quick_access: boolean }[]
  auto_evaluation_configs: {
    name: string
    trigger_type: string
    trigger_time: string | null
    day_of_week: number | null
    day_of_month: number | null
    evaluation_item_id: number | null
    score_change: number | null
    reason: string | null
    target_type: string | null
    target_group_id: number | null
    target_student_id: number | null
    is_enabled: boolean
  }[]
}
