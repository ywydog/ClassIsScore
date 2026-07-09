<template>
  <div class="app-sidebar">
    <div class="app-sidebar__logo">
      <div class="app-sidebar__logo-icon" aria-hidden="true">
        <el-icon :size="20"><Trophy /></el-icon>
      </div>
      <transition name="fade">
        <span v-if="!collapsed" class="app-sidebar__logo-text" translate="no">ClassIsScore</span>
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
        aria-label="主菜单"
      >
        <el-menu-item-group>
          <template #title><span v-if="!collapsed" class="menu-group-label">核心功能</span></template>
          <el-menu-item index="/admin/dashboard">
            <el-icon aria-hidden="true"><HomeFilled /></el-icon>
            <template #title>总览</template>
          </el-menu-item>
          <el-menu-item index="/admin/scores">
            <el-icon aria-hidden="true"><Trophy /></el-icon>
            <template #title>积分管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/students">
            <el-icon aria-hidden="true"><User /></el-icon>
            <template #title>学生管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/groups">
            <el-icon aria-hidden="true"><Grid /></el-icon>
            <template #title>分组管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/leaderboard">
            <el-icon aria-hidden="true"><Rank /></el-icon>
            <template #title>排行榜</template>
          </el-menu-item>
        </el-menu-item-group>
        <el-menu-item-group>
          <template #title><span v-if="!collapsed" class="menu-group-label">高级功能</span></template>
          <el-menu-item index="/admin/evaluation">
            <el-icon aria-hidden="true"><Timer /></el-icon>
            <template #title>自动评估</template>
          </el-menu-item>
          <el-menu-item index="/admin/settlement">
            <el-icon aria-hidden="true"><Finished /></el-icon>
            <template #title>结算</template>
          </el-menu-item>
        </el-menu-item-group>
        <el-menu-item-group>
          <template #title><span v-if="!collapsed" class="menu-group-label">系统</span></template>
          <el-menu-item index="/admin/settings">
            <el-icon aria-hidden="true"><Setting /></el-icon>
            <template #title>设置</template>
          </el-menu-item>
          <el-menu-item index="/admin/plugins">
            <el-icon aria-hidden="true"><Box /></el-icon>
            <template #title>插件管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/themes">
            <el-icon aria-hidden="true"><Brush /></el-icon>
            <template #title>主题包</template>
          </el-menu-item>
          <el-menu-item index="/admin/admin-settings">
            <el-icon aria-hidden="true"><Lock /></el-icon>
            <template #title>管理员设置</template>
          </el-menu-item>
          <el-menu-item index="/admin/about">
            <el-icon aria-hidden="true"><InfoFilled /></el-icon>
            <template #title>关于</template>
          </el-menu-item>
        </el-menu-item-group>
      </el-menu>
    </el-scrollbar>
    <div v-if="!collapsed" class="app-sidebar__version" aria-hidden="true">
      v1.0.0
    </div>
    <button
      type="button"
      class="app-sidebar__collapse-btn"
      :aria-label="collapsed ? '展开侧边栏' : '折叠侧边栏'"
      :aria-expanded="!collapsed"
      @click="$emit('toggle')"
    >
      <el-icon :size="16" aria-hidden="true">
        <Expand v-if="collapsed" />
        <Fold v-else />
      </el-icon>
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import {
  Trophy, User, Grid, Rank, Timer, Finished, Setting, Lock, InfoFilled,
  Box, Brush,
} from '@element-plus/icons-vue'
import { Expand, Fold } from '@element-plus/icons-vue'

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
  background: var(--cis-canvas);
}

/* Logo 区：纯实心蓝盒 + 米白衬线字（无渐变） */
.app-sidebar__logo {
  height: 52px;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 0 16px;
  border-bottom: 1px solid var(--cis-border);
  flex-shrink: 0;
}
.app-sidebar__logo-icon {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--cis-primary);
  border-radius: var(--cis-radius-btn);
  color: #fff;
  flex-shrink: 0;
}
.app-sidebar__logo-text {
  font-family: var(--cis-font-serif);
  font-size: 17px;
  font-weight: 700;
  color: var(--cis-text-display);
  letter-spacing: -0.3px;
  white-space: nowrap;
}

.app-sidebar__scroll {
  flex: 1;
  overflow: hidden;
}
.app-sidebar__menu {
  border-right: none;
  padding: 4px 8px;
}
.menu-group-label {
  font-size: 10px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  text-transform: uppercase;
  letter-spacing: 1px;
}

/* 菜单项：Linear 风（左侧 2px 竖线） */
.app-sidebar__menu :deep(.el-menu-item-group__title) {
  padding-top: 20px;
  padding-bottom: 4px;
  padding-left: 12px !important;
}
.app-sidebar__menu :deep(.el-menu-item) {
  margin: 1px 0;
  border-radius: var(--cis-radius-btn);
  height: 36px;
  line-height: 36px;
  font-size: 13px;
  font-weight: 500;
  transition: background-color var(--cis-transition-fast), color var(--cis-transition-fast);
}
.app-sidebar__menu :deep(.el-menu-item:hover) {
  background-color: var(--cis-surface-2) !important;
  color: var(--cis-text-primary) !important;
}
.app-sidebar__menu :deep(.el-menu-item.is-active) {
  background-color: var(--cis-primary-tint) !important;
  color: var(--cis-primary-press) !important;
  font-weight: 600;
  box-shadow: inset 2px 0 0 var(--cis-primary);
}
.app-sidebar__menu :deep(.el-menu-item.is-active .el-icon) {
  color: var(--cis-primary) !important;
}

.app-sidebar__version {
  padding: 8px 16px;
  font-family: var(--cis-font-mono);
  font-size: 10px;
  color: var(--cis-text-tertiary);
  text-align: center;
  border-top: 1px solid var(--cis-border);
  flex-shrink: 0;
  letter-spacing: 0.5px;
  font-variant-numeric: tabular-nums;
}

.app-sidebar__collapse-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 12px 0;
  border: none;
  border-top: 1px solid var(--cis-border);
  background: transparent;
  cursor: pointer;
  color: var(--cis-text-tertiary);
  transition: color var(--cis-transition-fast), background-color var(--cis-transition-fast);
  flex-shrink: 0;
  font-family: inherit;
}
.app-sidebar__collapse-btn:hover {
  color: var(--cis-primary);
  background-color: var(--cis-surface-2);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .app-sidebar__collapse-btn,
  .fade-enter-active,
  .fade-leave-active {
    transition: none;
  }
  .app-sidebar__menu :deep(.el-menu-item) {
    transition: none;
  }
}
</style>
