<template>
  <div class="app-sidebar">
    <div class="app-sidebar__logo">
      <el-icon :size="24" color="var(--cis-primary)"><Trophy /></el-icon>
      <transition name="fade">
        <span v-if="!collapsed" class="app-sidebar__logo-text">ClassIsScore</span>
      </transition>
    </div>
    <el-scrollbar class="app-sidebar__scroll">
      <el-menu
        :default-active="activeMenu"
        :collapse="collapsed"
        :collapse-transition="false"
        router
        class="app-sidebar__menu"
        background-color="transparent"
        :text-color="'var(--cis-text-secondary)'"
        :active-text-color="'var(--cis-primary)'"
      >
        <el-menu-item-group>
          <template #title><span v-if="!collapsed">核心功能</span></template>
          <el-menu-item index="/admin/scores">
            <el-icon><Trophy /></el-icon>
            <template #title>积分管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/students">
            <el-icon><User /></el-icon>
            <template #title>学生管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/groups">
            <el-icon><Grid /></el-icon>
            <template #title>分组管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/leaderboard">
            <el-icon><Rank /></el-icon>
            <template #title>排行榜</template>
          </el-menu-item>
        </el-menu-item-group>
        <el-menu-item-group>
          <template #title><span v-if="!collapsed">高级功能</span></template>
          <el-menu-item index="/admin/evaluation">
            <el-icon><Timer /></el-icon>
            <template #title>自动评估</template>
          </el-menu-item>
          <el-menu-item index="/admin/settlement">
            <el-icon><Finished /></el-icon>
            <template #title>结算</template>
          </el-menu-item>
        </el-menu-item-group>
        <el-menu-item-group>
          <template #title><span v-if="!collapsed">系统</span></template>
          <el-menu-item index="/admin/settings">
            <el-icon><Setting /></el-icon>
            <template #title>设置</template>
          </el-menu-item>
          <el-menu-item index="/admin/admin-settings">
            <el-icon><Lock /></el-icon>
            <template #title>管理员设置</template>
          </el-menu-item>
          <el-menu-item index="/admin/about">
            <el-icon><InfoFilled /></el-icon>
            <template #title>关于</template>
          </el-menu-item>
        </el-menu-item-group>
      </el-menu>
    </el-scrollbar>
    <div v-if="!collapsed" class="app-sidebar__version">
      v1.0.0-web
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import {
  Trophy, User, Grid, Rank, Timer, Finished, Setting, Lock, InfoFilled,
} from '@element-plus/icons-vue'

defineProps<{
  collapsed: boolean
}>()

defineEmits<{
  toggle: []
}>()

const route = useRoute()
const activeMenu = computed(() => route.path)
</script>

<style scoped>
.app-sidebar {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.app-sidebar__logo {
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 0 16px;
  border-bottom: 1px solid var(--cis-border-color);
  flex-shrink: 0;
}

.app-sidebar__logo-text {
  font-size: 16px;
  font-weight: 700;
  color: var(--cis-primary);
  white-space: nowrap;
}

.app-sidebar__scroll {
  flex: 1;
  overflow: hidden;
}

.app-sidebar__menu {
  border-right: none;
}

.app-sidebar__menu :deep(.el-menu-item-group__title) {
  font-size: 11px;
  color: var(--cis-text-tertiary);
  padding-top: 16px;
  padding-bottom: 4px;
}

.app-sidebar__menu :deep(.el-menu-item.is-active) {
  background-color: var(--cis-primary-light-9) !important;
  border-right: 3px solid var(--cis-primary);
}

.app-sidebar__version {
  padding: 8px 16px;
  font-size: 11px;
  color: var(--cis-text-tertiary);
  text-align: center;
  border-top: 1px solid var(--cis-border-color);
  flex-shrink: 0;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
