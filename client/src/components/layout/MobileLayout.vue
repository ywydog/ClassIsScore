<template>
  <div class="mobile-layout">
    <header class="mobile-layout__header" role="banner">
      <button
        type="button"
        class="mobile-layout__menu-btn"
        :aria-label="drawerOpen ? '关闭导航菜单' : '打开导航菜单'"
        :aria-expanded="drawerOpen"
        @click="drawerOpen = !drawerOpen"
      >
        <el-icon :size="22" aria-hidden="true"><Expand v-if="!drawerOpen" /><Fold v-else /></el-icon>
      </button>
      <h1 class="mobile-layout__title">{{ pageTitle }}</h1>
      <el-tooltip content="切换到桌面版视图" placement="bottom">
        <button
          type="button"
          class="mobile-layout__desktop-btn"
          aria-label="切换到桌面版视图"
          @click="switchToDesktop"
        >
          <el-icon :size="20" aria-hidden="true"><Monitor /></el-icon>
        </button>
      </el-tooltip>
      <button
        type="button"
        class="mobile-layout__display-btn"
        aria-label="打开大屏展示"
        @click="openDisplay"
      >
        <el-icon :size="20" aria-hidden="true"><Monitor /></el-icon>
      </button>
    </header>

    <main class="mobile-layout__main" :aria-label="pageTitle">
      <router-view v-slot="{ Component }">
        <transition name="page-fade" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </main>

    <nav class="mobile-layout__bottom-nav" role="navigation" aria-label="主导航">
      <router-link
        v-for="item in bottomNav"
        :key="item.path"
        :to="item.path"
        class="mobile-layout__nav-item"
        active-class="mobile-layout__nav-item--active"
        :aria-current="route.path.startsWith(item.path) ? 'page' : undefined"
        :aria-label="item.label"
      >
        <el-icon :size="22" aria-hidden="true"><component :is="item.icon" /></el-icon>
        <span class="mobile-layout__nav-label">{{ item.label }}</span>
      </router-link>
    </nav>

    <el-drawer
      v-model="drawerOpen"
      direction="ltr"
      :with-header="false"
      size="78%"
      class="mobile-layout__drawer"
      :aria-label="'次级导航菜单'"
    >
      <div class="mobile-layout__drawer-content">
        <div class="mobile-layout__drawer-header">
          <div class="mobile-layout__drawer-logo" aria-hidden="true">
            <span class="mobile-layout__drawer-logo-text">C</span>
          </div>
          <div class="mobile-layout__drawer-title" translate="no">ClassIsScore</div>
        </div>
        <el-scrollbar class="mobile-layout__drawer-scroll">
          <ul class="mobile-layout__drawer-list" role="list">
            <li v-for="group in drawerMenu" :key="group.title" class="mobile-layout__drawer-group">
              <h2 class="cis-eyebrow mobile-layout__drawer-group-title" :id="`drawer-group-${group.title}`">
                {{ group.title }}
              </h2>
              <ul role="list">
                <li v-for="item in group.items" :key="item.path" role="listitem">
                  <router-link
                    :to="item.path"
                    class="mobile-layout__drawer-item"
                    active-class="mobile-layout__drawer-item--active"
                    :aria-current="route.path === item.path ? 'page' : undefined"
                    @click="drawerOpen = false"
                  >
                    <el-icon :size="18" aria-hidden="true"><component :is="item.icon" /></el-icon>
                    <span>{{ item.label }}</span>
                  </router-link>
                </li>
              </ul>
            </li>
          </ul>
        </el-scrollbar>
        <div class="mobile-layout__drawer-footer">
          <span translate="no" class="cis-mono">ClassIsScore · v1.0.0</span>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  HomeFilled,
  Trophy,
  User,
  Grid,
  Rank,
  Timer,
  Finished,
  Setting,
  Box,
  Brush,
  Lock,
  InfoFilled,
  Monitor,
  Expand,
  Fold,
} from '@element-plus/icons-vue'
import { invoke } from '@/services/tauri'

const route = useRoute()
const router = useRouter()
const drawerOpen = ref(false)

let resizeTimer: ReturnType<typeof setTimeout> | null = null

// 真桌面设备判定：hover + pointer + 宽度同时满足才跳
// 触屏设备 hover: none / pointer: coarse → 永远留在 mobile（手机横屏不会被误判）
const desktopQuery = '(hover: hover) and (pointer: fine) and (min-width: 1024px)'

function isDesktopViewport(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia(desktopQuery).matches
}

function maybeRedirectToDesktop() {
  if (typeof window === 'undefined') return
  // 用户手动切换过视图 → 尊重选择，不自动跳转
  if (localStorage.getItem('viewPreference') === 'mobile') return
  if (!isDesktopViewport()) return
  // 仅在 /m/* 路由下才执行跳转
  if (route.path.startsWith('/m/')) {
    const desktopPath = route.path.replace(/^\/m/, '/admin')
    router.replace(desktopPath).catch(() => { /* 重复导航忽略 */ })
  }
}

function debouncedRedirect() {
  if (resizeTimer) clearTimeout(resizeTimer)
  resizeTimer = setTimeout(maybeRedirectToDesktop, 200)
}

onMounted(() => {
  maybeRedirectToDesktop()
  window.addEventListener('resize', debouncedRedirect)
})

onBeforeUnmount(() => {
  if (resizeTimer) clearTimeout(resizeTimer)
  if (typeof window !== 'undefined') {
    window.removeEventListener('resize', debouncedRedirect)
  }
})

const pageTitles: Record<string, string> = {
  '/m/dashboard': '总览',
  '/m/scores': '积分管理',
  '/m/students': '学生管理',
  '/m/groups': '分组管理',
  '/m/leaderboard': '排行榜',
  '/m/evaluation': '自动评估',
  '/m/settlement': '结算',
  '/m/settings': '设置',
  '/m/plugins': '插件管理',
  '/m/themes': '主题包',
  '/m/admin-settings': '管理员设置',
  '/m/about': '关于',
  '/display': '大屏展示',
}

const pageTitle = computed(() => pageTitles[route.path] || 'ClassIsScore')

const bottomNav = [
  { path: '/m/dashboard', label: '总览', icon: HomeFilled },
  { path: '/m/scores', label: '积分', icon: Trophy },
  { path: '/m/students', label: '学生', icon: User },
  { path: '/m/leaderboard', label: '排行', icon: Rank },
  { path: '/m/settings', label: '设置', icon: Setting },
]

const drawerMenu = [
  {
    title: 'Core',
    items: [
      { path: '/m/dashboard', label: '总览', icon: HomeFilled },
      { path: '/m/scores', label: '积分管理', icon: Trophy },
      { path: '/m/students', label: '学生管理', icon: User },
      { path: '/m/groups', label: '分组管理', icon: Grid },
      { path: '/m/leaderboard', label: '排行榜', icon: Rank },
    ],
  },
  {
    title: 'Advanced',
    items: [
      { path: '/m/evaluation', label: '自动评估', icon: Timer },
      { path: '/m/settlement', label: '结算', icon: Finished },
    ],
  },
  {
    title: 'System',
    items: [
      { path: '/m/settings', label: '设置', icon: Setting },
      { path: '/m/plugins', label: '插件管理', icon: Box },
      { path: '/m/themes', label: '主题包', icon: Brush },
      { path: '/m/admin-settings', label: '管理员设置', icon: Lock },
      { path: '/m/about', label: '关于', icon: InfoFilled },
    ],
  },
]

function openDisplay() {
  invoke('open_display_window').catch(() => {
    // 非 Tauri 环境：使用路由跳转
    if (typeof window !== 'undefined') {
      window.location.hash = '#/display'
    }
  })
}

function switchToDesktop() {
  // 记住用户手动选择
  localStorage.setItem('viewPreference', 'desktop')
  const desktopPath = route.path.replace(/^\/m/, '/admin')
  router.push(desktopPath).catch(() => { /* 重复导航忽略 */ })
}
</script>

<style scoped>
.mobile-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  min-height: 100dvh;
  background: var(--cis-canvas);
  padding-top: var(--cis-safe-top, 0);
  padding-bottom: var(--cis-safe-bottom, 0);
}

.mobile-layout__header {
  position: sticky;
  top: 0;
  z-index: 10;
  display: flex;
  align-items: center;
  height: 48px;
  padding: 0 8px;
  background: var(--cis-surface-1);
  border-bottom: 1px solid var(--cis-border);
}

.mobile-layout__menu-btn,
.mobile-layout__display-btn,
.mobile-layout__desktop-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border: none;
  border-radius: var(--cis-radius-btn);
  background: transparent;
  color: var(--cis-text-secondary);
  cursor: pointer;
  font-family: inherit;
  transition: background-color var(--cis-transition-fast), color var(--cis-transition-fast);
}

.mobile-layout__menu-btn:hover,
.mobile-layout__display-btn:hover,
.mobile-layout__desktop-btn:hover {
  background: var(--cis-primary-tint);
  color: var(--cis-primary);
}

.mobile-layout__title {
  flex: 1;
  font-family: var(--cis-font-serif);
  font-size: 16px;
  font-weight: 600;
  color: var(--cis-text-display);
  margin: 0;
  padding: 0 8px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.mobile-layout__main {
  flex: 1;
  padding: 12px 12px 80px;
  /* 底部 80px 留给 BottomNav */
}

.mobile-layout__bottom-nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 10;
  display: flex;
  align-items: stretch;
  justify-content: space-around;
  height: calc(56px + var(--cis-safe-bottom, 0));
  padding-bottom: var(--cis-safe-bottom, 0);
  background: var(--cis-surface-1);
  border-top: 1px solid var(--cis-border);
}

.mobile-layout__nav-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  color: var(--cis-text-tertiary);
  text-decoration: none;
  transition: color var(--cis-transition-fast);
  min-height: 48px;
  -webkit-tap-highlight-color: transparent;
  position: relative;
}

.mobile-layout__nav-item--active {
  color: var(--cis-primary);
}

/* 底部 nav 激活态：顶部 2px primary underline（Linear 风） */
.mobile-layout__nav-item--active::before {
  content: '';
  position: absolute;
  top: 0;
  left: 20%;
  right: 20%;
  height: 2px;
  background: var(--cis-primary);
  border-radius: 0 0 1px 1px;
}

.mobile-layout__nav-label {
  font-size: 11px;
  font-weight: 500;
}

.mobile-layout__drawer-content {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--cis-surface-1);
  padding-top: var(--cis-safe-top, 0);
}

.mobile-layout__drawer-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px 16px;
  border-bottom: 1px solid var(--cis-border);
}

.mobile-layout__drawer-logo {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--cis-radius-btn);
  background: var(--cis-primary);
  color: #fff;
  font-family: var(--cis-font-serif);
  font-weight: 700;
  font-size: 16px;
  flex-shrink: 0;
}

.mobile-layout__drawer-logo-text {
  line-height: 1;
}

.mobile-layout__drawer-title {
  font-family: var(--cis-font-serif);
  font-size: 16px;
  font-weight: 600;
  color: var(--cis-text-display);
}

.mobile-layout__drawer-scroll {
  flex: 1;
}

.mobile-layout__drawer-list {
  list-style: none;
  margin: 0;
  padding: 8px 0;
}

.mobile-layout__drawer-group-title {
  font-size: 11px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.4px;
  margin: 16px 16px 6px;
}

.mobile-layout__drawer-item {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 44px;
  padding: 10px 16px;
  color: var(--cis-text-secondary);
  text-decoration: none;
  transition: background-color var(--cis-transition-fast), color var(--cis-transition-fast);
  -webkit-tap-highlight-color: transparent;
  position: relative;
}

.mobile-layout__drawer-item--active {
  color: var(--cis-primary);
  background: var(--cis-primary-tint);
  font-weight: 500;
  box-shadow: inset 2px 0 0 var(--cis-primary);
}

.mobile-layout__drawer-footer {
  padding: 12px 16px;
  font-size: 11px;
  color: var(--cis-text-tertiary);
  border-top: 1px solid var(--cis-border);
  text-align: center;
}

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
  .mobile-layout__nav-item,
  .mobile-layout__menu-btn,
  .mobile-layout__display-btn,
  .mobile-layout__drawer-item,
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
