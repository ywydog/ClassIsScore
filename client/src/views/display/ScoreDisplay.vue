<template>
  <div class="score-display" :class="{ 'score-display--dark': isDark }">
    <div class="score-display__bg">
      <div class="score-display__particle" v-for="i in 6" :key="i" :style="particleStyle(i)"></div>
    </div>
    <div class="score-display__content">
      <div class="score-display__header">
        <h1 class="score-display__title">积分排行榜</h1>
        <div class="score-display__time">{{ currentTime }}</div>
        <div class="score-display__mode-toggle">
          <el-radio-group v-model="mode" size="small" @change="fetchLeaderboard">
            <el-radio-button value="personal">个人</el-radio-button>
            <el-radio-button value="group">小组</el-radio-button>
          </el-radio-group>
        </div>
      </div>

      <!-- 领奖台 -->
      <div v-if="topThree.length > 0" class="score-display__podium">
        <div class="score-display__podium-item score-display__podium--2" v-if="topThree.length >= 2">
          <div class="score-display__podium-avatar">
            <el-avatar :size="72">{{ topThree[1].name.charAt(0) }}</el-avatar>
            <div class="score-display__medal score-display__medal--silver">2</div>
          </div>
          <div class="score-display__podium-name">{{ topThree[1].name }}</div>
          <div class="score-display__podium-score">{{ topThree[1].score }}</div>
        </div>
        <div class="score-display__podium-item score-display__podium--1" v-if="topThree.length >= 1">
          <div class="score-display__podium-avatar">
            <el-avatar :size="88">{{ topThree[0].name.charAt(0) }}</el-avatar>
            <div class="score-display__medal score-display__medal--gold">1</div>
          </div>
          <div class="score-display__podium-name">{{ topThree[0].name }}</div>
          <div class="score-display__podium-score">{{ topThree[0].score }}</div>
        </div>
        <div class="score-display__podium-item score-display__podium--3" v-if="topThree.length >= 3">
          <div class="score-display__podium-avatar">
            <el-avatar :size="60">{{ topThree[2].name.charAt(0) }}</el-avatar>
            <div class="score-display__medal score-display__medal--bronze">3</div>
          </div>
          <div class="score-display__podium-name">{{ topThree[2].name }}</div>
          <div class="score-display__podium-score">{{ topThree[2].score }}</div>
        </div>
      </div>

      <!-- 其余排名列表 -->
      <div class="score-display__list" v-if="restEntries.length > 0">
        <div v-for="entry in restEntries" :key="entry.rank" class="score-display__item">
          <span class="score-display__rank">{{ entry.rank }}</span>
          <span class="score-display__name">{{ entry.name }}</span>
          <span class="score-display__score">{{ entry.score }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const leaderboard = ref<LeaderboardEntry[]>([])
const mode = ref<'personal' | 'group'>('personal')
const currentTime = ref('')
const isDark = ref(true)

let timeTimer: ReturnType<typeof setInterval> | null = null
let refreshTimer: ReturnType<typeof setInterval> | null = null

const topThree = computed(() => leaderboard.value.slice(0, 3))
const restEntries = computed(() => leaderboard.value.slice(3))

function updateTime() {
  currentTime.value = new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function particleStyle(i: number) {
  const size = 100 + i * 60
  const x = (i * 17) % 100
  const y = (i * 23) % 100
  const delay = i * 2
  return {
    width: `${size}px`,
    height: `${size}px`,
    left: `${x}%`,
    top: `${y}%`,
    animationDelay: `${delay}s`,
  }
}

onMounted(async () => {
  updateTime()
  timeTimer = setInterval(updateTime, 1000)
  await fetchLeaderboard()
  connectWebSocket({
    onScoreUpdate: () => fetchLeaderboard(),
  })
  refreshTimer = setInterval(fetchLeaderboard, 30000)
})

onUnmounted(() => {
  disconnectWebSocket()
  if (timeTimer) clearInterval(timeTimer)
  if (refreshTimer) clearInterval(refreshTimer)
})

async function fetchLeaderboard() {
  try {
    const endpoint = mode.value === 'personal' ? '/api/leaderboard/personal' : '/api/leaderboard/group'
    const response = await api.get<{ data: LeaderboardEntry[] }>(endpoint)
    leaderboard.value = response.data.data
  } catch {
    // silent
  }
}
</script>

<style scoped>
.score-display {
  width: 100vw;
  height: 100vh;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
  color: #fff;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

.score-display--dark {
  background: linear-gradient(135deg, #0a0a1a 0%, #0d1b2a 50%, #1b2838 100%);
}

/* 背景粒子 */
.score-display__bg {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.score-display__particle {
  position: absolute;
  border-radius: 50%;
  background: rgba(64, 158, 255, 0.06);
  animation: float 20s infinite ease-in-out;
}

@keyframes float {
  0%, 100% { transform: translateY(0) scale(1); opacity: 0.3; }
  50% { transform: translateY(-40px) scale(1.1); opacity: 0.6; }
}

.score-display__content {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  padding: 40px 60px;
  height: 100%;
}

.score-display__header {
  display: flex;
  align-items: center;
  gap: 24px;
  margin-bottom: 40px;
}

.score-display__title {
  font-size: 36px;
  font-weight: 700;
  margin: 0;
  background: linear-gradient(135deg, #409eff, #79bbff);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.score-display__time {
  font-size: 24px;
  font-weight: 300;
  color: rgba(255, 255, 255, 0.6);
  font-variant-numeric: tabular-nums;
}

.score-display__mode-toggle {
  margin-left: auto;
}

.score-display__mode-toggle :deep(.el-radio-button__inner) {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.2);
  color: rgba(255, 255, 255, 0.7);
}

.score-display__mode-toggle :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
  background: var(--cis-primary);
  border-color: var(--cis-primary);
  color: #fff;
}

/* 领奖台 */
.score-display__podium {
  display: flex;
  justify-content: center;
  align-items: flex-end;
  gap: 32px;
  margin-bottom: 40px;
}

.score-display__podium-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.score-display__podium--1 { order: 2; }
.score-display__podium--2 { order: 1; }
.score-display__podium--3 { order: 3; }

.score-display__podium-avatar {
  position: relative;
}

.score-display__podium-avatar :deep(.el-avatar) {
  background: rgba(255, 255, 255, 0.15);
  color: #fff;
  font-size: 28px;
  font-weight: 700;
  border: 3px solid rgba(255, 255, 255, 0.3);
}

.score-display__medal {
  position: absolute;
  bottom: -4px;
  right: -4px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 700;
  color: #fff;
}

.score-display__medal--gold { background: linear-gradient(135deg, #ffd700, #ffb700); box-shadow: 0 0 12px rgba(255, 215, 0, 0.5); }
.score-display__medal--silver { background: linear-gradient(135deg, #c0c0c0, #a0a0a0); box-shadow: 0 0 8px rgba(192, 192, 192, 0.4); }
.score-display__medal--bronze { background: linear-gradient(135deg, #cd7f32, #b06820); box-shadow: 0 0 8px rgba(205, 127, 50, 0.4); }

.score-display__podium-name {
  font-size: 20px;
  font-weight: 600;
}

.score-display__podium-score {
  font-size: 32px;
  font-weight: 700;
  color: #409eff;
}

/* 列表 */
.score-display__list {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 12px;
  align-content: start;
  overflow-y: auto;
}

.score-display__item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 14px 20px;
  background: rgba(255, 255, 255, 0.08);
  border-radius: 12px;
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  transition: background 0.2s;
}

.score-display__item:hover {
  background: rgba(255, 255, 255, 0.12);
}

.score-display__rank {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-weight: 700;
  font-size: 15px;
  background: rgba(255, 255, 255, 0.12);
}

.score-display__name {
  flex: 1;
  font-size: 16px;
  font-weight: 500;
}

.score-display__score {
  font-size: 22px;
  font-weight: 700;
  color: #409eff;
}
</style>
