import { invoke } from './tauri'
import type { ThemeManifest } from '@/types'

interface RustTheme {
  id: string
  name: string
  description: string
  version: string
  author: string
  css_path: string
  is_enabled: boolean
  installed_at: string
}

function toTheme(r: RustTheme): ThemeManifest & { enabled: boolean } {
  return {
    id: r.id,
    name: r.name,
    description: r.description,
    version: r.version,
    author: r.author,
    targetApiVersion: '1.0',
    enabled: r.is_enabled,
  }
}

export const themeApi = {
  async getAll() {
    const themes = await invoke<RustTheme[]>('theme_list', {})
    return { data: { data: themes.map(toTheme), code: 0 } }
  },

  async getById(id: string) {
    const theme = await invoke<RustTheme>('theme_get', { id })
    return { data: { data: toTheme(theme), code: 0 } }
  },

  async install(themePath: string) {
    const theme = await invoke<RustTheme>('theme_install', { path: themePath })
    return { data: { data: toTheme(theme), code: 0 } }
  },

  async uninstall(id: string) {
    await invoke('theme_delete', { id })
    return { data: { data: undefined, code: 0 } }
  },

  async apply(id: string, enabled: boolean = true) {
    await invoke('theme_toggle', { id, enabled })
    return { data: { data: undefined, code: 0 } }
  },
}
