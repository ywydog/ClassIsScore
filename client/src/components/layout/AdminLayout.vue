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
        <router-view v-slot="{ Component }">
          <transition name="fade-slide" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
      <el-footer class="admin-layout__footer" height="28px">
        <div class="admin-layout__footer-left">
          <span class="admin-layout__status-dot" :class="isConnected ? 'connected' : 'disconnected'"></span>
          <span class="admin-layout__status-text">{{ isConnected ? '已连接' : '未连接' }}</span>
        </div>
        <div class="admin-layout__footer-right">
          <span>ClassIsScore v1.0.0</span>
        </div>
      </el-footer>
    </el-container>
    <StatusToast />
  </el-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import StatusToast from '@/components/common/StatusToast.vue'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const isCollapsed = ref(false)
const isConnected = ref(false)

function toggleCollapse() {
  isCollapsed.value = !isCollapsed.value
}

onMounted(() => {
  connectWebSocket({
    onConnect: () => { isConnected.value = true },
    onDisconnect: () => { isConnected.value = false },
  })
})

onUnmounted(() => {
  disconnectWebSocket()
})
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
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
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

.admin-layout__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  background-color: var(--cis-header-bg);
  border-top: 1px solid var(--cis-border-color);
  font-size: 11px;
  color: var(--cis-text-tertiary);
  -webkit-app-region: drag;
}

.admin-layout__footer-left {
  display: flex;
  align-items: center;
  gap: 6px;
  -webkit-app-region: no-drag;
}

.admin-layout__status-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.admin-layout__status-dot.connected {
  background-color: #67c23a;
  box-shadow: 0 0 4px rgba(103, 194, 58, 0.5);
}

.admin-layout__status-dot.disconnected {
  background-color: #f56c6c;
}

.admin-layout__status-text {
  color: var(--cis-text-tertiary);
}

.admin-layout__footer-right {
  -webkit-app-region: no-drag;
}

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(8px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
