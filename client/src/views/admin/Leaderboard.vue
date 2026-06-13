<template>
  <div class="leaderboard">
    <div class="leaderboard__header">
      <h2>排行榜</h2>
      <div class="leaderboard__controls">
        <el-radio-group v-model="mode" size="small" @change="fetchLeaderboard">
          <el-radio-button value="personal">个人</el-radio-button>
          <el-radio-button value="group">小组</el-radio-button>
        </el-radio-group>
        <el-button size="small" text :icon="Refresh" @click="fetchLeaderboard" />
      </div>
    </div>

    <!-- 前三名展示 -->
    <div v-if="topThree.length > 0" class="leaderboard__podium">
      <div class="leaderboard__podium-item leaderboard__podium--2" v-if="topThree.length >= 2">
        <div class="leaderboard__podium-avatar">
          <el-avatar :size="48">{{ topThree[1].name.charAt(0) }}</el-avatar>
          <span class="leaderboard__medal leaderboard__medal--silver">2</span>
        </div>
        <span class="leaderboard__podium-name">{{ topThree[1].name }}</span>
        <span class="leaderboard__podium-score">{{ topThree[1].score }}</span>
      </div>
      <div class="leaderboard__podium-item leaderboard__podium--1" v-if="topThree.length >= 1">
        <div class="leaderboard__podium-avatar">
          <el-avatar :size="56">{{ topThree[0].name.charAt(0) }}</el-avatar>
          <span class="leaderboard__medal leaderboard__medal--gold">1</span>
        </div>
        <span class="leaderboard__podium-name">{{ topThree[0].name }}</span>
        <span class="leaderboard__podium-score">{{ topThree[0].score }}</span>
      </div>
      <div class="leaderboard__podium-item leaderboard__podium--3" v-if="topThree.length >= 3">
        <div class="leaderboard__podium-avatar">
          <el-avatar :size="44">{{ topThree[2].name.charAt(0) }}</el-avatar>
          <span class="leaderboard__medal leaderboard__medal--bronze">3</span>
        </div>
        <span class="leaderboard__podium-name">{{ topThree[2].name }}</span>
        <span class="leaderboard__podium-score">{{ topThree[2].score }}</span>
      </div>
    </div>

    <!-- 其余排名 -->
    <div class="leaderboard__list">
      <div
        v-for="entry in restEntries"
        :key="entry.rank"
        class="leaderboard__item"
      >
        <span class="leaderboard__rank">{{ entry.rank }}</span>
        <span class="leaderboard__name">{{ entry.name }}</span>
        <span class="leaderboard__score">{{ entry.score }}</span>
      </div>
      <el-empty v-if="entries.length === 0" description="暂无排行数据" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const mode = ref<'personal' | 'group'>('personal')
const entries = ref<LeaderboardEntry[]>([])

const topThree = computed(() => entries.value.slice(0, 3))
const restEntries = computed(() => entries.value.slice(3))

let refreshTimer: ReturnType<typeof setInterval> | null = null

onMounted(async () => {
  await fetchLeaderboard()
  connectWebSocket({
    onScoreUpdate: () => fetchLeaderboard(),
  })
  refreshTimer = setInterval(fetchLeaderboard, 30000)
})

onUnmounted(() => {
  disconnectWebSocket()
  if (refreshTimer) clearInterval(refreshTimer)
})

async function fetchLeaderboard() {
  try {
    const endpoint = mode.value === 'personal' ? '/api/leaderboard/personal' : '/api/leaderboard/group'
    const response = await api.get<{ data: LeaderboardEntry[] }>(endpoint)
    entries.value = response.data.data
  } catch {
    // silent
  }
}
</script>

<style scoped>
.leaderboard__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.leaderboard__header h2 {
  margin: 0;
  font-family: var(--cis-font-family-display);
  font-size: 22px;
  color: var(--cis-text-primary);
  padding-left: 12px;
  border-left: 3px solid var(--cis-primary);
  background: linear-gradient(135deg, var(--cis-primary), var(--cis-primary-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.leaderboard__controls {
  display: flex;
  align-items: center;
  gap: 8px;
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
</style>
