<template>
  <div class="m-profile">
    <header class="m-profile__head">
      <button type="button" class="m-profile__back" aria-label="返回学生列表" @click="$router.back()">
        <el-icon :size="20" aria-hidden="true"><ArrowLeft /></el-icon>
      </button>
      <h1 class="cis-display m-profile__name">{{ student?.name || '学生详情' }}</h1>
    </header>

    <section v-if="student" class="m-profile__hero cis-hairline">
      <div class="m-profile__avatar" aria-hidden="true">
        <span class="m-profile__avatar-text">{{ student.name.slice(0, 1) }}</span>
      </div>
      <span class="m-profile__hero-name">{{ student.name }}</span>
      <span class="m-profile__hero-id cis-mono">{{ student.studentNumber || '—' }} · {{ student.groupId || '未分组' }}</span>
      <span
        class="m-profile__hero-score cis-num"
        :class="student.score > 0 ? 'is-plus' : student.score < 0 ? 'is-minus' : ''"
      >
        {{ student.score > 0 ? '+' : '' }}{{ student.score }}
      </span>
      <svg class="m-profile__hero-spark" viewBox="0 0 100 24" preserveAspectRatio="none" aria-hidden="true">
        <path :d="sparkPath" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </section>

    <div class="m-profile__tabs" role="tablist" aria-label="详情分类">
      <button
        v-for="t in tabs"
        :key="t.key"
        type="button"
        role="tab"
        class="m-profile__tab"
        :class="{ 'is-active': activeTab === t.key }"
        :aria-selected="activeTab === t.key"
        @click="activeTab = t.key"
      >{{ t.label }}</button>
    </div>

    <section v-if="activeTab === 'records'" class="m-profile__records">
      <ol v-if="records.length > 0" class="m-profile__list" role="list">
        <li v-for="r in records" :key="r.id" class="m-profile__record">
          <span class="m-profile__record-time cis-mono">{{ formatTime(r.createdAt) }}</span>
          <span class="m-profile__record-score cis-num" :class="r.scoreChange >= 0 ? 'is-plus' : 'is-minus'">
            {{ r.scoreChange > 0 ? '+' : '' }}{{ r.scoreChange }}
          </span>
          <span class="m-profile__record-reason">{{ r.reason || '—' }}</span>
        </li>
      </ol>
      <MobileEmptyState v-else eyebrow="Empty" description="暂无积分记录" />
    </section>

    <section v-else-if="activeTab === 'evaluation'" class="m-profile__eval">
      <p class="m-profile__eval-hint">评估项管理请前往桌面端</p>
    </section>

    <button type="button" class="m-profile__cta" aria-label="加积分" @click="showSheet = true">
      加积分
    </button>

    <BottomSheet v-model="showSheet" title="加积分" height="half">
      <p class="m-profile__sheet-hint">从评价项快速选择。</p>
      <div v-if="evaluationItems.length > 0" class="m-profile__sheet-grid">
        <button
          v-for="item in evaluationItems"
          :key="`s-${item.id}`"
          type="button"
          class="m-profile__sheet-tile"
          :class="item.isPositive ? 'is-plus' : 'is-minus'"
          @click="applyEvaluationItem(item); showSheet = false"
        >
          <span class="m-profile__sheet-tile-name">{{ item.name }}</span>
          <span class="m-profile__sheet-tile-value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
        </button>
      </div>
      <MobileEmptyState v-else eyebrow="Empty" description="暂无评价项" />
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { useStudentStore } from '@/stores/student'
import { useScoreStore } from '@/stores/score'
import { invoke } from '@/services/tauri'
import type { EvaluationItem } from '@/types'

const route = useRoute()
const studentStore = useStudentStore()
const scoreStore = useScoreStore()

const studentId = computed(() => String(route.params.id))
const student = computed(() => studentStore.students.find(s => String(s.id) === studentId.value))
const records = computed(() => scoreStore.recentRecords.filter(r => String(r.studentId) === studentId.value))

const activeTab = ref<'records' | 'evaluation'>('records')
const tabs = [
  { key: 'records' as const, label: '积分记录' },
  { key: 'evaluation' as const, label: '评估' },
]
const evaluationItems = ref<EvaluationItem[]>([])
const showSheet = ref(false)

const sparkPath = computed(() => {
  const list = records.value.slice(0, 20).reverse()
  if (list.length < 2) return 'M0,12 L100,12'
  const max = Math.max(...list.map(r => Math.abs(r.scoreChange))) || 1
  return list.map((r, i) => {
    const x = (i / (list.length - 1)) * 100
    const y = 12 - (r.scoreChange / max) * 10
    return `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${y.toFixed(1)}`
  }).join(' ')
})

function formatTime(iso: string) {
  if (!iso) return '--:--'
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function applyEvaluationItem(item: EvaluationItem) {
  if (!student.value) return
  const value = Math.abs(item.scoreChange)
  scoreStore.addScore(student.value.id, item.isPositive ? value : -value, item.name)
    .then(() => ElMessage.success('已记录'))
    .catch(() => ElMessage.error('记录失败'))
}

onMounted(async () => {
  await Promise.all([
    studentStore.fetchStudents(),
    scoreStore.fetchRecords(),
    fetchEvaluationItems(),
  ])
})

async function fetchEvaluationItems() {
  try { evaluationItems.value = await invoke<EvaluationItem[]>('evaluation_list', {}) }
  catch { evaluationItems.value = [] }
}
</script>

<style scoped>
.m-profile { display: flex; flex-direction: column; gap: 16px; padding-bottom: 96px; }
.m-profile__head { display: flex; align-items: center; gap: 4px; }
.m-profile__back { display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px; border: none; border-radius: var(--cis-radius-btn); background: transparent; color: var(--cis-text-secondary); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; }
.m-profile__back:active { background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-profile__name { font-size: 20px; margin: 0; font-weight: 600; }
.m-profile__hero { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 24px 16px; background: var(--cis-surface-1); border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); text-align: center; }
.m-profile__avatar { width: 64px; height: 64px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; }
.m-profile__avatar-text { font-family: var(--cis-font-serif); font-size: 28px; font-weight: 600; line-height: 1; }
.m-profile__hero-name { font-size: 17px; font-weight: 600; }
.m-profile__hero-id { font-size: 12px; color: var(--cis-text-tertiary); }
.m-profile__hero-score { font-family: var(--cis-font-mono); font-size: 32px; font-weight: 700; font-variant-numeric: tabular-nums; line-height: 1; }
.m-profile__hero-score.is-plus { color: var(--cis-success); }
.m-profile__hero-score.is-minus { color: var(--cis-accent); }
.m-profile__hero-spark { width: 100%; max-width: 240px; height: 32px; color: var(--cis-primary); }
.m-profile__tabs { display: flex; align-items: stretch; gap: 0; border-bottom: 1px solid var(--cis-border); }
.m-profile__tab { position: relative; padding: 10px 16px; background: transparent; border: none; color: var(--cis-text-tertiary); font-size: 14px; font-weight: 500; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-profile__tab.is-active { color: var(--cis-primary); font-weight: 600; }
.m-profile__tab::after { content: ''; position: absolute; left: 0; right: 0; bottom: -1px; height: 2px; background: transparent; transition: background-color var(--cis-transition-fast); }
.m-profile__tab.is-active::after { background: var(--cis-primary); }
.m-profile__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-profile__record { display: grid; grid-template-columns: auto auto 1fr; gap: 4px 12px; align-items: baseline; padding: 12px 16px; border-bottom: 1px solid var(--cis-border-light); }
.m-profile__list .m-profile__record:last-child { border-bottom: none; }
.m-profile__record-time { font-size: 11px; color: var(--cis-text-tertiary); }
.m-profile__record-score { font-family: var(--cis-font-mono); font-size: 14px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-profile__record-score.is-plus { color: var(--cis-success); }
.m-profile__record-score.is-minus { color: var(--cis-accent); }
.m-profile__record-reason { font-size: 14px; color: var(--cis-text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-profile__eval-hint { padding: 32px 16px; text-align: center; color: var(--cis-text-tertiary); font-size: 14px; margin: 0; }
.m-profile__cta { position: fixed; right: 16px; left: 16px; bottom: calc(80px + env(safe-area-inset-bottom, 0)); z-index: 100; height: 48px; border: none; border-radius: var(--cis-radius-btn); background: var(--cis-primary); color: #fff; font-size: 15px; font-weight: 600; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3); }
.m-profile__cta:active { transform: scale(var(--cis-press-scale-strong)); }
.m-profile__sheet-hint { margin: 0 0 12px; font-size: 13px; color: var(--cis-text-tertiary); }
.m-profile__sheet-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-profile__sheet-tile { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px; min-height: 72px; padding: 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-profile__sheet-tile:active { transform: scale(var(--cis-press-scale)); }
.m-profile__sheet-tile.is-plus { border-color: rgba(21, 128, 61, 0.4); background: var(--cis-success-tint); color: var(--cis-success); }
.m-profile__sheet-tile.is-minus { border-color: rgba(185, 28, 28, 0.4); background: var(--cis-accent-tint); color: var(--cis-accent); }
.m-profile__sheet-tile-name { font-size: 13px; font-weight: 600; }
.m-profile__sheet-tile-value { font-size: 18px; font-weight: 700; font-variant-numeric: tabular-nums; }
</style>
