<template>
  <div class="m-themes">
    <header class="m-themes__head">
      <span class="cis-eyebrow">Themes</span>
      <h1 class="cis-display m-themes__title">主题</h1>
    </header>
    <div class="m-themes__grid">
      <button
        v-for="t in themes"
        :key="t.id"
        type="button"
        class="m-themes__cell"
        :class="{ 'is-active': activeId === t.id }"
        :aria-label="`切换到 ${t.name}`"
        :aria-pressed="activeId === t.id"
        @click="selectTheme(t.id)"
      >
        <div class="m-themes__palette" aria-hidden="true">
          <span class="m-themes__swatch" :style="{ background: t.colors[0] }"></span>
          <span class="m-themes__swatch" :style="{ background: t.colors[1] }"></span>
          <span class="m-themes__swatch" :style="{ background: t.colors[2] }"></span>
          <span class="m-themes__swatch" :style="{ background: t.colors[3] }"></span>
        </div>
        <span class="m-themes__name">{{ t.name }}</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()
const activeId = ref<'default' | 'xianxia' | 'emerald' | 'crimson' | 'obsidian'>('default')

const themes: Array<{ id: 'default' | 'xianxia' | 'emerald' | 'crimson' | 'obsidian'; name: string; colors: string[] }> = [
  { id: 'default',  name: '釉蓝×朱砂', colors: ['#1E40AF', '#B91C1C', '#F8FAFC', '#15803D'] },
  { id: 'xianxia',  name: '仙侠',       colors: ['#4C1D95', '#B45309', '#F8FAFC', '#15803D'] },
  { id: 'emerald',  name: '青苔',       colors: ['#15803D', '#0D7C5F', '#F8FAFC', '#B45309'] },
  { id: 'obsidian', name: '墨砚',       colors: ['#0B1220', '#475569', '#F8FAFC', '#B91C1C'] },
]

onMounted(async () => {
  await settingsStore.fetchSettings()
  const mode = settingsStore.settings.themeMode
  activeId.value = mode === 'xianxia' ? 'xianxia' : 'default'
})

async function selectTheme(id: 'default' | 'xianxia' | 'emerald' | 'crimson' | 'obsidian') {
  activeId.value = id
  // themeMode 仅支持 'default' | 'xianxia'，其他保留为 'default'
  const newMode: 'default' | 'xianxia' = id === 'xianxia' ? 'xianxia' : 'default'
  try {
    await settingsStore.updateSettings({ themeMode: newMode })
    ElMessage.success('已应用')
  } catch {
    ElMessage.error('应用失败')
  }
}
</script>

<style scoped>
.m-themes { display: flex; flex-direction: column; gap: 16px; }
.m-themes__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-themes__grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; }
.m-themes__cell { display: flex; flex-direction: column; gap: 8px; padding: 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); font-family: inherit; cursor: pointer; text-align: left; -webkit-tap-highlight-color: transparent; }
.m-themes__cell:active { transform: scale(var(--cis-press-scale)); }
.m-themes__cell.is-active { border-color: var(--cis-primary); box-shadow: inset 0 0 0 1px var(--cis-primary); }
.m-themes__palette { display: flex; gap: 4px; }
.m-themes__swatch { flex: 1; height: 32px; border-radius: var(--cis-radius-btn); border: 1px solid var(--cis-border); }
.m-themes__name { font-size: 13px; font-weight: 600; }
</style>
