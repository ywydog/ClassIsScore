<template>
  <div class="app-sidebar">
    <div class="app-sidebar__logo">
      <div class="app-sidebar__logo-icon">
        <el-icon :size="22"><Trophy /></el-icon>
      </div>
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
          <template #title><span v-if="!collapsed" class="menu-group-label">核心功能</span></template>
          <el-menu-item index="/admin/dashboard">
            <el-icon><HomeFilled /></el-icon>
            <template #title>总览</template>
          </el-menu-item>
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
          <template #title><span v-if="!collapsed" class="menu-group-label">高级功能</span></template>
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
          <template #title><span v-if="!collapsed" class="menu-group-label">系统</span></template>
          <el-menu-item index="/admin/settings">
            <el-icon><Setting /></el-icon>
            <template #title>设置</template>
          </el-menu-item>
          <el-menu-item index="/admin/plugins">
            <el-icon><Box /></el-icon>
            <template #title>插件管理</template>
          </el-menu-item>
          <el-menu-item index="/admin/themes">
            <el-icon><Brush /></el-icon>
            <template #title>主题包</template>
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
    <div class="app-sidebar__collapse-btn" @click="$emit('toggle')">
      <el-icon :size="16">
        <Expand v-if="collapsed" />
        <Fold v-else />
      </el-icon>
    </div>
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
}

.app-sidebar__logo {
  height: 52px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 0 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
  flex-shrink: 0;
}

.app-sidebar__logo-icon {
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--cis-gradient-primary);
  border-radius: var(--cis-radius-md);
  color: #fff;
  flex-shrink: 0;
  box-shadow: 0 2px 8px rgba(13, 148, 136, 0.3);
}

.app-sidebar__logo-text {
  font-family: var(--cis-font-family-display);
  font-size: 17px;
  font-weight: 700;
  background: var(--cis-gradient-primary);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
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

.app-sidebar__menu :deep(.el-menu-item-group__title) {
  padding-top: 20px;
  padding-bottom: 4px;
  padding-left: 12px !important;
}

.app-sidebar__menu :deep(.el-menu-item) {
  margin: 2px 0;
  border-radius: var(--cis-radius-md);
  height: 40px;
  line-height: 40px;
  transition: all var(--cis-transition-fast);
}

.app-sidebar__menu :deep(.el-menu-item:hover) {
  background-color: var(--cis-primary-light-9) !important;
}

.app-sidebar__menu :deep(.el-menu-item.is-active) {
  background: var(--cis-gradient-primary) !important;
  color: #fff !important;
  font-weight: 600;
  box-shadow: var(--cis-shadow-glow);
}

.app-sidebar__menu :deep(.el-menu-item.is-active .el-icon) {
  color: #fff !important;
}

.app-sidebar__version {
  padding: 8px 16px;
  font-size: 10px;
  color: var(--cis-text-tertiary);
  text-align: center;
  border-top: 1px solid var(--cis-border-color-light);
  flex-shrink: 0;
  letter-spacing: 0.5px;
}

.app-sidebar__collapse-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 12px 0;
  border-top: 1px solid var(--cis-border-color-light);
  cursor: pointer;
  color: var(--cis-text-tertiary);
  transition: color var(--cis-transition-fast), background-color var(--cis-transition-fast);
  flex-shrink: 0;
}

.app-sidebar__collapse-btn:hover {
  color: var(--cis-primary);
  background-color: var(--cis-primary-light-9);
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
