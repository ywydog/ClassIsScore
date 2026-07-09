import { createRouter, createWebHashHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import AdminLayout from '@/components/layout/AdminLayout.vue'
import MobileLayout from '@/components/layout/MobileLayout.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/admin/dashboard',
  },
  {
    // 桌面端：嵌套 AdminLayout
    path: '/admin',
    component: AdminLayout,
    meta: { layout: 'admin' },
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
    // 移动端：扁平化路由，MobileLayout 包裹
    path: '/m',
    component: MobileLayout,
    meta: { layout: 'mobile' },
    children: [
      {
        path: '',
        redirect: '/m/dashboard',
      },
      {
        path: 'dashboard',
        name: 'MobileDashboard',
        component: () => import('@/views/mobile/Dashboard.vue'),
        meta: { title: '总览' },
      },
      {
        path: 'scores',
        name: 'MobileScoreManagement',
        component: () => import('@/views/mobile/ScoreManagement.vue'),
        meta: { title: '积分' },
      },
      {
        path: 'students',
        name: 'MobileStudentManagement',
        component: () => import('@/views/mobile/StudentManagement.vue'),
        meta: { title: '学生' },
      },
      {
        path: 'students/:id',
        name: 'MobileStudentProfile',
        component: () => import('@/views/mobile/StudentProfile.vue'),
        meta: { title: '学生详情' },
        props: true,
      },
      {
        path: 'groups',
        name: 'MobileGroupManagement',
        component: () => import('@/views/mobile/GroupManagement.vue'),
        meta: { title: '分组' },
      },
      {
        path: 'leaderboard',
        name: 'MobileLeaderboard',
        component: () => import('@/views/mobile/Leaderboard.vue'),
        meta: { title: '排行' },
      },
      {
        path: 'evaluation',
        name: 'MobileAutoEvaluation',
        component: () => import('@/views/mobile/Evaluation.vue'),
        meta: { title: '评估' },
      },
      {
        path: 'settlement',
        name: 'MobileSettlement',
        component: () => import('@/views/mobile/Settlement.vue'),
        meta: { title: '结算' },
      },
      {
        path: 'settings',
        name: 'MobileSettings',
        component: () => import('@/views/mobile/Settings.vue'),
        meta: { title: '设置' },
      },
      {
        path: 'plugins',
        name: 'MobilePluginManagement',
        component: () => import('@/views/mobile/PluginManagement.vue'),
        meta: { title: '插件' },
      },
      {
        path: 'themes',
        name: 'MobileThemeManagement',
        component: () => import('@/views/mobile/ThemeManagement.vue'),
        meta: { title: '主题' },
      },
      {
        path: 'admin-settings',
        name: 'MobileAdminSettings',
        component: () => import('@/views/mobile/AdminSettings.vue'),
        meta: { title: '管理员' },
      },
      {
        path: 'about',
        name: 'MobileAbout',
        component: () => import('@/views/mobile/About.vue'),
        meta: { title: '关于' },
      },
    ],
  },
  {
    // 桌面：大屏展示走独立窗口；移动端：单窗口内 /display 路由
    path: '/display',
    name: 'ScoreDisplay',
    component: () => import('@/views/display/ScoreDisplay.vue'),
    meta: { title: '大屏展示', layout: 'none' },
  },
  {
    path: '/onboarding',
    name: 'Onboarding',
    component: () => import('@/views/onboarding/Onboarding.vue'),
    meta: { title: '引导', layout: 'none' },
  },
]

const router = createRouter({
  history: createWebHashHistory(),
  routes,
})

// 路由守卫：首次启动引导
router.beforeEach(async (to, _from, next) => {
  // 引导页面和展示页面不需要检查
  if (to.name === 'Onboarding' || to.name === 'ScoreDisplay') {
    next()
    return
  }

  // 检查是否已完成引导（localStorage 快速路径）
  if (localStorage.getItem('onboardingCompleted') === 'true') {
    next()
    return
  }

  // 检查后端设置的引导状态
  try {
    const { invoke } = await import('@/services/tauri')
    const settings = await invoke<Array<{ setting_key: string; setting_value: string | null }>>('settings_get_all', {})
    const onboardingSetting = settings.find(s => s.setting_key === 'onboardingCompleted')
    if (onboardingSetting?.setting_value === 'true') {
      localStorage.setItem('onboardingCompleted', 'true')
      next()
      return
    }
  } catch {
    // 后端不可用，检查 localStorage
    if (localStorage.getItem('onboardingCompleted') === 'true') {
      next()
      return
    }
  }

  // 未完成引导，重定向到引导页面
  next({ name: 'Onboarding' })
})

export default router
