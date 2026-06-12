<template>
  <div class="score-display">
    <div class="score-display__header">
      <h1 class="score-display__title">积分排行</h1>
    </div>
    <div class="score-display__content">
      <div
        v-for="entry in leaderboard"
        :key="entry.rank"
        class="score-display__item"
        :class="{ 'score-display__item--top': entry.rank <= 3 }"
      >
        <span class="score-display__rank">{{ entry.rank }}</span>
        <span class="score-display__name">{{ entry.name }}</span>
        <span class="score-display__score">{{ entry.score }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const leaderboard = ref<LeaderboardEntry[]>([])

onMounted(async () => {
  await fetchLeaderboard()
  connectWebSocket({
    onScoreUpdate: async () => {
      await fetchLeaderboard()
    },
  })
})

onUnmounted(() => {
  disconnectWebSocket()
})

async function fetchLeaderboard() {
  try {
    const response = await api.get<{ data: LeaderboardEntry[] }>('/api/leaderboard/personal')
    leaderboard.value = response.data.data
  } catch {
    // 静默处理
  }
}
</script>

<style scoped>
.score-display {
  width: 100vw;
  height: 100vh;
  background: linear-gradient(135deg, var(--cis-primary-light-3), var(--cis-primary));
  color: #fff;
  display: flex;
  flex-direction: column;
  padding: 40px;
  overflow: hidden;
}

.score-display__header {
  text-align: center;
  margin-bottom: 40px;
}

.score-display__title {
  font-size: 36px;
  font-weight: 700;
  margin: 0;
}

.score-display__content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow-y: auto;
}

.score-display__item {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 16px 24px;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 12px;
  backdrop-filter: blur(10px);
}

.score-display__item--top {
  background: rgba(255, 255, 255, 0.25);
}

.score-display__rank {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-weight: 700;
  font-size: 20px;
  background: rgba(255, 255, 255, 0.2);
}

.score-display__name {
  flex: 1;
  font-size: 24px;
  font-weight: 500;
}

.score-display__score {
  font-size: 28px;
  font-weight: 700;
}
</style>
