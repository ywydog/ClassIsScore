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
        <el-button :icon="Monitor" text size="small" @click="openDisplayWindow" />
      </el-tooltip>
      <el-tooltip content="打开浮动积分条" placement="bottom">
        <el-button :icon="DataLine" text size="small" @click="openFloatingBar" />
      </el-tooltip>
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

const pageTitle = computed(() => pageTitles[route.path] || 'ClassIsScore')

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
}

.app-header__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
  margin-left: 8px;
}

.app-header__actions {
  margin-left: auto;
  -webkit-app-region: no-drag;
  display: flex;
  align-items: center;
  gap: 4px;
}
</style>
