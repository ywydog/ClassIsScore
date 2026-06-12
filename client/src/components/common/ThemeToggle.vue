<template>
  <el-switch
    :model-value="isDark"
    active-text="深色"
    inactive-text="浅色"
    @change="toggleTheme"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()

const isDark = computed(() => {
  if (settingsStore.settings.theme === 'system') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  }
  return settingsStore.settings.theme === 'dark'
})

async function toggleTheme(val: boolean | string | number) {
  const theme = val ? 'dark' : 'light'
  await settingsStore.updateSettings({ theme })
}
</script>
