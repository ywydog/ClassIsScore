<template>
  <div class="app-header">
    <el-button
      :icon="Fold"
      text
      class="app-header__toggle"
      @click="$emit('toggle-sidebar')"
    />
    <span class="app-header__title">{{ pageTitle }}</span>
    <div class="app-header__actions">
      <el-tooltip content="打开大屏展示" placement="bottom">
        <el-button :icon="Monitor" text size="small" class="app-header__action-btn" @click="openDisplayWindow" />
      </el-tooltip>
      <el-tooltip content="打开浮动积分条" placement="bottom">
        <el-button :icon="DataLine" text size="small" class="app-header__action-btn" @click="openFloatingBar" />
      </el-tooltip>
      <div class="app-header__divider"></div>
      <ThemeToggle />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { Fold, Monitor, DataLine } from '@element-plus/icons-vue'
import ThemeToggle from '@/components/common/ThemeToggle.vue'

defineEmits<{
  'toggle-sidebar': []
}>()

const route = useRoute()

const pageTitles: Record<string, string> = {
  '/admin/scores': '积分管理',
  '/admin/students': '学生管理',
  '/admin/groups': '分组管理',
  '/admin/leaderboard': '排行榜',
  '/admin/evaluation': '自动评估',
  '/admin/settlement': '结算',
  '/admin/settings': '设置',
  '/admin/admin-settings': '管理员设置',
  '/admin/about': '关于',
}

const routeNameTitles: Record<string, string> = {
  'StudentProfile': '学生详情',
}

const pageTitle = computed(() => {
  if (route.name && routeNameTitles[route.name as string]) {
    return routeNameTitles[route.name as string]
  }
  return pageTitles[route.path] || 'ClassIsScore'
})

function openDisplayWindow() {
  window.electronAPI?.openWindow?.('display')
}

function openFloatingBar() {
  window.electronAPI?.openWindow?.('floating')
}
</script>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  width: 100%;
  height: 100%;
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

.app-header__title {
  font-family: var(--cis-font-family-display);
  font-size: 16px;
  font-weight: 600;
  color: var(--cis-text-primary);
  margin-left: 4px;
}

.app-header__actions {
  margin-left: auto;
  -webkit-app-region: no-drag;
  display: flex;
  align-items: center;
  gap: 4px;
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
  height: 16px;
  background-color: var(--cis-border-color);
  margin: 0 4px;
}
</style>
