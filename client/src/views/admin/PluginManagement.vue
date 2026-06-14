<template>
  <div class="plugin-page">
    <div class="plugin-page__header">
      <h2>插件管理</h2>
      <p class="plugin-page__desc">管理已安装的插件，启用或禁用功能扩展</p>
    </div>

    <div class="plugin-page__content">
      <div v-if="loading" class="plugin-page__loading">
        <el-skeleton :rows="4" animated />
      </div>
      <template v-else>
        <div class="plugin-list">
          <div v-for="plugin in plugins" :key="plugin.id" class="plugin-card">
            <div class="plugin-card__icon">
              <el-icon :size="28" color="var(--cis-primary)"><Box /></el-icon>
            </div>
            <div class="plugin-card__info">
              <div class="plugin-card__header">
                <span class="plugin-card__name">{{ plugin.name }}</span>
                <el-tag size="small" type="info">v{{ plugin.version }}</el-tag>
              </div>
              <div class="plugin-card__meta">
                <span v-if="plugin.author" class="plugin-card__author">{{ plugin.author }}</span>
                <span class="plugin-card__desc">{{ plugin.description || '暂无描述' }}</span>
              </div>
            </div>
            <div class="plugin-card__action">
              <el-switch
                v-model="plugin.enabled"
                active-text="启用"
                inactive-text="禁用"
                @change="handlePluginToggle(plugin)"
              />
            </div>
          </div>
        </div>
        <el-empty v-if="plugins.length === 0" description="暂无已安装插件">
          <template #image>
            <el-icon :size="64" color="var(--cis-text-quaternary)"><Box /></el-icon>
          </template>
        </el-empty>
      </template>
    </div>

    <div class="plugin-page__footer">
      <el-button @click="handleOpenPluginDir">
        <el-icon><FolderOpened /></el-icon>
        打开插件目录
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Box, FolderOpened } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { PluginManifest } from '@/types'
import { pluginApi } from '@/services/plugin'
import { invoke } from '@/services/tauri'

const plugins = ref<Array<PluginManifest & { enabled: boolean }>>([])
const loading = ref(true)

onMounted(async () => {
  await fetchPlugins()
  loading.value = false
})

async function fetchPlugins() {
  try {
    const response = await pluginApi.getAll()
    plugins.value = response.data.data || []
  } catch {
    plugins.value = []
  }
}

async function handlePluginToggle(plugin: PluginManifest & { enabled: boolean }) {
  try {
    if (plugin.enabled) {
      await pluginApi.enable(String(plugin.id))
    } else {
      await pluginApi.disable(String(plugin.id))
    }
    ElMessage.success(plugin.enabled ? '已启用插件' : '已禁用插件')
    promptRelaunch('插件状态变更')
  } catch {
    plugin.enabled = !plugin.enabled
  }
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

function handleOpenPluginDir() {
  ElMessage.info('请将插件文件放入插件目录')
}
</script>

<style scoped>
.plugin-page__header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.plugin-page__header h2 {
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

.plugin-page__desc {
  margin: 0;
  padding-left: 12px;
  font-size: 13px;
  color: var(--cis-text-tertiary);
}

.plugin-page__content {
  max-width: 800px;
}

.plugin-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.plugin-card {
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

.plugin-card:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-1px);
}

.plugin-card__icon {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--cis-radius-md);
  background: var(--cis-primary-light-9);
  flex-shrink: 0;
}

.plugin-card__info {
  flex: 1;
  min-width: 0;
}

.plugin-card__header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.plugin-card__name {
  font-weight: 600;
  font-size: 15px;
  color: var(--cis-text-primary);
}

.plugin-card__meta {
  display: flex;
  gap: 12px;
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.plugin-card__author {
  color: var(--cis-text-secondary);
}

.plugin-card__desc {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.plugin-card__action {
  flex-shrink: 0;
}

.plugin-page__footer {
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid var(--cis-border-color-light);
  max-width: 800px;
}
</style>
