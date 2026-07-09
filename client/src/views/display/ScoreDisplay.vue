<template>
  <div class="score-display">
    <header class="score-display__head">
      <div class="score-display__brand">
        <span class="score-display__brand-mark" aria-hidden="true">C</span>
        <span class="score-display__brand-name">ClassIsScore</span>
      </div>
      <div class="score-display__meta">
        <span class="score-display__time" aria-label="当前时间">
          <span class="score-display__time-hm cis-mono">{{ timeHM }}</span>
          <span class="score-display__time-sec cis-mono">{{ timeSec }}</span>
        </span>
        <span class="score-display__date cis-mono">{{ dateText }}</span>
      </div>
    </header>

    <main class="score-display__main" :aria-label="displayClassName">
      <h1 class="score-display__title" :title="displayClassName">{{ displayClassName }}</h1>
      <p class="score-display__subtitle">{{ subtitleText }}</p>

      <ol v-if="entries.length > 0" class="score-display__podium" aria-label="实时排行">
        <li
          v-for="entry in topThree"
          :key="`top-${entry.rank}`"
          class="score-display__cell"
          :class="`score-display__cell--${entry.rank}`"
        >
          <span class="score-display__rank cis-mono">{{ entry.rank.toString().padStart(2, '0') }}</span>
          <span class="score-display__name">{{ entry.name }}</span>
          <span
            class="score-display__score"
            :class="entry.score >= 0 ? 'is-plus' : 'is-minus'"
          >
            <span v-if="entry.score > 0" class="score-display__score-sign">+</span>{{ entry.score }}
          </span>
        </li>
      </ol>
      <div v-else class="score-display__empty">
        <span class="cis-eyebrow">No data</span>
        <p class="score-display__empty-text">暂无排行数据</p>
      </div>

      <ol v-if="restEntries.length > 0" class="score-display__list" aria-label="其余排名">
        <li
          v-for="entry in restEntries"
          :key="entry.rank"
          class="score-display__row"
        >
          <span class="score-display__row-rank cis-mono">{{ entry.rank.toString().padStart(2, '0') }}</span>
          <span class="score-display__row-name">{{ entry.name }}</span>
          <span
            class="score-display__row-score"
            :class="entry.score >= 0 ? 'is-plus' : 'is-minus'"
          >
            <span v-if="entry.score > 0" class="score-display__row-score-sign">+</span>{{ entry.score }}
          </span>
        </li>
      </ol>
    </main>

    <footer class="score-display__foot">
      <span class="score-display__foot-eyebrow">Live</span>
      <span class="score-display__foot-text">自动刷新 · 每 30 秒</span>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useSettingsStore } from '@/stores/settings'
import { invoke } from '@/services/tauri'
import type { LeaderboardEntry } from '@/types'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

const settingsStore = useSettingsStore()
const entries = ref<LeaderboardEntry[]>([])

const timeHM = ref('--:--')
const timeSec = ref('--')
const dateText = ref('')

let timer: ReturnType<typeof setInterval> | null = null

const displayClassName = computed(() => {
  const settings = settingsStore.settings as unknown as { className?: string }
  const name = settings.className?.trim()
  return name || 'ClassIsScore'
})

const subtitleText = computed(() => {
  if (entries.value.length === 0) return '等待数据载入…'
  return `共 ${entries.value.length} 名学生参与`
})

const topThree = computed(() => entries.value.slice(0, 3))
const restEntries = computed(() => entries.value.slice(3))

function tick() {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  timeHM.value = `${pad(d.getHours())}:${pad(d.getMinutes())}`
  timeSec.value = pad(d.getSeconds())
  const weeks = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']
  dateText.value = `${d.getFullYear()}.${pad(d.getMonth() + 1)}.${pad(d.getDate())} · ${weeks[d.getDay()]}`
}

onMounted(async () => {
  tick()
  timer = setInterval(tick, 1000)
  await refresh()
  connectWebSocket({ onScoreUpdate: () => refresh() })
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
  disconnectWebSocket()
})

async function refresh() {
  try {
    // IPC：leaderboard_query，按"全部"窗口取数
    const data = await invoke<Array<{ student: { name: string; total_score: number } }>>(
      'leaderboard_query',
      { startTime: null, endTime: null }
    )
    entries.value = data.map((item, index) => ({
      rank: index + 1,
      name: item.student.name,
      score: item.student.total_score ?? 0,
      isGroup: false,
    }))
  } catch {
    entries.value = []
  }
}
</script>

<style scoped>
.score-display {
  width: 100vw;
  min-height: 100vh;
  background: var(--cis-surface-inverse);
  color: #fff;
  display: flex;
  flex-direction: column;
  padding: 48px 64px;
  box-sizing: border-box;
  overflow: hidden;
}

@media (max-width: 900px) {
  .score-display {
    padding: 24px;
  }
}

/* ===== 头部 ===== */
.score-display__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
}

.score-display__brand {
  display: flex;
  align-items: center;
  gap: 10px;
}

.score-display__brand-mark {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--cis-primary);
  color: #fff;
  border-radius: var(--cis-radius-btn);
  font-family: var(--cis-font-serif);
  font-weight: 700;
  font-size: 16px;
  line-height: 1;
}

.score-display__brand-name {
  font-family: var(--cis-font-serif);
  font-size: 16px;
  font-weight: 600;
  color: #fff;
  letter-spacing: 0.2px;
}

.score-display__meta {
  display: flex;
  align-items: baseline;
  gap: 16px;
  color: rgba(255, 255, 255, 0.7);
}

.score-display__time {
  display: flex;
  align-items: baseline;
  gap: 1px;
  font-feature-settings: 'tnum' 1;
}

.score-display__time-hm {
  font-size: 18px;
  font-weight: 600;
  color: #fff;
  letter-spacing: 0.5px;
}

.score-display__time-sec {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.45);
}

.score-display__date {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.45);
  letter-spacing: 0.3px;
}

/* ===== 主体 ===== */
.score-display__main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 32px;
  padding: 32px 0;
}

.score-display__title {
  font-family: var(--cis-font-serif);
  font-size: 64px;
  font-weight: 700;
  letter-spacing: -1px;
  color: #fff;
  margin: 0;
  line-height: 1.05;
  max-width: 90%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 900px) {
  .score-display__title {
    font-size: 40px;
  }
}

.score-display__subtitle {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.45);
  margin: -24px 0 0;
  font-family: var(--cis-font-mono);
  letter-spacing: 0.3px;
}

/* ===== 前三名领奖台 ===== */
.score-display__podium {
  display: grid;
  grid-template-columns: 1fr 1.4fr 1fr;
  gap: 0;
  list-style: none;
  margin: 0;
  padding: 0;
  align-items: end;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  padding: 24px 0;
}

.score-display__cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 16px 12px;
  position: relative;
}

.score-display__cell--1 { order: 2; }
.score-display__cell--2 { order: 1; }
.score-display__cell--3 { order: 3; }

.score-display__rank {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.45);
  letter-spacing: 0.5px;
  font-weight: 600;
}

.score-display__cell--1 .score-display__rank {
  color: var(--cis-primary-hover);
}

.score-display__name {
  font-family: var(--cis-font-serif);
  font-size: 24px;
  font-weight: 600;
  color: #fff;
  text-align: center;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-display__cell--1 .score-display__name {
  font-size: 32px;
}

.score-display__score {
  font-family: var(--cis-font-mono);
  font-feature-settings: 'tnum' 1;
  font-variant-numeric: tabular-nums;
  font-size: 40px;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.85);
  line-height: 1;
  margin-top: 4px;
}

.score-display__cell--1 .score-display__score {
  font-size: 56px;
  color: #fff;
}

.score-display__score.is-plus { color: #6EE7B7; }
.score-display__score.is-minus { color: #FCA5A5; }

.score-display__score-sign {
  font-size: 0.7em;
  color: inherit;
  margin-right: 1px;
  opacity: 0.7;
}

/* ===== 其余排名 ===== */
.score-display__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0;
  max-height: 30vh;
  overflow-y: auto;
}

.score-display__row {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 12px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
}

.score-display__row:last-child {
  border-bottom: none;
}

.score-display__row-rank {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.4);
  min-width: 32px;
  font-weight: 600;
}

.score-display__row-name {
  flex: 1;
  font-size: 16px;
  color: rgba(255, 255, 255, 0.9);
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-display__row-score {
  font-family: var(--cis-font-mono);
  font-feature-settings: 'tnum' 1;
  font-variant-numeric: tabular-nums;
  font-size: 20px;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.7);
  min-width: 80px;
  text-align: right;
}

.score-display__row-score.is-plus { color: #6EE7B7; }
.score-display__row-score.is-minus { color: #FCA5A5; }

.score-display__row-score-sign {
  font-size: 0.75em;
  opacity: 0.7;
  margin-right: 1px;
}

/* ===== Empty ===== */
.score-display__empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 64px 16px;
  border: 1px dashed rgba(255, 255, 255, 0.12);
  border-radius: var(--cis-radius-card);
  text-align: center;
}

.score-display__empty .cis-eyebrow {
  color: rgba(255, 255, 255, 0.4);
}

.score-display__empty-text {
  margin: 0;
  color: rgba(255, 255, 255, 0.5);
  font-size: 14px;
}

/* ===== 底部 ===== */
.score-display__foot {
  display: flex;
  align-items: center;
  gap: 8px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  padding-top: 16px;
}

.score-display__foot-eyebrow {
  font-family: var(--cis-font-mono);
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.6px;
  text-transform: uppercase;
  color: var(--cis-primary-hover);
}

.score-display__foot-text {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.4);
  font-family: var(--cis-font-mono);
  letter-spacing: 0.3px;
}

@media (prefers-reduced-motion: reduce) {
  .score-display__row,
  .score-display__list {
    transition: none;
  }
}
</style>
