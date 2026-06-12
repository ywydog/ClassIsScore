<template>
  <el-container class="admin-layout">
    <el-aside :width="isCollapsed ? '64px' : '220px'" class="admin-layout__aside">
      <AppSidebar :collapsed="isCollapsed" @toggle="toggleCollapse" />
    </el-aside>
    <el-container class="admin-layout__main">
      <el-header class="admin-layout__header" height="48px">
        <AppHeader @toggle-sidebar="toggleCollapse" />
      </el-header>
      <el-main class="admin-layout__content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'

const isCollapsed = ref(false)

function toggleCollapse() {
  isCollapsed.value = !isCollapsed.value
}
</script>

<style scoped>
.admin-layout {
  height: 100vh;
  width: 100vw;
  overflow: hidden;
}

.admin-layout__aside {
  background-color: var(--cis-sidebar-bg);
  border-right: 1px solid var(--cis-border-color);
  transition: width 0.3s ease;
  overflow: hidden;
}

.admin-layout__main {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.admin-layout__header {
  -webkit-app-region: drag;
  display: flex;
  align-items: center;
  background-color: var(--cis-header-bg);
  border-bottom: 1px solid var(--cis-border-color);
  padding: 0 16px;
}

.admin-layout__content {
  flex: 1;
  overflow-y: auto;
  background-color: var(--cis-bg);
  padding: 20px;
}
</style>
