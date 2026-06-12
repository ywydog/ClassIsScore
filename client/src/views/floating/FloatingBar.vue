<template>
  <div class="floating-bar" @dblclick="openMainWindow">
    <div class="floating-bar__content">
      <div class="floating-bar__brand">
        <el-icon :size="14" color="#409eff"><Trophy /></el-icon>
      </div>
      <div class="floating-bar__items">
        <div v-for="(entry, index) in topStudents" :key="entry.name" class="floating-bar__item">
          <span class="floating-bar__rank" :class="`floating-bar__rank--${index + 1}`">{{ index + 1 }}</span>
          <span class="floating-bar__name">{{ entry.name }}</span>
          <span class="floating-bar__score">{{ entry.score }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { Trophy } from '@element-plus/icons-vue'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const topStudents = ref<LeaderboardEntry[]>([])

onMounted(async () => {
  await fetchTopStudents()
  connectWebSocket({
    onScoreUpdate: () => fetchTopStudents(),
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
    // silent
  }
}

function openMainWindow() {
  window.electronAPI?.openWindow?.('main')
}
</script>

<style scoped>
.floating-bar {
  width: 100%;
  height: 100%;
  background: rgba(20, 20, 30, 0.88);
  border-radius: 12px;
  display: flex;
  align-items: center;
  padding: 0 12px;
  -webkit-app-region: drag;
  user-select: none;
  backdrop-filter: blur(16px);
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.floating-bar__content {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}

.floating-bar__brand {
  display: flex;
  align-items: center;
  padding-right: 8px;
  border-right: 1px solid rgba(255, 255, 255, 0.1);
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
  color: rgba(255, 255, 255, 0.85);
  white-space: nowrap;
}

.floating-bar__rank {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 9px;
  font-weight: 700;
  background: rgba(255, 255, 255, 0.15);
  color: rgba(255, 255, 255, 0.6);
}

.floating-bar__rank--1 {
  background: #ffd700;
  color: #1a1a2e;
}

.floating-bar__rank--2 {
  background: #c0c0c0;
  color: #1a1a2e;
}

.floating-bar__rank--3 {
  background: #cd7f32;
  color: #1a1a2e;
}

.floating-bar__name {
  font-weight: 500;
}

.floating-bar__score {
  font-weight: 700;
  color: #409eff;
}
</style>
