<template>
  <div class="app-sidebar">
    <div class="app-sidebar__logo">
      <el-icon :size="24" color="var(--cis-primary)"><Trophy /></el-icon>
      <span v-if="!collapsed" class="app-sidebar__logo-text">ClassIsScore</span>
    </div>
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
      <el-menu-item index="/admin/evaluation">
        <el-icon><Timer /></el-icon>
        <template #title>自动评估</template>
      </el-menu-item>
      <el-menu-item index="/admin/settlement">
        <el-icon><Finished /></el-icon>
        <template #title>结算</template>
      </el-menu-item>
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
    </el-menu>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import {
  Trophy,
  User,
  Grid,
  Rank,
  Timer,
  Finished,
  Setting,
  Lock,
  InfoFilled,
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
}

.app-sidebar__logo-img {
  width: 24px;
  height: 24px;
}

.app-sidebar__logo-text {
  font-size: 16px;
  font-weight: 700;
  color: var(--cis-primary);
  white-space: nowrap;
}

.app-sidebar__menu {
  border-right: none;
  flex: 1;
  overflow-y: auto;
}
</style>
