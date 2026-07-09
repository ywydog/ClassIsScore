<template>
  <div class="m-dashboard">
    <header class="m-dashboard__head">
      <span class="cis-eyebrow">Dashboard</span>
      <h1 class="cis-display m-dashboard__title">总览</h1>
      <p class="m-dashboard__sub">
        <span class="cis-mono">{{ entries.length }}</span> 名 ·
        更新于 <span class="cis-mono">{{ updatedAt }}</span>
      </p>
    </header>

    <section class="m-dashboard__stats">
      <article v-for="block in statBlocks" :key="block.label" class="m-dashboard__stat">
        <span class="cis-eyebrow">{{ block.eyebrow }}</span>
        <span class="m-dashboard__stat-num cis-num" :class="block.tone">{{ block.value }}</span>
        <span class="m-dashboard__stat-label">{{ block.label }}</span>
        <svg v-if="block.spark" class="m-dashboard__stat-spark" viewBox="0 0 100 24" preserveAspectRatio="none" aria-hidden="true">
          <path :d="block.spark" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </article>
    </section>

    <section class="m-dashboard__quick">
      <h2 class="cis-eyebrow m-dashboard__quick-title">快捷操作</h2>
      <ul class="m-dashboard__quick-list" role="list">
        <li v-for="action in quickActions" :key="action.label">
          <router-link :to="action.to" class="m-dashboard__quick-row">
            <span class="m-dashboard__quick-label">{{ action.label }}</span>
            <span class="m-dashboard__quick-arrow" aria-hidden="true">→</span>
          </router-link>
        </li>
      </ul>
    </section>

    <section class="m-dashboard__recent">
      <h2 class="cis-eyebrow m-dashboard__recent-title">最近积分</h2>
      <ol v-if="recentRecords.length > 0" class="m-dashboard__recent-list" role="list">
        <li v-for="r in recentRecords" :key="r.id" class="m-dashboard__recent-row">
          <span class="m-dashboard__recent-name">{{ r.studentName || '—' }}</span>
          <span class="m-dashboard__recent-score cis-num" :class="r.scoreChange >= 0 ? 'is-plus' : 'is-minus'">
            {{ r.scoreChange > 0 ? '+' : '' }}{{ r.scoreChange }}
          </span>
          <span class="m-dashboard__recent-reason">{{ r.reason || '—' }}</span>
          <span class="m-dashboard__recent-time cis-mono">{{ formatTime(r.createdAt) }}</span>
        </li>
      </ol>
      <MobileEmptyState v-else eyebrow="Empty" description="今日还没有积分记录" />
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()

const entries = computed(() => studentStore.students)
const updatedAt = ref('--:--')
const recentRecords = computed(() => scoreStore.recentRecords.slice(0, 5))

const statBlocks = computed(() => [
  { eyebrow: 'Today', label: '今日积分', value: '+24', tone: 'is-plus', spark: 'M0,18 L10,15 L20,12 L30,10 L40,8 L50,9 L60,6 L70,5 L80,4 L90,3 L100,2' },
  { eyebrow: 'Week',  label: '本周积分', value: '+187', tone: 'is-plus', spark: 'M0,20 L10,18 L20,15 L30,12 L40,14 L50,10 L60,8 L70,9 L80,6 L90,5 L100,4' },
  { eyebrow: 'Count', label: '学生数',   value: String(studentStore.students.length),   tone: '',        spark: 'M0,12 L100,12' },
  { eyebrow: 'Avg',   label: '平均分',   value: '+12',  tone: 'is-plus', spark: 'M0,14 L10,12 L20,11 L30,10 L40,9 L50,8 L60,7 L70,8 L80,6 L90,5 L100,4' },
])

const quickActions = [
  { label: '加积分', to: '/m/scores' },
  { label: '看排行', to: '/m/leaderboard' },
  { label: '学生管理', to: '/m/students' },
]

function formatTime(iso: string) {
  if (!iso) return '--:--'
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`
}

onMounted(async () => {
  await Promise.all([
    scoreStore.fetchRecords(),
    studentStore.fetchStudents(),
  ])
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  updatedAt.value = `${pad(d.getHours())}:${pad(d.getMinutes())}`
})
</script>

<style scoped>
.m-dashboard { display: flex; flex-direction: column; gap: 24px; }
.m-dashboard__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-dashboard__sub { margin: 4px 0 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-dashboard__stats { display: flex; flex-direction: column; gap: 1px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-border); overflow: hidden; }
.m-dashboard__stat { display: grid; grid-template-columns: 1fr auto; grid-template-areas: 'eyebrow eyebrow' 'num spark' 'label label'; gap: 4px; padding: 14px 16px; background: var(--cis-surface-1); }
.m-dashboard__stat .cis-eyebrow { grid-area: eyebrow; color: var(--cis-text-tertiary); }
.m-dashboard__stat-num { grid-area: num; font-family: var(--cis-font-mono); font-size: 28px; font-weight: 700; color: var(--cis-text-primary); line-height: 1; font-variant-numeric: tabular-nums; }
.m-dashboard__stat-num.is-plus { color: var(--cis-success); }
.m-dashboard__stat-num.is-minus { color: var(--cis-accent); }
.m-dashboard__stat-label { grid-area: label; font-size: 12px; color: var(--cis-text-tertiary); }
.m-dashboard__stat-spark { grid-area: spark; width: 80px; height: 24px; color: var(--cis-primary); }
.m-dashboard__quick-list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-dashboard__quick-title, .m-dashboard__recent-title { margin: 0 0 8px; color: var(--cis-text-tertiary); }
.m-dashboard__quick-row { display: flex; align-items: center; justify-content: space-between; min-height: 48px; padding: 0 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); text-decoration: none; color: var(--cis-text-primary); font-weight: 500; font-size: 15px; -webkit-tap-highlight-color: transparent; }
.m-dashboard__quick-row:active { background: var(--cis-primary-tint); }
.m-dashboard__quick-list li:last-child .m-dashboard__quick-row { border-bottom: none; }
.m-dashboard__quick-arrow { color: var(--cis-text-tertiary); font-size: 16px; }
.m-dashboard__recent-list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-dashboard__recent-row { display: grid; grid-template-columns: 1fr auto; grid-template-areas: 'name score' 'reason time'; gap: 2px 8px; padding: 10px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-dashboard__recent-row:last-child { border-bottom: none; }
.m-dashboard__recent-name { grid-area: name; font-size: 14px; font-weight: 500; }
.m-dashboard__recent-score { grid-area: score; font-size: 14px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-dashboard__recent-score.is-plus { color: var(--cis-success); }
.m-dashboard__recent-score.is-minus { color: var(--cis-accent); }
.m-dashboard__recent-reason { grid-area: reason; font-size: 12px; color: var(--cis-text-tertiary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-dashboard__recent-time { grid-area: time; font-size: 11px; color: var(--cis-text-tertiary); }
</style>
