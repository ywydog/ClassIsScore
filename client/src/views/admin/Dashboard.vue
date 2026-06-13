<template>
  <div class="dashboard">
    <!-- Section 1: Stats Row -->
    <div class="dashboard__stats">
      <div class="dashboard__stat-card dashboard__stat-card--students">
        <div class="dashboard__stat-icon">
          <el-icon :size="24"><User /></el-icon>
        </div>
        <div class="dashboard__stat-info">
          <span class="dashboard__stat-number">{{ stats.studentCount }}</span>
          <span class="dashboard__stat-label">学生总数</span>
        </div>
      </div>
      <div class="dashboard__stat-card dashboard__stat-card--avg">
        <div class="dashboard__stat-icon">
          <el-icon :size="24"><TrendCharts /></el-icon>
        </div>
        <div class="dashboard__stat-info">
          <span class="dashboard__stat-number">{{ stats.avgScore }}</span>
          <span class="dashboard__stat-label">平均积分</span>
        </div>
      </div>
      <div class="dashboard__stat-card dashboard__stat-card--today">
        <div class="dashboard__stat-icon">
          <el-icon :size="24"><Timer /></el-icon>
        </div>
        <div class="dashboard__stat-info">
          <span class="dashboard__stat-number">{{ stats.todayCount }}</span>
          <span class="dashboard__stat-label">今日变动</span>
        </div>
      </div>
      <div class="dashboard__stat-card dashboard__stat-card--groups">
        <div class="dashboard__stat-icon">
          <el-icon :size="24"><Grid /></el-icon>
        </div>
        <div class="dashboard__stat-info">
          <span class="dashboard__stat-number">{{ stats.groupCount }}</span>
          <span class="dashboard__stat-label">活跃分组</span>
        </div>
      </div>
    </div>

    <!-- Section 2: Score Trend Chart -->
    <div class="dashboard__chart">
      <div class="dashboard__section-header">
        <h3>积分趋势</h3>
        <router-link to="/admin/scores" class="dashboard__link">查看详情</router-link>
      </div>
      <div class="dashboard__chart-body">
        <svg
          v-if="trendData.length"
          :viewBox="`0 0 ${chartWidth} ${chartHeight}`"
          class="dashboard__svg"
          preserveAspectRatio="xMidYMid meet"
        >
          <!-- Grid lines -->
          <line
            v-for="i in 4"
            :key="'grid-' + i"
            :x1="chartPadding.left"
            :y1="chartPadding.top + (i - 1) * ((chartHeight - chartPadding.top - chartPadding.bottom) / 3)"
            :x2="chartWidth - chartPadding.right"
            :y2="chartPadding.top + (i - 1) * ((chartHeight - chartPadding.top - chartPadding.bottom) / 3)"
            stroke="var(--cis-border-color-light)"
            stroke-width="1"
          />
          <!-- X-axis labels -->
          <text
            v-for="(point, idx) in trendData"
            :key="'label-' + idx"
            :x="point.x"
            :y="chartHeight - 6"
            text-anchor="middle"
            fill="var(--cis-text-tertiary)"
            font-size="11"
          >{{ point.label }}</text>
          <!-- Area fill -->
          <path
            :d="areaPath"
            fill="url(#trendGradient)"
            opacity="0.25"
          />
          <!-- Line -->
          <path
            :d="linePath"
            fill="none"
            stroke="var(--cis-primary)"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
          <!-- Dots -->
          <circle
            v-for="(point, idx) in trendData"
            :key="'dot-' + idx"
            :cx="point.x"
            :cy="point.y"
            r="4"
            fill="var(--cis-primary)"
            stroke="#fff"
            stroke-width="2"
          />
          <!-- Gradient definition -->
          <defs>
            <linearGradient id="trendGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stop-color="var(--cis-primary)" stop-opacity="0.4" />
              <stop offset="100%" stop-color="var(--cis-primary)" stop-opacity="0" />
            </linearGradient>
          </defs>
        </svg>
        <div v-else class="dashboard__chart-empty">
          <el-icon :size="32" color="var(--cis-text-tertiary)"><TrendCharts /></el-icon>
          <span>暂无趋势数据</span>
        </div>
      </div>
    </div>

    <!-- Section 3: Quick Actions Grid -->
    <div class="dashboard__actions">
      <div class="dashboard__section-header">
        <h3>快捷操作</h3>
      </div>
      <div class="dashboard__actions-grid">
        <div
          v-for="action in quickActions"
          :key="action.path"
          class="dashboard__action-card"
          @click="router.push(action.path)"
        >
          <el-icon :size="28"><component :is="action.icon" /></el-icon>
          <span class="dashboard__action-label">{{ action.label }}</span>
        </div>
      </div>
    </div>

    <!-- Section 4: Recent Activity -->
    <div class="dashboard__activity">
      <div class="dashboard__section-header">
        <h3>最近动态</h3>
        <router-link to="/admin/scores" class="dashboard__link">查看全部</router-link>
      </div>
      <div class="dashboard__activity-list">
        <div
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
        </div>
        <div v-if="!recentActivity.length" class="dashboard__activity-empty">
          暂无积分记录
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { User, Grid, Timer, Trophy, Rank, Finished, TrendCharts } from '@element-plus/icons-vue'
import { useStudentStore } from '@/stores/student'
import { useScoreStore } from '@/stores/score'
import { groupApi } from '@/services/group'
import api from '@/services/api'

const router = useRouter()
const studentStore = useStudentStore()
const scoreStore = useScoreStore()

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
  { label: '积分管理', path: '/admin/scores', icon: Trophy },
  { label: '学生管理', path: '/admin/students', icon: User },
  { label: '分组管理', path: '/admin/groups', icon: Grid },
  { label: '排行榜', path: '/admin/leaderboard', icon: Rank },
  { label: '自动评估', path: '/admin/evaluation', icon: Timer },
  { label: '结算', path: '/admin/settlement', icon: Finished },
]

const recentActivity = computed(() => {
  return scoreStore.recentRecords.slice(0, 5)
})

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
  return trendData.value
    .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x},${p.y}`)
    .join(' ')
})

const areaPath = computed(() => {
  if (trendData.value.length < 2) return ''
  const bottom = chartHeight - chartPadding.bottom
  const line = trendData.value
    .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x},${p.y}`)
    .join(' ')
  const last = trendData.value[trendData.value.length - 1]
  const first = trendData.value[0]
  return `${line} L${last.x},${bottom} L${first.x},${bottom} Z`
})

function formatTime(dateStr: string): string {
  const d = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffMin = Math.floor(diffMs / 60000)
  if (diffMin < 1) return '刚刚'
  if (diffMin < 60) return `${diffMin}分钟前`
  const diffHour = Math.floor(diffMin / 60)
  if (diffHour < 24) return `${diffHour}小时前`
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

async function fetchDashboardData() {
  try {
    const [studentsRes, groupsRes] = await Promise.all([
      api.get('/api/students'),
      groupApi.getAll(),
    ])

    const students: { score: number }[] = studentsRes.data.data || []
    const groups: unknown[] = groupsRes.data.data || []

    stats.value.studentCount = students.length
    stats.value.groupCount = groups.length

    if (students.length > 0) {
      const total = students.reduce((sum, s) => sum + s.score, 0)
      stats.value.avgScore = (total / students.length).toFixed(1)
    }
  } catch {
    // fallback to store data
    stats.value.studentCount = studentStore.studentCount
    if (studentStore.students.length > 0) {
      const total = studentStore.students.reduce((sum, s) => sum + s.score, 0)
      stats.value.avgScore = (total / studentStore.students.length).toFixed(1)
    }
  }

  // Fetch today's count
  try {
    const res = await api.get('/api/scores/today-count')
    stats.value.todayCount = res.data.data ?? 0
  } catch {
    // Fallback: compute from recent records
    const today = new Date().toISOString().slice(0, 10)
    stats.value.todayCount = scoreStore.scoreRecords.filter(
      r => r.createdAt.slice(0, 10) === today
    ).length
  }

  // Fetch trend data
  try {
    const res = await api.get('/api/scores/trend', { params: { days: 7 } })
    trendRaw.value = res.data.data || []
  } catch {
    // Fallback: compute client-side from records
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
    if (key in days) {
      days[key]++
    }
  })
  trendRaw.value = Object.entries(days).map(([date, count]) => ({ date, count }))
}

onMounted(async () => {
  await Promise.all([
    studentStore.fetchStudents(),
    scoreStore.fetchRecords(),
  ])
  await fetchDashboardData()
})
</script>

<style scoped>
.dashboard {
  padding: var(--cis-spacing-lg);
  display: flex;
  flex-direction: column;
  gap: var(--cis-spacing-lg);
  max-width: 960px;
}

/* Section 1: Stats Row */
.dashboard__stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--cis-spacing-md);
}

.dashboard__stat-card {
  display: flex;
  align-items: center;
  gap: var(--cis-spacing-md);
  padding: var(--cis-spacing-lg);
  border-radius: var(--cis-radius-lg);
  background: var(--cis-card-bg);
  box-shadow: var(--cis-shadow-card);
  transition: transform var(--cis-transition-fast), box-shadow var(--cis-transition-fast);
  cursor: default;
}

.dashboard__stat-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--cis-shadow-card-hover);
}

.dashboard__stat-card--students {
  background: linear-gradient(135deg, var(--cis-primary-light-9), var(--cis-card-bg));
}

.dashboard__stat-card--avg {
  background: linear-gradient(135deg, #fef9c3, var(--cis-card-bg));
}

.dashboard__stat-card--today {
  background: linear-gradient(135deg, #dbeafe, var(--cis-card-bg));
}

.dashboard__stat-card--groups {
  background: linear-gradient(135deg, #fce7f3, var(--cis-card-bg));
}

.dashboard__stat-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--cis-radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.dashboard__stat-card--students .dashboard__stat-icon {
  background: var(--cis-gradient-primary);
  color: #fff;
}

.dashboard__stat-card--avg .dashboard__stat-icon {
  background: var(--cis-gradient-warm);
  color: #fff;
}

.dashboard__stat-card--today .dashboard__stat-icon {
  background: var(--cis-gradient-cool);
  color: #fff;
}

.dashboard__stat-card--groups .dashboard__stat-icon {
  background: linear-gradient(135deg, #ec4899, #f472b6);
  color: #fff;
}

.dashboard__stat-info {
  display: flex;
  flex-direction: column;
}

.dashboard__stat-number {
  font-family: var(--cis-font-family-display);
  font-size: 28px;
  font-weight: 700;
  color: var(--cis-text-primary);
  line-height: 1.1;
}

.dashboard__stat-label {
  font-size: var(--cis-font-size-sm);
  color: var(--cis-text-tertiary);
  margin-top: 2px;
}

/* Section header */
.dashboard__section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--cis-spacing-md);
}

.dashboard__section-header h3 {
  font-family: var(--cis-font-family-display);
  font-size: var(--cis-font-size-lg);
  font-weight: 600;
  color: var(--cis-text-primary);
  margin: 0;
}

.dashboard__link {
  font-size: var(--cis-font-size-sm);
  color: var(--cis-primary);
  text-decoration: none;
  transition: color var(--cis-transition-fast);
}

.dashboard__link:hover {
  color: var(--cis-primary-dark);
  text-decoration: underline;
}

/* Section 2: Chart */
.dashboard__chart {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  padding: var(--cis-spacing-lg);
  box-shadow: var(--cis-shadow-card);
}

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
  align-items: center;
  justify-content: center;
  gap: var(--cis-spacing-sm);
  min-height: 180px;
  color: var(--cis-text-tertiary);
  font-size: var(--cis-font-size-sm);
}

/* Section 3: Quick Actions */
.dashboard__actions {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  padding: var(--cis-spacing-lg);
  box-shadow: var(--cis-shadow-card);
}

.dashboard__actions-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--cis-spacing-md);
}

.dashboard__action-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--cis-spacing-sm);
  padding: var(--cis-spacing-lg) var(--cis-spacing-md);
  border-radius: var(--cis-radius-lg);
  border: 1px solid var(--cis-border-color-light);
  background: var(--cis-bg);
  cursor: pointer;
  transition: all var(--cis-transition-fast);
}

.dashboard__action-card:hover {
  border-color: var(--cis-primary);
  transform: translateY(-2px);
  box-shadow: var(--cis-shadow-card-hover);
  color: var(--cis-primary);
}

.dashboard__action-card .el-icon {
  color: var(--cis-primary);
}

.dashboard__action-label {
  font-size: var(--cis-font-size-sm);
  font-weight: 500;
  color: var(--cis-text-secondary);
}

.dashboard__action-card:hover .dashboard__action-label {
  color: var(--cis-primary);
}

/* Section 4: Recent Activity */
.dashboard__activity {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  padding: var(--cis-spacing-lg);
  box-shadow: var(--cis-shadow-card);
}

.dashboard__activity-list {
  display: flex;
  flex-direction: column;
}

.dashboard__activity-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--cis-spacing-sm) 0;
  border-bottom: 1px solid var(--cis-border-color-light);
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
  font-size: var(--cis-font-size-base);
  font-weight: 500;
  color: var(--cis-text-primary);
}

.dashboard__activity-reason {
  font-size: var(--cis-font-size-sm);
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
  margin-left: var(--cis-spacing-md);
}

.dashboard__activity-score {
  font-family: var(--cis-font-family-display);
  font-size: var(--cis-font-size-lg);
  font-weight: 700;
}

.dashboard__activity-score.is-positive {
  color: var(--cis-success);
}

.dashboard__activity-score.is-negative {
  color: var(--cis-danger);
}

.dashboard__activity-time {
  font-size: var(--cis-font-size-sm);
  color: var(--cis-text-tertiary);
}

.dashboard__activity-empty {
  text-align: center;
  padding: var(--cis-spacing-xl) 0;
  color: var(--cis-text-tertiary);
  font-size: var(--cis-font-size-sm);
}

/* Responsive */
@media (max-width: 768px) {
  .dashboard__stats {
    grid-template-columns: repeat(2, 1fr);
  }

  .dashboard__actions-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>
