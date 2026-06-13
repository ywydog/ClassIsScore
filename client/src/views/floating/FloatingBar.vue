<template>
  <div class="floating-bar" @dblclick="openMainWindow">
    <div class="floating-bar__content">
      <div class="floating-bar__brand">
        <div class="floating-bar__brand-icon">
          <el-icon :size="12"><Trophy /></el-icon>
        </div>
      </div>
      <div class="floating-bar__divider"></div>
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
  background: rgba(10, 22, 40, 0.92);
  border-radius: 14px;
  display: flex;
  align-items: center;
  padding: 0 10px;
  -webkit-app-region: drag;
  user-select: none;
  backdrop-filter: blur(20px);
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.4), inset 0 1px 0 rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.floating-bar__content {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
}

.floating-bar__brand {
  display: flex;
  align-items: center;
}

.floating-bar__brand-icon {
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  border-radius: 6px;
  color: #fff;
}

.floating-bar__divider {
  width: 1px;
  height: 16px;
  background: rgba(255, 255, 255, 0.1);
}

.floating-bar__items {
  display: flex;
  gap: 14px;
  overflow: hidden;
}

.floating-bar__item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  color: rgba(255, 255, 255, 0.8);
  white-space: nowrap;
}

.floating-bar__rank {
  width: 14px;
  height: 14px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 8px;
  font-weight: 700;
  background: rgba(255, 255, 255, 0.1);
  color: rgba(255, 255, 255, 0.4);
}

.floating-bar__rank--1 {
  background: linear-gradient(135deg, #ffd700, #ffb700);
  color: #1a1a2e;
}
.floating-bar__rank--2 {
  background: linear-gradient(135deg, #e8e8e8, #b0b0b0);
  color: #1a1a2e;
}
.floating-bar__rank--3 {
  background: linear-gradient(135deg, #cd7f32, #a0622a);
  color: #fff;
}

.floating-bar__name {
  font-weight: 500;
}

.floating-bar__score {
  font-weight: 700;
  color: #2dd4bf;
}
</style>
