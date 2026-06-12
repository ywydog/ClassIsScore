<template>
  <div class="floating-bar" @mousedown="startDrag">
    <div class="floating-bar__content">
      <span class="floating-bar__label">积分</span>
      <div class="floating-bar__items">
        <div v-for="entry in topStudents" :key="entry.name" class="floating-bar__item">
          <span class="floating-bar__name">{{ entry.name }}</span>
          <span class="floating-bar__score">{{ entry.score }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const topStudents = ref<LeaderboardEntry[]>([])

onMounted(async () => {
  await fetchTopStudents()
  connectWebSocket({
    onScoreUpdate: async () => {
      await fetchTopStudents()
    },
  })
})

onUnmounted(() => {
  disconnectWebSocket()
})

async function fetchTopStudents() {
  try {
    const response = await api.get<{ data: LeaderboardEntry[] }>('/api/leaderboard/personal', {
      params: { limit: 5 },
    })
    topStudents.value = response.data.data.slice(0, 5)
  } catch {
    // 静默处理
  }
}

function startDrag(event: MouseEvent) {
  // 通过 Electron 的 -webkit-app-region 实现拖拽
  // 此处为备用方案
  void event
}
</script>

<style scoped>
.floating-bar {
  width: 100%;
  height: 100%;
  background: rgba(30, 30, 30, 0.85);
  border-radius: 12px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  -webkit-app-region: drag;
  user-select: none;
  backdrop-filter: blur(10px);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.3);
}

.floating-bar__content {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}

.floating-bar__label {
  font-size: 13px;
  font-weight: 600;
  color: var(--cis-primary);
  white-space: nowrap;
}

.floating-bar__items {
  display: flex;
  gap: 16px;
  overflow: hidden;
}

.floating-bar__item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #fff;
}

.floating-bar__name {
  font-weight: 500;
}

.floating-bar__score {
  font-weight: 700;
  color: var(--cis-primary);
}
</style>
