import type { PluginManifest } from '@/types'
import { pluginApi } from '@/services/plugin'

interface LoadedPlugin {
  manifest: PluginManifest
  enabled: boolean
  module?: unknown
}

const loadedPlugins = new Map<string, LoadedPlugin>()

export async function loadPlugins(): Promise<LoadedPlugin[]> {
  try {
    const response = await pluginApi.getAll()
    const manifests = response.data.data

    for (const manifest of manifests) {
      if (!loadedPlugins.has(manifest.id)) {
        loadedPlugins.set(manifest.id, {
          manifest,
          enabled: true,
        })
      }
    }

    return Array.from(loadedPlugins.values())
  } catch {
    return []
  }
}

export function getPlugin(id: string): LoadedPlugin | undefined {
  return loadedPlugins.get(id)
}

export function getAllPlugins(): LoadedPlugin[] {
  return Array.from(loadedPlugins.values())
}

export async function enablePlugin(id: string): Promise<void> {
  await pluginApi.enable(id)
  const plugin = loadedPlugins.get(id)
  if (plugin) {
    plugin.enabled = true
  }
}

export async function disablePlugin(id: string): Promise<void> {
  await pluginApi.disable(id)
  const plugin = loadedPlugins.get(id)
  if (plugin) {
    plugin.enabled = false
  }
}

export async function installPlugin(pluginPath: string): Promise<PluginManifest> {
  const response = await pluginApi.install(pluginPath)
  const manifest = response.data.data
  loadedPlugins.set(manifest.id, { manifest, enabled: true })
  return manifest
}

export async function uninstallPlugin(id: string): Promise<void> {
  await pluginApi.uninstall(id)
  loadedPlugins.delete(id)
}
