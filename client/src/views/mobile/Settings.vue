<template>
  <div class="m-settings">
    <header class="m-settings__head">
      <span class="cis-eyebrow">Settings</span>
      <h1 class="cis-display m-settings__title">设置</h1>
    </header>
    <ul class="m-settings__list" role="list">
      <li class="m-settings__group">
        <span class="cis-eyebrow m-settings__group-label">外观</span>
        <div class="m-settings__group-body cis-hairline">
          <div class="m-settings__row">
            <span class="m-settings__row-label">主题</span>
            <el-radio-group
              :model-value="theme"
              class="m-settings__theme-group"
              aria-label="主题"
              @change="updateTheme"
            >
              <el-radio-button value="light">浅色</el-radio-button>
              <el-radio-button value="dark">深色</el-radio-button>
              <el-radio-button value="system">跟随系统</el-radio-button>
            </el-radio-group>
          </div>
          <div class="m-settings__row">
            <span class="m-settings__row-label">字号</span>
            <div class="m-settings__font-size">
              <el-slider
                v-model="fontSize"
                :min="12"
                :max="18"
                :step="1"
                :show-tooltip="true"
                aria-label="字号"
                @change="updateFontSize"
              />
              <span class="m-settings__font-size-value cis-mono">{{ fontSize }}px</span>
            </div>
          </div>
          <div class="m-settings__row">
            <span class="m-settings__row-label">主题包</span>
            <el-radio-group
              :model-value="themeMode"
              class="m-settings__theme-group"
              aria-label="主题包"
              @change="updateThemeMode"
            >
              <el-radio-button value="default">釉蓝×朱砂</el-radio-button>
              <el-radio-button value="xianxia">仙侠</el-radio-button>
            </el-radio-group>
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
          <div class="m-settings__row">
            <span class="m-settings__row-label">重置引导</span>
            <el-button text type="primary" @click="resetOnboarding">重新走引导</el-button>
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()
const theme = ref<'light' | 'dark' | 'system'>('light')
const fontSize = ref(14)
const themeMode = ref<'default' | 'xianxia'>('default')

onMounted(async () => {
  await settingsStore.fetchSettings()
  theme.value = (settingsStore.settings.theme || 'light') as 'light' | 'dark' | 'system'
  fontSize.value = settingsStore.settings.fontSize || 14
  themeMode.value = (settingsStore.settings.themeMode || 'default') as 'default' | 'xianxia'
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

async function updateFontSize(val: number | number[]) {
  const next = Array.isArray(val) ? val[0] : val
  fontSize.value = next
  try {
    await settingsStore.updateSettings({ fontSize: next })
  } catch {
    ElMessage.error('应用失败')
  }
}

async function updateThemeMode(val: 'default' | 'xianxia') {
  themeMode.value = val
  try {
    await settingsStore.updateSettings({ themeMode: val })
    ElMessage.success('已应用')
  } catch {
    ElMessage.error('应用失败')
  }
}

async function resetOnboarding() {
  try {
    await ElMessageBox.confirm('重置后下次启动会重新走引导流程。', '重置引导', {
      confirmButtonText: '重置',
      cancelButtonText: '取消',
      type: 'warning',
    })
    localStorage.removeItem('onboardingCompleted')
    ElMessage.success('已重置，下次启动生效')
  } catch { /* 用户取消 */ }
}
</script>

<style scoped>
.m-settings { display: flex; flex-direction: column; gap: 16px; }
.m-settings__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-settings__list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 16px; }
.m-settings__group-label { display: block; margin-bottom: 8px; color: var(--cis-text-tertiary); }
.m-settings__group-body { display: flex; flex-direction: column; }
.m-settings__row { display: flex; flex-direction: column; gap: 8px; padding: 14px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-settings__group-body > .m-settings__row:last-child { border-bottom: none; }
.m-settings__row-label { font-size: 12px; color: var(--cis-text-tertiary); font-weight: 500; }
.m-settings__row-value-text { font-size: 14px; color: var(--cis-text-secondary); }
.m-settings__theme-group { display: flex; gap: 8px; flex-wrap: wrap; }
.m-settings__theme-group :deep(.el-radio-button__inner) {
  padding: 6px 14px;
  font-size: 13px;
  border-radius: var(--cis-radius-btn) !important;
}
.m-settings__font-size {
  display: flex;
  align-items: center;
  gap: 12px;
}
.m-settings__font-size :deep(.el-slider) { flex: 1; }
.m-settings__font-size-value {
  font-size: 13px;
  color: var(--cis-text-secondary);
  min-width: 40px;
  text-align: right;
  font-variant-numeric: tabular-nums;
}
</style>
