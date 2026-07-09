<template>
  <div class="m-settle">
    <header class="m-settle__head">
      <span class="cis-eyebrow">Settlement</span>
      <h1 class="cis-display m-settle__title">学期结算</h1>
    </header>

    <section class="m-settle__kpi">
      <article class="m-settle__kpi-cell">
        <span class="cis-eyebrow">Total</span>
        <span class="m-settle__kpi-num cis-num">{{ totalScore }}</span>
        <span class="m-settle__kpi-label">总分</span>
      </article>
      <article class="m-settle__kpi-cell">
        <span class="cis-eyebrow">Top +</span>
        <span class="m-settle__kpi-num is-plus cis-num">+{{ topPlus }}</span>
        <span class="m-settle__kpi-label">{{ topPlusName }}</span>
      </article>
      <article class="m-settle__kpi-cell">
        <span class="cis-eyebrow">Top −</span>
        <span class="m-settle__kpi-num is-minus cis-num">−{{ topMinus }}</span>
        <span class="m-settle__kpi-label">{{ topMinusName }}</span>
      </article>
    </section>

    <ol v-if="ranking.length > 0" class="m-settle__list" role="list">
      <li v-for="r in ranking" :key="r.id" class="m-settle__row">
        <span class="m-settle__rank cis-mono">{{ r.rank.toString().padStart(2, '0') }}</span>
        <span class="m-settle__name">{{ r.name }}</span>
        <span class="m-settle__score cis-num" :class="r.score >= 0 ? 'is-plus' : 'is-minus'">
          {{ r.score > 0 ? '+' : '' }}{{ r.score }}
        </span>
      </li>
    </ol>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无数据" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { useStudentStore } from '@/stores/student'

const studentStore = useStudentStore()
const ranking = ref<Array<{ id: string; rank: number; name: string; score: number }>>([])

onMounted(async () => {
  await studentStore.fetchStudents()
  ranking.value = studentStore.students
    .map(s => ({ id: s.id, rank: 0, name: s.name, score: s.score }))
    .sort((a, b) => b.score - a.score)
    .map((s, i) => ({ ...s, rank: i + 1 }))
})

const totalScore = computed(() => ranking.value.reduce((sum, r) => sum + r.score, 0))
const topPlus = computed(() => ranking.value[0]?.score || 0)
const topPlusName = computed(() => ranking.value[0]?.name || '—')
const topMinus = computed(() => Math.abs(ranking.value[ranking.value.length - 1]?.score || 0))
const topMinusName = computed(() => ranking.value[ranking.value.length - 1]?.name || '—')
</script>

<style scoped>
.m-settle { display: flex; flex-direction: column; gap: 16px; }
.m-settle__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-settle__kpi { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-border); overflow: hidden; }
.m-settle__kpi-cell { display: flex; flex-direction: column; gap: 4px; padding: 14px 10px; background: var(--cis-surface-1); }
.m-settle__kpi-cell .cis-eyebrow { color: var(--cis-text-tertiary); }
.m-settle__kpi-num { font-family: var(--cis-font-mono); font-size: 20px; font-weight: 700; font-variant-numeric: tabular-nums; line-height: 1; }
.m-settle__kpi-num.is-plus { color: var(--cis-success); }
.m-settle__kpi-num.is-minus { color: var(--cis-accent); }
.m-settle__kpi-label { font-size: 11px; color: var(--cis-text-tertiary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-settle__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-settle__row { display: flex; align-items: center; gap: 12px; min-height: 48px; padding: 0 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); }
.m-settle__row:last-child { border-bottom: none; }
.m-settle__row::before { content: ''; width: 4px; height: 16px; background: var(--cis-text-tertiary); border-radius: 2px; flex-shrink: 0; }
.m-settle__rank { font-family: var(--cis-font-mono); font-size: 13px; font-weight: 600; color: var(--cis-text-tertiary); min-width: 24px; }
.m-settle__name { flex: 1; font-size: 14px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-settle__score { font-size: 15px; font-weight: 700; min-width: 56px; text-align: right; font-variant-numeric: tabular-nums; }
.m-settle__score.is-plus { color: var(--cis-success); }
.m-settle__score.is-minus { color: var(--cis-accent); }
</style>
