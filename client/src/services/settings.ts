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
    return api.get<ApiResponse<AdminSettings>>('/api/admin-settings')
  },

  updateAdminSettings(settings: Partial<AdminSettings>) {
    return api.put<ApiResponse<AdminSettings>>('/api/admin-settings', settings)
  },

  verifyAdmin(password: string) {
    return api.post<ApiResponse<boolean>>('/api/admin-settings/verify', { password })
  },
}
