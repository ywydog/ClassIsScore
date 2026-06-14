import { createRouter, createWebHashHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import AdminLayout from '@/components/layout/AdminLayout.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/admin/dashboard',
  },
  {
    path: '/admin',
    component: AdminLayout,
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/admin/Dashboard.vue'),
        meta: { title: '总览', icon: 'HomeFilled' },
      },
      {
        path: 'scores',
        name: 'ScoreManagement',
        component: () => import('@/views/admin/ScoreManagement.vue'),
        meta: { title: '积分管理', icon: 'Trophy' },
      },
      {
        path: 'students',
        name: 'StudentManagement',
        component: () => import('@/views/admin/StudentManagement.vue'),
        meta: { title: '学生管理', icon: 'User' },
      },
      {
        path: 'students/:id',
        name: 'StudentProfile',
        component: () => import('@/views/admin/StudentProfile.vue'),
        meta: { title: '学生详情', icon: 'User' },
      },
      {
        path: 'groups',
        name: 'GroupManagement',
        component: () => import('@/views/admin/GroupManagement.vue'),
        meta: { title: '分组管理', icon: 'Grid' },
      },
      {
        path: 'leaderboard',
        name: 'Leaderboard',
        component: () => import('@/views/admin/Leaderboard.vue'),
        meta: { title: '排行榜', icon: 'Rank' },
      },
      {
        path: 'evaluation',
        name: 'AutoEvaluation',
        component: () => import('@/views/admin/AutoEvaluation.vue'),
        meta: { title: '自动评估', icon: 'Timer' },
      },
      {
        path: 'settlement',
        name: 'Settlement',
        component: () => import('@/views/admin/Settlement.vue'),
        meta: { title: '结算', icon: 'Finished' },
      },
      {
        path: 'settings',
        name: 'Settings',
        component: () => import('@/views/admin/Settings.vue'),
        meta: { title: '设置', icon: 'Setting' },
      },
      {
        path: 'plugins',
        name: 'PluginManagement',
        component: () => import('@/views/admin/PluginManagement.vue'),
        meta: { title: '插件管理', icon: 'Box' },
      },
      {
        path: 'themes',
        name: 'ThemeManagement',
        component: () => import('@/views/admin/ThemeManagement.vue'),
        meta: { title: '主题包', icon: 'Brush' },
      },
      {
        path: 'admin-settings',
        name: 'AdminSettings',
        component: () => import('@/views/admin/AdminSettings.vue'),
        meta: { title: '管理员设置', icon: 'Lock' },
      },
      {
        path: 'about',
        name: 'About',
        component: () => import('@/views/admin/About.vue'),
        meta: { title: '关于', icon: 'InfoFilled' },
      },
    ],
  },
  {
    path: '/display',
    name: 'ScoreDisplay',
    component: () => import('@/views/display/ScoreDisplay.vue'),
    meta: { title: '大屏展示' },
  },
  {
    path: '/floating',
    name: 'FloatingBar',
    component: () => import('@/views/floating/FloatingBar.vue'),
    meta: { title: '浮动积分条' },
  },
  {
    path: '/onboarding',
    name: 'Onboarding',
    component: () => import('@/views/onboarding/Onboarding.vue'),
    meta: { title: '引导' },
  },
]

const router = createRouter({
  history: createWebHashHistory(),
  routes,
})

// 路由守卫：首次启动跳转引导页
router.beforeEach((to, _from, next) => {
  if (to.path === '/onboarding' || to.path === '/display' || to.path === '/floating') {
    next()
    return
  }

  // 检查引导是否完成
  const onboardingDone = localStorage.getItem('onboardingCompleted') === 'true'
  if (!onboardingDone) {
    // 延迟检查：app store 可能还没初始化
    import('@/stores/app').then(({ useAppStore }) => {
      const appStore = useAppStore()
      if (!appStore.appState.isOnboardingCompleted && appStore.initialized) {
        next('/onboarding')
      } else {
        next()
      }
    }).catch(() => next())
  } else {
    next()
  }
})

export default router
