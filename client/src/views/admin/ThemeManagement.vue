<template>
  <div class="theme-page">
    <div class="theme-page__header">
      <h2>主题包管理</h2>
      <p class="theme-page__desc">安装和管理主题包，自定义应用外观</p>
    </div>

    <div class="theme-page__content">
      <div v-if="loading" class="theme-page__loading">
        <el-skeleton :rows="4" animated />
      </div>
      <template v-else>
        <div class="theme-list">
          <div v-for="theme in themes" :key="theme.id" class="theme-card">
            <div class="theme-card__preview">
              <div class="theme-card__preview-bg" :style="{ background: (theme as any).previewGradient || 'var(--cis-gradient-primary)' }">
                <span class="theme-card__preview-label">Aa</span>
              </div>
            </div>
            <div class="theme-card__info">
              <div class="theme-card__header">
                <span class="theme-card__name">{{ theme.name }}</span>
                <el-tag size="small" type="info">v{{ theme.version }}</el-tag>
              </div>
              <div class="theme-card__meta">
                <span v-if="theme.author">{{ theme.author }}</span>
                <span>{{ theme.description || '暂无描述' }}</span>
              </div>
            </div>
            <div class="theme-card__actions">
              <el-switch
                v-model="theme.enabled"
                active-text="启用"
                inactive-text="禁用"
                @change="handleThemeToggle(theme)"
              />
              <el-button type="danger" size="small" text @click="handleDeleteTheme(theme.id)">
                删除
              </el-button>
            </div>
          </div>
        </div>
        <el-empty v-if="themes.length === 0" description="暂无已安装主题包">
          <template #image>
            <el-icon :size="64" color="var(--cis-text-quaternary)"><Brush /></el-icon>
          </template>
        </el-empty>
      </template>
    </div>

    <div class="theme-page__footer">
      <el-button @click="handleImportTheme">
        <el-icon><Upload /></el-icon>
        导入主题包 (.cisui)
      </el-button>
      <el-button @click="handleOpenThemeDir">
        <el-icon><FolderOpened /></el-icon>
        打开主题目录
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Brush, Upload, FolderOpened } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { ThemeManifest } from '@/types'
import { themeApi } from '@/services/theme'
import { useSettingsStore } from '@/stores/settings'
import { invoke } from '@tauri-apps/api/core'

const settingsStore = useSettingsStore()
const themes = ref<Array<ThemeManifest & { enabled: boolean }>>([])
const loading = ref(true)

onMounted(async () => {
  await fetchThemes()
  loading.value = false
})

async function fetchThemes() {
  try {
    const response = await themeApi.getAll()
    themes.value = response.data.data || []
  } catch {
    themes.value = []
  }
}

async function handleThemeToggle(theme: ThemeManifest & { enabled: boolean }) {
  try {
    await themeApi.apply(String(theme.id), theme.enabled)
    // 同步 settings store，使 CSS 立即生效
    const mode = theme.enabled ? (theme.id as 'default' | 'xianxia') : 'default'
    await settingsStore.updateSettings({ themeMode: mode })
    ElMessage.success(theme.enabled ? '已启用主题' : '已禁用主题')
  } catch {
    theme.enabled = !theme.enabled
  }
}

async function handleDeleteTheme(id: string) {
  await ElMessageBox.confirm('确定删除该主题包？', '确认删除', { type: 'warning' })
  try {
    await themeApi.uninstall(String(id))
    ElMessage.success('已删除')
    await fetchThemes()
    promptRelaunch('删除主题包')
  } catch { /* ignore */ }
}

function handleImportTheme() {
  ElMessage.info('请将 .cisui 主题包文件放入主题目录')
}

function handleOpenThemeDir() {
  ElMessage.info('请将主题文件放入主题目录')
}

async function promptRelaunch(reason: string) {
  try {
    await ElMessageBox.confirm(
      `${reason}需要重启应用才能完全生效。是否立即重启？`,
      '需要重启',
      { confirmButtonText: '立即重启', cancelButtonText: '稍后手动重启', type: 'info' }
    )
    try {
      await invoke('restart_app')
    } catch {
      ElMessage.info('当前环境不支持自动重启，请手动关闭并重新打开应用')
    }
  } catch { /* 用户选择稍后 */ }
}
</script>

<style scoped>
.theme-page__header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.theme-page__header h2 {
  margin: 0 0 4px;
  font-family: var(--cis-font-family-display);
  font-size: 22px;
  color: var(--cis-text-primary);
  padding-left: 12px;
  border-left: 3px solid var(--cis-primary);
  background: linear-gradient(135deg, var(--cis-primary), var(--cis-primary-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.theme-page__desc {
  margin: 0;
  padding-left: 12px;
  font-size: 13px;
  color: var(--cis-text-tertiary);
}

.theme-page__content {
  max-width: 800px;
}

.theme-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.theme-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 20px;
  border: 1px solid var(--cis-border-color-light);
  border-radius: var(--cis-radius-lg);
  background: var(--cis-card-bg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast), transform var(--cis-transition-fast);
}

.theme-card:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-1px);
}

.theme-card__preview {
  flex-shrink: 0;
}

.theme-card__preview-bg {
  width: 56px;
  height: 40px;
  border-radius: var(--cis-radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.1);
}

.theme-card__preview-label {
  font-size: 16px;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.9);
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

.theme-card__info {
  flex: 1;
  min-width: 0;
}

.theme-card__header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.theme-card__name {
  font-weight: 600;
  font-size: 15px;
  color: var(--cis-text-primary);
}

.theme-card__meta {
  display: flex;
  gap: 12px;
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.theme-card__actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.theme-page__footer {
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid var(--cis-border-color-light);
  max-width: 800px;
  display: flex;
  gap: 12px;
}
</style>
