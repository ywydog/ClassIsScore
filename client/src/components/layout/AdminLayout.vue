<template>
  <el-container class="admin-layout">
    <el-aside
      :width="isCollapsed ? '64px' : '232px'"
      class="admin-layout__aside"
      aria-label="主导航"
    >
      <AppSidebar :collapsed="isCollapsed" @toggle="toggleCollapse" />
    </el-aside>
    <el-container class="admin-layout__main">
      <el-header class="admin-layout__header" height="56px">
        <AppHeader @toggle-sidebar="toggleCollapse" />
      </el-header>
      <el-main class="admin-layout__content">
        <router-view v-slot="{ Component }">
          <transition name="page-fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
      <el-footer class="admin-layout__footer" height="26px">
        <div class="admin-layout__footer-left">
          <span
            class="admin-layout__status-dot"
            :class="isConnected ? 'connected' : 'disconnected'"
            :aria-label="isConnected ? '服务已连接' : '服务未连接'"
            role="status"
          ></span>
          <span class="admin-layout__status-text" aria-live="polite">{{ isConnected ? '已连接' : '未连接' }}</span>
          <span class="admin-layout__footer-sep" aria-hidden="true">·</span>
          <span class="admin-layout__status-time cis-num">{{ nowText }}</span>
        </div>
        <div class="admin-layout__footer-right">
          <span translate="no" class="cis-mono">ClassIsScore · v1.0.0</span>
        </div>
      </el-footer>
    </el-container>
    <StatusToast />
  </el-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, onBeforeUnmount } from 'vue'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import StatusToast from '@/components/common/StatusToast.vue'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const isCollapsed = ref(false)
const isConnected = ref(false)
const nowText = ref('')

let timeTimer: ReturnType<typeof setInterval> | null = null

function updateTime() {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  nowText.value = `${pad(d.getHours())}:${pad(d.getMinutes())}`
}

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

  updateTime()
  timeTimer = setInterval(updateTime, 30000)

  connectWebSocket({
    onConnect: () => { isConnected.value = true },
    onDisconnect: () => { isConnected.value = false },
  })
})

onUnmounted(() => {
  disconnectWebSocket()
})

onBeforeUnmount(() => {
  if (timeTimer) clearInterval(timeTimer)
})
</script>

<style scoped>
.admin-layout {
  height: 100vh;
  width: 100vw;
  overflow: hidden;
  background: var(--cis-canvas);
}

.admin-layout__aside {
  background: var(--cis-sidebar-bg);
  border-right: 1px solid var(--cis-border);
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
  /* Apple 风格 frosted-glass：80% 透明白 + backdrop-filter blur(20px) saturate(180%) */
  background: rgba(255, 255, 255, 0.78);
  backdrop-filter: saturate(180%) blur(20px);
  -webkit-backdrop-filter: saturate(180%) blur(20px);
  border-bottom: 1px solid var(--cis-border);
  padding: 0 20px;
  /* 防止 backdrop-filter 与内容重叠时的边缘白边 */
  position: relative;
  z-index: 5;
}

/* 暗色主题下 frosted-glass 用深色墨海 */
:global([data-theme='dark']) .admin-layout__header {
  background: rgba(11, 18, 32, 0.78);
}

.admin-layout__content {
  flex: 1;
  overflow-y: auto;
  background-color: var(--cis-canvas);
  padding: 24px;
}

.admin-layout__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  /* Apple 风格 footer：frosted-glass + 12px 链接列呼吸行高 1.8 */
  background: rgba(255, 255, 255, 0.78);
  backdrop-filter: saturate(180%) blur(20px);
  -webkit-backdrop-filter: saturate(180%) blur(20px);
  border-top: 1px solid var(--cis-border);
  font-size: 11px;
  color: var(--cis-text-tertiary);
  -webkit-app-region: drag;
  line-height: 1.8;
  letter-spacing: 0.1px;
}

:global([data-theme='dark']) .admin-layout__footer {
  background: rgba(11, 18, 32, 0.78);
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
  border-radius: 9999px;
  background-color: var(--cis-border-strong);
  transition: background-color var(--cis-transition-fast);
}

.admin-layout__status-dot.connected {
  background-color: var(--cis-success);
}

.admin-layout__status-dot.disconnected {
  background-color: var(--cis-text-tertiary);
}

.admin-layout__status-text {
  color: var(--cis-text-tertiary);
}

.admin-layout__footer-sep {
  color: var(--cis-border-strong);
}

.admin-layout__status-time {
  color: var(--cis-text-tertiary);
  font-size: 11px;
}

.admin-layout__footer-right {
  -webkit-app-region: no-drag;
  color: var(--cis-text-tertiary);
  font-size: 11px;
  letter-spacing: 0.2px;
}

/* 页面切换动画：克制的 fade，不要 translateY（避免 12px 滑动感） */
.page-fade-enter-active {
  transition: opacity 0.18s ease;
}
.page-fade-leave-active {
  transition: opacity 0.12s ease;
}
.page-fade-enter-from {
  opacity: 0;
}
.page-fade-leave-to {
  opacity: 0;
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
