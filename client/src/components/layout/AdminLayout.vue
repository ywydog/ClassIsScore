<template>
  <el-container class="admin-layout">
    <el-aside
      :width="isCollapsed ? '64px' : '240px'"
      class="admin-layout__aside"
      aria-label="主导航"
    >
      <AppSidebar :collapsed="isCollapsed" @toggle="toggleCollapse" />
    </el-aside>
    <el-container class="admin-layout__main">
      <el-header class="admin-layout__header" height="52px">
        <AppHeader @toggle-sidebar="toggleCollapse" />
      </el-header>
      <el-main class="admin-layout__content">
        <router-view v-slot="{ Component }">
          <transition name="page-fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
      <el-footer class="admin-layout__footer" height="28px">
        <div class="admin-layout__footer-left">
          <span
            class="admin-layout__status-dot"
            :class="isConnected ? 'connected' : 'disconnected'"
            :aria-label="isConnected ? '服务已连接' : '服务未连接'"
            role="status"
          ></span>
          <span class="admin-layout__status-text" aria-live="polite">{{ isConnected ? '已连接' : '未连接' }}</span>
        </div>
        <div class="admin-layout__footer-right">
          <span translate="no">ClassIsScore v1.0.0</span>
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
  localStorage.setItem('sidebarCollapsed', JSON.stringify(isCollapsed.value))
}

onMounted(() => {
  // 从 localStorage 恢复折叠状态
  try {
    const stored = localStorage.getItem('sidebarCollapsed')
    if (stored !== null) {
      isCollapsed.value = JSON.parse(stored)
    }
  } catch { /* ignore */ }

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
  background: var(--cis-sidebar-bg);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-right: 1px solid var(--cis-border-color-light);
  transition: width var(--cis-transition-normal);
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
  background: var(--cis-header-bg);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border-bottom: 1px solid var(--cis-border-color-light);
  padding: 0 20px;
}

.admin-layout__content {
  flex: 1;
  overflow-y: auto;
  background-color: var(--cis-bg);
  padding: 24px;
}

.admin-layout__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  background: var(--cis-header-bg);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  border-top: 1px solid var(--cis-border-color-light);
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
  transition: background-color var(--cis-transition-fast), box-shadow var(--cis-transition-fast);
}

.admin-layout__status-dot.connected {
  background-color: var(--cis-success);
  box-shadow: 0 0 6px rgba(34, 197, 94, 0.4);
}

.admin-layout__status-dot.disconnected {
  background-color: var(--cis-danger);
}

.admin-layout__status-text {
  color: var(--cis-text-tertiary);
}

.admin-layout__footer-right {
  -webkit-app-region: no-drag;
}

/* 页面切换动画 */
.page-fade-enter-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.page-fade-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.page-fade-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.page-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}

@media (prefers-reduced-motion: reduce) {
  .admin-layout__aside,
  .admin-layout__status-dot,
  .page-fade-enter-active,
  .page-fade-leave-active {
    transition: none;
  }
  .page-fade-enter-from,
  .page-fade-leave-to {
    transform: none;
  }
}
</style>
