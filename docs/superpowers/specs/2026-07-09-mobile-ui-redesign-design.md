# 移动端 UI 全套重设计 — 设计文档

> **状态**：已批准（2026-07-09）
> **范围**：13 个 mobile 视图 + 1 个通用 BottomSheet 组件
> **目标平台**：手机竖屏（≤ 600 px）；平板 / 横屏自动跳桌面

---

## 1. 背景

ClassIsScore 是一套 Tauri 2 + Vue 3 教室积分管理应用。当前 `/m/*` 路由虽然已经存在 MobileLayout 容器（顶部 header + 底部 bottom nav + 左侧 drawer），但内部的视图组件与 `/admin/*` 共用桌面版本。在 ≤ 600 px 窄屏上：

- Dashboard 的 4 列 stat blocks 横向溢出
- ScoreManagement 复杂表格无法滚动到底
- 触控目标过小（< 32 px），错点率高
- bottom sheet / FAB / 滑动手势等 mobile 原生交互模式缺失

教室场景中，老师经常需要用手机或安卓平板快速加分、看排行。本次任务是为移动端提供完整的、独立的、touch-first 的 UI。

## 2. 平台分工

| 设备 | 宽度 | 走哪套 |
|---|---|---|
| 手机竖屏 | ≤ 600 px | `/m/*` MobileLayout + mobile 视图 |
| 手机横屏 | 601-1023 px | 自动 redirect 到 `/admin/*` |
| iPad / Android 平板 | ≥ 1024 px | `/admin/*` 桌面端 |
| 桌面 | 任意 | `/admin/*` 桌面端 |

> **设计决策**：平板不写独立布局，直接复用桌面端。理由：(1) 平板用户已经习惯密集信息布局，(2) 避免维护两套相近布局，(3) 平板端 touch 事件在桌面视图上工作良好（Element Plus 默认支持）。代价：平板上没有 mobile 视图的极简感，但可在未来重做。

**redirect 策略**：MobileLayout 在挂载和窗口尺寸变化时检测宽度。若 `window.innerWidth >= 768` 或 `window.matchMedia('(orientation: landscape)').matches && window.innerWidth >= 601`，自动 `router.replace(toAdminPath(currentPath))`。

## 3. 架构

### 3.1 目录结构

```
client/src/
├── components/
│   ├── layout/
│   │   ├── AdminLayout.vue
│   │   └── MobileLayout.vue        # 增强：宽度断点监听 + 自动跳桌面
│   └── mobile/
│       ├── BottomSheet.vue          # 通用底部抽屉
│       ├── MobileEmptyState.vue     # 通用空态
│       └── MobileListItem.vue       # 通用列表项
├── views/
│   ├── admin/                       # 桌面视图（不动）
│   │   └── ...
│   └── mobile/                      # 新建
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
    └── index.ts                     # 增强：/m/* 路由表
```

### 3.2 路由表

```ts
// router/index.ts (增量)
const mobileRoutes = [
  { path: '/m/dashboard',         component: () => import('@/views/mobile/Dashboard.vue'),         meta: { title: '总览', mobile: true } },
  { path: '/m/scores',            component: () => import('@/views/mobile/ScoreManagement.vue'),  meta: { title: '积分', mobile: true } },
  { path: '/m/students',          component: () => import('@/views/mobile/StudentManagement.vue'), meta: { title: '学生', mobile: true } },
  { path: '/m/students/:id',      component: () => import('@/views/mobile/StudentProfile.vue'),   meta: { title: '学生详情', mobile: true } },
  { path: '/m/groups',            component: () => import('@/views/mobile/GroupManagement.vue'),   meta: { title: '分组', mobile: true } },
  { path: '/m/leaderboard',       component: () => import('@/views/mobile/Leaderboard.vue'),       meta: { title: '排行', mobile: true } },
  { path: '/m/evaluation',        component: () => import('@/views/mobile/Evaluation.vue'),        meta: { title: '评估', mobile: true } },
  { path: '/m/settlement',        component: () => import('@/views/mobile/Settlement.vue'),        meta: { title: '结算', mobile: true } },
  { path: '/m/settings',          component: () => import('@/views/mobile/Settings.vue'),          meta: { title: '设置', mobile: true } },
  { path: '/m/plugins',           component: () => import('@/views/mobile/PluginManagement.vue'),  meta: { title: '插件', mobile: true } },
  { path: '/m/themes',            component: () => import('@/views/mobile/ThemeManagement.vue'),   meta: { title: '主题', mobile: true } },
  { path: '/m/about',             component: () => import('@/views/mobile/About.vue'),             meta: { title: '关于', mobile: true } },
  { path: '/m/admin-settings',    component: () => import('@/views/mobile/AdminSettings.vue'),     meta: { title: '管理员', mobile: true } },
]

// MobileLayout 已存在，新增 children
{
  path: '/m',
  component: MobileLayout,
  children: mobileRoutes,
}
```

### 3.3 路由分流

`MobileLayout.vue` 在挂载时执行：

```ts
function maybeRedirectToDesktop() {
  const isWide = window.innerWidth >= 768 || (
    window.matchMedia('(orientation: landscape)').matches &&
    window.innerWidth >= 601
  )
  if (isWide && route.path.startsWith('/m/')) {
    const desktopPath = route.path.replace(/^\/m/, '/admin')
    router.replace(desktopPath)
  }
}

onMounted(() => {
  maybeRedirectToDesktop()
  window.addEventListener('resize', debounce(maybeRedirectToDesktop, 200))
})
```

> **注意**：从桌面端跳回 mobile 端需用户手动操作（URL 栏）。这一方向不做自动 redirect（避免在 resize 边缘抖动）。

## 4. 设计语言

延续"釉蓝 × 朱砂"调色板 + Apple 化微交互，叠加 mobile-specific 调整：

| 维度 | 桌面值 | mobile 值 | 理由 |
|---|---|---|---|
| body 字号 | 15px | 16px | 触屏阅读舒适 |
| 触控目标 | — | ≥ 44 × 44 px | Apple HIG / Material 3 |
| 主按钮高度 | 32px | 44px | 拇指可达 |
| 列表行高 | 36-44px | 56px | 触屏点准 |
| 圆角 btn | 4px | 4px | 不变 |
| 圆角 card | 8px | 8px | 不变 |
| 圆角 bottom sheet | — | 12px (顶部) | mobile 专属 |
| 状态条 | 6px 圆 | 6px 圆 | 不变 |
| 字体 | Noto Sans/Serif SC | 同 | 字体本地化已就位 |
| 交互 | hover 强调 | tap / active 强调 | mobile 无 hover |
| 间距 | 24-32px section | 16-20px section | 紧凑 |

## 5. 通用组件

### 5.1 BottomSheet.vue

自实现的底部抽屉组件。Element Plus 无原生 bottom sheet，需要从零实现。

**Props**：
- `modelValue: boolean` — 显示状态
- `title?: string` — 标题
- `height?: 'auto' | 'half' | 'full'` — 默认 'auto'
- `closeOnBackdrop?: boolean` — 默认 true
- `dismissible?: boolean` — 默认 true

**结构**：
- 背景：半透明黑色 + backdrop-filter blur(4px)
- 内容：底部 fixed 容器，transform translateY 进入
- 顶部 12px 圆角 + 1px hairline 顶边
- 顶部中央 32×4px 拖动手柄（`background: var(--cis-border-strong)`）

**事件**：
- `update:modelValue` — 状态同步
- `close` — 关闭

**动画**：
- 进入：transform translateY(100%) → translateY(0)，250ms cubic-bezier(0.32, 0.72, 0, 1)
- 离开：反之
- 背景 fade 200ms

**API 示例**：
```vue
<BottomSheet v-model="showSheet" title="加积分">
  <ScoreFormContent :student="currentStudent" @submit="onSubmit" />
</BottomSheet>
```

### 5.2 MobileEmptyState.vue

通用空态：图标 + eyebrow + 文字 + 可选 CTA。

### 5.3 MobileListItem.vue

通用列表项：左侧 avatar / icon，右侧主标题 + 副标题，右侧 chevron / 数字。

## 6. 各视图详细形态

### 6.1 Dashboard

```
┌─────────────────────────────┐
│ Header: "ClassIsScore" + menu│
├─────────────────────────────┤
│ Dashboard  02  班级名        │
│ 总览                       │
│ X 名 · 更新于 21:34         │
├─────────────────────────────┤
│ 今日积分                    │
│ +24     ▁▂▃▅▆▇            │
├─────────────────────────────┤
│ 本周积分                    │
│ +187    ▁▃▅▇▆▅            │
├─────────────────────────────┤
│ 学生数  班级名              │
│  42     高一(3)班          │
├─────────────────────────────┤
│ 快捷操作                    │
│  加积分              →      │
│  看排行              →      │
│  学生管理            →      │
├─────────────────────────────┤
│ 最近积分                    │
│ 张三  +2  回答正确  10:23  │
│ 李四  -1  迟到      10:21  │
│ ...                         │
└─────────────────────────────┘
```

- 顶部 Serif "总览" + 闪烁光标
- 4 个 stat blocks 单列大数字 + sparkline（与桌面一致，但垂直堆叠）
- 快捷操作：纯文字行（与桌面一致）
- 最近积分：3-5 条 timeline，超过点击"查看全部"进入 Score

### 6.2 ScoreManagement

```
┌─────────────────────────────┐
│ Header                      │
├─────────────────────────────┤
│ Score  01                   │
│ 积分                        │
├─────────────────────────────┤
│ ┌─────────────────────┐    │
│ │ 学生 ▾   [+2] [原因]│    │
│ │ [加] [减]           │    │
│ └─────────────────────┘    │
├─────────────────────────────┤
│ 评价项：                    │
│ [回答问题 +2] [迟到 -1]...  │
├─────────────────────────────┤
│ 今日记录                    │
│ 张三  +2  回答问题  10:23   │
│ 李四  -1  迟到      10:21   │
│ ...                         │
│                             │
│                    [+] FAB │
└─────────────────────────────┘
```

- 顶部内联 form：学生 select / 数字 / 原因 / 加 / 减（与桌面一致但单列）
- 评价项 chips：横向滚动
- 今日 timeline
- 右下 FAB "+" → bottom sheet（评价项大网格 + 数字输入 + 学生选择）

### 6.3 Leaderboard

```
┌─────────────────────────────┐
│ Header                      │
├─────────────────────────────┤
│ Leaderboard                 │
│ 排行榜     个人/小组 ▾      │
├─────────────────────────────┤
│ [01]  02  03  ← Tabs       │
│ ─────────────               │
│  01                        │
│  [3 柱领奖台 紧凑]          │
│  🥇 张三  +24              │
│  🥈 李四  +18              │
│  🥉 王五  +12              │
├─────────────────────────────┤
│ 04  赵六    +8             │
│ 05  钱七    +5             │
│ ...                         │
└─────────────────────────────┘
```

- 顶部 Tab：个人/小组（与桌面一致）
- 时间范围：1 个 select 触发 bottom sheet（不直接展示，节省顶部空间）
- 3 柱领奖台：横排紧凑，< 360px 宽时第二柱置顶（1-2-3 → 2-1-3），< 320px 改纵排
- 下面列表：每行 4px 排名条 + 名字 + Mono 分数（与桌面一致）

### 6.4 StudentManagement

```
┌─────────────────────────────┐
│ Header                      │
├─────────────────────────────┤
│ Students                    │
│ 学生                        │
│ [🔍 搜索学生...]            │
│ 排序: 分数↓ ▾               │
├─────────────────────────────┤
│ [Z] 张三  001  +24      ›  │
│ [L] 李四  002  +18      ›  │
│ [W] 王五  003  +12      ›  │
│ ...                         │
└─────────────────────────────┘
```

- 顶部搜索 + 排序选择（bottom sheet）
- 列表：avatar 字母 + 名字 + 学号 + Mono 分数
- 点击 → /m/students/:id 详情

### 6.5 StudentProfile

```
┌─────────────────────────────┐
│ ←  张三                     │
├─────────────────────────────┤
│                             │
│        [Z]  张三            │
│        001  高一(3)班       │
│                             │
│         +24                 │
│   ──────────                │
│   ▁▂▃▅▆▇  (sparkline)      │
│                             │
│  [积分记录] [评估]           │
├─────────────────────────────┤
│ 10:23  +2  回答问题         │
│ 10:21  -1  迟到             │
│ ...                         │
├─────────────────────────────┤
│ [   加积分   ]              │ ← sticky bottom
└─────────────────────────────┘
```

- Hero：名字大号 Serif + 当前分数 Mono 32px + 趋势 sparkline
- Tabs：积分记录 / 评估
- 底部 sticky "加积分" 按钮 → bottom sheet

### 6.6 GroupManagement

- 列表：组名 + 人数 + 总分 Mono
- 点击 → 成员 grid 视图（4 列 avatar + 名字 + 分数）

### 6.7 Evaluation

- 分组列表：学科 / 行为 / 表现
- 长按拖动排序（SortableJS，触屏已支持）
- 评价项编辑：底部抽屉

### 6.8 Settlement

- 顶部 KPI 卡片（总分 / 加分最多学生 / 减分最多学生）
- 时间窗切换
- 学生排名 list

### 6.9 Settings

- 列表项卡片：label 上 / value 下
- 班级名称、主题、退出登录、版本号

### 6.10 PluginManagement

- 列表（插件名 + 状态 toggle + 版本）
- 底部 "从本地安装" 按钮

### 6.11 ThemeManagement

- 4 列网格，每格 1 主题
- 每个格：色板（4 圆色块）+ 名称
- 点击 → 切换主题，prefers-color-scheme 自动同步

### 6.12 About

- 顶部 logo + Serif 标题
- 版本号 / 仓库链接 / 致谢列表

### 6.13 AdminSettings

- 列表项（管理员密码 / 撤销权限 / 撤销时限）
- 全部走 form

## 7. 关键技术点

### 7.1 断点检测

`useResizeObserver` + `matchMedia` 双向检测：
- 初始挂载时检查
- resize 事件 debounce 200ms
- 媒体查询 `(min-width: 768px)` 与 orientation media query 结合

### 7.2 触屏优化

- 移除所有 `:hover` 视觉变化
- 用 `:active` / `:focus-visible` 替代
- 列表项 `-webkit-tap-highlight-color: transparent`
- 按钮 `:active { transform: scale(var(--cis-press-scale)) }`

### 7.3 safe-area

```css
.mobile-layout {
  padding-top: env(safe-area-inset-top);
  padding-bottom: env(safe-area-inset-bottom);
}

.mobile-layout__bottom-nav {
  padding-bottom: env(safe-area-inset-bottom);
}
```

### 7.4 数字键盘

`<el-input type="number" inputmode="numeric">` 调起系统数字键盘。

### 7.5 拖动排序

SortableJS（已有依赖）。触屏支持：

```js
new Sortable(el, {
  animation: 150,
  handle: '.drag-handle',
  delay: 100,         // 长按触发
  delayOnTouchOnly: true,
})
```

## 8. 范围外

- 不做 PWA / Service Worker（Tauri 桌面不需要）
- 不重写 IPC / 后端逻辑
- 不做推送通知 / 后台运行
- 不做 iPad 专属布局（统一走桌面端）
- 不做 desktop 视图在窄屏上的响应式优化（mobile 视图独立写）
- 不做手机横屏专属布局（自动跳桌面）

## 9. 工作量估算

- 13 个 mobile 视图 + 1 个 BottomSheet + 2 个通用组件
- ~2500 行 Vue + ~1200 行 CSS
- 不引入新依赖
- 4-5 个提交

## 10. 验证

- `pnpm typecheck` 0 错误
- Chrome DevTools 切 iPhone 14 Pro (390×844) / iPhone 15 (393×852) / Pixel 7 (412×915) / Galaxy S22 (360×800) 截核心 3 页（Dashboard / Score / Leaderboard）
- iPad (1024×768) 验证自动跳桌面
- Android 真机（可用 Tauri 模拟器）
- 触屏点准率：每个按钮 ≥ 44 × 44 px

## 11. 风险与权衡

| 风险 | 缓解 |
|---|---|
| 13 个视图工作量大，时间紧张 | 拆 4-5 个提交，每组 2-3 个视图 |
| SortableJS 触屏长按与 scroll 冲突 | `delayOnTouchOnly: true` + handle 限定 |
| BottomSheet 跨 iOS Safari / Android Chrome 行为差异 | 用 transform 而非 top/left 动画，CSS contain |
| 路由 redirect 在 resize 边缘抖动 | debounce 200ms + 仅 m→admin 单向 |
| 平板跳转后用户想回 mobile | 提供"切换到移动版"链接（在 AdminLayout footer） |
