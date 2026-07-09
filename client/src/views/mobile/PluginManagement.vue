<template>
  <div class="m-plugins">
    <header class="m-plugins__head">
      <span class="cis-eyebrow">Plugins</span>
      <h1 class="cis-display m-plugins__title">插件</h1>
    </header>
    <ul v-if="plugins.length > 0" class="m-plugins__list" role="list">
      <li v-for="p in plugins" :key="p.id" class="m-plugins__row">
        <div class="m-plugins__body">
          <span class="m-plugins__name">{{ p.name }}</span>
          <span class="m-plugins__version cis-mono">v{{ p.version }}</span>
        </div>
        <el-switch
          :model-value="p.enabled"
          :aria-label="`${p.name} 启用开关`"
          @change="togglePlugin(p, $event as boolean)"
        />
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无插件" />
    <button type="button" class="m-plugins__install" aria-label="从本地安装插件" @click="installLocal">
      从本地安装
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { invoke } from '@/services/tauri'

interface Plugin { id: string; name: string; version: string; enabled: boolean }
const plugins = ref<Plugin[]>([])

onMounted(async () => {
  try { plugins.value = await invoke<Plugin[]>('plugin_list', {}) } catch { plugins.value = [] }
})

async function togglePlugin(p: Plugin, enabled: boolean) {
  try {
    await invoke('plugin_toggle', { id: p.id, enabled })
    p.enabled = enabled
    ElMessage.success('已更新')
  } catch {
    ElMessage.error('更新失败')
  }
}

async function installLocal() {
  try { await invoke('plugin_install_local', {}) }
  catch { ElMessage.warning('请前往桌面端安装') }
}
</script>

<style scoped>
.m-plugins { display: flex; flex-direction: column; gap: 16px; }
.m-plugins__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-plugins__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-plugins__row { display: flex; align-items: center; gap: 12px; min-height: 56px; padding: 8px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-plugins__list li:last-child .m-plugins__row { border-bottom: none; }
.m-plugins__body { flex: 1; display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.m-plugins__name { font-size: 15px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-plugins__version { font-size: 12px; color: var(--cis-text-tertiary); }
.m-plugins__install { height: 44px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-primary); font-size: 14px; font-weight: 600; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-plugins__install:active { transform: scale(var(--cis-press-scale)); border-color: var(--cis-primary); color: var(--cis-primary); }
</style>
