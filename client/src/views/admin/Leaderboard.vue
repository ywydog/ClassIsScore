<template>
  <div class="leaderboard">
    <h2 id="leaderboard-title" class="leaderboard__header">排行榜</h2>
    <div class="leaderboard__toolbar">
      <el-radio-group v-model="mode" size="small" @change="fetchLeaderboard" aria-label="排行榜类型">
        <el-radio-button value="personal">个人</el-radio-button>
        <el-radio-button value="group">小组</el-radio-button>
      </el-radio-group>
      <el-radio-group v-model="timeRange" size="small" @change="handleTimeRangeChange" aria-label="时间范围">
        <el-radio-button value="today">今日</el-radio-button>
        <el-radio-button value="week">本周</el-radio-button>
        <el-radio-button value="month">本月</el-radio-button>
        <el-radio-button value="all">全部</el-radio-button>
      </el-radio-group>
      <el-button size="small" text :icon="Refresh" aria-label="刷新排行榜" @click="fetchLeaderboard" />
      <el-button size="small" type="primary" :icon="Download" @click="handleExport">
        导出
      </el-button>
    </div>

    <!-- 前三名展示 -->
    <el-skeleton v-if="loading" :loading="loading" :rows="5" animated />
    <template v-else>
    <div v-if="topThree.length > 0" class="leaderboard__podium" role="list" aria-label="前三名">
      <div class="leaderboard__podium-item leaderboard__podium--2" v-if="topThree.length >= 2" role="listitem">
        <div class="leaderboard__podium-avatar">
          <el-avatar :size="48" :aria-label="`第二名 ${topThree[1].name}`">{{ topThree[1].name.charAt(0) }}</el-avatar>
          <span class="leaderboard__medal leaderboard__medal--silver" aria-hidden="true">2</span>
        </div>
        <span class="leaderboard__podium-name">{{ topThree[1].name }}</span>
        <span class="leaderboard__podium-score" style="font-variant-numeric: tabular-nums">{{ topThree[1].score }}</span>
      </div>
      <div class="leaderboard__podium-item leaderboard__podium--1" v-if="topThree.length >= 1" role="listitem">
        <div class="leaderboard__podium-avatar">
          <el-avatar :size="56" :aria-label="`第一名 ${topThree[0].name}`">{{ topThree[0].name.charAt(0) }}</el-avatar>
          <span class="leaderboard__medal leaderboard__medal--gold" aria-hidden="true">1</span>
        </div>
        <span class="leaderboard__podium-name">{{ topThree[0].name }}</span>
        <span class="leaderboard__podium-score" style="font-variant-numeric: tabular-nums">{{ topThree[0].score }}</span>
      </div>
      <div class="leaderboard__podium-item leaderboard__podium--3" v-if="topThree.length >= 3" role="listitem">
        <div class="leaderboard__podium-avatar">
          <el-avatar :size="44" :aria-label="`第三名 ${topThree[2].name}`">{{ topThree[2].name.charAt(0) }}</el-avatar>
          <span class="leaderboard__medal leaderboard__medal--bronze" aria-hidden="true">3</span>
        </div>
        <span class="leaderboard__podium-name">{{ topThree[2].name }}</span>
        <span class="leaderboard__podium-score" style="font-variant-numeric: tabular-nums">{{ topThree[2].score }}</span>
      </div>
    </div>

    <!-- 其余排名 -->
    <ol class="leaderboard__list" aria-label="其余排名">
      <li
        v-for="entry in restEntries"
        :key="entry.rank"
        class="leaderboard__item"
      >
        <span class="leaderboard__rank" style="font-variant-numeric: tabular-nums" :aria-label="`第 ${entry.rank} 名`">{{ entry.rank }}</span>
        <span class="leaderboard__name">{{ entry.name }}</span>
        <span class="leaderboard__score" style="font-variant-numeric: tabular-nums">{{ entry.score }}</span>
      </li>
      <li v-if="entries.length === 0">
        <el-empty description="暂无排行数据" />
      </li>
    </ol>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { Refresh, Download } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'
import { exportToExcel, type ExcelColumn } from '@/utils/excelHelper'

const mode = ref<'personal' | 'group'>('personal')
const timeRange = ref<'today' | 'week' | 'month' | 'all'>('all')
const entries = ref<LeaderboardEntry[]>([])
const loading = ref(true)
const topThree = computed(() => entries.value.slice(0, 3))
const restEntries = computed(() => entries.value.slice(3))

let refreshTimer: ReturnType<typeof setInterval> | null = null

onMounted(async () => {
  await fetchLeaderboard()
  loading.value = false
  connectWebSocket({
    onScoreUpdate: () => fetchLeaderboard(),
  })
  refreshTimer = setInterval(fetchLeaderboard, 30000)
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

function handleTimeRangeChange() {
  fetchLeaderboard()
}

async function fetchLeaderboard() {
  try {
    const endpoint = mode.value === 'personal' ? '/api/leaderboard/personal' : '/api/leaderboard/group'
    const params = getTimeRangeParams()
    const response = await api.get<{ data: LeaderboardEntry[] }>(endpoint, { params })
    const data = response.data.data
    // 为每条数据添加 rank
    entries.value = data.map((item: any, index: number) => ({
      rank: index + 1,
      name: item.name,
      score: item.score ?? item.totalScore ?? 0,
      isGroup: mode.value === 'group',
    }))
  } catch {
    // silent
  }
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
.leaderboard__header {
  margin: 0 0 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
  font-family: var(--cis-font-family-display);
  font-size: 22px;
  color: var(--cis-text-primary);
  padding-left: 12px;
  border-left: 3px solid var(--cis-primary);
  background: linear-gradient(135deg, var(--cis-primary), var(--cis-primary-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  scroll-margin-top: 80px;
}

.leaderboard__toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 16px;
}

/* 领奖台 */
.leaderboard__podium {
  display: flex;
  justify-content: center;
  align-items: flex-end;
  gap: 16px;
  margin-bottom: 32px;
  padding: 24px 0;
}

.leaderboard__podium-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.leaderboard__podium--1 {
  order: 2;
}

.leaderboard__podium--2 {
  order: 1;
}

.leaderboard__podium--3 {
  order: 3;
}

.leaderboard__podium-avatar {
  position: relative;
}

.leaderboard__medal {
  position: absolute;
  bottom: -4px;
  right: -4px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
  color: #fff;
}

.leaderboard__medal--gold {
  background: linear-gradient(135deg, #ffd700, #ffb800);
  box-shadow: 0 2px 8px rgba(255, 215, 0, 0.4);
}

.leaderboard__medal--silver {
  background: linear-gradient(135deg, #c0c0c0, #a8a8a8);
  box-shadow: 0 2px 8px rgba(192, 192, 192, 0.4);
}

.leaderboard__medal--bronze {
  background: linear-gradient(135deg, #cd7f32, #b8722e);
  box-shadow: 0 2px 8px rgba(205, 127, 50, 0.4);
}

.leaderboard__podium-name {
  font-weight: 600;
  font-size: 14px;
  color: var(--cis-text-primary);
}

.leaderboard__podium-score {
  font-weight: 700;
  font-size: 18px;
  background: var(--cis-gradient-primary);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

/* 列表 */
.leaderboard__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  list-style: none;
  margin: 0;
  padding: 0;
}

.leaderboard__item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px 16px;
  background-color: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  border: 1px solid var(--cis-border-color-light);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast), transform var(--cis-transition-fast);
}

.leaderboard__item:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-1px);
}

.leaderboard__rank {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--cis-radius-full);
  font-weight: 700;
  font-size: 14px;
  background-color: var(--cis-bg-secondary);
  color: var(--cis-text-secondary);
}

.leaderboard__name {
  flex: 1;
  font-weight: 500;
  color: var(--cis-text-primary);
}

.leaderboard__score {
  font-weight: 700;
  font-size: 16px;
  background: var(--cis-gradient-primary);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}
</style>
