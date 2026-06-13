<template>
  <div class="score-display">
    <div class="score-display__bg">
      <div class="score-display__orb" v-for="i in 5" :key="i" :style="orbStyle(i)"></div>
    </div>
    <div class="score-display__content">
      <div class="score-display__header">
        <div class="score-display__brand">
          <div class="score-display__brand-icon">
            <el-icon :size="20"><Trophy /></el-icon>
          </div>
          <h1 class="score-display__title">积分排行榜</h1>
        </div>
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
          <div class="score-display__podium-crown">👑</div>
          <div class="score-display__podium-avatar">
            <el-avatar :size="96">{{ topThree[0].name.charAt(0) }}</el-avatar>
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
import { Trophy } from '@element-plus/icons-vue'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const leaderboard = ref<LeaderboardEntry[]>([])
const mode = ref<'personal' | 'group'>('personal')
const currentTime = ref('')

let timeTimer: ReturnType<typeof setInterval> | null = null
let refreshTimer: ReturnType<typeof setInterval> | null = null

const topThree = computed(() => leaderboard.value.slice(0, 3))
const restEntries = computed(() => leaderboard.value.slice(3))

function updateTime() {
  currentTime.value = new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function orbStyle(i: number) {
  const size = 120 + i * 80
  const x = (i * 19 + 10) % 90
  const y = (i * 13 + 5) % 80
  const delay = i * 3
  return {
    width: `${size}px`,
    height: `${size}px`,
    left: `${x}%`,
    top: `${y}%`,
    animationDelay: `${delay}s`,
    animationDuration: `${18 + i * 2}s`,
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
  background: linear-gradient(160deg, #0a1628 0%, #0d2137 40%, #0a2a3c 70%, #061a2e 100%);
  color: #fff;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

/* 背景光球 */
.score-display__bg {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.score-display__orb {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(13, 148, 136, 0.12) 0%, transparent 70%);
  animation: orb-float 20s infinite ease-in-out;
  filter: blur(2px);
}

@keyframes orb-float {
  0%, 100% { transform: translate(0, 0) scale(1); opacity: 0.4; }
  33% { transform: translate(20px, -30px) scale(1.1); opacity: 0.6; }
  66% { transform: translate(-15px, 20px) scale(0.95); opacity: 0.5; }
}

.score-display__content {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  padding: 32px 48px;
  height: 100%;
}

.score-display__header {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 36px;
}

.score-display__brand {
  display: flex;
  align-items: center;
  gap: 12px;
}

.score-display__brand-icon {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--cis-gradient-primary, linear-gradient(135deg, #0d9488, #14b8a6));
  border-radius: var(--cis-radius-md, 8px);
  color: #fff;
}

.score-display__title {
  font-family: var(--cis-font-family-display, serif);
  font-size: 28px;
  font-weight: 700;
  margin: 0;
  background: linear-gradient(135deg, #2dd4bf, #5eead4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.score-display__time {
  font-size: 20px;
  font-weight: 300;
  color: rgba(255, 255, 255, 0.45);
  font-variant-numeric: tabular-nums;
  letter-spacing: 2px;
}

.score-display__mode-toggle {
  margin-left: auto;
}

.score-display__mode-toggle :deep(.el-radio-button__inner) {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.12);
  color: rgba(255, 255, 255, 0.5);
  font-size: 12px;
  padding: 6px 16px;
}

.score-display__mode-toggle :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  border-color: #0d9488;
  color: #fff;
  box-shadow: 0 0 16px rgba(13, 148, 136, 0.3);
}

/* 领奖台 */
.score-display__podium {
  display: flex;
  justify-content: center;
  align-items: flex-end;
  gap: 40px;
  margin-bottom: 36px;
  padding: 20px 0;
}

.score-display__podium-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.score-display__podium--1 { order: 2; }
.score-display__podium--2 { order: 1; }
.score-display__podium--3 { order: 3; }

.score-display__podium-crown {
  font-size: 28px;
  animation: crown-bounce 2s infinite ease-in-out;
}

@keyframes crown-bounce {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-4px); }
}

.score-display__podium-avatar {
  position: relative;
}

.score-display__podium-avatar :deep(.el-avatar) {
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
  font-size: 28px;
  font-weight: 700;
  border: 3px solid rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.score-display__medal {
  position: absolute;
  bottom: -2px;
  right: -2px;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 700;
  color: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}

.score-display__medal--gold {
  background: linear-gradient(135deg, #ffd700, #ffb700);
  box-shadow: 0 0 16px rgba(255, 215, 0, 0.4);
}
.score-display__medal--silver {
  background: linear-gradient(135deg, #e8e8e8, #b0b0b0);
  box-shadow: 0 0 12px rgba(192, 192, 192, 0.3);
}
.score-display__medal--bronze {
  background: linear-gradient(135deg, #cd7f32, #a0622a);
  box-shadow: 0 0 12px rgba(205, 127, 50, 0.3);
}

.score-display__podium-name {
  font-size: 18px;
  font-weight: 600;
  letter-spacing: 0.5px;
}

.score-display__podium-score {
  font-size: 36px;
  font-weight: 700;
  background: linear-gradient(135deg, #2dd4bf, #5eead4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

/* 列表 */
.score-display__list {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 10px;
  align-content: start;
  overflow-y: auto;
}

.score-display__item {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px 18px;
  background: rgba(255, 255, 255, 0.04);
  border-radius: var(--cis-radius-lg, 12px);
  border: 1px solid rgba(255, 255, 255, 0.06);
  transition: all var(--cis-transition-fast, 0.12s ease);
}

.score-display__item:hover {
  background: rgba(255, 255, 255, 0.08);
  border-color: rgba(13, 148, 136, 0.2);
}

.score-display__rank {
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-weight: 700;
  font-size: 13px;
  background: rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.5);
}

.score-display__name {
  flex: 1;
  font-size: 15px;
  font-weight: 500;
}

.score-display__score {
  font-size: 20px;
  font-weight: 700;
  background: linear-gradient(135deg, #2dd4bf, #5eead4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}
</style>
