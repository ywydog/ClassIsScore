import { invoke } from './tauri'

export const logApi = {
  async query(lines?: number) {
    const result = await invoke<{ lines: string[] }>('log_query', { lines: lines ?? 200 })
    return result.lines
  },

  async clear() {
    await invoke('log_clear')
  },

  async setLevel(level: 'debug' | 'info' | 'warn' | 'error') {
    await invoke('log_set_level', { level })
  },

  async write(level: 'debug' | 'info' | 'warn' | 'error', message: string) {
    await invoke('log_write', { level, message })
  },
}
