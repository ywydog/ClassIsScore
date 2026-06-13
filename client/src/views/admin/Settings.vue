<template>
  <div class="settings-page">
    <div class="settings-page__header">
      <h2>设置</h2>
    </div>

    <el-tabs v-model="activeTab" class="settings-page__tabs">
      <el-tab-pane label="通用" name="general">
        <el-card class="settings-page__card">
          <el-form label-width="100px">
            <el-form-item label="主题">
              <el-select v-model="settings.theme" @change="handleThemeChange">
                <el-option label="浅色" value="light" />
                <el-option label="深色" value="dark" />
                <el-option label="跟随系统" value="system" />
              </el-select>
            </el-form-item>
            <el-form-item label="字体大小">
              <el-slider v-model="settings.fontSize" :min="12" :max="20" :step="1" show-input @change="handleFontSizeChange" />
            </el-form-item>
            <el-form-item label="显示模式">
              <el-radio-group v-model="settings.displayMode" @change="handleSave">
                <el-radio-button value="Card">卡片</el-radio-button>
                <el-radio-button value="Circle">圆形</el-radio-button>
                <el-radio-button value="Pet">宠物</el-radio-button>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="主题色">
              <el-color-picker v-model="settings.customAccentColor" @change="handleSave" />
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="插件管理" name="plugins">
        <el-card class="settings-page__card">
          <div class="plugin-list">
            <div v-for="plugin in plugins" :key="plugin.id" class="plugin-item">
              <div class="plugin-item__info">
                <div class="plugin-item__header">
                  <el-icon color="var(--cis-primary)"><Box /></el-icon>
                  <span class="plugin-item__name">{{ plugin.name }}</span>
                  <el-tag size="small" type="info">v{{ plugin.version }}</el-tag>
                </div>
                <div class="plugin-item__meta">
                  <span>作者: {{ plugin.author }}</span>
                  <span>{{ plugin.description }}</span>
                </div>
              </div>
              <el-switch v-model="plugin.enabled" @change="handlePluginToggle(plugin)" />
            </div>
            <el-empty v-if="plugins.length === 0" description="暂无已安装插件" />
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="主题包" name="themes">
        <el-card class="settings-page__card">
          <div class="theme-list">
            <div v-for="theme in themes" :key="theme.id" class="theme-item">
              <div class="theme-item__info">
                <div class="theme-item__header">
                  <el-icon color="var(--cis-primary)"><Brush /></el-icon>
                  <span class="theme-item__name">{{ theme.name }}</span>
                  <el-tag size="small" type="info">v{{ theme.version }}</el-tag>
                </div>
                <div class="theme-item__meta">
                  <span>作者: {{ theme.author }}</span>
                  <span>{{ theme.description }}</span>
                </div>
              </div>
              <div class="theme-item__actions">
                <el-switch v-model="theme.enabled" @change="handleThemeToggle(theme)" />
                <el-button type="danger" size="small" text @click="handleDeleteTheme(theme.id)">删除</el-button>
              </div>
            </div>
            <el-empty v-if="themes.length === 0" description="暂无已安装主题包" />
          </div>
          <div class="theme-import">
            <el-button @click="handleImportTheme">
              <el-icon><Upload /></el-icon>
              导入主题包 (.cisui)
            </el-button>
          </div>
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Box, Brush, Upload } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'
import { DisplayMode } from '@/types'
import type { PluginManifest, ThemeManifest } from '@/types'
import api from '@/services/api'

const settingsStore = useSettingsStore()
const activeTab = ref('general')

const settings = reactive({
  theme: 'light' as 'light' | 'dark' | 'system',
  fontSize: 14,
  displayMode: DisplayMode.Card,
  customAccentColor: '',
})

const plugins = ref<Array<PluginManifest & { enabled: boolean }>>([])
const themes = ref<Array<ThemeManifest & { enabled: boolean }>>([])

onMounted(async () => {
  await settingsStore.fetchSettings()
  Object.assign(settings, settingsStore.settings)
  await Promise.all([fetchPlugins(), fetchThemes()])
})

async function fetchPlugins() {
  try {
    const response = await api.get<{ data: Array<PluginManifest & { enabled: boolean }> }>('/api/plugins')
    plugins.value = response.data.data
  } catch { /* ignore */ }
}

async function fetchThemes() {
  try {
    const response = await api.get<{ data: Array<ThemeManifest & { enabled: boolean }> }>('/api/themes')
    themes.value = response.data.data
  } catch { /* ignore */ }
}

async function handleThemeChange(theme: 'light' | 'dark' | 'system') {
  await settingsStore.updateSettings({ theme })
}

async function handleFontSizeChange(fontSize: number) {
  await settingsStore.updateSettings({ fontSize })
}

async function handleSave() {
  await settingsStore.updateSettings(settings)
}

async function handlePluginToggle(plugin: PluginManifest & { enabled: boolean }) {
  try {
    await api.put(`/api/plugins/${plugin.id}/toggle`, { enabled: plugin.enabled })
    ElMessage.success(plugin.enabled ? '已启用插件' : '已禁用插件')
  } catch { /* ignore */ }
}

async function handleThemeToggle(theme: ThemeManifest & { enabled: boolean }) {
  try {
    await api.put(`/api/themes/${theme.id}/toggle`, { enabled: theme.enabled })
    ElMessage.success(theme.enabled ? '已启用主题' : '已禁用主题')
  } catch { /* ignore */ }
}

async function handleDeleteTheme(id: string) {
  await ElMessageBox.confirm('确定删除该主题包？', '确认删除', { type: 'warning' })
  try {
    await api.delete(`/api/themes/${id}`)
    ElMessage.success('已删除')
    await fetchThemes()
  } catch { /* ignore */ }
}

function handleImportTheme() {
  ElMessage.info('请将 .cisui 主题包文件放入主题目录')
}
</script>

<style scoped>
.settings-page__header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.settings-page__header h2 {
  margin: 0;
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

.settings-page__card {
  max-width: 700px;
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.settings-page__card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

/* 插件列表 */
.plugin-list, .theme-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.plugin-item, .theme-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border: 1px solid var(--cis-border-color-light);
  border-radius: var(--cis-radius-lg);
  background: var(--cis-card-bg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast), transform var(--cis-transition-fast);
}

.plugin-item:hover, .theme-item:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-1px);
}

.plugin-item__info, .theme-item__info {
  flex: 1;
}

.plugin-item__header, .theme-item__header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.plugin-item__name, .theme-item__name {
  font-weight: 600;
  font-size: 14px;
  color: var(--cis-text-primary);
}

.plugin-item__meta, .theme-item__meta {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.theme-item__actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.theme-import {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid var(--cis-border-color-light);
}
</style>
