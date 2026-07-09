<template>
  <div class="m-leaderboard">
    <header class="m-leaderboard__head">
      <span class="cis-eyebrow">Leaderboard</span>
      <h1 class="cis-display m-leaderboard__title">排行榜</h1>
      <p class="m-leaderboard__sub">
        <span class="cis-mono">{{ entries.length }}</span> 名 ·
        更新于 <span class="cis-mono">{{ updatedAt }}</span>
      </p>
    </header>

    <div class="m-leaderboard__tabs" role="tablist" aria-label="排行榜筛选">
      <button
        v-for="opt in modeOptions"
        :key="opt.value"
        type="button"
        role="tab"
        class="m-leaderboard__tab"
        :class="{ 'is-active': mode === opt.value }"
        :aria-selected="mode === opt.value"
        @click="onModeChange(opt.value)"
      >
        <span class="m-leaderboard__tab-label">{{ opt.label }}</span>
      </button>
      <span class="m-leaderboard__tab-sep" aria-hidden="true"></span>
      <button
        type="button"
        class="m-leaderboard__tab m-leaderboard__tab--time"
        :aria-label="`时间范围：${currentTimeLabel}`"
        @click="showTimeSheet = true"
      >
        <span class="m-leaderboard__tab-label">{{ currentTimeLabel }}</span>
        <el-icon :size="14" aria-hidden="true"><ArrowDown /></el-icon>
      </button>
    </div>

    <template v-if="!loading && entries.length > 0">
      <div v-if="topThree.length > 0" class="m-leaderboard__podium" role="list" aria-label="前三名">
        <div
          v-for="entry in topThree"
          :key="`podium-${entry.rank}`"
          class="m-leaderboard__cell"
          :class="`m-leaderboard__cell--${entry.rank}`"
          role="listitem"
        >
          <span class="cis-eyebrow">No. {{ entry.rank }}</span>
          <div class="m-leaderboard__avatar">
            <span class="m-leaderboard__avatar-text">{{ nameInitial(entry.name) }}</span>
          </div>
          <span class="m-leaderboard__name">{{ entry.name }}</span>
          <span class="m-leaderboard__score cis-num" :class="entry.score >= 0 ? 'is-plus' : 'is-minus'">
            {{ entry.score > 0 ? '+' : '' }}{{ entry.score }}
          </span>
        </div>
      </div>

      <ol v-if="restEntries.length > 0" class="m-leaderboard__list" aria-label="其余排名">
        <li v-for="entry in restEntries" :key="entry.rank" class="m-leaderboard__row">
          <span class="m-leaderboard__row-rank cis-mono">{{ entry.rank.toString().padStart(2, '0') }}</span>
          <span class="m-leaderboard__row-name">{{ entry.name }}</span>
          <span class="m-leaderboard__row-score cis-num" :class="entry.score >= 0 ? 'is-plus' : 'is-minus'">
            {{ entry.score > 0 ? '+' : '' }}{{ entry.score }}
          </span>
        </li>
      </ol>
    </template>
    <MobileEmptyState v-else-if="!loading" eyebrow="Empty" description="该时段暂无数据" />
    <div v-else class="m-leaderboard__loading" aria-live="polite">
      <el-skeleton :rows="5" animated />
    </div>

    <BottomSheet v-model="showTimeSheet" title="选择时间范围" height="auto">
      <ul class="m-leaderboard__time-list" role="list">
        <li v-for="opt in timeOptions" :key="opt.value">
          <button
            type="button"
            class="m-leaderboard__time-item"
            :class="{ 'is-active': timeRange === opt.value }"
            :aria-checked="timeRange === opt.value"
            role="radio"
            @click="onTimeRangeChange(opt.value); showTimeSheet = false"
          >
            <span class="m-leaderboard__time-label">{{ opt.label }}</span>
            <el-icon v-if="timeRange === opt.value" :size="16" aria-hidden="true"><Check /></el-icon>
          </button>
        </li>
      </ul>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ArrowDown, Check } from '@element-plus/icons-vue'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { invoke } from '@/services/tauri'
import type { LeaderboardEntry } from '@/types'

type Mode = 'personal' | 'group'
type TimeRange = 'today' | 'week' | 'month' | 'all'

const mode = ref<Mode>('personal')
const timeRange = ref<TimeRange>('all')
const entries = ref<LeaderboardEntry[]>([])
const loading = ref(true)
const updatedAt = ref('--:--')
const showTimeSheet = ref(false)

const topThree = computed(() => entries.value.slice(0, 3))
const restEntries = computed(() => entries.value.slice(3))

const modeOptions: Array<{ value: Mode; label: string }> = [
  { value: 'personal', label: '个人' },
  { value: 'group', label: '小组' },
]
const timeOptions: Array<{ value: TimeRange; label: string }> = [
  { value: 'today', label: '今日' },
  { value: 'week', label: '本周' },
  { value: 'month', label: '本月' },
  { value: 'all', label: '全部' },
]
const currentTimeLabel = computed(() => timeOptions.find(o => o.value === timeRange.value)?.label || '全部')

function nameInitial(name: string) { return name?.slice(0, 1) ?? '?' }

function setUpdatedAt() {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  updatedAt.value = `${pad(d.getHours())}:${pad(d.getMinutes())}`
}

onMounted(async () => {
  await fetchLeaderboard()
  loading.value = false
})

function getTimeRangeParams(): { startTime: string | null; endTime: string | null } {
  const now = new Date()
  let startTime: Date | undefined
  switch (timeRange.value) {
    case 'today': startTime = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0); break
    case 'week': { const day = now.getDay() || 7; startTime = new Date(now.getFullYear(), now.getMonth(), now.getDate() - day + 1, 0, 0, 0); break }
    case 'month': startTime = new Date(now.getFullYear(), now.getMonth(), 1, 0, 0, 0); break
    case 'all': return { startTime: null, endTime: null }
  }
  return { startTime: startTime!.toISOString().slice(0, 19), endTime: now.toISOString().slice(0, 19) }
}

async function fetchLeaderboard() {
  try {
    const { startTime, endTime } = getTimeRangeParams()
    const args = { startTime, endTime }
    if (mode.value === 'personal') {
      const data = await invoke<Array<{ student: { name: string; total_score: number } }>>('leaderboard_query', args)
      entries.value = data.map((item, i) => ({ rank: i + 1, name: item.student.name, score: item.student.total_score ?? 0, isGroup: false }))
    } else {
      const groups = await invoke<Array<{ group_name: string; total_score: number }>>('leaderboard_all_groups', args)
      entries.value = groups.map((g, i) => ({ rank: i + 1, name: g.group_name, score: g.total_score, isGroup: true }))
    }
    setUpdatedAt()
  } catch {
    entries.value = []
  }
}

async function onModeChange(value: Mode) {
  mode.value = value
  loading.value = true
  await fetchLeaderboard()
  loading.value = false
}

async function onTimeRangeChange(value: TimeRange) {
  timeRange.value = value
  loading.value = true
  await fetchLeaderboard()
  loading.value = false
}
</script>

<style scoped>
.m-leaderboard { display: flex; flex-direction: column; gap: 20px; }
.m-leaderboard__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-leaderboard__sub { margin: 4px 0 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-leaderboard__loading { padding: 8px 0; }
.m-leaderboard__tabs { display: flex; align-items: stretch; gap: 8px; border-bottom: 1px solid var(--cis-border); }
.m-leaderboard__tab { position: relative; display: flex; align-items: center; gap: 4px; padding: 10px 8px; background: transparent; border: none; color: var(--cis-text-tertiary); font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-leaderboard__tab.is-active { color: var(--cis-primary); font-weight: 600; }
.m-leaderboard__tab::after { content: ''; position: absolute; left: 0; right: 0; bottom: -1px; height: 2px; background: transparent; transition: background-color var(--cis-transition-fast); }
.m-leaderboard__tab.is-active::after { background: var(--cis-primary); }
.m-leaderboard__tab-sep { width: 1px; background: var(--cis-border-light); margin: 6px 0; }
.m-leaderboard__tab--time { margin-left: auto; }
.m-leaderboard__tab-label { font-size: 14px; }
.m-leaderboard__podium { display: grid; grid-template-columns: 1fr 1.2fr 1fr; gap: 0; align-items: end; padding: 16px 8px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); }
.m-leaderboard__cell { display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 0 4px; }
.m-leaderboard__cell--1 { order: 2; }
.m-leaderboard__cell--2 { order: 1; }
.m-leaderboard__cell--3 { order: 3; }
.m-leaderboard__avatar { width: 48px; height: 48px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; }
.m-leaderboard__cell--1 .m-leaderboard__avatar { width: 64px; height: 64px; background: var(--cis-primary); border-color: var(--cis-primary); }
.m-leaderboard__cell--1 .m-leaderboard__avatar-text { color: #fff; }
.m-leaderboard__avatar-text { font-family: var(--cis-font-serif); font-size: 20px; font-weight: 600; color: var(--cis-text-primary); line-height: 1; }
.m-leaderboard__name { font-size: 13px; font-weight: 500; text-align: center; max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-leaderboard__cell--1 .m-leaderboard__name { font-size: 14px; }
.m-leaderboard__score { font-family: var(--cis-font-mono); font-size: 16px; font-weight: 700; font-variant-numeric: tabular-nums; line-height: 1; }
.m-leaderboard__cell--1 .m-leaderboard__score { font-size: 22px; }
.m-leaderboard__score.is-plus, .m-leaderboard__row-score.is-plus { color: var(--cis-success); }
.m-leaderboard__score.is-minus, .m-leaderboard__row-score.is-minus { color: var(--cis-accent); }
.m-leaderboard__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-leaderboard__row { display: flex; align-items: center; gap: 12px; min-height: 48px; padding: 0 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); }
.m-leaderboard__row:last-child { border-bottom: none; }
.m-leaderboard__row::before { content: ''; width: 4px; height: 16px; background: var(--cis-text-tertiary); border-radius: 2px; flex-shrink: 0; }
.m-leaderboard__row-rank { font-family: var(--cis-font-mono); font-size: 13px; font-weight: 600; color: var(--cis-text-tertiary); min-width: 24px; }
.m-leaderboard__row-name { flex: 1; font-size: 14px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-leaderboard__row-score { font-size: 15px; font-weight: 700; min-width: 56px; text-align: right; font-variant-numeric: tabular-nums; }
.m-leaderboard__time-list { list-style: none; margin: 0; padding: 0; }
.m-leaderboard__time-item { display: flex; align-items: center; justify-content: space-between; width: 100%; min-height: 48px; padding: 0 4px; border: none; background: transparent; color: var(--cis-text-primary); font-size: 15px; font-weight: 500; font-family: inherit; cursor: pointer; border-bottom: 1px solid var(--cis-border-light); -webkit-tap-highlight-color: transparent; }
.m-leaderboard__time-list li:last-child .m-leaderboard__time-item { border-bottom: none; }
.m-leaderboard__time-item.is-active { color: var(--cis-primary); font-weight: 600; }
.m-leaderboard__time-item:active { background: var(--cis-primary-tint); }
</style>
