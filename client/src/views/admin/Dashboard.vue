<template>
  <div class="dashboard">
    <!-- Serif 标题 -->
    <div class="dashboard__greeting">
      <p class="dashboard__eyebrow">{{ greetingEyebrow }}</p>
      <h1 class="dashboard__title">{{ greetingTitle }}<span class="dashboard__title-cursor" aria-hidden="true"></span></h1>
      <p class="dashboard__sub">{{ todayText }} · {{ className }}</p>
    </div>

    <!-- Section 1: Stats Row（Linear 风：单行大数字 + sparkline） -->
    <div class="dashboard__stats" aria-label="总体数据概览">
      <article class="stat-block" v-for="item in statBlocks" :key="item.key">
        <div class="stat-block__head">
          <span class="stat-block__label">{{ item.label }}</span>
          <span v-if="item.delta" class="stat-block__delta" :class="{ 'is-up': item.deltaUp }">
            {{ item.deltaUp ? '↑' : '↓' }} {{ item.delta }}
          </span>
        </div>
        <div class="stat-block__num">{{ item.value }}</div>
        <div class="stat-block__foot">
          <span class="stat-block__suffix">{{ item.suffix }}</span>
          <svg v-if="item.spark" class="stat-block__spark" viewBox="0 0 100 24" aria-hidden="true">
            <polyline
              :points="item.spark"
              fill="none"
              stroke="var(--cis-primary)"
              stroke-width="1.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </div>
      </article>
    </div>

    <!-- Section 2: Score Trend Chart -->
    <section class="dashboard__panel" aria-labelledby="dashboard-trend-title">
      <div class="dashboard__panel-head">
        <h2 id="dashboard-trend-title" class="dashboard__panel-title">积分趋势</h2>
        <router-link to="/admin/scores" class="dashboard__link">查看详情 →</router-link>
      </div>
      <div class="dashboard__chart-body" role="img" aria-labelledby="dashboard-trend-title" aria-label="最近7日积分变动趋势图">
        <svg
          v-if="trendData.length"
          :viewBox="`0 0 ${chartWidth} ${chartHeight}`"
          class="dashboard__svg"
          preserveAspectRatio="none"
          aria-hidden="true"
        >
          <!-- 极简 grid 4 根水平线 -->
          <line
            v-for="i in 4"
            :key="'grid-' + i"
            :x1="chartPadding.left"
            :y1="chartPadding.top + (i - 1) * ((chartHeight - chartPadding.top - chartPadding.bottom) / 3)"
            :x2="chartWidth - chartPadding.right"
            :y2="chartPadding.top + (i - 1) * ((chartHeight - chartPadding.top - chartPadding.bottom) / 3)"
            stroke="var(--cis-border-light)"
            stroke-width="1"
            stroke-dasharray="2 4"
          />
          <!-- X 轴日期 -->
          <text
            v-for="(point, idx) in trendData"
            :key="'label-' + idx"
            :x="point.x"
            :y="chartHeight - 6"
            text-anchor="middle"
            fill="var(--cis-text-tertiary)"
            font-size="11"
            font-family="var(--cis-font-mono)"
            font-variant-numeric="tabular-nums"
          >{{ point.label }}</text>
          <!-- 单线 1.5px 实线 + 端点 2px 圆点（无填充区域） -->
          <path
            :d="linePath"
            fill="none"
            stroke="var(--cis-primary)"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
          <circle
            v-for="(point, idx) in trendData"
            :key="'dot-' + idx"
            :cx="point.x"
            :cy="point.y"
            r="2"
            fill="var(--cis-primary)"
          />
        </svg>
        <div v-else class="dashboard__chart-empty">
          <p class="dashboard__chart-empty-title">暂无趋势数据</p>
          <p class="dashboard__chart-empty-hint">开始给学生加分后，这里会显示近 7 天的积分变动</p>
        </div>
      </div>
    </section>

    <!-- Section 3: Quick Actions（Linear 风：纯文字行 + 1px 下划线） -->
    <section class="dashboard__panel" aria-labelledby="dashboard-actions-title">
      <div class="dashboard__panel-head">
        <h2 id="dashboard-actions-title" class="dashboard__panel-title">快捷操作</h2>
      </div>
      <ul class="dashboard__action-list" role="list">
        <li v-for="action in quickActions" :key="action.path">
          <router-link
            :to="action.path"
            class="dashboard__action-row"
            :aria-label="`前往${action.label}`"
          >
            <span class="dashboard__action-label">{{ action.label }}</span>
            <span class="dashboard__action-hint">{{ action.hint }}</span>
            <span class="dashboard__action-arrow" aria-hidden="true">→</span>
          </router-link>
        </li>
      </ul>
    </section>

    <!-- Section 4: Recent Activity -->
    <section class="dashboard__panel" aria-labelledby="dashboard-activity-title">
      <div class="dashboard__panel-head">
        <h2 id="dashboard-activity-title" class="dashboard__panel-title">最近动态</h2>
        <router-link to="/admin/scores" class="dashboard__link">查看全部 →</router-link>
      </div>
      <ol class="dashboard__activity-list" aria-labelledby="dashboard-activity-title">
        <li
          v-for="record in recentActivity"
          :key="record.id"
          class="dashboard__activity-item"
        >
          <div class="dashboard__activity-left">
            <span class="dashboard__activity-name">{{ record.studentName }}</span>
            <span class="dashboard__activity-reason">{{ record.reason }}</span>
          </div>
          <div class="dashboard__activity-right">
            <span
              class="dashboard__activity-score"
              :class="record.scoreChange >= 0 ? 'is-positive' : 'is-negative'"
            >
              {{ record.scoreChange >= 0 ? '+' : '' }}{{ record.scoreChange }}
            </span>
            <span class="dashboard__activity-time">{{ formatTime(record.createdAt) }}</span>
          </div>
        </li>
        <li v-if="!recentActivity.length" class="dashboard__activity-empty">
          暂无积分记录
        </li>
      </ol>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useStudentStore } from '@/stores/student'
import { useScoreStore } from '@/stores/score'
import { useSettingsStore } from '@/stores/settings'
import { groupApi } from '@/services/group'
import { invoke } from '@/services/tauri'

const studentStore = useStudentStore()
const scoreStore = useScoreStore()
const settingsStore = useSettingsStore()

const stats = ref({
  studentCount: 0,
  avgScore: '0',
  todayCount: 0,
  groupCount: 0,
})
const trendRaw = ref<{ date: string; count: number }[]>([])

const chartWidth = 600
const chartHeight = 200
const chartPadding = { top: 16, right: 24, bottom: 28, left: 24 }

const quickActions = [
  { label: '积分管理', hint: '加减分 / 撤销', path: '/admin/scores' },
  { label: '学生管理', hint: '导入 / 导出', path: '/admin/students' },
  { label: '分组管理', hint: '小组 / 班级', path: '/admin/groups' },
  { label: '排行榜', hint: '个人 / 小组', path: '/admin/leaderboard' },
  { label: '自动评估', hint: '批量规则', path: '/admin/evaluation' },
  { label: '结算', hint: '周月结算', path: '/admin/settlement' },
]

const recentActivity = computed(() => scoreStore.recentRecords.slice(0, 5))

const trendData = computed(() => {
  if (!trendRaw.value.length) return []
  const maxCount = Math.max(...trendRaw.value.map(d => d.count), 1)
  const plotWidth = chartWidth - chartPadding.left - chartPadding.right
  const plotHeight = chartHeight - chartPadding.top - chartPadding.bottom
  return trendRaw.value.map((d, i) => ({
    x: chartPadding.left + (i / Math.max(trendRaw.value.length - 1, 1)) * plotWidth,
    y: chartPadding.top + plotHeight - (d.count / maxCount) * plotHeight,
    label: d.date.slice(5),
    count: d.count,
  }))
})

const linePath = computed(() => {
  if (trendData.value.length < 2) return ''
  return trendData.value.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x},${p.y}`).join(' ')
})

const timeFormatter = new Intl.DateTimeFormat('zh-CN', {
  month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
})

function formatTime(dateStr: string): string {
  const d = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffMin = Math.floor(diffMs / 60000)
  if (diffMin < 1) return '刚刚'
  if (diffMin < 60) return `${diffMin} 分钟前`
  const diffHour = Math.floor(diffMin / 60)
  if (diffHour < 24) return `${diffHour} 小时前`
  return timeFormatter.format(d)
}

// 顶部欢迎语（Serif 字体）
const greetingEyebrow = computed(() => {
  const h = new Date().getHours()
  if (h < 6) return '凌晨好'
  if (h < 12) return '上午好'
  if (h < 14) return '中午好'
  if (h < 18) return '下午好'
  return '晚上好'
})
const greetingTitle = computed(() => {
  const t = new Date()
  const weekday = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'][t.getDay()]
  return `${weekday}，今天先做一件小事。`
})
const todayText = computed(() => {
  const t = new Date()
  return `${t.getFullYear()} 年 ${t.getMonth() + 1} 月 ${t.getDate()} 日`
})
const className = computed(() => {
  const settings = settingsStore.settings as unknown as { className?: string }
  return settings.className?.trim() || '未命名班级'
})

// 4 个 Linear 风数据块
const statBlocks = computed(() => {
  const t = trendRaw.value
  const sparkPath = () => {
    if (t.length < 2) return ''
    // 简单用 trend 的前 7 天作为 sparkline；不同数据用 trend 整体
    return t.map((d, i) => {
      const x = (i / (t.length - 1)) * 100
      const y = 22 - (d.count / Math.max(...t.map(p => p.count), 1)) * 18
      return `${x.toFixed(1)},${y.toFixed(1)}`
    }).join(' ')
  }
  return [
    {
      key: 'todayCount',
      label: '今日变动',
      value: String(stats.value.todayCount),
      suffix: '条',
      delta: stats.value.todayCount > 0 ? `${stats.value.todayCount}` : '0',
      deltaUp: true,
      spark: sparkPath(),
    },
    {
      key: 'studentCount',
      label: '学生总数',
      value: String(stats.value.studentCount),
      suffix: '人',
      delta: '',
      deltaUp: false,
      spark: '',
    },
    {
      key: 'avgScore',
      label: '平均积分',
      value: String(stats.value.avgScore),
      suffix: '分',
      delta: '',
      deltaUp: true,
      spark: '',
    },
    {
      key: 'groupCount',
      label: '活跃分组',
      value: String(stats.value.groupCount),
      suffix: '组',
      delta: '',
      deltaUp: false,
      spark: '',
    },
  ]
})

async function fetchDashboardData() {
  try {
    const [students, groupsRes] = await Promise.all([
      invoke<Array<{ score: number }>>('student_list', {}),
      groupApi.getAll(),
    ])
    const groups: unknown[] = groupsRes.data.data || []
    stats.value.studentCount = students.length
    stats.value.groupCount = groups.length
    if (students.length > 0) {
      const total = students.reduce((sum, s) => sum + s.score, 0)
      stats.value.avgScore = (total / students.length).toFixed(1)
    }
  } catch {
    stats.value.studentCount = studentStore.studentCount
    if (studentStore.students.length > 0) {
      const total = studentStore.students.reduce((sum, s) => sum + s.score, 0)
      stats.value.avgScore = (total / studentStore.students.length).toFixed(1)
    }
  }

  try {
    stats.value.todayCount = await invoke<number>('score_today_count', {})
  } catch {
    const today = new Date().toISOString().slice(0, 10)
    stats.value.todayCount = scoreStore.scoreRecords.filter(
      r => r.createdAt.slice(0, 10) === today
    ).length
  }

  try {
    const trend = await invoke<Array<{ date: string; count: number }>>('score_trend', { days: 7 })
    trendRaw.value = trend
  } catch {
    computeTrendFromRecords()
  }
}

function computeTrendFromRecords() {
  const days: Record<string, number> = {}
  const now = new Date()
  for (let i = 6; i >= 0; i--) {
    const d = new Date(now)
    d.setDate(d.getDate() - i)
    const key = d.toISOString().slice(0, 10)
    days[key] = 0
  }
  scoreStore.scoreRecords.forEach(r => {
    const key = r.createdAt.slice(0, 10)
    if (key in days) days[key]++
  })
  trendRaw.value = Object.entries(days).map(([date, count]) => ({ date, count }))
}

onMounted(async () => {
  await Promise.all([studentStore.fetchStudents(), scoreStore.fetchRecords()])
  await fetchDashboardData()
})
</script>

<style scoped>
.dashboard {
  padding: 32px 32px 48px;
  display: flex;
  flex-direction: column;
  gap: 32px;
  max-width: 1000px;
  margin: 0 auto;
}

/* ===== 顶部 Serif 欢迎语 ===== */
.dashboard__greeting {
  margin-bottom: 8px;
}
.dashboard__eyebrow {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 1.2px;
  text-transform: uppercase;
  color: var(--cis-text-tertiary);
  margin: 0 0 8px 0;
  font-family: var(--cis-font-sans);
}
.dashboard__title {
  font-family: var(--cis-font-serif);
  font-size: 32px;
  font-weight: 600;
  color: var(--cis-text-display);
  margin: 0;
  letter-spacing: -0.5px;
  line-height: 1.2;
  display: inline;
}
.dashboard__title-cursor {
  display: inline-block;
  width: 2px;
  height: 0.9em;
  background: var(--cis-primary);
  margin-left: 4px;
  vertical-align: -2px;
  animation: cis-blink 1.1s steps(1) infinite;
}
@keyframes cis-blink {
  50% { opacity: 0; }
}
.dashboard__sub {
  font-size: 13px;
  color: var(--cis-text-tertiary);
  margin: 8px 0 0 0;
  font-family: var(--cis-font-mono);
  font-variant-numeric: tabular-nums;
}

/* ===== 4 个数据块（Linear 风：单行大数字 + sparkline） ===== */
.dashboard__stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0;
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-card);
  background: var(--cis-surface-1);
  overflow: hidden;
}
.stat-block {
  padding: 18px 20px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  border-right: 1px solid var(--cis-border-light);
}
.stat-block:last-child { border-right: none; }
.stat-block__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.stat-block__label {
  font-size: 11px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.8px;
}
.stat-block__delta {
  font-family: var(--cis-font-mono);
  font-size: 10px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  font-variant-numeric: tabular-nums;
}
.stat-block__delta.is-up { color: var(--cis-success); }
.stat-block__num {
  font-family: var(--cis-font-mono);
  font-size: 32px;
  font-weight: 600;
  color: var(--cis-text-display);
  line-height: 1;
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.5px;
}
.stat-block__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
.stat-block__suffix {
  font-size: 12px;
  color: var(--cis-text-tertiary);
}
.stat-block__spark {
  width: 60px;
  height: 18px;
  flex-shrink: 0;
}

/* ===== 通用面板（1px hairline，不用 box-shadow） ===== */
.dashboard__panel {
  background: var(--cis-surface-1);
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-card);
  padding: 20px 24px;
}
.dashboard__panel-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  margin-bottom: 16px;
}
.dashboard__panel-title {
  font-family: var(--cis-font-serif);
  font-size: 18px;
  font-weight: 600;
  color: var(--cis-text-display);
  margin: 0;
  letter-spacing: -0.2px;
}
.dashboard__link {
  font-size: 12px;
  color: var(--cis-primary);
  text-decoration: none;
  font-weight: 500;
  transition: color var(--cis-transition-fast);
}
.dashboard__link:hover { color: var(--cis-primary-hover); }

/* ===== 图表 ===== */
.dashboard__chart-body {
  width: 100%;
  min-height: 200px;
}
.dashboard__svg {
  width: 100%;
  height: auto;
}
.dashboard__chart-empty {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  gap: 4px;
  min-height: 180px;
  padding: 24px 0;
}
.dashboard__chart-empty-title {
  font-family: var(--cis-font-serif);
  font-size: 16px;
  font-weight: 500;
  color: var(--cis-text-primary);
  margin: 0;
}
.dashboard__chart-empty-hint {
  font-size: 13px;
  color: var(--cis-text-tertiary);
  margin: 0;
}

/* ===== 快捷操作（Linear 风：纯文字行 + 1px 下划线） ===== */
.dashboard__action-list {
  list-style: none;
  margin: 0;
  padding: 0;
}
.dashboard__action-list li {
  border-bottom: 1px solid var(--cis-border-light);
}
.dashboard__action-list li:last-child {
  border-bottom: none;
}
.dashboard__action-row {
  display: grid;
  grid-template-columns: 1fr 2fr auto;
  align-items: center;
  gap: 16px;
  padding: 14px 0;
  text-decoration: none;
  color: var(--cis-text-primary);
  font-size: 14px;
  transition: color var(--cis-transition-fast);
}
.dashboard__action-row:hover {
  color: var(--cis-primary);
}
.dashboard__action-label {
  font-weight: 600;
}
.dashboard__action-hint {
  font-size: 12px;
  color: var(--cis-text-tertiary);
}
.dashboard__action-arrow {
  font-size: 14px;
  color: var(--cis-text-tertiary);
  transition: transform var(--cis-transition-fast), color var(--cis-transition-fast);
}
.dashboard__action-row:hover .dashboard__action-arrow {
  color: var(--cis-primary);
  transform: translateX(2px);
}

/* ===== 最近动态 ===== */
.dashboard__activity-list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
}
.dashboard__activity-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 0;
  border-bottom: 1px solid var(--cis-border-light);
}
.dashboard__activity-item:last-child {
  border-bottom: none;
}
.dashboard__activity-left {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.dashboard__activity-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--cis-text-primary);
}
.dashboard__activity-reason {
  font-size: 12px;
  color: var(--cis-text-tertiary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.dashboard__activity-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
  flex-shrink: 0;
  margin-left: 16px;
}
.dashboard__activity-score {
  font-family: var(--cis-font-mono);
  font-size: 16px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.2px;
}
.dashboard__activity-score.is-positive { color: var(--cis-success); }
.dashboard__activity-score.is-negative { color: var(--cis-accent); }
.dashboard__activity-time {
  font-size: 11px;
  color: var(--cis-text-tertiary);
  font-family: var(--cis-font-mono);
  font-variant-numeric: tabular-nums;
}
.dashboard__activity-empty {
  text-align: center;
  padding: 24px 0;
  color: var(--cis-text-tertiary);
  font-size: 13px;
}

@media (max-width: 768px) {
  .dashboard__stats { grid-template-columns: repeat(2, 1fr); }
  .stat-block { border-right: 1px solid var(--cis-border-light); border-bottom: 1px solid var(--cis-border-light); }
  .stat-block:nth-child(2n) { border-right: none; }
  .stat-block:nth-last-child(-n+2) { border-bottom: none; }
  .dashboard__action-row { grid-template-columns: 1fr auto; }
  .dashboard__action-hint { display: none; }
}
</style>
