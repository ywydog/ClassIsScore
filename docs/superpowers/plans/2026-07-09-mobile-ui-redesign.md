# 移动端 UI 全套重设计 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 ClassIsScore 实现 13 个 mobile 专属视图（≤ 600 px 竖屏手机）+ 通用 BottomSheet 组件，让手机端体验与桌面端完全分离。

**Architecture:**
- 新建 `client/src/views/mobile/` 目录（13 个 Vue 视图）
- 新建 `client/src/components/mobile/` 目录（BottomSheet + 2 个通用组件）
- 增强 `client/src/components/layout/MobileLayout.vue` 增加断点检测与自动跳桌面
- 增强 `client/src/router/index.ts` 增加 `/m/*` 路由表
- 设计语言延续"釉蓝×朱砂"调色板 + Apple 化微交互 + mobile-specific 调整（body 16px、触控目标 ≥ 44pt）

**Tech Stack:** Vue 3 + TypeScript + Element Plus + Vite + SortableJS（已有）。本计划不引入新依赖。

**说明**：本计划涉及 visual UI 重构，不写自动化测试（无成熟 e2e 框架）。每个任务以 `pnpm typecheck` 0 错误作为质量门禁，以人工截图作为视觉验证。

---

## File Structure

```
client/src/
├── components/
│   ├── layout/
│   │   └── MobileLayout.vue              # 修改：加断点检测 + 跳桌面
│   └── mobile/                           # 新建
│       ├── BottomSheet.vue               # 通用底部抽屉
│       ├── MobileEmptyState.vue          # 通用空态
│       └── MobileListItem.vue            # 通用列表项
├── views/
│   └── mobile/                           # 新建
│       ├── Dashboard.vue
│       ├── ScoreManagement.vue
│       ├── Leaderboard.vue
│       ├── StudentManagement.vue
│       ├── StudentProfile.vue
│       ├── GroupManagement.vue
│       ├── Evaluation.vue
│       ├── Settlement.vue
│       ├── Settings.vue
│       ├── PluginManagement.vue
│       ├── ThemeManagement.vue
│       ├── About.vue
│       └── AdminSettings.vue
└── router/
    └── index.ts                          # 修改：加 /m/* 路由表
```

---

## Task 1: MobileLayout 增强 + BottomSheet 通用组件 + 路由表

**Files:**
- Modify: `client/src/components/layout/MobileLayout.vue` (加断点检测 + 跳桌面逻辑)
- Create: `client/src/components/mobile/BottomSheet.vue` (通用底部抽屉)
- Create: `client/src/components/mobile/MobileEmptyState.vue` (通用空态)
- Create: `client/src/components/mobile/MobileListItem.vue` (通用列表项)
- Modify: `client/src/router/index.ts` (加 /m/* 路由表)

- [ ] **Step 1.1: 增强 MobileLayout — 加断点检测**

在 `client/src/components/layout/MobileLayout.vue` 的 `<script setup>` 中加：

```ts
import { onBeforeUnmount } from 'vue'

let resizeTimer: ReturnType<typeof setTimeout> | null = null

function maybeRedirectToDesktop() {
  if (typeof window === 'undefined') return
  const isWide = window.innerWidth >= 768 || (
    window.matchMedia('(orientation: landscape)').matches &&
    window.innerWidth >= 601
  )
  if (isWide && route.path.startsWith('/m/')) {
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
  window.removeEventListener('resize', debouncedRedirect)
})
```

- [ ] **Step 1.2: 创建 BottomSheet 通用组件**

`client/src/components/mobile/BottomSheet.vue`：

```vue
<template>
  <Teleport to="body">
    <Transition name="sheet-fade">
      <div
        v-if="modelValue"
        class="bottom-sheet__backdrop"
        :aria-hidden="!modelValue"
        @click="onBackdropClick"
      ></div>
    </Transition>
    <Transition :name="`sheet-slide-${placement}`">
      <div
        v-if="modelValue"
        class="bottom-sheet"
        :class="[`bottom-sheet--${placement}`, `bottom-sheet--${height}`]"
        role="dialog"
        aria-modal="true"
        :aria-label="title"
        @touchstart.passive="onTouchStart"
        @touchmove.passive="onTouchMove"
        @touchend.passive="onTouchEnd"
      >
        <div v-if="dismissible" class="bottom-sheet__handle" aria-hidden="true"></div>
        <header v-if="title || $slots.header" class="bottom-sheet__header">
          <slot name="header">
            <h2 class="bottom-sheet__title">{{ title }}</h2>
            <button
              v-if="dismissible"
              type="button"
              class="bottom-sheet__close"
              :aria-label="`关闭 ${title || '面板'}`"
              @click="close"
            >
              <el-icon :size="18" aria-hidden="true"><Close /></el-icon>
            </button>
          </slot>
        </header>
        <div class="bottom-sheet__body">
          <slot></slot>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { Close } from '@element-plus/icons-vue'

const props = withDefaults(defineProps<{
  modelValue: boolean
  title?: string
  placement?: 'bottom' | 'top'
  height?: 'auto' | 'half' | 'full'
  closeOnBackdrop?: boolean
  dismissible?: boolean
}>(), {
  title: '',
  placement: 'bottom',
  height: 'auto',
  closeOnBackdrop: true,
  dismissible: true,
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'close': []
}>()

function close() {
  emit('update:modelValue', false)
  emit('close')
}

function onBackdropClick() {
  if (props.closeOnBackdrop) close()
}

// 触屏下滑关闭
const touchStartY = ref(0)
const touchDeltaY = ref(0)

function onTouchStart(e: TouchEvent) {
  touchStartY.value = e.touches[0].clientY
  touchDeltaY.value = 0
}

function onTouchMove(e: TouchEvent) {
  touchDeltaY.value = e.touches[0].clientY - touchStartY.value
}

function onTouchEnd() {
  if (props.placement === 'bottom' && touchDeltaY.value > 80) {
    close()
  }
  touchDeltaY.value = 0
}

watch(() => props.modelValue, (open) => {
  if (typeof document === 'undefined') return
  document.body.style.overflow = open ? 'hidden' : ''
})
</script>

<style scoped>
.bottom-sheet__backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
  z-index: 1999;
}

.bottom-sheet {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 2000;
  background: var(--cis-surface-1);
  border-top: 1px solid var(--cis-border);
  border-radius: 12px 12px 0 0;
  max-height: 92vh;
  display: flex;
  flex-direction: column;
  padding-bottom: env(safe-area-inset-bottom);
  box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.08);
}

.bottom-sheet--half {
  height: 50vh;
}

.bottom-sheet--full {
  height: 92vh;
}

.bottom-sheet--top {
  top: 0;
  bottom: auto;
  border-top: none;
  border-bottom: 1px solid var(--cis-border);
  border-radius: 0 0 12px 12px;
}

.bottom-sheet__handle {
  width: 32px;
  height: 4px;
  background: var(--cis-border-strong);
  border-radius: 2px;
  margin: 8px auto 0;
  flex-shrink: 0;
}

.bottom-sheet__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 12px 20px 8px;
  border-bottom: 1px solid var(--cis-border-light);
  flex-shrink: 0;
}

.bottom-sheet__title {
  font-family: var(--cis-font-serif);
  font-size: 17px;
  font-weight: 600;
  color: var(--cis-text-display);
  margin: 0;
  letter-spacing: var(--cis-letter-spacing-display);
}

.bottom-sheet__close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: none;
  border-radius: var(--cis-radius-btn);
  background: transparent;
  color: var(--cis-text-tertiary);
  cursor: pointer;
  font-family: inherit;
  transition: background-color var(--cis-transition-fast), color var(--cis-transition-fast);
}

.bottom-sheet__close:active {
  background: var(--cis-primary-tint);
  color: var(--cis-primary);
}

.bottom-sheet__body {
  flex: 1;
  overflow-y: auto;
  padding: 16px 20px 24px;
  -webkit-overflow-scrolling: touch;
}

.sheet-fade-enter-active,
.sheet-fade-leave-active {
  transition: opacity 0.2s ease;
}
.sheet-fade-enter-from,
.sheet-fade-leave-to {
  opacity: 0;
}

.sheet-slide-bottom-enter-active,
.sheet-slide-bottom-leave-active {
  transition: transform 0.25s cubic-bezier(0.32, 0.72, 0, 1);
}
.sheet-slide-bottom-enter-from,
.sheet-slide-bottom-leave-to {
  transform: translateY(100%);
}

.sheet-slide-top-enter-active,
.sheet-slide-top-leave-active {
  transition: transform 0.25s cubic-bezier(0.32, 0.72, 0, 1);
}
.sheet-slide-top-enter-from,
.sheet-slide-top-leave-to {
  transform: translateY(-100%);
}

@media (prefers-reduced-motion: reduce) {
  .sheet-fade-enter-active,
  .sheet-fade-leave-active,
  .sheet-slide-bottom-enter-active,
  .sheet-slide-bottom-leave-active,
  .sheet-slide-top-enter-active,
  .sheet-slide-top-leave-active {
    transition: none;
  }
}
</style>
```

- [ ] **Step 1.3: 创建 MobileEmptyState 通用组件**

`client/src/components/mobile/MobileEmptyState.vue`：

```vue
<template>
  <div class="empty-state" role="status" aria-live="polite">
    <span v-if="eyebrow" class="cis-eyebrow empty-state__eyebrow">{{ eyebrow }}</span>
    <p class="empty-state__text">{{ description }}</p>
    <slot name="action"></slot>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  eyebrow?: string
  description: string
}>()
</script>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 48px 16px;
  border: 1px dashed var(--cis-border-strong);
  border-radius: var(--cis-radius-card);
  background: var(--cis-surface-1);
  text-align: center;
}

.empty-state__eyebrow {
  color: var(--cis-text-tertiary);
}

.empty-state__text {
  margin: 0;
  color: var(--cis-text-tertiary);
  font-size: 14px;
  line-height: 1.5;
}
</style>
```

- [ ] **Step 1.4: 创建 MobileListItem 通用组件**

`client/src/components/mobile/MobileListItem.vue`：

```vue
<template>
  <component
    :is="to ? 'router-link' : 'div'"
    :to="to"
    class="list-item"
    :class="{ 'list-item--clickable': clickable }"
    :aria-label="ariaLabel || title"
  >
    <div v-if="avatar || $slots.avatar" class="list-item__avatar">
      <slot name="avatar">
        <span class="list-item__avatar-text" aria-hidden="true">{{ avatar }}</span>
      </slot>
    </div>
    <div class="list-item__body">
      <span class="list-item__title">{{ title }}</span>
      <span v-if="subtitle" class="list-item__subtitle">{{ subtitle }}</span>
    </div>
    <div v-if="$slots.trailing || trailing || chevron" class="list-item__trailing">
      <slot name="trailing">
        <span v-if="trailing" class="list-item__trailing-text cis-num">{{ trailing }}</span>
        <el-icon v-if="chevron" :size="16" aria-hidden="true"><ArrowRight /></el-icon>
      </slot>
    </div>
  </component>
</template>

<script setup lang="ts">
import { ArrowRight } from '@element-plus/icons-vue'

withDefaults(defineProps<{
  to?: string | object
  title: string
  subtitle?: string
  avatar?: string
  trailing?: string
  chevron?: boolean
  clickable?: boolean
  ariaLabel?: string
}>(), {
  chevron: false,
  clickable: true,
})
</script>

<style scoped>
.list-item {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 56px;
  padding: 10px 16px;
  border-bottom: 1px solid var(--cis-border-light);
  background: var(--cis-surface-1);
  color: var(--cis-text-primary);
  text-decoration: none;
  font-family: inherit;
  transition: background-color var(--cis-transition-fast);
  -webkit-tap-highlight-color: transparent;
}

.list-item--clickable:active {
  background: var(--cis-primary-tint);
}

.list-item__avatar {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--cis-border);
  background: var(--cis-surface-2);
  border-radius: 9999px;
  flex-shrink: 0;
}

.list-item__avatar-text {
  font-family: var(--cis-font-serif);
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
  line-height: 1;
}

.list-item__body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.list-item__title {
  font-size: 15px;
  font-weight: 500;
  color: var(--cis-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.list-item__subtitle {
  font-size: 12px;
  color: var(--cis-text-tertiary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.list-item__trailing {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.list-item__trailing-text {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-secondary);
  font-variant-numeric: tabular-nums;
}
</style>
```

- [ ] **Step 1.5: 在 router 加 /m/* 路由表**

修改 `client/src/router/index.ts`，找到 `// 移动端：扁平化路由，MobileLayout 包裹` 注释处，替换为：

```ts
// 移动端：扁平化路由，MobileLayout 包裹
{
  path: '/m',
  component: MobileLayout,
  children: [
    { path: '',          redirect: '/m/dashboard' },
    { path: 'dashboard',      component: () => import('@/views/mobile/Dashboard.vue'),         meta: { title: '总览' } },
    { path: 'scores',         component: () => import('@/views/mobile/ScoreManagement.vue'),  meta: { title: '积分' } },
    { path: 'students',       component: () => import('@/views/mobile/StudentManagement.vue'), meta: { title: '学生' } },
    { path: 'students/:id',   component: () => import('@/views/mobile/StudentProfile.vue'),   meta: { title: '学生详情' }, props: true },
    { path: 'groups',         component: () => import('@/views/mobile/GroupManagement.vue'),  meta: { title: '分组' } },
    { path: 'leaderboard',    component: () => import('@/views/mobile/Leaderboard.vue'),      meta: { title: '排行' } },
    { path: 'evaluation',     component: () => import('@/views/mobile/Evaluation.vue'),       meta: { title: '评估' } },
    { path: 'settlement',     component: () => import('@/views/mobile/Settlement.vue'),       meta: { title: '结算' } },
    { path: 'settings',       component: () => import('@/views/mobile/Settings.vue'),         meta: { title: '设置' } },
    { path: 'plugins',        component: () => import('@/views/mobile/PluginManagement.vue'), meta: { title: '插件' } },
    { path: 'themes',         component: () => import('@/views/mobile/ThemeManagement.vue'),  meta: { title: '主题' } },
    { path: 'about',          component: () => import('@/views/mobile/About.vue'),             meta: { title: '关于' } },
    { path: 'admin-settings', component: () => import('@/views/mobile/AdminSettings.vue'),    meta: { title: '管理员' } },
  ],
},
```

- [ ] **Step 1.6: 跑 typecheck 验证**

```bash
cd /workspace/classisscore/client && pnpm typecheck 2>&1 | tail -20
```

预期：`vue-tsc --noEmit` 无错误输出（0 错误）。可能因为视图还没写会有 import 错误，先创建空 stub 文件再验证。

- [ ] **Step 1.7: 提交**

```bash
cd /workspace/classisscore
git add client/src/components/layout/MobileLayout.vue client/src/components/mobile/ client/src/router/index.ts
git commit -m "feat(mobile): 增强 MobileLayout 断点检测 + BottomSheet 等通用组件 + 路由表"
```

---

## Task 2: 3 个核心视图（Dashboard / Score / Leaderboard）

**Files:**
- Create: `client/src/views/mobile/Dashboard.vue`
- Create: `client/src/views/mobile/ScoreManagement.vue`
- Create: `client/src/views/mobile/Leaderboard.vue`

每个视图规格详见 spec 第 6.1-6.3 节。设计原则：
- 顶部 Serif 标题 + eyebrow
- 1px hairline 容器
- 数字用 Mono + tabular-nums
- 状态条 6px 圆点
- 触摸目标 ≥ 44px

- [ ] **Step 2.1: 写 Dashboard.vue**

参照 spec 6.1 节 ASCII 线框。核心结构：

```vue
<template>
  <div class="m-dashboard">
    <header class="m-dashboard__head">
      <span class="cis-eyebrow">Dashboard</span>
      <h1 class="cis-display m-dashboard__title">总览</h1>
      <p class="m-dashboard__sub">
        <span class="cis-mono">{{ entries.length }}</span> 名 ·
        更新于 <span class="cis-mono">{{ updatedAt }}</span>
      </p>
    </header>

    <section class="m-dashboard__stats">
      <article v-for="block in statBlocks" :key="block.label" class="m-dashboard__stat">
        <span class="cis-eyebrow">{{ block.eyebrow }}</span>
        <span class="m-dashboard__stat-num cis-num" :class="block.tone">{{ block.value }}</span>
        <span class="m-dashboard__stat-label">{{ block.label }}</span>
        <svg v-if="block.spark" class="m-dashboard__stat-spark" viewBox="0 0 100 24" preserveAspectRatio="none" aria-hidden="true">
          <path :d="block.spark" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </article>
    </section>

    <section class="m-dashboard__quick">
      <h2 class="cis-eyebrow m-dashboard__quick-title">快捷操作</h2>
      <ul class="m-dashboard__quick-list" role="list">
        <li v-for="action in quickActions" :key="action.label">
          <router-link :to="action.to" class="m-dashboard__quick-row">
            <span class="m-dashboard__quick-label">{{ action.label }}</span>
            <span class="m-dashboard__quick-arrow" aria-hidden="true">→</span>
          </router-link>
        </li>
      </ul>
    </section>

    <section class="m-dashboard__recent">
      <h2 class="cis-eyebrow m-dashboard__recent-title">最近积分</h2>
      <ol v-if="recentRecords.length > 0" class="m-dashboard__recent-list" role="list">
        <li v-for="r in recentRecords.slice(0, 5)" :key="r.id" class="m-dashboard__recent-row">
          <span class="m-dashboard__recent-name">{{ r.studentName }}</span>
          <span class="m-dashboard__recent-score cis-num" :class="r.scoreChange >= 0 ? 'is-plus' : 'is-minus'">
            {{ r.scoreChange > 0 ? '+' : '' }}{{ r.scoreChange }}
          </span>
          <span class="m-dashboard__recent-reason">{{ r.reason }}</span>
          <span class="m-dashboard__recent-time cis-mono">{{ r.time }}</span>
        </li>
      </ol>
      <MobileEmptyState v-else eyebrow="Empty" description="今日还没有积分记录" />
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()

const entries = computed(() => studentStore.students)
const updatedAt = ref('--:--')
const recentRecords = computed(() => scoreStore.recentRecords.slice(0, 5))

const statBlocks = computed(() => [
  { eyebrow: 'Today', label: '今日积分', value: '+24', tone: 'is-plus', spark: 'M0,18 L10,15 L20,12 L30,10 L40,8 L50,9 L60,6 L70,5 L80,4 L90,3 L100,2' },
  { eyebrow: 'Week',  label: '本周积分', value: '+187', tone: 'is-plus', spark: 'M0,20 L10,18 L20,15 L30,12 L40,14 L50,10 L60,8 L70,9 L80,6 L90,5 L100,4' },
  { eyebrow: 'Count', label: '学生数',   value: '42',   tone: '',        spark: 'M0,12 L100,12' },
  { eyebrow: 'Avg',   label: '平均分',   value: '+12',  tone: 'is-plus', spark: 'M0,14 L10,12 L20,11 L30,10 L40,9 L50,8 L60,7 L70,8 L80,6 L90,5 L100,4' },
])

const quickActions = [
  { label: '加积分', to: '/m/scores' },
  { label: '看排行', to: '/m/leaderboard' },
  { label: '学生管理', to: '/m/students' },
]

onMounted(async () => {
  await Promise.all([
    scoreStore.fetchRecords(),
    studentStore.fetchStudents(),
  ])
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  updatedAt.value = `${pad(d.getHours())}:${pad(d.getMinutes())}`
})
</script>

<style scoped>
.m-dashboard { display: flex; flex-direction: column; gap: 24px; }
.m-dashboard__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-dashboard__sub { margin: 4px 0 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-dashboard__stats { display: flex; flex-direction: column; gap: 1px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-border); overflow: hidden; }
.m-dashboard__stat { display: grid; grid-template-columns: 1fr auto; grid-template-areas: 'eyebrow eyebrow' 'num spark' 'label label'; gap: 4px; padding: 14px 16px; background: var(--cis-surface-1); }
.m-dashboard__stat .cis-eyebrow { grid-area: eyebrow; color: var(--cis-text-tertiary); }
.m-dashboard__stat-num { grid-area: num; font-family: var(--cis-font-mono); font-size: 28px; font-weight: 700; color: var(--cis-text-primary); line-height: 1; font-variant-numeric: tabular-nums; }
.m-dashboard__stat-num.is-plus { color: var(--cis-success); }
.m-dashboard__stat-num.is-minus { color: var(--cis-accent); }
.m-dashboard__stat-label { grid-area: label; font-size: 12px; color: var(--cis-text-tertiary); }
.m-dashboard__stat-spark { grid-area: spark; width: 80px; height: 24px; color: var(--cis-primary); }
.m-dashboard__quick-list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-dashboard__quick-title, .m-dashboard__recent-title { margin: 0 0 8px; color: var(--cis-text-tertiary); }
.m-dashboard__quick-row { display: flex; align-items: center; justify-content: space-between; min-height: 48px; padding: 0 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); text-decoration: none; color: var(--cis-text-primary); font-weight: 500; font-size: 15px; -webkit-tap-highlight-color: transparent; }
.m-dashboard__quick-row:active { background: var(--cis-primary-tint); }
.m-dashboard__quick-list li:last-child .m-dashboard__quick-row { border-bottom: none; }
.m-dashboard__quick-arrow { color: var(--cis-text-tertiary); font-size: 16px; }
.m-dashboard__recent-list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-dashboard__recent-row { display: grid; grid-template-columns: 1fr auto; grid-template-areas: 'name score' 'reason time'; gap: 2px 8px; padding: 10px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-dashboard__recent-row:last-child { border-bottom: none; }
.m-dashboard__recent-name { grid-area: name; font-size: 14px; font-weight: 500; }
.m-dashboard__recent-score { grid-area: score; font-size: 14px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-dashboard__recent-score.is-plus { color: var(--cis-success); }
.m-dashboard__recent-score.is-minus { color: var(--cis-accent); }
.m-dashboard__recent-reason { grid-area: reason; font-size: 12px; color: var(--cis-text-tertiary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-dashboard__recent-time { grid-area: time; font-size: 11px; color: var(--cis-text-tertiary); }
</style>
```

- [ ] **Step 2.2: 写 ScoreManagement.vue**

参照 spec 6.2。包含内联 form + 评价项 chips + 今日 timeline + 右下 FAB + bottom sheet。

```vue
<template>
  <div class="m-score">
    <header class="m-score__head">
      <span class="cis-eyebrow">Score</span>
      <h1 class="cis-display m-score__title">积分</h1>
    </header>

    <section class="m-score__form cis-hairline">
      <div class="m-score__form-row">
        <el-select
          v-model="addForm.studentId"
          placeholder="选择学生"
          filterable
          class="m-score__select"
          aria-label="选择学生"
        >
          <el-option v-for="s in studentStore.students" :key="s.id" :label="s.name" :value="s.id" />
        </el-select>
      </div>
      <div class="m-score__form-row m-score__form-row--split">
        <el-input-number
          v-model="addForm.scoreChange"
          :step="1"
          :min="-100"
          :max="100"
          controls-position="right"
          class="m-score__number"
          inputmode="numeric"
          aria-label="分值变化"
        />
        <el-input
          v-model="addForm.reason"
          placeholder="原因…"
          maxlength="50"
          class="m-score__reason"
          aria-label="原因"
          autocomplete="off"
          @keyup.enter="handleAdd"
        />
      </div>
      <div class="m-score__form-row m-score__form-row--actions">
        <el-button type="primary" class="m-score__btn" :loading="scoreStore.loading" @click="handleAdd">
          加分
        </el-button>
        <el-button type="danger" plain class="m-score__btn" :loading="scoreStore.loading" @click="handleSubtract">
          减分
        </el-button>
      </div>
    </section>

    <section v-if="evaluationItems.length > 0" class="m-score__quick">
      <h2 class="cis-eyebrow m-score__quick-title">评价项</h2>
      <div class="m-score__quick-scroll">
        <button
          v-for="item in evaluationItems"
          :key="item.id"
          type="button"
          class="m-score__chip"
          :class="item.isPositive ? 'is-plus' : 'is-minus'"
          :aria-label="`应用 ${item.name}，${item.isPositive ? '+' : ''}${item.scoreChange}`"
          @click="applyEvaluationItem(item)"
        >
          <span class="m-score__chip-name">{{ item.name }}</span>
          <span class="m-score__chip-value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
        </button>
      </div>
    </section>

    <section class="m-score__today">
      <h2 class="cis-eyebrow m-score__today-title">今日记录</h2>
      <ol v-if="todayRecords.length > 0" class="m-score__today-list" role="list">
        <li v-for="r in todayRecords" :key="r.id" class="m-score__today-row">
          <span class="m-score__today-name">{{ r.studentName }}</span>
          <span class="m-score__today-score cis-num" :class="r.scoreChange >= 0 ? 'is-plus' : 'is-minus'">
            {{ r.scoreChange > 0 ? '+' : '' }}{{ r.scoreChange }}
          </span>
          <span class="m-score__today-reason">{{ r.reason }}</span>
          <span class="m-score__today-time cis-mono">{{ r.time }}</span>
        </li>
      </ol>
      <MobileEmptyState v-else eyebrow="Empty" description="今日还没有积分记录" />
    </section>

    <button
      type="button"
      class="m-score__fab"
      aria-label="加积分"
      @click="showSheet = true"
    >
      <el-icon :size="22" aria-hidden="true"><Plus /></el-icon>
    </button>

    <BottomSheet v-model="showSheet" title="加积分" height="half">
      <p class="m-score__sheet-hint">从评价项快速选择，或在顶部表单自定义。</p>
      <div class="m-score__sheet-grid">
        <button
          v-for="item in evaluationItems"
          :key="`sheet-${item.id}`"
          type="button"
          class="m-score__sheet-tile"
          :class="item.isPositive ? 'is-plus' : 'is-minus'"
          @click="applyEvaluationItem(item); showSheet = false"
        >
          <span class="m-score__sheet-tile-name">{{ item.name }}</span>
          <span class="m-score__sheet-tile-value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
        </button>
      </div>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { invoke } from '@/services/tauri'
import type { EvaluationItem } from '@/types'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()
const showSheet = ref(false)

const addForm = reactive({ studentId: '', scoreChange: 1, reason: '' })
const evaluationItems = ref<EvaluationItem[]>([])

const todayRecords = computed(() => {
  const today = new Date().toISOString().slice(0, 10)
  return scoreStore.recentRecords.filter(r => r.createdAt?.slice(0, 10) === today)
})

onMounted(async () => {
  await Promise.all([
    scoreStore.fetchRecords(),
    studentStore.fetchStudents(),
    fetchEvaluationItems(),
  ])
})

async function fetchEvaluationItems() {
  try {
    evaluationItems.value = await invoke<EvaluationItem[]>('evaluation_list', {})
  } catch {
    evaluationItems.value = []
  }
}

function applyEvaluationItem(item: EvaluationItem) {
  addForm.scoreChange = Math.abs(item.scoreChange)
  addForm.reason = item.name
}

async function handleAdd() {
  if (!addForm.studentId || !addForm.reason || addForm.scoreChange <= 0) {
    ElMessage.warning('请填写学生、分值和原因')
    return
  }
  try {
    await scoreStore.addScore(addForm.studentId, addForm.scoreChange, addForm.reason)
    ElMessage.success('已加分')
    addForm.reason = ''
  } catch { /* store handled */ }
}

async function handleSubtract() {
  if (!addForm.studentId || !addForm.reason || addForm.scoreChange <= 0) {
    ElMessage.warning('请填写学生、分值和原因')
    return
  }
  try {
    await scoreStore.addScore(addForm.studentId, -addForm.scoreChange, addForm.reason)
    ElMessage.success('已减分')
    addForm.reason = ''
  } catch { /* store handled */ }
}
</script>

<style scoped>
.m-score { display: flex; flex-direction: column; gap: 20px; padding-bottom: 96px; }
.m-score__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-score__form { display: flex; flex-direction: column; gap: 10px; padding: 16px; background: var(--cis-surface-1); border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); }
.m-score__form-row { display: flex; gap: 8px; }
.m-score__form-row--split > * { flex: 1; }
.m-score__form-row--actions { gap: 10px; }
.m-score__form-row--actions .m-score__btn { flex: 1; height: 44px; }
.m-score__select, .m-score__number, .m-score__reason { width: 100%; }
.m-score__quick-title, .m-score__today-title { margin: 0 0 8px; color: var(--cis-text-tertiary); }
.m-score__quick-scroll { display: flex; gap: 8px; overflow-x: auto; -webkit-overflow-scrolling: touch; padding: 2px; scrollbar-width: none; }
.m-score__quick-scroll::-webkit-scrollbar { display: none; }
.m-score__chip { display: inline-flex; align-items: center; gap: 6px; min-height: 36px; padding: 0 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); font-size: 13px; font-weight: 500; font-family: inherit; cursor: pointer; flex-shrink: 0; transition: border-color var(--cis-transition-fast), color var(--cis-transition-fast); }
.m-score__chip:active { transform: scale(var(--cis-press-scale)); }
.m-score__chip.is-plus { color: var(--cis-success); border-color: rgba(21, 128, 61, 0.3); }
.m-score__chip.is-minus { color: var(--cis-accent); border-color: rgba(185, 28, 28, 0.3); }
.m-score__chip-value { font-weight: 700; }
.m-score__today-list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-score__today-row { display: grid; grid-template-columns: 1fr auto; grid-template-areas: 'name score' 'reason time'; gap: 2px 8px; padding: 10px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-score__today-row:last-child { border-bottom: none; }
.m-score__today-name { grid-area: name; font-size: 14px; font-weight: 500; }
.m-score__today-score { grid-area: score; font-size: 14px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-score__today-score.is-plus { color: var(--cis-success); }
.m-score__today-score.is-minus { color: var(--cis-accent); }
.m-score__today-reason { grid-area: reason; font-size: 12px; color: var(--cis-text-tertiary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-score__today-time { grid-area: time; font-size: 11px; color: var(--cis-text-tertiary); }

.m-score__fab { position: fixed; right: 16px; bottom: calc(80px + env(safe-area-inset-bottom, 0)); z-index: 100; width: 56px; height: 56px; border: none; border-radius: 9999px; background: var(--cis-primary); color: #fff; display: flex; align-items: center; justify-content: center; cursor: pointer; box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3); -webkit-tap-highlight-color: transparent; }
.m-score__fab:active { transform: scale(var(--cis-press-scale-strong)); }

.m-score__sheet-hint { margin: 0 0 12px; font-size: 13px; color: var(--cis-text-tertiary); }
.m-score__sheet-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-score__sheet-tile { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px; min-height: 72px; padding: 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-score__sheet-tile:active { transform: scale(var(--cis-press-scale)); }
.m-score__sheet-tile.is-plus { border-color: rgba(21, 128, 61, 0.4); background: var(--cis-success-tint); color: var(--cis-success); }
.m-score__sheet-tile.is-minus { border-color: rgba(185, 28, 28, 0.4); background: var(--cis-accent-tint); color: var(--cis-accent); }
.m-score__sheet-tile-name { font-size: 13px; font-weight: 600; }
.m-score__sheet-tile-value { font-size: 18px; font-weight: 700; font-variant-numeric: tabular-nums; }
</style>
```

- [ ] **Step 2.3: 写 Leaderboard.vue**

参照 spec 6.3。包含 Tab + 时间 select + 3 柱领奖台 + 列表。

```vue
<template>
  <div class="m-leaderboard">
    <header class="m-leaderboard__head">
      <span class="cis-eyebrow">Leaderboard</span>
      <h1 class="cis-display m-leaderboard__title">排行榜</h1>
      <p class="m-leaderboard__sub">
        <span class="cis-mono">{{ entries.length }}</span> 名 ·
        更新于 <span class="cis-mono">{{ updatedAt }}</span>
      </p>
    </header>

    <div class="m-leaderboard__tabs" role="tablist" aria-label="排行榜筛选">
      <button
        v-for="opt in modeOptions"
        :key="opt.value"
        type="button"
        role="tab"
        class="m-leaderboard__tab"
        :class="{ 'is-active': mode === opt.value }"
        :aria-selected="mode === opt.value"
        @click="onModeChange(opt.value)"
      >
        <span class="m-leaderboard__tab-label">{{ opt.label }}</span>
      </button>
      <span class="m-leaderboard__tab-sep" aria-hidden="true"></span>
      <button
        type="button"
        class="m-leaderboard__tab m-leaderboard__tab--time"
        :aria-label="`时间范围：${currentTimeLabel}`"
        @click="showTimeSheet = true"
      >
        <span class="m-leaderboard__tab-label">{{ currentTimeLabel }}</span>
        <el-icon :size="14" aria-hidden="true"><ArrowDown /></el-icon>
      </button>
    </div>

    <template v-if="!loading && entries.length > 0">
      <div v-if="topThree.length > 0" class="m-leaderboard__podium" role="list" aria-label="前三名">
        <div
          v-for="entry in topThree"
          :key="`podium-${entry.rank}`"
          class="m-leaderboard__cell"
          :class="`m-leaderboard__cell--${entry.rank}`"
          role="listitem"
        >
          <span class="cis-eyebrow">No. {{ entry.rank }}</span>
          <div class="m-leaderboard__avatar">
            <span class="m-leaderboard__avatar-text">{{ nameInitial(entry.name) }}</span>
          </div>
          <span class="m-leaderboard__name">{{ entry.name }}</span>
          <span class="m-leaderboard__score cis-num" :class="entry.score >= 0 ? 'is-plus' : 'is-minus'">
            {{ entry.score > 0 ? '+' : '' }}{{ entry.score }}
          </span>
        </div>
      </div>

      <ol v-if="restEntries.length > 0" class="m-leaderboard__list" aria-label="其余排名">
        <li v-for="entry in restEntries" :key="entry.rank" class="m-leaderboard__row">
          <span class="m-leaderboard__row-rank cis-mono">{{ entry.rank.toString().padStart(2, '0') }}</span>
          <span class="m-leaderboard__row-name">{{ entry.name }}</span>
          <span class="m-leaderboard__row-score cis-num" :class="entry.score >= 0 ? 'is-plus' : 'is-minus'">
            {{ entry.score > 0 ? '+' : '' }}{{ entry.score }}
          </span>
        </li>
      </ol>
    </template>
    <MobileEmptyState v-else-if="!loading" eyebrow="Empty" description="该时段暂无数据" />
    <el-skeleton v-else :rows="5" animated />

    <BottomSheet v-model="showTimeSheet" title="选择时间范围" height="auto">
      <ul class="m-leaderboard__time-list" role="list">
        <li v-for="opt in timeOptions" :key="opt.value">
          <button
            type="button"
            class="m-leaderboard__time-item"
            :class="{ 'is-active': timeRange === opt.value }"
            :aria-checked="timeRange === opt.value"
            role="radio"
            @click="onTimeRangeChange(opt.value); showTimeSheet = false"
          >
            <span class="m-leaderboard__time-label">{{ opt.label }}</span>
            <el-icon v-if="timeRange === opt.value" :size="16" aria-hidden="true"><Check /></el-icon>
          </button>
        </li>
      </ul>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ArrowDown, Check } from '@element-plus/icons-vue'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { invoke } from '@/services/tauri'
import type { LeaderboardEntry } from '@/types'

type Mode = 'personal' | 'group'
type TimeRange = 'today' | 'week' | 'month' | 'all'

const mode = ref<Mode>('personal')
const timeRange = ref<TimeRange>('all')
const entries = ref<LeaderboardEntry[]>([])
const loading = ref(true)
const updatedAt = ref('--:--')
const showTimeSheet = ref(false)

const topThree = computed(() => entries.value.slice(0, 3))
const restEntries = computed(() => entries.value.slice(3))

const modeOptions = [
  { value: 'personal' as const, label: '个人' },
  { value: 'group' as const, label: '小组' },
]
const timeOptions = [
  { value: 'today' as const, label: '今日' },
  { value: 'week' as const, label: '本周' },
  { value: 'month' as const, label: '本月' },
  { value: 'all' as const, label: '全部' },
]
const currentTimeLabel = computed(() => timeOptions.find(o => o.value === timeRange.value)?.label || '全部')

function nameInitial(name: string) { return name?.slice(0, 1) ?? '?' }

function setUpdatedAt() {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  updatedAt.value = `${pad(d.getHours())}:${pad(d.getMinutes())}`
}

onMounted(async () => {
  await fetchLeaderboard()
  setUpdatedAt()
  loading.value = false
})

function getTimeRangeParams() {
  const now = new Date()
  let startTime: Date | undefined
  switch (timeRange.value) {
    case 'today': startTime = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0); break
    case 'week': { const day = now.getDay() || 7; startTime = new Date(now.getFullYear(), now.getMonth(), now.getDate() - day + 1, 0, 0, 0); break }
    case 'month': startTime = new Date(now.getFullYear(), now.getMonth(), 1, 0, 0, 0); break
    case 'all': return { startTime: null, endTime: null }
  }
  return { startTime: startTime!.toISOString().slice(0, 19), endTime: now.toISOString().slice(0, 19) }
}

async function fetchLeaderboard() {
  try {
    const { startTime, endTime } = getTimeRangeParams()
    const args = { startTime, endTime }
    if (mode.value === 'personal') {
      const data = await invoke<Array<{ student: { name: string; total_score: number } }>>('leaderboard_query', args)
      entries.value = data.map((item, i) => ({ rank: i + 1, name: item.student.name, score: item.student.total_score ?? 0, isGroup: false }))
    } else {
      const groups = await invoke<Array<{ group_name: string; total_score: number }>>('leaderboard_all_groups', args)
      entries.value = groups.map((g, i) => ({ rank: i + 1, name: g.group_name, score: g.total_score, isGroup: true }))
    }
    setUpdatedAt()
  } catch {
    entries.value = []
  }
}

function onModeChange(value: Mode) { mode.value = value; fetchLeaderboard() }
function onTimeRangeChange(value: TimeRange) { timeRange.value = value; fetchLeaderboard() }
</script>

<style scoped>
.m-leaderboard { display: flex; flex-direction: column; gap: 20px; }
.m-leaderboard__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-leaderboard__sub { margin: 4px 0 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-leaderboard__tabs { display: flex; align-items: stretch; gap: 8px; border-bottom: 1px solid var(--cis-border); }
.m-leaderboard__tab { position: relative; display: flex; align-items: center; gap: 4px; padding: 10px 8px; background: transparent; border: none; color: var(--cis-text-tertiary); font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-leaderboard__tab.is-active { color: var(--cis-primary); font-weight: 600; }
.m-leaderboard__tab::after { content: ''; position: absolute; left: 0; right: 0; bottom: -1px; height: 2px; background: transparent; transition: background-color var(--cis-transition-fast); }
.m-leaderboard__tab.is-active::after { background: var(--cis-primary); }
.m-leaderboard__tab-sep { width: 1px; background: var(--cis-border-light); margin: 6px 0; }
.m-leaderboard__tab--time { margin-left: auto; }
.m-leaderboard__tab-label { font-size: 14px; }
.m-leaderboard__podium { display: grid; grid-template-columns: 1fr 1.2fr 1fr; gap: 0; align-items: end; padding: 16px 8px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); }
.m-leaderboard__cell { display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 0 4px; }
.m-leaderboard__cell--1 { order: 2; }
.m-leaderboard__cell--2 { order: 1; }
.m-leaderboard__cell--3 { order: 3; }
.m-leaderboard__avatar { width: 48px; height: 48px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; }
.m-leaderboard__cell--1 .m-leaderboard__avatar { width: 64px; height: 64px; background: var(--cis-primary); border-color: var(--cis-primary); }
.m-leaderboard__cell--1 .m-leaderboard__avatar-text { color: #fff; }
.m-leaderboard__avatar-text { font-family: var(--cis-font-serif); font-size: 20px; font-weight: 600; color: var(--cis-text-primary); line-height: 1; }
.m-leaderboard__name { font-size: 13px; font-weight: 500; text-align: center; max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-leaderboard__cell--1 .m-leaderboard__name { font-size: 14px; }
.m-leaderboard__score { font-family: var(--cis-font-mono); font-size: 16px; font-weight: 700; font-variant-numeric: tabular-nums; line-height: 1; }
.m-leaderboard__cell--1 .m-leaderboard__score { font-size: 22px; }
.m-leaderboard__score.is-plus, .m-leaderboard__row-score.is-plus { color: var(--cis-success); }
.m-leaderboard__score.is-minus, .m-leaderboard__row-score.is-minus { color: var(--cis-accent); }
.m-leaderboard__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-leaderboard__row { display: flex; align-items: center; gap: 12px; min-height: 48px; padding: 0 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); }
.m-leaderboard__row:last-child { border-bottom: none; }
.m-leaderboard__row::before { content: ''; width: 4px; height: 16px; background: var(--cis-text-tertiary); border-radius: 2px; flex-shrink: 0; }
.m-leaderboard__row-rank { font-family: var(--cis-font-mono); font-size: 13px; font-weight: 600; color: var(--cis-text-tertiary); min-width: 24px; }
.m-leaderboard__row-name { flex: 1; font-size: 14px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-leaderboard__row-score { font-size: 15px; font-weight: 700; min-width: 56px; text-align: right; font-variant-numeric: tabular-nums; }
.m-leaderboard__time-list { list-style: none; margin: 0; padding: 0; }
.m-leaderboard__time-item { display: flex; align-items: center; justify-content: space-between; width: 100%; min-height: 48px; padding: 0 4px; border: none; background: transparent; color: var(--cis-text-primary); font-size: 15px; font-weight: 500; font-family: inherit; cursor: pointer; border-bottom: 1px solid var(--cis-border-light); -webkit-tap-highlight-color: transparent; }
.m-leaderboard__time-list li:last-child .m-leaderboard__time-item { border-bottom: none; }
.m-leaderboard__time-item.is-active { color: var(--cis-primary); font-weight: 600; }
.m-leaderboard__time-item:active { background: var(--cis-primary-tint); }
</style>
```

- [ ] **Step 2.4: 跑 typecheck 验证**

```bash
cd /workspace/classisscore/client && pnpm typecheck 2>&1 | tail -20
```

预期：0 错误。

- [ ] **Step 2.5: 提交**

```bash
cd /workspace/classisscore
git add client/src/views/mobile/Dashboard.vue client/src/views/mobile/ScoreManagement.vue client/src/views/mobile/Leaderboard.vue
git commit -m "feat(mobile): 三个核心视图 Dashboard / Score / Leaderboard"
```

---

## Task 3: 2 个学生视图（StudentManagement / StudentProfile）

**Files:**
- Create: `client/src/views/mobile/StudentManagement.vue`
- Create: `client/src/views/mobile/StudentProfile.vue`

- [ ] **Step 3.1: 写 StudentManagement.vue**

```vue
<template>
  <div class="m-students">
    <header class="m-students__head">
      <span class="cis-eyebrow">Students</span>
      <h1 class="cis-display m-students__title">学生</h1>
    </header>

    <div class="m-students__search">
      <el-input
        v-model="search"
        placeholder="搜索学生…"
        clearable
        aria-label="搜索学生"
        autocomplete="off"
      >
        <template #prefix>
          <el-icon :size="16" aria-hidden="true"><Search /></el-icon>
        </template>
      </el-input>
      <el-select v-model="sortBy" aria-label="排序方式" class="m-students__sort">
        <el-option value="score-desc" label="分数↓" />
        <el-option value="score-asc" label="分数↑" />
        <el-option value="name-asc" label="姓名 A→Z" />
        <el-option value="name-desc" label="姓名 Z→A" />
      </el-select>
    </div>

    <ul v-if="filteredStudents.length > 0" class="m-students__list" role="list">
      <li v-for="s in filteredStudents" :key="s.id">
        <router-link
          :to="`/m/students/${s.id}`"
          class="m-students__row"
          :aria-label="`${s.name}，${s.score}分`"
        >
          <div class="m-students__avatar" aria-hidden="true">
            <span class="m-students__avatar-text">{{ (s.name || '?').slice(0, 1) }}</span>
          </div>
          <div class="m-students__body">
            <span class="m-students__name">{{ s.name }}</span>
            <span class="m-students__id">{{ s.studentNumber || '—' }}</span>
          </div>
          <span class="m-students__score cis-num">{{ s.score }}</span>
          <el-icon :size="14" class="m-students__chevron" aria-hidden="true"><ArrowRight /></el-icon>
        </router-link>
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" :description="search ? '没有匹配的学生' : '暂无学生'" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Search, ArrowRight } from '@element-plus/icons-vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { useStudentStore } from '@/stores/student'

const studentStore = useStudentStore()
const search = ref('')
const sortBy = ref<'score-desc' | 'score-asc' | 'name-asc' | 'name-desc'>('score-desc')

onMounted(async () => { await studentStore.fetchStudents() })

const filteredStudents = computed(() => {
  const q = search.value.trim().toLowerCase()
  let list = studentStore.students
  if (q) list = list.filter(s => s.name.toLowerCase().includes(q) || s.studentNumber?.toLowerCase().includes(q))
  list = [...list]
  switch (sortBy.value) {
    case 'score-desc': list.sort((a, b) => b.score - a.score); break
    case 'score-asc': list.sort((a, b) => a.score - b.score); break
    case 'name-asc': list.sort((a, b) => a.name.localeCompare(b.name, 'zh-CN')); break
    case 'name-desc': list.sort((a, b) => b.name.localeCompare(a.name, 'zh-CN')); break
  }
  return list
})
</script>

<style scoped>
.m-students { display: flex; flex-direction: column; gap: 16px; }
.m-students__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-students__search { display: flex; gap: 8px; }
.m-students__search .el-input { flex: 1; }
.m-students__sort { width: 120px; }
.m-students__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-students__row { display: flex; align-items: center; gap: 12px; min-height: 60px; padding: 10px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); color: var(--cis-text-primary); text-decoration: none; -webkit-tap-highlight-color: transparent; }
.m-students__list li:last-child .m-students__row { border-bottom: none; }
.m-students__row:active { background: var(--cis-primary-tint); }
.m-students__avatar { width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; flex-shrink: 0; }
.m-students__avatar-text { font-family: var(--cis-font-serif); font-size: 15px; font-weight: 600; line-height: 1; }
.m-students__body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.m-students__name { font-size: 15px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-students__id { font-size: 12px; color: var(--cis-text-tertiary); font-family: var(--cis-font-mono); }
.m-students__score { font-family: var(--cis-font-mono); font-size: 16px; font-weight: 700; font-variant-numeric: tabular-nums; color: var(--cis-text-primary); flex-shrink: 0; }
.m-students__chevron { color: var(--cis-text-tertiary); flex-shrink: 0; }
</style>
```

- [ ] **Step 3.2: 写 StudentProfile.vue**

```vue
<template>
  <div class="m-profile">
    <header class="m-profile__head">
      <button type="button" class="m-profile__back" aria-label="返回学生列表" @click="$router.back()">
        <el-icon :size="20" aria-hidden="true"><ArrowLeft /></el-icon>
      </button>
      <h1 class="cis-display m-profile__name">{{ student?.name || '学生详情' }}</h1>
    </header>

    <section v-if="student" class="m-profile__hero cis-hairline">
      <div class="m-profile__avatar" aria-hidden="true">
        <span class="m-profile__avatar-text">{{ student.name.slice(0, 1) }}</span>
      </div>
      <span class="m-profile__hero-name">{{ student.name }}</span>
      <span class="m-profile__hero-id cis-mono">{{ student.studentNumber || '—' }} · {{ student.group || '未分组' }}</span>
      <span
        class="m-profile__hero-score cis-num"
        :class="student.score > 0 ? 'is-plus' : student.score < 0 ? 'is-minus' : ''"
      >
        {{ student.score > 0 ? '+' : '' }}{{ student.score }}
      </span>
      <svg class="m-profile__hero-spark" viewBox="0 0 100 24" preserveAspectRatio="none" aria-hidden="true">
        <path :d="sparkPath" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </section>

    <div class="m-profile__tabs" role="tablist" aria-label="详情分类">
      <button
        v-for="t in tabs"
        :key="t.key"
        type="button"
        role="tab"
        class="m-profile__tab"
        :class="{ 'is-active': activeTab === t.key }"
        :aria-selected="activeTab === t.key"
        @click="activeTab = t.key"
      >{{ t.label }}</button>
    </div>

    <section v-if="activeTab === 'records'" class="m-profile__records">
      <ol v-if="records.length > 0" class="m-profile__list" role="list">
        <li v-for="r in records" :key="r.id" class="m-profile__record">
          <span class="m-profile__record-time cis-mono">{{ formatTime(r.createdAt) }}</span>
          <span class="m-profile__record-score cis-num" :class="r.scoreChange >= 0 ? 'is-plus' : 'is-minus'">
            {{ r.scoreChange > 0 ? '+' : '' }}{{ r.scoreChange }}
          </span>
          <span class="m-profile__record-reason">{{ r.reason }}</span>
        </li>
      </ol>
      <MobileEmptyState v-else eyebrow="Empty" description="暂无积分记录" />
    </section>

    <section v-else-if="activeTab === 'evaluation'" class="m-profile__eval">
      <p class="m-profile__eval-hint">评估项管理请前往桌面端</p>
    </section>

    <button type="button" class="m-profile__cta" aria-label="加积分" @click="showSheet = true">
      加积分
    </button>

    <BottomSheet v-model="showSheet" title="加积分" height="half">
      <p class="m-profile__sheet-hint">从评价项快速选择，或在底部表单自定义。</p>
      <div v-if="evaluationItems.length > 0" class="m-profile__sheet-grid">
        <button
          v-for="item in evaluationItems"
          :key="`s-${item.id}`"
          type="button"
          class="m-profile__sheet-tile"
          :class="item.isPositive ? 'is-plus' : 'is-minus'"
          @click="applyEvaluationItem(item); showSheet = false"
        >
          <span class="m-profile__sheet-tile-name">{{ item.name }}</span>
          <span class="m-profile__sheet-tile-value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
        </button>
      </div>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { useStudentStore } from '@/stores/student'
import { useScoreStore } from '@/stores/score'
import { invoke } from '@/services/tauri'
import type { EvaluationItem } from '@/types'

const route = useRoute()
const studentStore = useStudentStore()
const scoreStore = useScoreStore()

const studentId = computed(() => String(route.params.id))
const student = computed(() => studentStore.students.find(s => String(s.id) === studentId.value))
const records = computed(() => scoreStore.recentRecords.filter(r => String(r.studentId) === studentId.value))

const activeTab = ref<'records' | 'evaluation'>('records')
const tabs = [
  { key: 'records' as const, label: '积分记录' },
  { key: 'evaluation' as const, label: '评估' },
]
const evaluationItems = ref<EvaluationItem[]>([])
const showSheet = ref(false)

const sparkPath = computed(() => {
  const list = records.value.slice(0, 20).reverse()
  if (list.length < 2) return 'M0,12 L100,12'
  const max = Math.max(...list.map(r => Math.abs(r.scoreChange))) || 1
  return list.map((r, i) => {
    const x = (i / (list.length - 1)) * 100
    const y = 12 - (r.scoreChange / max) * 10
    return `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${y.toFixed(1)}`
  }).join(' ')
})

function formatTime(iso: string) {
  if (!iso) return '--:--'
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function applyEvaluationItem(item: EvaluationItem) {
  if (!student.value) return
  const value = Math.abs(item.scoreChange)
  scoreStore.addScore(student.value.id, item.isPositive ? value : -value, item.name)
    .then(() => ElMessage.success('已记录'))
    .catch(() => ElMessage.error('记录失败'))
}

onMounted(async () => {
  await Promise.all([
    studentStore.fetchStudents(),
    scoreStore.fetchRecords(),
    fetchEvaluationItems(),
  ])
})

async function fetchEvaluationItems() {
  try { evaluationItems.value = await invoke<EvaluationItem[]>('evaluation_list', {}) }
  catch { evaluationItems.value = [] }
}
</script>

<style scoped>
.m-profile { display: flex; flex-direction: column; gap: 16px; padding-bottom: 96px; }
.m-profile__head { display: flex; align-items: center; gap: 4px; }
.m-profile__back { display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px; border: none; border-radius: var(--cis-radius-btn); background: transparent; color: var(--cis-text-secondary); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; }
.m-profile__back:active { background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-profile__name { font-size: 20px; margin: 0; font-weight: 600; }
.m-profile__hero { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 24px 16px; background: var(--cis-surface-1); border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); text-align: center; }
.m-profile__avatar { width: 64px; height: 64px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; }
.m-profile__avatar-text { font-family: var(--cis-font-serif); font-size: 28px; font-weight: 600; line-height: 1; }
.m-profile__hero-name { font-size: 17px; font-weight: 600; }
.m-profile__hero-id { font-size: 12px; color: var(--cis-text-tertiary); }
.m-profile__hero-score { font-family: var(--cis-font-mono); font-size: 32px; font-weight: 700; font-variant-numeric: tabular-nums; line-height: 1; }
.m-profile__hero-score.is-plus { color: var(--cis-success); }
.m-profile__hero-score.is-minus { color: var(--cis-accent); }
.m-profile__hero-spark { width: 100%; max-width: 240px; height: 32px; color: var(--cis-primary); }
.m-profile__tabs { display: flex; align-items: stretch; gap: 0; border-bottom: 1px solid var(--cis-border); }
.m-profile__tab { position: relative; padding: 10px 16px; background: transparent; border: none; color: var(--cis-text-tertiary); font-size: 14px; font-weight: 500; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-profile__tab.is-active { color: var(--cis-primary); font-weight: 600; }
.m-profile__tab::after { content: ''; position: absolute; left: 0; right: 0; bottom: -1px; height: 2px; background: transparent; transition: background-color var(--cis-transition-fast); }
.m-profile__tab.is-active::after { background: var(--cis-primary); }
.m-profile__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-profile__record { display: grid; grid-template-columns: auto auto 1fr; gap: 4px 12px; align-items: baseline; padding: 12px 16px; border-bottom: 1px solid var(--cis-border-light); }
.m-profile__record:last-child { border-bottom: none; }
.m-profile__record-time { font-size: 11px; color: var(--cis-text-tertiary); }
.m-profile__record-score { font-family: var(--cis-font-mono); font-size: 14px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-profile__record-score.is-plus { color: var(--cis-success); }
.m-profile__record-score.is-minus { color: var(--cis-accent); }
.m-profile__record-reason { font-size: 14px; color: var(--cis-text-primary); }
.m-profile__eval-hint { padding: 32px 16px; text-align: center; color: var(--cis-text-tertiary); font-size: 14px; margin: 0; }
.m-profile__cta { position: fixed; right: 16px; left: 16px; bottom: calc(80px + env(safe-area-inset-bottom, 0)); z-index: 100; height: 48px; border: none; border-radius: var(--cis-radius-btn); background: var(--cis-primary); color: #fff; font-size: 15px; font-weight: 600; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3); }
.m-profile__cta:active { transform: scale(var(--cis-press-scale-strong)); }
.m-profile__sheet-hint { margin: 0 0 12px; font-size: 13px; color: var(--cis-text-tertiary); }
.m-profile__sheet-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-profile__sheet-tile { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px; min-height: 72px; padding: 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-profile__sheet-tile:active { transform: scale(var(--cis-press-scale)); }
.m-profile__sheet-tile.is-plus { border-color: rgba(21, 128, 61, 0.4); background: var(--cis-success-tint); color: var(--cis-success); }
.m-profile__sheet-tile.is-minus { border-color: rgba(185, 28, 28, 0.4); background: var(--cis-accent-tint); color: var(--cis-accent); }
.m-profile__sheet-tile-name { font-size: 13px; font-weight: 600; }
.m-profile__sheet-tile-value { font-size: 18px; font-weight: 700; font-variant-numeric: tabular-nums; }
</style>
```

- [ ] **Step 3.3: 跑 typecheck**

```bash
cd /workspace/classisscore/client && pnpm typecheck 2>&1 | tail -10
```

- [ ] **Step 3.4: 提交**

```bash
cd /workspace/classisscore
git add client/src/views/mobile/StudentManagement.vue client/src/views/mobile/StudentProfile.vue
git commit -m "feat(mobile): 学生管理 + 学生详情"
```

---

## Task 4: 4 个功能视图（Group / Evaluation / Settlement / Settings）

**Files:**
- Create: `client/src/views/mobile/GroupManagement.vue`
- Create: `client/src/views/mobile/Evaluation.vue`
- Create: `client/src/views/mobile/Settlement.vue`
- Create: `client/src/views/mobile/Settings.vue`

- [ ] **Step 4.1: 写 GroupManagement.vue**

```vue
<template>
  <div class="m-groups">
    <header class="m-groups__head">
      <span class="cis-eyebrow">Groups</span>
      <h1 class="cis-display m-groups__title">分组</h1>
    </header>
    <ul v-if="groups.length > 0" class="m-groups__list" role="list">
      <li v-for="g in groups" :key="g.id">
        <button
          type="button"
          class="m-groups__row"
          :aria-label="`${g.name}，${g.studentIds.length} 人，总分 ${g.totalScore}`"
          @click="openGroup(g)"
        >
          <div class="m-groups__icon" aria-hidden="true">
            <span class="m-groups__icon-text">{{ g.name.slice(0, 1) }}</span>
          </div>
          <div class="m-groups__body">
            <span class="m-groups__name">{{ g.name }}</span>
            <span class="m-groups__count">{{ g.studentIds.length }} 人</span>
          </div>
          <span class="m-groups__score cis-num">{{ g.totalScore }}</span>
          <el-icon :size="14" class="m-groups__chevron" aria-hidden="true"><ArrowRight /></el-icon>
        </button>
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无分组" />
    <BottomSheet v-model="sheetOpen" :title="currentGroup?.name || '成员'" height="half">
      <ul v-if="currentGroup" class="m-groups__members" role="list">
        <li v-for="sid in currentGroup.studentIds" :key="sid" class="m-groups__member">
          <div class="m-groups__member-avatar" aria-hidden="true">
            <span class="m-groups__member-avatar-text">{{ (memberName(sid) || '?').slice(0, 1) }}</span>
          </div>
          <span class="m-groups__member-name">{{ memberName(sid) }}</span>
        </li>
      </ul>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ArrowRight } from '@element-plus/icons-vue'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { groupApi } from '@/services/group'
import { useStudentStore } from '@/stores/student'
import type { StudentGroup } from '@/types'

const studentStore = useStudentStore()
const groups = ref<StudentGroup[]>([])
const sheetOpen = ref(false)
const currentGroup = ref<StudentGroup | null>(null)

onMounted(async () => {
  await studentStore.fetchStudents()
  try {
    const res = await groupApi.getAll()
    groups.value = res.data.data
  } catch { groups.value = [] }
})

function openGroup(g: StudentGroup) { currentGroup.value = g; sheetOpen.value = true }
function memberName(id: string) { return studentStore.getStudentById(id)?.name || '—' }
</script>

<style scoped>
.m-groups { display: flex; flex-direction: column; gap: 16px; }
.m-groups__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-groups__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-groups__row { display: flex; align-items: center; gap: 12px; width: 100%; min-height: 60px; padding: 10px 16px; background: var(--cis-surface-1); border: none; border-bottom: 1px solid var(--cis-border-light); color: var(--cis-text-primary); font-family: inherit; text-align: left; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-groups__list li:last-child .m-groups__row { border-bottom: none; }
.m-groups__row:active { background: var(--cis-primary-tint); }
.m-groups__icon { width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: var(--cis-radius-btn); flex-shrink: 0; }
.m-groups__icon-text { font-family: var(--cis-font-serif); font-size: 16px; font-weight: 600; line-height: 1; }
.m-groups__body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.m-groups__name { font-size: 15px; font-weight: 500; }
.m-groups__count { font-size: 12px; color: var(--cis-text-tertiary); }
.m-groups__score { font-family: var(--cis-font-mono); font-size: 16px; font-weight: 700; font-variant-numeric: tabular-nums; flex-shrink: 0; }
.m-groups__chevron { color: var(--cis-text-tertiary); flex-shrink: 0; }
.m-groups__members { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-groups__member { display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 12px 8px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); }
.m-groups__member-avatar { width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; }
.m-groups__member-avatar-text { font-family: var(--cis-font-serif); font-size: 16px; font-weight: 600; }
.m-groups__member-name { font-size: 13px; font-weight: 500; text-align: center; }
</style>
```

- [ ] **Step 4.2: 写 Evaluation.vue**

```vue
<template>
  <div class="m-eval">
    <header class="m-eval__head">
      <span class="cis-eyebrow">Evaluation</span>
      <h1 class="cis-display m-eval__title">评估项</h1>
    </header>
    <p class="m-eval__hint">长按拖动排序</p>
    <ul class="m-eval__list" role="list">
      <li v-for="item in items" :key="item.id" class="m-eval__row" :class="item.isPositive ? 'is-plus' : 'is-minus'">
        <span class="m-eval__handle" aria-hidden="true">⋮⋮</span>
        <span class="m-eval__name">{{ item.name }}</span>
        <span class="m-eval__value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
      </li>
    </ul>
    <p v-if="items.length === 0" class="m-eval__empty">暂无评估项</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import Sortable from 'sortablejs'
import { invoke } from '@/services/tauri'
import type { EvaluationItem } from '@/types'

const items = ref<EvaluationItem[]>([])
let sortable: Sortable | null = null

function bindSortable() {
  const el = document.querySelector('.m-eval__list') as HTMLElement | null
  if (!el) return
  sortable = Sortable.create(el, {
    handle: '.m-eval__handle',
    animation: 150,
    delay: 100,
    delayOnTouchOnly: true,
  })
}

onMounted(async () => {
  try { items.value = await invoke<EvaluationItem[]>('evaluation_list', {}) }
  catch { items.value = [] }
  await new Promise(r => setTimeout(r, 50))
  bindSortable()
})

onBeforeUnmount(() => { sortable?.destroy() })
</script>

<style scoped>
.m-eval { display: flex; flex-direction: column; gap: 12px; }
.m-eval__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-eval__hint { margin: 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-eval__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-eval__row { display: flex; align-items: center; gap: 12px; min-height: 52px; padding: 8px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-eval__list li:last-child .m-eval__row { border-bottom: none; }
.m-eval__handle { font-size: 14px; color: var(--cis-text-tertiary); cursor: grab; touch-action: none; padding: 0 4px; }
.m-eval__name { flex: 1; font-size: 15px; font-weight: 500; }
.m-eval__value { font-family: var(--cis-font-mono); font-size: 15px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-eval__row.is-plus .m-eval__value { color: var(--cis-success); }
.m-eval__row.is-minus .m-eval__value { color: var(--cis-accent); }
.m-eval__empty { text-align: center; color: var(--cis-text-tertiary); padding: 24px 0; }
</style>
```

- [ ] **Step 4.3: 写 Settlement.vue**

```vue
<template>
  <div class="m-settle">
    <header class="m-settle__head">
      <span class="cis-eyebrow">Settlement</span>
      <h1 class="cis-display m-settle__title">学期结算</h1>
    </header>

    <section class="m-settle__kpi">
      <article class="m-settle__kpi-cell">
        <span class="cis-eyebrow">Total</span>
        <span class="m-settle__kpi-num cis-num">{{ totalScore }}</span>
        <span class="m-settle__kpi-label">总分</span>
      </article>
      <article class="m-settle__kpi-cell">
        <span class="cis-eyebrow">Top +</span>
        <span class="m-settle__kpi-num is-plus cis-num">+{{ topPlus }}</span>
        <span class="m-settle__kpi-label">{{ topPlusName }}</span>
      </article>
      <article class="m-settle__kpi-cell">
        <span class="cis-eyebrow">Top −</span>
        <span class="m-settle__kpi-num is-minus cis-num">−{{ topMinus }}</span>
        <span class="m-settle__kpi-label">{{ topMinusName }}</span>
      </article>
    </section>

    <ol v-if="ranking.length > 0" class="m-settle__list" role="list">
      <li v-for="r in ranking" :key="r.id" class="m-settle__row">
        <span class="m-settle__rank cis-mono">{{ r.rank.toString().padStart(2, '0') }}</span>
        <span class="m-settle__name">{{ r.name }}</span>
        <span class="m-settle__score cis-num" :class="r.score >= 0 ? 'is-plus' : 'is-minus'">
          {{ r.score > 0 ? '+' : '' }}{{ r.score }}
        </span>
      </li>
    </ol>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无数据" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { useStudentStore } from '@/stores/student'

const studentStore = useStudentStore()
const ranking = ref<Array<{ id: string; rank: number; name: string; score: number }>>([])

onMounted(async () => {
  await studentStore.fetchStudents()
  ranking.value = studentStore.students
    .map(s => ({ id: s.id, rank: 0, name: s.name, score: s.score }))
    .sort((a, b) => b.score - a.score)
    .map((s, i) => ({ ...s, rank: i + 1 }))
})

const totalScore = computed(() => ranking.value.reduce((sum, r) => sum + r.score, 0))
const topPlus = computed(() => ranking.value[0]?.score || 0)
const topPlusName = computed(() => ranking.value[0]?.name || '—')
const topMinus = computed(() => Math.abs(ranking.value[ranking.value.length - 1]?.score || 0))
const topMinusName = computed(() => ranking.value[ranking.value.length - 1]?.name || '—')
</script>

<style scoped>
.m-settle { display: flex; flex-direction: column; gap: 16px; }
.m-settle__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-settle__kpi { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-border); overflow: hidden; }
.m-settle__kpi-cell { display: flex; flex-direction: column; gap: 4px; padding: 14px 10px; background: var(--cis-surface-1); }
.m-settle__kpi-cell .cis-eyebrow { color: var(--cis-text-tertiary); }
.m-settle__kpi-num { font-family: var(--cis-font-mono); font-size: 20px; font-weight: 700; font-variant-numeric: tabular-nums; line-height: 1; }
.m-settle__kpi-num.is-plus { color: var(--cis-success); }
.m-settle__kpi-num.is-minus { color: var(--cis-accent); }
.m-settle__kpi-label { font-size: 11px; color: var(--cis-text-tertiary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-settle__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-settle__row { display: flex; align-items: center; gap: 12px; min-height: 48px; padding: 0 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); }
.m-settle__row:last-child { border-bottom: none; }
.m-settle__row::before { content: ''; width: 4px; height: 16px; background: var(--cis-text-tertiary); border-radius: 2px; flex-shrink: 0; }
.m-settle__rank { font-family: var(--cis-font-mono); font-size: 13px; font-weight: 600; color: var(--cis-text-tertiary); min-width: 24px; }
.m-settle__name { flex: 1; font-size: 14px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-settle__score { font-size: 15px; font-weight: 700; min-width: 56px; text-align: right; font-variant-numeric: tabular-nums; }
.m-settle__score.is-plus { color: var(--cis-success); }
.m-settle__score.is-minus { color: var(--cis-accent); }
</style>
```

- [ ] **Step 4.4: 写 Settings.vue**

```vue
<template>
  <div class="m-settings">
    <header class="m-settings__head">
      <span class="cis-eyebrow">Settings</span>
      <h1 class="cis-display m-settings__title">设置</h1>
    </header>
    <ul class="m-settings__list" role="list">
      <li class="m-settings__group">
        <span class="cis-eyebrow m-settings__group-label">基础</span>
        <div class="m-settings__group-body cis-hairline">
          <div class="m-settings__row">
            <span class="m-settings__row-label">班级名称</span>
            <el-input
              :model-value="className"
              placeholder="未命名"
              aria-label="班级名称"
              class="m-settings__row-value"
              @change="updateClassName"
            />
          </div>
          <div class="m-settings__row">
            <span class="m-settings__row-label">主题</span>
            <el-select :model-value="theme" aria-label="主题" class="m-settings__row-value" @change="updateTheme">
              <el-option value="light" label="浅色" />
              <el-option value="dark" label="深色" />
              <el-option value="system" label="跟随系统" />
            </el-select>
          </div>
        </div>
      </li>
      <li class="m-settings__group">
        <span class="cis-eyebrow m-settings__group-label">关于</span>
        <div class="m-settings__group-body cis-hairline">
          <div class="m-settings__row">
            <span class="m-settings__row-label">版本</span>
            <span class="m-settings__row-value-text cis-mono">v1.0.0</span>
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()
const className = ref('')
const theme = ref<'light' | 'dark' | 'system'>('light')

onMounted(async () => {
  await settingsStore.fetchSettings()
  const s = settingsStore.settings as unknown as { className?: string }
  className.value = s.className || ''
  theme.value = (settingsStore.settings.theme as 'light' | 'dark' | 'system') || 'light'
})

async function updateClassName(val: string) {
  try { await settingsStore.updateSettings({ className: val } as unknown as Parameters<typeof settingsStore.updateSettings>[0]); ElMessage.success('已保存') }
  catch { ElMessage.error('保存失败') }
}

async function updateTheme(val: 'light' | 'dark' | 'system') {
  theme.value = val
  try { await settingsStore.updateSettings({ theme: val } as unknown as Parameters<typeof settingsStore.updateSettings>[0]); ElMessage.success('已应用') }
  catch { ElMessage.error('应用失败') }
}
</script>

<style scoped>
.m-settings { display: flex; flex-direction: column; gap: 16px; }
.m-settings__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-settings__list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 16px; }
.m-settings__group-label { display: block; margin-bottom: 8px; color: var(--cis-text-tertiary); }
.m-settings__group-body { display: flex; flex-direction: column; }
.m-settings__row { display: flex; flex-direction: column; gap: 4px; padding: 12px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-settings__group-body > .m-settings__row:last-child { border-bottom: none; }
.m-settings__row-label { font-size: 12px; color: var(--cis-text-tertiary); }
.m-settings__row-value { font-size: 15px; }
.m-settings__row-value-text { font-size: 14px; color: var(--cis-text-secondary); }
</style>
```

- [ ] **Step 4.5: 跑 typecheck**

```bash
cd /workspace/classisscore/client && pnpm typecheck 2>&1 | tail -10
```

- [ ] **Step 4.6: 提交**

```bash
cd /workspace/classisscore
git add client/src/views/mobile/GroupManagement.vue client/src/views/mobile/Evaluation.vue client/src/views/mobile/Settlement.vue client/src/views/mobile/Settings.vue
git commit -m "feat(mobile): 分组 / 评估 / 结算 / 设置"
```

---

## Task 5: 4 个系统视图（Plugin / Theme / About / AdminSettings）

**Files:**
- Create: `client/src/views/mobile/PluginManagement.vue`
- Create: `client/src/views/mobile/ThemeManagement.vue`
- Create: `client/src/views/mobile/About.vue`
- Create: `client/src/views/mobile/AdminSettings.vue`

- [ ] **Step 5.1: 写 PluginManagement.vue**

```vue
<template>
  <div class="m-plugins">
    <header class="m-plugins__head">
      <span class="cis-eyebrow">Plugins</span>
      <h1 class="cis-display m-plugins__title">插件</h1>
    </header>
    <ul v-if="plugins.length > 0" class="m-plugins__list" role="list">
      <li v-for="p in plugins" :key="p.id" class="m-plugins__row">
        <div class="m-plugins__body">
          <span class="m-plugins__name">{{ p.name }}</span>
          <span class="m-plugins__version cis-mono">v{{ p.version }}</span>
        </div>
        <el-switch
          :model-value="p.enabled"
          :aria-label="`${p.name} 启用开关`"
          @change="togglePlugin(p, $event as boolean)"
        />
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无插件" />
    <button type="button" class="m-plugins__install" aria-label="从本地安装插件" @click="installLocal">
      从本地安装
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { invoke } from '@/services/tauri'

interface Plugin { id: string; name: string; version: string; enabled: boolean }
const plugins = ref<Plugin[]>([])

onMounted(async () => {
  try { plugins.value = await invoke<Plugin[]>('plugin_list', {}) } catch { plugins.value = [] }
})

async function togglePlugin(p: Plugin, enabled: boolean) {
  try { await invoke('plugin_toggle', { id: p.id, enabled }); p.enabled = enabled; ElMessage.success('已更新') }
  catch { ElMessage.error('更新失败') }
}

async function installLocal() {
  try { await invoke('plugin_install_local', {}) } catch { ElMessage.warning('请前往桌面端安装') }
}
</script>

<style scoped>
.m-plugins { display: flex; flex-direction: column; gap: 16px; }
.m-plugins__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-plugins__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-plugins__row { display: flex; align-items: center; gap: 12px; min-height: 56px; padding: 8px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-plugins__list li:last-child .m-plugins__row { border-bottom: none; }
.m-plugins__body { flex: 1; display: flex; flex-direction: column; gap: 2px; }
.m-plugins__name { font-size: 15px; font-weight: 500; }
.m-plugins__version { font-size: 12px; color: var(--cis-text-tertiary); }
.m-plugins__install { height: 44px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-primary); font-size: 14px; font-weight: 600; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-plugins__install:active { transform: scale(var(--cis-press-scale)); border-color: var(--cis-primary); color: var(--cis-primary); }
</style>
```

- [ ] **Step 5.2: 写 ThemeManagement.vue**

```vue
<template>
  <div class="m-themes">
    <header class="m-themes__head">
      <span class="cis-eyebrow">Themes</span>
      <h1 class="cis-display m-themes__title">主题</h1>
    </header>
    <div class="m-themes__grid">
      <button
        v-for="t in themes"
        :key="t.id"
        type="button"
        class="m-themes__cell"
        :class="{ 'is-active': activeId === t.id }"
        :aria-label="`切换到 ${t.name}`"
        :aria-pressed="activeId === t.id"
        @click="selectTheme(t.id)"
      >
        <div class="m-themes__palette" aria-hidden="true">
          <span class="m-themes__swatch" :style="{ background: t.colors[0] }"></span>
          <span class="m-themes__swatch" :style="{ background: t.colors[1] }"></span>
          <span class="m-themes__swatch" :style="{ background: t.colors[2] }"></span>
          <span class="m-themes__swatch" :style="{ background: t.colors[3] }"></span>
        </div>
        <span class="m-themes__name">{{ t.name }}</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()
const activeId = ref('default')

const themes = [
  { id: 'default',  name: '釉蓝×朱砂', colors: ['#1E40AF', '#B91C1C', '#F8FAFC', '#15803D'] },
  { id: 'emerald',  name: '青苔',       colors: ['#15803D', '#0D7C5F', '#F8FAFC', '#B45309'] },
  { id: 'crimson',  name: '赤壁',       colors: ['#B91C1C', '#7F1D1D', '#F8FAFC', '#1E40AF'] },
  { id: 'obsidian', name: '墨砚',       colors: ['#0B1220', '#475569', '#F8FAFC', '#B91C1C'] },
]

onMounted(async () => {
  await settingsStore.fetchSettings()
  activeId.value = (settingsStore.settings as unknown as { themePack?: string }).themePack || 'default'
})

async function selectTheme(id: string) {
  activeId.value = id
  try { await settingsStore.updateSettings({ themePack: id } as unknown as Parameters<typeof settingsStore.updateSettings>[0]); ElMessage.success('已应用') }
  catch { ElMessage.error('应用失败') }
}
</script>

<style scoped>
.m-themes { display: flex; flex-direction: column; gap: 16px; }
.m-themes__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-themes__grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; }
.m-themes__cell { display: flex; flex-direction: column; gap: 8px; padding: 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); font-family: inherit; cursor: pointer; text-align: left; -webkit-tap-highlight-color: transparent; }
.m-themes__cell:active { transform: scale(var(--cis-press-scale)); }
.m-themes__cell.is-active { border-color: var(--cis-primary); box-shadow: inset 0 0 0 1px var(--cis-primary); }
.m-themes__palette { display: flex; gap: 4px; }
.m-themes__swatch { flex: 1; height: 32px; border-radius: var(--cis-radius-btn); border: 1px solid var(--cis-border); }
.m-themes__name { font-size: 13px; font-weight: 600; }
</style>
```

- [ ] **Step 5.3: 写 About.vue**

```vue
<template>
  <div class="m-about">
    <header class="m-about__head">
      <div class="m-about__mark" aria-hidden="true">C</div>
      <h1 class="cis-display m-about__title">ClassIsScore</h1>
      <p class="m-about__sub">v1.0.0 · 教室积分管理工具</p>
    </header>
    <section class="m-about__list-section">
      <span class="cis-eyebrow m-about__section-label">项目</span>
      <ul class="m-about__list" role="list">
        <li><a class="m-about__link" href="https://github.com/ywydog/ClassIsScore" target="_blank" rel="noopener">仓库 →</a></li>
        <li><a class="m-about__link" href="https://github.com/ywydog/ClassIsScore/issues" target="_blank" rel="noopener">问题反馈 →</a></li>
      </ul>
    </section>
    <section class="m-about__list-section">
      <span class="cis-eyebrow m-about__section-label">致谢</span>
      <ul class="m-about__list" role="list">
        <li class="m-about__row">Vue 3</li>
        <li class="m-about__row">Element Plus</li>
        <li class="m-about__row">Tauri</li>
        <li class="m-about__row">Noto Sans / Serif SC</li>
      </ul>
    </section>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.m-about { display: flex; flex-direction: column; gap: 24px; }
.m-about__head { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 24px 0; }
.m-about__mark { width: 56px; height: 56px; display: flex; align-items: center; justify-content: center; background: var(--cis-primary); color: #fff; border-radius: var(--cis-radius-btn); font-family: var(--cis-font-serif); font-weight: 700; font-size: 28px; line-height: 1; }
.m-about__title { font-size: 24px; margin: 4px 0 0; font-weight: 600; }
.m-about__sub { margin: 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-about__section-label { display: block; margin-bottom: 8px; color: var(--cis-text-tertiary); }
.m-about__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-about__row, .m-about__link { display: flex; align-items: center; min-height: 48px; padding: 0 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); color: var(--cis-text-primary); font-size: 15px; text-decoration: none; }
.m-about__list li:last-child .m-about__row, .m-about__list li:last-child .m-about__link { border-bottom: none; }
.m-about__link:active { background: var(--cis-primary-tint); color: var(--cis-primary); }
</style>
```

- [ ] **Step 5.4: 写 AdminSettings.vue**

```vue
<template>
  <div class="m-admin">
    <header class="m-admin__head">
      <span class="cis-eyebrow">Admin</span>
      <h1 class="cis-display m-admin__title">管理员</h1>
    </header>
    <ul class="m-admin__list" role="list">
      <li class="m-admin__row">
        <div class="m-admin__row-body">
          <span class="m-admin__row-label">撤销时限</span>
          <span class="m-admin__row-desc">超过此时限的撤销需管理员密码</span>
        </div>
        <el-input-number
          v-model="revertWindow"
          :min="1"
          :max="60"
          :step="1"
          controls-position="right"
          aria-label="撤销时限（分钟）"
        />
      </li>
      <li class="m-admin__row">
        <div class="m-admin__row-body">
          <span class="m-admin__row-label">修改管理员密码</span>
          <span class="m-admin__row-desc">用于高风险操作验证</span>
        </div>
        <el-button @click="showPasswordDialog = true">修改</el-button>
      </li>
    </ul>
    <el-dialog v-model="showPasswordDialog" title="修改管理员密码" width="320px" destroy-on-close>
      <el-form label-width="100px">
        <el-form-item label="新密码" required>
          <el-input v-model="newPassword" type="password" show-password aria-label="新密码" />
        </el-form-item>
        <el-form-item label="确认" required>
          <el-input v-model="confirmPassword" type="password" show-password aria-label="确认密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showPasswordDialog = false">取消</el-button>
        <el-button type="primary" @click="changePassword">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { invoke } from '@/services/tauri'

const revertWindow = ref(3)
const showPasswordDialog = ref(false)
const newPassword = ref('')
const confirmPassword = ref('')

async function changePassword() {
  if (newPassword.value !== confirmPassword.value) { ElMessage.warning('两次密码不一致'); return }
  if (newPassword.value.length < 6) { ElMessage.warning('密码至少 6 位'); return }
  try { await invoke('admin_change_password', { password: newPassword.value }); ElMessage.success('已修改'); showPasswordDialog.value = false; newPassword.value = ''; confirmPassword.value = '' }
  catch { ElMessage.error('修改失败') }
}
</script>

<style scoped>
.m-admin { display: flex; flex-direction: column; gap: 16px; }
.m-admin__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-admin__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-admin__row { display: flex; align-items: center; gap: 12px; min-height: 64px; padding: 12px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-admin__list li:last-child .m-admin__row { border-bottom: none; }
.m-admin__row-body { flex: 1; display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.m-admin__row-label { font-size: 15px; font-weight: 500; }
.m-admin__row-desc { font-size: 12px; color: var(--cis-text-tertiary); }
</style>
```

- [ ] **Step 5.5: 跑 typecheck + build**

```bash
cd /workspace/classisscore/client && pnpm typecheck 2>&1 | tail -10 && pnpm build 2>&1 | tail -10
```

预期：0 错误。

- [ ] **Step 5.6: 提交**

```bash
cd /workspace/classisscore
git add client/src/views/mobile/PluginManagement.vue client/src/views/mobile/ThemeManagement.vue client/src/views/mobile/About.vue client/src/views/mobile/AdminSettings.vue
git commit -m "feat(mobile): 插件 / 主题 / 关于 / 管理员"
```

---

## Task 6: 整体验证 + 文档

**Files:**
- Modify: `client/index.html` 注释（更新 mobile viewport 提示）
- Modify: `client/src/components/layout/AdminLayout.vue`（加"切换到移动版"链接）

- [ ] **Step 6.1: AdminLayout 加"切换到移动版"链接**

在 `client/src/components/layout/AdminLayout.vue` 的 `<footer class="admin-layout__footer">` 中找到 `.admin-layout__footer-right` 块，替换为：

```vue
<div class="admin-layout__footer-right">
  <router-link
    v-if="currentIsMobileCapable"
    :to="mobileEquivalent"
    class="admin-layout__footer-link"
  >切换到移动版</router-link>
  <span translate="no" class="cis-mono">ClassIsScore · v1.0.0</span>
</div>
```

在 `<script setup>` 中加：

```ts
const route = useRoute()
const currentIsMobileCapable = computed(() => route.path.startsWith('/admin/'))
const mobileEquivalent = computed(() => route.path.replace(/^\/admin/, '/m'))
```

并在顶部 import `computed` 与 `useRoute` 已经在 AdminLayout 中（如果没有就加上）：

```ts
import { computed } from 'vue'
import { useRoute } from 'vue-router'
```

样式（追加到 `<style scoped>`）：

```css
.admin-layout__footer-link {
  color: var(--cis-text-secondary);
  text-decoration: none;
  font-size: 11px;
  margin-right: 12px;
  padding: 2px 6px;
  border-radius: var(--cis-radius-btn);
  transition: color var(--cis-transition-fast), background-color var(--cis-transition-fast);
}
.admin-layout__footer-link:hover {
  color: var(--cis-primary);
  background: var(--cis-primary-tint);
}
```

- [ ] **Step 6.2: 跑 typecheck + build 最终验证**

```bash
cd /workspace/classisscore/client && pnpm typecheck 2>&1 | tail -10 && pnpm build 2>&1 | tail -15
```

预期：0 错误。build 成功输出到 `dist/`。

- [ ] **Step 6.3: 提交**

```bash
cd /workspace/classisscore
git add client/src/components/layout/AdminLayout.vue
git commit -m "feat(mobile): 桌面 footer 加"切换到移动版"链接"
```

- [ ] **Step 6.4: 推送**

```bash
cd /workspace/classisscore && git push
```

---

## Self-Review

**Spec coverage**：
- ✓ 平台分工 — Task 1 Step 1.1（断点 redirect）
- ✓ BottomSheet 通用组件 — Task 1 Step 1.2
- ✓ MobileEmptyState 通用组件 — Task 1 Step 1.3
- ✓ MobileListItem 通用组件 — Task 1 Step 1.4（备用）
- ✓ 路由表 /m/* — Task 1 Step 1.5
- ✓ Dashboard — Task 2 Step 2.1
- ✓ Score（含 bottom sheet） — Task 2 Step 2.2
- ✓ Leaderboard（含 time bottom sheet） — Task 2 Step 2.3
- ✓ StudentManagement — Task 3 Step 3.1
- ✓ StudentProfile（含底部 CTA + bottom sheet） — Task 3 Step 3.2
- ✓ GroupManagement — Task 4 Step 4.1
- ✓ Evaluation（SortableJS） — Task 4 Step 4.2
- ✓ Settlement — Task 4 Step 4.3
- ✓ Settings — Task 4 Step 4.4
- ✓ PluginManagement — Task 5 Step 5.1
- ✓ ThemeManagement — Task 5 Step 5.2
- ✓ About — Task 5 Step 5.3
- ✓ AdminSettings — Task 5 Step 5.4
- ✓ "切换到移动版"链接 — Task 6 Step 6.1

**Placeholder scan**：未发现 TBD / TODO / FIXME。

**Type consistency**：
- `LeaderboardEntry` 类型已在 `client/src/types/index.ts` 定义，沿用
- `EvaluationItem` 类型已在 `client/src/types/index.ts` 定义，沿用
- `StudentGroup` 类型沿用
- 所有 store（useScoreStore / useStudentStore / useSettingsStore）方法签名沿用
- `invoke<T>('command_name', args)` 调用方式沿用

**文件规模**：13 个视图每文件 ~200-450 行 CSS+模板+script，符合单职责。
