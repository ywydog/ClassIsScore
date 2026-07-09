<template>
  <div class="leaderboard">
    <!-- 顶部标题区 -->
    <header class="leaderboard__head">
      <div class="leaderboard__head-left">
        <span class="cis-eyebrow">Leaderboard</span>
        <h1 class="cis-display leaderboard__title">排行榜</h1>
        <p class="leaderboard__sub">
          <span class="cis-mono">{{ entries.length }}</span> 名 ·
          更新于 <span class="cis-mono">{{ updatedAt }}</span>
        </p>
      </div>
      <div class="leaderboard__head-actions">
        <button class="leaderboard__icon-btn" :aria-label="'刷新排行榜'" @click="fetchLeaderboard">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M3 12a9 9 0 0 1 15.5-6.3L21 8" />
            <path d="M21 3v5h-5" />
            <path d="M21 12a9 9 0 0 1-15.5 6.3L3 16" />
            <path d="M3 21v-5h5" />
          </svg>
        </button>
        <button class="leaderboard__icon-btn" :aria-label="'导出排行榜'" @click="handleExport">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
            <polyline points="7 10 12 15 17 10" />
            <line x1="12" y1="15" x2="12" y2="3" />
          </svg>
        </button>
      </div>
    </header>

    <!-- Tabs：模式 + 时间范围 -->
    <div class="leaderboard__tabs" role="tablist" aria-label="排行榜筛选">
      <div class="leaderboard__tab-group">
        <button
          v-for="opt in modeOptions"
          :key="opt.value"
          type="button"
          role="tab"
          class="leaderboard__tab"
          :class="{ 'is-active': mode === opt.value }"
          :aria-selected="mode === opt.value"
          @click="onModeChange(opt.value)"
        >
          <span class="leaderboard__tab-eyebrow cis-mono">{{ opt.eyebrow }}</span>
          <span class="leaderboard__tab-label">{{ opt.label }}</span>
        </button>
      </div>
      <div class="leaderboard__tab-sep" aria-hidden="true"></div>
      <div class="leaderboard__tab-group">
        <button
          v-for="opt in timeOptions"
          :key="opt.value"
          type="button"
          role="tab"
          class="leaderboard__tab leaderboard__tab--time"
          :class="{ 'is-active': timeRange === opt.value }"
          :aria-selected="timeRange === opt.value"
          @click="onTimeRangeChange(opt.value)"
        >
          <span class="leaderboard__tab-eyebrow cis-mono">{{ opt.eyebrow }}</span>
          <span class="leaderboard__tab-label">{{ opt.label }}</span>
        </button>
      </div>
    </div>

    <!-- 内容 -->
    <el-skeleton v-if="loading" :rows="5" animated />
    <template v-else>
      <!-- 前三名：领奖台 -->
      <div v-if="topThree.length > 0" class="leaderboard__podium" role="list" aria-label="前三名">
        <div
          v-for="entry in topThree"
          :key="entry.rank"
          class="leaderboard__podium-cell"
          :class="`leaderboard__podium-cell--${entry.rank}`"
          role="listitem"
        >
          <span class="cis-eyebrow">No. {{ entry.rank }}</span>
          <div class="leaderboard__podium-avatar" :aria-label="`第 ${entry.rank} 名 ${entry.name}`">
            <span class="leaderboard__podium-initial">{{ nameInitial(entry.name) }}</span>
          </div>
          <span class="leaderboard__podium-name">{{ entry.name }}</span>
          <span class="leaderboard__podium-score cis-num" :class="entry.score >= 0 ? 'is-plus' : 'is-minus'">
            {{ formatScore(entry.score) }}
          </span>
        </div>
      </div>

      <!-- 列表 -->
      <ol v-if="restEntries.length > 0" class="leaderboard__list" aria-label="其余排名">
        <li
          v-for="entry in restEntries"
          :key="entry.rank"
          class="leaderboard__row"
        >
          <span class="leaderboard__row-rank" :aria-label="`第 ${entry.rank} 名`">
            <span class="leaderboard__row-rank-num cis-mono">{{ entry.rank }}</span>
          </span>
          <span class="leaderboard__row-name">{{ entry.name }}</span>
          <span
            class="leaderboard__row-score cis-num"
            :class="entry.score >= 0 ? 'is-plus' : 'is-minus'"
          >
            {{ formatScore(entry.score) }}
          </span>
        </li>
      </ol>
      <div v-else-if="entries.length === 0" class="leaderboard__empty">
        <span class="cis-eyebrow">Empty</span>
        <p class="leaderboard__empty-text">该时段暂无数据</p>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import type { LeaderboardEntry } from '@/types'
import { invoke } from '@/services/tauri'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'
import { exportToExcel, type ExcelColumn } from '@/utils/excelHelper'

type Mode = 'personal' | 'group'
type TimeRange = 'today' | 'week' | 'month' | 'all'

const mode = ref<Mode>('personal')
const timeRange = ref<TimeRange>('all')
const entries = ref<LeaderboardEntry[]>([])
const loading = ref(true)
const updatedAt = ref('--:--')

const topThree = computed(() => entries.value.slice(0, 3))
const restEntries = computed(() => entries.value.slice(3))

const modeOptions: { value: Mode; label: string; eyebrow: string }[] = [
  { value: 'personal', label: '个人', eyebrow: '01' },
  { value: 'group', label: '小组', eyebrow: '02' },
]

const timeOptions: { value: TimeRange; label: string; eyebrow: string }[] = [
  { value: 'today', label: '今日', eyebrow: 'Day' },
  { value: 'week', label: '本周', eyebrow: 'Week' },
  { value: 'month', label: '本月', eyebrow: 'Month' },
  { value: 'all', label: '全部', eyebrow: 'All' },
]

let refreshTimer: ReturnType<typeof setInterval> | null = null

function nameInitial(name: string) {
  return name?.slice(0, 1) ?? '?'
}

function formatScore(val: number) {
  if (val === 0) return '0'
  return val > 0 ? `+${val}` : `${val}`
}

function setUpdatedAt() {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  updatedAt.value = `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

onMounted(async () => {
  await fetchLeaderboard()
  setUpdatedAt()
  loading.value = false
  connectWebSocket({
    onScoreUpdate: () => fetchLeaderboard(),
  })
  refreshTimer = setInterval(() => {
    fetchLeaderboard()
    setUpdatedAt()
  }, 30000)
})

onUnmounted(() => {
  disconnectWebSocket()
  if (refreshTimer) clearInterval(refreshTimer)
})

function getTimeRangeParams(): { startTime?: string; endTime?: string } {
  const now = new Date()
  let startTime: Date | undefined

  switch (timeRange.value) {
    case 'today': {
      startTime = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0)
      break
    }
    case 'week': {
      const day = now.getDay() || 7
      startTime = new Date(now.getFullYear(), now.getMonth(), now.getDate() - day + 1, 0, 0, 0)
      break
    }
    case 'month': {
      startTime = new Date(now.getFullYear(), now.getMonth(), 1, 0, 0, 0)
      break
    }
    case 'all':
    default:
      return {}
  }

  return {
    startTime: startTime!.toISOString().slice(0, 19),
    endTime: now.toISOString().slice(0, 19),
  }
}

async function fetchLeaderboard() {
  try {
    const { startTime, endTime } = getTimeRangeParams()
    const rangeArgs = {
      startTime: startTime ?? null,
      endTime: endTime ?? null,
    }

    if (mode.value === 'personal') {
      // IPC：leaderboard_query，按时间窗口聚合
      const data = await invoke<Array<{ student: { name: string; total_score: number } }>>(
        'leaderboard_query',
        rangeArgs
      )
      entries.value = data.map((item, index) => ({
        rank: index + 1,
        name: item.student.name,
        score: item.student.total_score ?? 0,
        isGroup: false,
      }))
    } else {
      // IPC：leaderboard_all_groups，按时间窗口聚合
      const groups = await invoke<Array<{ group_name: string; total_score: number }>>(
        'leaderboard_all_groups',
        rangeArgs
      )
      entries.value = groups.map((g, index) => ({
        rank: index + 1,
        name: g.group_name,
        score: g.total_score,
        isGroup: true,
      }))
    }
    setUpdatedAt()
  } catch {
    entries.value = []
  }
}

function onModeChange(value: Mode) {
  mode.value = value
  fetchLeaderboard()
}

function onTimeRangeChange(value: TimeRange) {
  timeRange.value = value
  fetchLeaderboard()
}

const dateFilenameFormatter = new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit' })

function handleExport() {
  if (entries.value.length === 0) {
    ElMessage.warning('暂无数据可导出')
    return
  }

  const columns: ExcelColumn[] = [
    { header: '排名', key: 'rank' },
    { header: '名称', key: 'name' },
    { header: '积分', key: 'score' },
  ]

  const data = entries.value.map(entry => ({
    rank: entry.rank,
    name: entry.name,
    score: entry.score,
  }))

  const modeLabel = mode.value === 'personal' ? '个人' : '小组'
  const rangeLabel = { today: '今日', week: '本周', month: '本月', all: '全部' }[timeRange.value]
  const filename = `排行榜_${modeLabel}_${rangeLabel}_${dateFilenameFormatter.format(new Date())}`

  exportToExcel(data, columns, filename)
  ElMessage.success('导出成功')
}
</script>

<style scoped>
.leaderboard {
  max-width: 920px;
}

/* ===== 顶部标题 ===== */
.leaderboard__head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
}

.leaderboard__head-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.leaderboard__title {
  font-size: 28px;
  margin: 0;
  font-weight: 600;
}

.leaderboard__sub {
  margin: 4px 0 0;
  font-size: 12px;
  color: var(--cis-text-tertiary);
  letter-spacing: 0.1px;
}

.leaderboard__head-actions {
  display: flex;
  gap: 4px;
  flex-shrink: 0;
}

.leaderboard__icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-btn);
  background: var(--cis-surface-1);
  color: var(--cis-text-secondary);
  cursor: pointer;
  transition: border-color var(--cis-transition-fast), color var(--cis-transition-fast);
}

.leaderboard__icon-btn:hover {
  border-color: var(--cis-primary);
  color: var(--cis-primary);
}

/* ===== Tabs：Linear 风 underline ===== */
.leaderboard__tabs {
  display: flex;
  align-items: stretch;
  gap: 24px;
  border-bottom: 1px solid var(--cis-border);
  margin-bottom: 24px;
}

.leaderboard__tab-group {
  display: flex;
  align-items: stretch;
  gap: 0;
}

.leaderboard__tab {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 1px;
  padding: 8px 14px 10px;
  background: transparent;
  border: none;
  color: var(--cis-text-tertiary);
  cursor: pointer;
  font-family: inherit;
  transition: color var(--cis-transition-fast);
  text-align: left;
}

.leaderboard__tab:hover:not(.is-active) {
  color: var(--cis-text-primary);
}

.leaderboard__tab.is-active {
  color: var(--cis-primary);
}

/* 底部 2px underline：active 时显示 */
.leaderboard__tab::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  bottom: -1px;
  height: 2px;
  background: transparent;
  transition: background-color var(--cis-transition-fast);
}

.leaderboard__tab.is-active::after {
  background: var(--cis-primary);
}

.leaderboard__tab-eyebrow {
  font-size: 9px;
  font-weight: 600;
  letter-spacing: 0.6px;
  text-transform: uppercase;
  line-height: 1;
  opacity: 0.7;
}

.leaderboard__tab-label {
  font-size: 13px;
  font-weight: 600;
  line-height: 1.2;
  margin-top: 1px;
}

.leaderboard__tab-sep {
  width: 1px;
  background: var(--cis-border-light);
  align-self: stretch;
  margin: 6px 0;
}

/* ===== 领奖台 ===== */
.leaderboard__podium {
  display: grid;
  grid-template-columns: 1fr 1.2fr 1fr;
  gap: 0;
  align-items: end;
  margin-bottom: 32px;
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-card);
  background: var(--cis-surface-1);
  padding: 24px 16px 20px;
}

.leaderboard__podium-cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  position: relative;
}

.leaderboard__podium-cell--1 {
  order: 2;
}

.leaderboard__podium-cell--2 {
  order: 1;
}

.leaderboard__podium-cell--3 {
  order: 3;
}

.leaderboard__podium-avatar {
  width: 64px;
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--cis-border);
  background: var(--cis-surface-2);
  border-radius: 9999px;
  position: relative;
}

.leaderboard__podium-cell--1 .leaderboard__podium-avatar {
  width: 80px;
  height: 80px;
  background: var(--cis-primary);
  border-color: var(--cis-primary);
}

.leaderboard__podium-cell--1 .leaderboard__podium-initial {
  color: #fff;
}

.leaderboard__podium-cell--2 .leaderboard__podium-avatar {
  background: var(--cis-primary-tint);
  border-color: var(--cis-primary-tint-2);
}

.leaderboard__podium-cell--3 .leaderboard__podium-avatar {
  background: var(--cis-surface-2);
  border-color: var(--cis-border);
}

.leaderboard__podium-initial {
  font-family: var(--cis-font-serif);
  font-size: 26px;
  font-weight: 600;
  color: var(--cis-text-primary);
  line-height: 1;
}

.leaderboard__podium-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--cis-text-primary);
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: center;
}

.leaderboard__podium-score {
  font-size: 22px;
  font-weight: 700;
  font-family: var(--cis-font-mono);
  font-variant-numeric: tabular-nums;
  line-height: 1;
}

.leaderboard__podium-cell--1 .leaderboard__podium-score {
  font-size: 28px;
}

/* ===== 列表 ===== */
.leaderboard__list {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-card);
  background: var(--cis-surface-1);
  overflow: hidden;
}

.leaderboard__row {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 0 20px;
  min-height: 44px;
  border-bottom: 1px solid var(--cis-border-light);
  transition: background-color var(--cis-transition-fast);
}

.leaderboard__row:last-child {
  border-bottom: none;
}

.leaderboard__row:hover {
  background: var(--cis-primary-tint);
}

/* 左侧 4px 排名条 */
.leaderboard__row::before {
  content: '';
  width: 4px;
  height: 16px;
  background: var(--cis-text-tertiary);
  border-radius: 2px;
  flex-shrink: 0;
}

.leaderboard__row-rank {
  display: flex;
  align-items: center;
  min-width: 48px;
}

.leaderboard__row-rank-num {
  font-size: 14px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
}

.leaderboard__row-name {
  flex: 1;
  font-size: 14px;
  color: var(--cis-text-primary);
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.leaderboard__row-score {
  font-size: 16px;
  font-weight: 700;
  min-width: 64px;
  text-align: right;
  font-variant-numeric: tabular-nums;
}

/* 分数正负染色（青绿 / 砖红） */
.leaderboard__podium-score.is-plus,
.leaderboard__row-score.is-plus {
  color: #15803D; /* 苔绿 */
}

.leaderboard__podium-score.is-minus,
.leaderboard__row-score.is-minus {
  color: #991B1B; /* 砖红 */
}

/* ===== Empty ===== */
.leaderboard__empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 64px 16px;
  border: 1px dashed var(--cis-border-strong);
  border-radius: var(--cis-radius-card);
  background: var(--cis-surface-1);
  text-align: center;
}

.leaderboard__empty-text {
  margin: 0;
  color: var(--cis-text-tertiary);
  font-size: 14px;
}

@media (max-width: 720px) {
  .leaderboard__tabs {
    flex-direction: column;
    gap: 4px;
  }
  .leaderboard__tab-sep {
    display: none;
  }
  .leaderboard__podium {
    grid-template-columns: 1fr;
    gap: 16px;
  }
  .leaderboard__podium-cell { order: unset !important; }
}

@media (prefers-reduced-motion: reduce) {
  .leaderboard__row,
  .leaderboard__tab::after,
  .leaderboard__icon-btn {
    transition: none;
  }
}
</style>
