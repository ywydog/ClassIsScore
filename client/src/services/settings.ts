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
    await invoke('auth_set_passwords', { admin_password: password })
    return { data: { data: undefined } }
  },

  /**
   * 设置/更新网络伺服 PIN。
   * - 非空字符串：保存 Argon2id 散列到 admin_settings.network_serve_pin
   * - 空字符串：清除 PIN 并关闭当前网络伺服
   */
  async setNetworkPin(pin: string) {
    const result = await invoke<{ success: boolean; message: string }>('auth_set_network_pin', {
      network_serve_pin: pin,
    })
    return { data: { data: result } }
  },

  /**
   * 查询当前是否已配置网络伺服 PIN。
   * 通过 settings_get_all 中是否存在 network_serve_pin 键判断（值已散列，永不返回明文）。
   */
  async hasNetworkPin(): Promise<boolean> {
    const settings = await invoke<RustSetting[]>('settings_get_all', {})
    return settings.some((s) => s.setting_key === 'network_serve_pin' && !!s.setting_value)
  },

  async exportSettings() {
    const result = await invoke('settings_export', {})
    return { data: { data: result } }
  },

  async importSettings(data: Record<string, unknown>) {
    await invoke('settings_import', { data })
    return { data: { data: undefined } }
  },
}
