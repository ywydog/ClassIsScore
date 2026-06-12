import api from './api'
import type { ApiResponse, ThemeManifest } from '@/types'

export const themeApi = {
  getAll() {
    return api.get<ApiResponse<ThemeManifest[]>>('/api/themes')
  },

  getById(id: string) {
    return api.get<ApiResponse<ThemeManifest>>(`/api/themes/${id}`)
  },

  install(themePath: string) {
    return api.post<ApiResponse<ThemeManifest>>('/api/themes/install', { path: themePath })
  },

  uninstall(id: string) {
    return api.delete<ApiResponse<void>>(`/api/themes/${id}`)
  },

  apply(id: string) {
    return api.post<ApiResponse<void>>(`/api/themes/${id}/apply`)
  },
}
