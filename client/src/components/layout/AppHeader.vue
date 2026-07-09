<template>
  <div class="app-header">
    <div class="app-header__left">
      <el-button
        :icon="Fold"
        text
        class="app-header__toggle"
        aria-label="切换侧边栏"
        @click="$emit('toggle-sidebar')"
      />
      <div class="app-header__titles">
        <span class="cis-eyebrow app-header__eyebrow">{{ routeMetaEyebrow }}</span>
        <h1 class="app-header__title">{{ pageTitle }}</h1>
      </div>
    </div>
    <div class="app-header__actions">
      <el-tooltip content="打开大屏展示" placement="bottom">
        <el-button
          :icon="Monitor"
          text
          size="small"
          class="app-header__action-btn"
          aria-label="打开大屏展示窗口"
          @click="openDisplayWindow"
        />
      </el-tooltip>
      <el-tooltip content="打开浮动积分条" placement="bottom">
        <el-button
          :icon="DataLine"
          text
          size="small"
          class="app-header__action-btn"
          aria-label="打开浮动积分条窗口"
          @click="openFloatingBar"
        />
      </el-tooltip>
      <div class="app-header__divider" aria-hidden="true"></div>
      <ThemeToggle />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { Fold, Monitor, DataLine } from '@element-plus/icons-vue'
import ThemeToggle from '@/components/common/ThemeToggle.vue'
import { invoke } from '@tauri-apps/api/core'

defineEmits<{
  'toggle-sidebar': []
}>()

const route = useRoute()

const pageTitles: Record<string, string> = {
  '/admin/dashboard': '总览',
  '/admin/scores': '积分管理',
  '/admin/students': '学生管理',
  '/admin/groups': '分组管理',
  '/admin/leaderboard': '排行榜',
  '/admin/evaluation': '自动评估',
  '/admin/settlement': '结算',
  '/admin/settings': '设置',
  '/admin/plugins': '插件管理',
  '/admin/themes': '主题包',
  '/admin/admin-settings': '管理员设置',
  '/admin/about': '关于',
}

const routeNameTitles: Record<string, string> = {
  'StudentProfile': '学生详情',
}

const pageEyebrows: Record<string, string> = {
  '/admin/dashboard': 'Dashboard',
  '/admin/scores': 'Score',
  '/admin/students': 'Students',
  '/admin/groups': 'Groups',
  '/admin/leaderboard': 'Leaderboard',
  '/admin/evaluation': 'Evaluation',
  '/admin/settlement': 'Settlement',
  '/admin/settings': 'Settings',
  '/admin/plugins': 'Plugins',
  '/admin/themes': 'Themes',
  '/admin/admin-settings': 'Admin',
  '/admin/about': 'About',
}

const pageTitle = computed(() => {
  if (route.name && routeNameTitles[route.name as string]) {
    return routeNameTitles[route.name as string]
  }
  return pageTitles[route.path] || 'ClassIsScore'
})

const routeMetaEyebrow = computed(() => pageEyebrows[route.path] || 'Workspace')

function openDisplayWindow() {
  invoke('open_display_window').catch(() => {
    // 非Tauri环境忽略
  })
}

function openFloatingBar() {
  invoke('open_floating_window').catch(() => {
    // 非Tauri环境忽略
  })
}
</script>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  height: 100%;
  gap: 12px;
}

.app-header__left {
  display: flex;
  align-items: center;
  gap: 4px;
  min-width: 0;
}

.app-header__titles {
  display: flex;
  flex-direction: column;
  gap: 1px;
  margin-left: 4px;
  min-width: 0;
}

.app-header__eyebrow {
  line-height: 1;
}

.app-header__title {
  font-family: var(--cis-font-serif);
  font-size: 17px;
  font-weight: 600;
  color: var(--cis-text-display);
  margin: 0;
  line-height: 1.2;
  letter-spacing: var(--cis-letter-spacing-display);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.app-header__toggle {
  -webkit-app-region: no-drag;
  cursor: pointer;
  color: var(--cis-text-secondary);
  transition: color var(--cis-transition-fast);
}

.app-header__toggle:hover {
  color: var(--cis-primary);
}

.app-header__actions {
  -webkit-app-region: no-drag;
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.app-header__action-btn {
  color: var(--cis-text-tertiary);
  transition: color var(--cis-transition-fast);
}

.app-header__action-btn:hover {
  color: var(--cis-primary);
}

.app-header__divider {
  width: 1px;
  height: 18px;
  background-color: var(--cis-border);
  margin: 0 6px;
}
</style>
