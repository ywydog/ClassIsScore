import api from './api'
import type { ApiResponse, AppSettings, AdminSettings } from '@/types'

export const settingsApi = {
  getSettings() {
    return api.get<ApiResponse<AppSettings>>('/api/settings')
  },

  updateSettings(settings: Partial<AppSettings>) {
    return api.put<ApiResponse<AppSettings>>('/api/settings', settings)
  },

  getAdminSettings() {
    return api.get<ApiResponse<AdminSettings & { hasPassword?: boolean; usbDeviceId?: string }>>('/api/admin/settings')
  },

  updateAdminSettings(settings: Partial<AdminSettings> & { usbDeviceId?: string }) {
    return api.put<ApiResponse<void>>('/api/admin/settings', settings)
  },

  verifyAdmin(method: string, credential: string) {
    return api.post<ApiResponse<boolean>>('/api/admin/verify', { method, credential })
  },

  setPassword(password: string) {
    return api.post<ApiResponse<void>>('/api/admin/set-password', { password })
  },
}
