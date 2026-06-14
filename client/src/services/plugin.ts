import { invoke } from './tauri'
import type { PluginManifest } from '@/types'

interface RustPlugin {
  id: string
  name: string
  description: string
  version: string
  author: string
  is_enabled: boolean
  installed_at: string
}

function toPlugin(r: RustPlugin): PluginManifest {
  return {
    id: r.id,
    name: r.name,
    description: r.description,
    version: r.version,
    author: r.author,
    entranceAssembly: '',
  }
}

export const pluginApi = {
  async getAll() {
    const plugins = await invoke<RustPlugin[]>('plugin_list', {})
    return { data: { data: plugins.map(toPlugin), code: 0 } }
  },

  async getById(id: string) {
    const plugin = await invoke<RustPlugin>('plugin_get', { id })
    return { data: { data: toPlugin(plugin), code: 0 } }
  },

  async install(pluginPath: string) {
    const plugin = await invoke<RustPlugin>('plugin_install', { path: pluginPath })
    return { data: { data: toPlugin(plugin), code: 0 } }
  },

  async uninstall(id: string) {
    await invoke('plugin_delete', { id })
    return { data: { data: undefined, code: 0 } }
  },

  async enable(id: string) {
    await invoke('plugin_toggle', { id, enabled: true })
    return { data: { data: undefined, code: 0 } }
  },

  async disable(id: string) {
    await invoke('plugin_toggle', { id, enabled: false })
    return { data: { data: undefined, code: 0 } }
  },
}
