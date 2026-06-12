import api from './api'
import type { ApiResponse, PluginManifest } from '@/types'

export const pluginApi = {
  getAll() {
    return api.get<ApiResponse<PluginManifest[]>>('/api/plugins')
  },

  getById(id: string) {
    return api.get<ApiResponse<PluginManifest>>(`/api/plugins/${id}`)
  },

  install(pluginPath: string) {
    return api.post<ApiResponse<PluginManifest>>('/api/plugins/install', { path: pluginPath })
  },

  uninstall(id: string) {
    return api.delete<ApiResponse<void>>(`/api/plugins/${id}`)
  },

  enable(id: string) {
    return api.post<ApiResponse<void>>(`/api/plugins/${id}/enable`)
  },

  disable(id: string) {
    return api.post<ApiResponse<void>>(`/api/plugins/${id}/disable`)
  },
}
