<template>
  <div class="m-settle">
    <header class="m-settle__head">
      <div class="m-settle__head-text">
        <span class="cis-eyebrow">Settlement</span>
        <h1 class="cis-display m-settle__title">学期结算</h1>
      </div>
      <button
        type="button"
        class="m-settle__settle-btn"
        :disabled="studentStore.studentCount === 0"
        aria-label="执行结算"
        @click="handleSettle"
      >
        <el-icon :size="16" aria-hidden="true"><Finished /></el-icon>
        <span>执行结算</span>
      </button>
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

    <section v-if="settlements.length > 0" class="m-settle__history">
      <h2 class="cis-eyebrow m-settle__history-title">结算记录</h2>
      <ol class="m-settle__history-list" role="list">
        <li v-for="record in settlements" :key="record.id" class="m-settle__history-row">
          <div class="m-settle__history-time">
            <el-icon :size="14" aria-hidden="true"><Clock /></el-icon>
            <span class="cis-mono">{{ formatTime(record.settledAt || record.createdAt || '') }}</span>
          </div>
          <div class="m-settle__history-meta">
            <span>{{ record.name || '积分结算' }}</span>
            <span v-if="record.isReverted || record.status === 2" class="m-settle__history-tag">已撤销</span>
          </div>
        </li>
      </ol>
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

    <button
      type="button"
      class="m-settle__fab"
      aria-label="更多操作"
      @click="showActionSheet = true"
    >
      <el-icon :size="22" aria-hidden="true"><MoreFilled /></el-icon>
    </button>

    <BottomSheet v-model="showActionSheet" title="结算操作" height="auto">
      <div class="m-settle__actions">
        <button
          type="button"
          class="m-settle__action"
          :disabled="!latestRevertable"
          @click="handleRevertLatest"
        >
          <el-icon :size="18" aria-hidden="true"><RefreshLeft /></el-icon>
          <span class="m-settle__action-text">撤销最近一次</span>
        </button>
        <button
          type="button"
          class="m-settle__action"
          :disabled="settlements.length === 0"
          @click="handleExportSettlement"
        >
          <el-icon :size="18" aria-hidden="true"><Download /></el-icon>
          <span class="m-settle__action-text">导出结算数据</span>
        </button>
      </div>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Finished, Clock, MoreFilled, RefreshLeft, Download } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import { useStudentStore } from '@/stores/student'
import { invoke } from '@/services/tauri'
import { exportToExcel, type ExcelColumn } from '@/utils/excelHelper'
import type { SettlementRecord } from '@/types'

const studentStore = useStudentStore()
const ranking = ref<Array<{ id: string; rank: number; name: string; score: number }>>([])
const settlements = ref<SettlementRecord[]>([])
const showActionSheet = ref(false)

onMounted(async () => {
  await studentStore.fetchStudents()
  ranking.value = studentStore.students
    .map(s => ({ id: s.id, rank: 0, name: s.name, score: s.score }))
    .sort((a, b) => b.score - a.score)
    .map((s, i) => ({ ...s, rank: i + 1 }))
  await fetchSettlements()
})

async function fetchSettlements() {
  try {
    const records = await invoke<SettlementRecord[]>('settlement_list', {})
    settlements.value = records || []
  } catch { /* ignore */ }
}

const totalScore = computed(() => ranking.value.reduce((sum, r) => sum + r.score, 0))
const topPlus = computed(() => ranking.value[0]?.score || 0)
const topPlusName = computed(() => ranking.value[0]?.name || '—')
const topMinus = computed(() => Math.abs(ranking.value[ranking.value.length - 1]?.score || 0))
const topMinusName = computed(() => ranking.value[ranking.value.length - 1]?.name || '—')

const latestRevertable = computed(() => {
  return settlements.value.find(r => !r.isReverted && r.status !== 2) || null
})

const settlementTimeFormatter = new Intl.DateTimeFormat('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false })

function formatTime(dateStr: string): string {
  if (!dateStr) return '—'
  return settlementTimeFormatter.format(new Date(dateStr))
}

async function handleSettle() {
  try {
    await ElMessageBox.confirm(
      '结算后所有学生积分将被重置为 0，此操作不可自动恢复。确定继续？',
      '确认结算',
      { type: 'warning', confirmButtonText: '确认结算', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  try {
    await invoke('settlement_create', {
      input: {
        name: '积分结算',
        period: new Date().toISOString().slice(0, 10),
      },
    })
    ElMessage.success('结算完成')
    await fetchSettlements()
    await studentStore.fetchStudents()
  } catch { /* ignore */ }
}

async function handleRevertLatest() {
  const record = latestRevertable.value
  if (!record) {
    ElMessage.warning('没有可撤销的结算')
    return
  }
  try {
    await ElMessageBox.confirm('撤销最近一次结算将恢复所有学生积分。确定？', '确认撤销', { type: 'warning' })
  } catch {
    return
  }
  try {
    await invoke('settlement_rollback', { id: Number(record.id) })
    ElMessage.success('已撤销结算')
    showActionSheet.value = false
    await fetchSettlements()
    await studentStore.fetchStudents()
  } catch { /* ignore */ }
}

function handleExportSettlement() {
  if (settlements.value.length === 0) {
    ElMessage.warning('暂无结算数据')
    return
  }

  const rows = settlements.value.map(r => ({
    settlementTime: formatTime(r.settledAt || r.createdAt || ''),
    name: r.name || '积分结算',
    period: r.period || '',
    studentCount: r.studentCount ?? '',
    totalScore: r.totalScore ?? '',
    status: r.isReverted || r.status === 2 ? '已撤销' : '有效',
  }))

  const columns: ExcelColumn[] = [
    { header: '结算时间', key: 'settlementTime' },
    { header: '名称', key: 'name' },
    { header: '周期', key: 'period' },
    { header: '参与学生数', key: 'studentCount' },
    { header: '总积分', key: 'totalScore' },
    { header: '状态', key: 'status' },
  ]

  const filename = `结算数据_${new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())}`

  exportToExcel(rows, columns, filename)
  ElMessage.success(`已导出 ${rows.length} 条结算记录`)
  showActionSheet.value = false
}
</script>

<style scoped>
.m-settle { display: flex; flex-direction: column; gap: 16px; padding-bottom: 96px; }
.m-settle__head { display: flex; align-items: flex-end; justify-content: space-between; gap: 12px; }
.m-settle__head-text { display: flex; flex-direction: column; gap: 2px; }
.m-settle__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-settle__settle-btn { display: inline-flex; align-items: center; gap: 4px; height: 36px; padding: 0 12px; border: 1px solid var(--cis-accent); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-accent); font-size: 13px; font-weight: 500; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; flex-shrink: 0; }
.m-settle__settle-btn:active { transform: scale(var(--cis-press-scale)); background: var(--cis-accent-tint); }
.m-settle__settle-btn:disabled { opacity: 0.5; cursor: not-allowed; }
.m-settle__kpi { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-border); overflow: hidden; }
.m-settle__kpi-cell { display: flex; flex-direction: column; gap: 4px; padding: 14px 10px; background: var(--cis-surface-1); }
.m-settle__kpi-cell .cis-eyebrow { color: var(--cis-text-tertiary); }
.m-settle__kpi-num { font-family: var(--cis-font-mono); font-size: 20px; font-weight: 700; font-variant-numeric: tabular-nums; line-height: 1; }
.m-settle__kpi-num.is-plus { color: var(--cis-success); }
.m-settle__kpi-num.is-minus { color: var(--cis-accent); }
.m-settle__kpi-label { font-size: 11px; color: var(--cis-text-tertiary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-settle__history { display: flex; flex-direction: column; gap: 8px; }
.m-settle__history-title { margin: 0; color: var(--cis-text-tertiary); }
.m-settle__history-list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-settle__history-row { display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 10px 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); }
.m-settle__history-list li:last-child .m-settle__history-row { border-bottom: none; }
.m-settle__history-time { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; color: var(--cis-text-secondary); }
.m-settle__history-meta { display: inline-flex; align-items: center; gap: 8px; font-size: 13px; color: var(--cis-text-secondary); }
.m-settle__history-tag { font-size: 11px; color: var(--cis-text-tertiary); padding: 1px 6px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); }
.m-settle__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-settle__row { display: flex; align-items: center; gap: 12px; min-height: 48px; padding: 0 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); }
.m-settle__row:last-child { border-bottom: none; }
.m-settle__row::before { content: ''; width: 4px; height: 16px; background: var(--cis-text-tertiary); border-radius: 2px; flex-shrink: 0; }
.m-settle__rank { font-family: var(--cis-font-mono); font-size: 13px; font-weight: 600; color: var(--cis-text-tertiary); min-width: 24px; }
.m-settle__name { flex: 1; font-size: 14px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-settle__score { font-size: 15px; font-weight: 700; min-width: 56px; text-align: right; font-variant-numeric: tabular-nums; }
.m-settle__score.is-plus { color: var(--cis-success); }
.m-settle__score.is-minus { color: var(--cis-accent); }

.m-settle__fab { position: fixed; right: 16px; bottom: calc(80px + env(safe-area-inset-bottom, 0)); z-index: 100; width: 56px; height: 56px; border: none; border-radius: 9999px; background: var(--cis-primary); color: #fff; display: flex; align-items: center; justify-content: center; cursor: pointer; box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3); -webkit-tap-highlight-color: transparent; }
.m-settle__fab:active { transform: scale(var(--cis-press-scale-strong)); }

.m-settle__actions { display: flex; flex-direction: column; gap: 8px; }
.m-settle__action { display: flex; align-items: center; gap: 12px; min-height: 48px; padding: 12px 16px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); color: var(--cis-text-primary); font-size: 14px; font-weight: 500; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; text-align: left; }
.m-settle__action:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-settle__action:disabled { opacity: 0.5; cursor: not-allowed; }
.m-settle__action-text { flex: 1; }
</style>
