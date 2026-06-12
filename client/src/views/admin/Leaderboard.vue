<template>
  <div class="leaderboard">
    <div class="leaderboard__header">
      <h2>排行榜</h2>
      <el-radio-group v-model="mode" size="small">
        <el-radio-button value="personal">个人</el-radio-button>
        <el-radio-button value="group">小组</el-radio-button>
      </el-radio-group>
    </div>

    <div class="leaderboard__list">
      <div
        v-for="entry in entries"
        :key="entry.rank"
        class="leaderboard__item"
        :class="{ 'leaderboard__item--top': entry.rank <= 3 }"
      >
        <span class="leaderboard__rank" :class="`leaderboard__rank--${entry.rank}`">
          {{ entry.rank }}
        </span>
        <span class="leaderboard__name">{{ entry.name }}</span>
        <span class="leaderboard__score">{{ entry.score }}</span>
      </div>
      <el-empty v-if="entries.length === 0" description="暂无排行数据" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { LeaderboardEntry } from '@/types'
import api from '@/services/api'

const mode = ref<'personal' | 'group'>('personal')
const entries = ref<LeaderboardEntry[]>([])

onMounted(async () => {
  await fetchLeaderboard()
})

async function fetchLeaderboard() {
  const endpoint = mode.value === 'personal' ? '/api/leaderboard/personal' : '/api/leaderboard/group'
  const response = await api.get<{ data: LeaderboardEntry[] }>(endpoint)
  entries.value = response.data.data
}
</script>

<style scoped>
.leaderboard__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.leaderboard__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

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
  border-radius: 8px;
  border: 1px solid var(--cis-border-color);
}

.leaderboard__item--top {
  border-color: var(--cis-primary);
  background-color: var(--cis-primary-light-9);
}

.leaderboard__rank {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-weight: 700;
  font-size: 14px;
  background-color: var(--cis-bg-secondary);
  color: var(--cis-text-secondary);
}

.leaderboard__rank--1 {
  background-color: #ffd700;
  color: #fff;
}

.leaderboard__rank--2 {
  background-color: #c0c0c0;
  color: #fff;
}

.leaderboard__rank--3 {
  background-color: #cd7f32;
  color: #fff;
}

.leaderboard__name {
  flex: 1;
  font-weight: 500;
  color: var(--cis-text-primary);
}

.leaderboard__score {
  font-weight: 700;
  font-size: 16px;
  color: var(--cis-primary);
}
</style>
