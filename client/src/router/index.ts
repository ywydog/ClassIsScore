import { createRouter, createWebHashHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import AdminLayout from '@/components/layout/AdminLayout.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/admin/scores',
  },
  {
    path: '/admin',
    component: AdminLayout,
    children: [
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

export default router
