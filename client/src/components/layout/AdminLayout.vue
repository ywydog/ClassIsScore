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
          <router-link
            v-if="mobileEquivalent"
            :to="mobileEquivalent"
            class="admin-layout__footer-link"
          >切换到移动版</router-link>
          <span translate="no" class="cis-mono">ClassIsScore · v1.0.0</span>
        </div>
      </el-footer>
    </el-container>
    <StatusToast />
  </el-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import StatusToast from '@/components/common/StatusToast.vue'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const route = useRoute()
const router = useRouter()

// 桌面端页面 → 对应移动端页面映射（仅展示给桌面内嵌 admin 视图用）
const mobileEquivalent = computed(() => {
  if (!route.path.startsWith('/admin/')) return ''
  return route.path.replace(/^\/admin/, '/m')
})

// 触屏设备访问 /admin/* → 自动跳到对应 /m/*（避免 desktop admin 在 mobile viewport 挤压错位）
const mobileQuery = '(hover: none), (pointer: coarse)'

function isMobileViewport(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia(mobileQuery).matches
}

let adminResizeTimer: ReturnType<typeof setTimeout> | null = null

function maybeRedirectToMobile() {
  if (typeof window === 'undefined') return
  if (!isMobileViewport()) return
  if (route.path.startsWith('/admin/')) {
    const mobilePath = route.path.replace(/^\/admin/, '/m')
    router.replace(mobilePath).catch(() => { /* 重复导航忽略 */ })
  }
}

function debouncedAdminRedirect() {
  if (adminResizeTimer) clearTimeout(adminResizeTimer)
  adminResizeTimer = setTimeout(maybeRedirectToMobile, 200)
}

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

  // 触屏设备检测 → 跳移动端
  maybeRedirectToMobile()
  window.addEventListener('resize', debouncedAdminRedirect)

  connectWebSocket({
    onConnect: () => { isConnected.value = true },
    onDisconnect: () => { isConnected.value = false },
  })
})

onUnmounted(() => {
  disconnectWebSocket()
  if (adminResizeTimer) clearTimeout(adminResizeTimer)
  if (typeof window !== 'undefined') {
    window.removeEventListener('resize', debouncedAdminRedirect)
  }
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
  display: flex;
  align-items: center;
  gap: 12px;
}

.admin-layout__footer-link {
  color: var(--cis-text-secondary);
  text-decoration: none;
  font-size: 11px;
  padding: 2px 6px;
  border-radius: var(--cis-radius-btn);
  transition: color var(--cis-transition-fast), background-color var(--cis-transition-fast);
  -webkit-app-region: no-drag;
}

.admin-layout__footer-link:hover {
  color: var(--cis-primary);
  background: var(--cis-primary-tint);
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
