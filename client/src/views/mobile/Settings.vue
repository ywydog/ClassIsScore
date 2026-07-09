<template>
  <div class="m-settings">
    <header class="m-settings__head">
      <span class="cis-eyebrow">Settings</span>
      <h1 class="cis-display m-settings__title">设置</h1>
    </header>
    <ul class="m-settings__list" role="list">
      <li class="m-settings__group">
        <span class="cis-eyebrow m-settings__group-label">基础</span>
        <div class="m-settings__group-body cis-hairline">
          <div class="m-settings__row">
            <span class="m-settings__row-label">班级名称</span>
            <span class="m-settings__row-value-text">—</span>
          </div>
          <div class="m-settings__row">
            <span class="m-settings__row-label">主题</span>
            <el-select :model-value="theme" aria-label="主题" class="m-settings__row-value" @change="updateTheme">
              <el-option value="light" label="浅色" />
              <el-option value="dark" label="深色" />
              <el-option value="system" label="跟随系统" />
            </el-select>
          </div>
          <div class="m-settings__row">
            <span class="m-settings__row-label">字号</span>
            <span class="m-settings__row-value-text cis-mono">{{ fontSize }}px</span>
          </div>
        </div>
      </li>
      <li class="m-settings__group">
        <span class="cis-eyebrow m-settings__group-label">关于</span>
        <div class="m-settings__group-body cis-hairline">
          <div class="m-settings__row">
            <span class="m-settings__row-label">版本</span>
            <span class="m-settings__row-value-text cis-mono">v1.0.0</span>
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()
const theme = ref<'light' | 'dark' | 'system'>('light')
const fontSize = ref(14)

onMounted(async () => {
  await settingsStore.fetchSettings()
  theme.value = (settingsStore.settings.theme || 'light') as 'light' | 'dark' | 'system'
  fontSize.value = settingsStore.settings.fontSize || 14
})

async function updateTheme(val: 'light' | 'dark' | 'system') {
  theme.value = val
  try {
    await settingsStore.updateSettings({ theme: val })
    ElMessage.success('已应用')
  } catch {
    ElMessage.error('应用失败')
  }
}
</script>

<style scoped>
.m-settings { display: flex; flex-direction: column; gap: 16px; }
.m-settings__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-settings__list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 16px; }
.m-settings__group-label { display: block; margin-bottom: 8px; color: var(--cis-text-tertiary); }
.m-settings__group-body { display: flex; flex-direction: column; }
.m-settings__row { display: flex; flex-direction: column; gap: 4px; padding: 12px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-settings__group-body > .m-settings__row:last-child { border-bottom: none; }
.m-settings__row-label { font-size: 12px; color: var(--cis-text-tertiary); }
.m-settings__row-value { font-size: 15px; width: 100%; }
.m-settings__row-value-text { font-size: 14px; color: var(--cis-text-secondary); }
</style>
