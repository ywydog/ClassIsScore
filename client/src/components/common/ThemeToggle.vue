<template>
  <el-tooltip :content="isDark ? '切换浅色模式' : '切换深色模式'" placement="bottom">
    <el-button :icon="isDark ? Moon : Sunny" circle text size="small" @click="toggleTheme" />
  </el-tooltip>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Moon, Sunny } from '@element-plus/icons-vue'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()

const isDark = computed(() => {
  if (settingsStore.settings.theme === 'system') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  }
  return settingsStore.settings.theme === 'dark'
})

async function toggleTheme() {
  const theme = isDark.value ? 'light' : 'dark'
  await settingsStore.updateSettings({ theme })
}
</script>
