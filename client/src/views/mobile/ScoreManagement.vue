<template>
  <div class="m-score">
    <header class="m-score__head">
      <div class="m-score__head-text">
        <span class="cis-eyebrow">Score</span>
        <h1 class="cis-display m-score__title">积分</h1>
      </div>
      <button
        type="button"
        class="m-score__export-btn"
        aria-label="导出报表"
        @click="handleExportReport"
      >
        <el-icon :size="16" aria-hidden="true"><Download /></el-icon>
        <span>导出报表</span>
      </button>
    </header>

    <section class="m-score__form cis-hairline">
      <div class="m-score__form-row">
        <el-select
          v-model="addForm.studentId"
          placeholder="选择学生"
          filterable
          class="m-score__select"
          aria-label="选择学生"
        >
          <el-option v-for="s in studentStore.students" :key="s.id" :label="s.name" :value="s.id" />
        </el-select>
      </div>
      <div class="m-score__form-row m-score__form-row--split">
        <el-input-number
          v-model="addForm.scoreChange"
          :step="1"
          :min="-100"
          :max="100"
          controls-position="right"
          class="m-score__number"
          inputmode="numeric"
          aria-label="分值变化"
        />
        <el-input
          v-model="addForm.reason"
          placeholder="原因…"
          maxlength="50"
          class="m-score__reason"
          aria-label="原因"
          autocomplete="off"
          @keyup.enter="handleAdd"
        />
      </div>
      <div class="m-score__form-row m-score__form-row--actions">
        <el-button type="primary" class="m-score__btn" :loading="scoreStore.loading" @click="handleAdd">
          加分
        </el-button>
        <el-button type="danger" plain class="m-score__btn" :loading="scoreStore.loading" @click="handleSubtract">
          减分
        </el-button>
      </div>
    </section>

    <section v-if="evaluationItems.length > 0" class="m-score__quick">
      <h2 class="cis-eyebrow m-score__quick-title">评价项</h2>
      <div class="m-score__quick-scroll">
        <button
          v-for="item in evaluationItems"
          :key="item.id"
          type="button"
          class="m-score__chip"
          :class="item.isPositive ? 'is-plus' : 'is-minus'"
          :aria-label="`应用 ${item.name}，${item.isPositive ? '+' : ''}${item.scoreChange}`"
          @click="applyEvaluationItem(item)"
        >
          <span class="m-score__chip-name">{{ item.name }}</span>
          <span class="m-score__chip-value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
        </button>
      </div>
    </section>

    <section class="m-score__today">
      <h2 class="cis-eyebrow m-score__today-title">今日记录</h2>
      <ol v-if="todayRecords.length > 0" class="m-score__today-list" role="list">
        <li v-for="r in todayRecords" :key="r.id" class="m-score__today-row">
          <span class="m-score__today-name">{{ r.studentName || '—' }}</span>
          <span class="m-score__today-score cis-num" :class="r.scoreChange >= 0 ? 'is-plus' : 'is-minus'">
            {{ r.scoreChange > 0 ? '+' : '' }}{{ r.scoreChange }}
          </span>
          <span class="m-score__today-reason">{{ r.reason || '—' }}</span>
          <span class="m-score__today-time cis-mono">{{ formatTime(r.createdAt) }}</span>
        </li>
      </ol>
      <MobileEmptyState v-else eyebrow="Empty" description="今日还没有积分记录" />
    </section>

    <button
      type="button"
      class="m-score__fab-batch"
      aria-label="批量加分"
      @click="showBatchSheet = true"
    >
      <el-icon :size="20" aria-hidden="true"><Operation /></el-icon>
    </button>
    <button
      type="button"
      class="m-score__fab"
      aria-label="加积分"
      @click="showSheet = true"
    >
      <el-icon :size="22" aria-hidden="true"><Plus /></el-icon>
    </button>

    <BottomSheet v-model="showSheet" title="加积分" height="half">
      <p class="m-score__sheet-hint">从评价项快速选择，或在顶部表单自定义。</p>
      <div v-if="evaluationItems.length > 0" class="m-score__sheet-grid">
        <button
          v-for="item in evaluationItems"
          :key="`sheet-${item.id}`"
          type="button"
          class="m-score__sheet-tile"
          :class="item.isPositive ? 'is-plus' : 'is-minus'"
          @click="applyEvaluationItem(item); showSheet = false"
        >
          <span class="m-score__sheet-tile-name">{{ item.name }}</span>
          <span class="m-score__sheet-tile-value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
        </button>
      </div>
      <MobileEmptyState v-else eyebrow="Empty" description="暂无评价项" />
    </BottomSheet>

    <BottomSheet v-model="showBatchSheet" title="批量加分" height="half">
      <el-form label-position="top" class="m-score__batch-form">
        <el-form-item label="选择学生（可多选）">
          <el-select
            v-model="batchForm.studentIds"
            multiple
            filterable
            collapse-tags
            collapse-tags-tooltip
            placeholder="选择学生…"
            class="m-score__batch-select"
            aria-label="选择学生"
          >
            <el-option
              v-for="s in studentStore.students"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="或选择小组（自动勾选组内学生）">
          <el-select
            v-model="batchForm.groupId"
            placeholder="选择小组…"
            clearable
            class="m-score__batch-group"
            aria-label="选择小组"
            @change="handleGroupSelect"
          >
            <el-option
              v-for="g in groups"
              :key="g.id"
              :label="g.name"
              :value="g.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="分值" required>
          <el-input-number
            v-model="batchForm.scoreChange"
            :step="1"
            :min="-100"
            :max="100"
            controls-position="right"
            class="m-score__batch-number"
            inputmode="numeric"
            aria-label="分值"
          />
        </el-form-item>
        <el-form-item label="原因" required>
          <el-input
            v-model="batchForm.reason"
            placeholder="请输入原因…"
            maxlength="50"
            show-word-limit
            class="m-score__batch-reason"
            aria-label="原因"
            autocomplete="off"
          />
        </el-form-item>
      </el-form>
      <div class="m-score__batch-actions">
        <el-button @click="showBatchSheet = false">取消</el-button>
        <el-button
          type="primary"
          :loading="scoreStore.loading"
          :disabled="batchForm.studentIds.length === 0 || !batchForm.reason.trim()"
          @click="handleBatchScore"
        >确定 ({{ batchForm.studentIds.length }}人)</el-button>
      </div>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Plus, Download, Operation } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { invoke } from '@/services/tauri'
import { groupApi } from '@/services/group'
import type { EvaluationItem, StudentGroup } from '@/types'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()
const showSheet = ref(false)
const showBatchSheet = ref(false)
const groups = ref<StudentGroup[]>([])

const addForm = reactive({ studentId: '', scoreChange: 1, reason: '' })
const batchForm = reactive({ studentIds: [] as string[], groupId: '', scoreChange: 1, reason: '' })
const evaluationItems = ref<EvaluationItem[]>([])

const todayRecords = computed(() => {
  const today = new Date().toISOString().slice(0, 10)
  return scoreStore.recentRecords.filter(r => r.createdAt?.slice(0, 10) === today)
})

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
    fetchEvaluationItems(),
    fetchGroups(),
  ])
})

async function fetchEvaluationItems() {
  try {
    evaluationItems.value = await invoke<EvaluationItem[]>('evaluation_list', {})
  } catch {
    evaluationItems.value = []
  }
}

async function fetchGroups() {
  try {
    const res = await groupApi.getAll()
    groups.value = res.data.data
  } catch {
    groups.value = []
  }
}

function applyEvaluationItem(item: EvaluationItem) {
  addForm.scoreChange = Math.abs(item.scoreChange)
  addForm.reason = item.name
}

async function handleAdd() {
  if (!addForm.studentId || !addForm.reason || addForm.scoreChange <= 0) {
    ElMessage.warning('请填写学生、分值和原因')
    return
  }
  try {
    await scoreStore.addScore(addForm.studentId, addForm.scoreChange, addForm.reason)
    ElMessage.success('已加分')
    addForm.reason = ''
  } catch { /* store handled */ }
}

async function handleSubtract() {
  if (!addForm.studentId || !addForm.reason || addForm.scoreChange <= 0) {
    ElMessage.warning('请填写学生、分值和原因')
    return
  }
  try {
    await scoreStore.addScore(addForm.studentId, -addForm.scoreChange, addForm.reason)
    ElMessage.success('已减分')
    addForm.reason = ''
  } catch { /* store handled */ }
}

function handleGroupSelect(groupId: string) {
  if (!groupId) {
    return
  }
  const group = groups.value.find(g => g.id === groupId)
  if (group) {
    batchForm.studentIds = [...group.studentIds]
  }
}

async function handleBatchScore() {
  if (batchForm.studentIds.length === 0 || !batchForm.reason.trim()) {
    ElMessage.warning('请选择学生并填写原因')
    return
  }
  try {
    await scoreStore.batchAddScore(batchForm.studentIds, batchForm.scoreChange, batchForm.reason.trim())
    ElMessage.success(`已为 ${batchForm.studentIds.length} 名学生加分`)
    showBatchSheet.value = false
    batchForm.studentIds = []
    batchForm.groupId = ''
    batchForm.scoreChange = 1
    batchForm.reason = ''
  } catch { /* store handled */ }
}

function handleExportReport() {
  ElMessage.warning('报表导出请前往桌面端')
}
</script>

<style scoped>
.m-score { display: flex; flex-direction: column; gap: 20px; padding-bottom: 96px; }
.m-score__head { display: flex; align-items: flex-end; justify-content: space-between; gap: 12px; }
.m-score__head-text { display: flex; flex-direction: column; gap: 2px; }
.m-score__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-score__export-btn { display: inline-flex; align-items: center; gap: 4px; height: 36px; padding: 0 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); font-size: 13px; font-weight: 500; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; flex-shrink: 0; }
.m-score__export-btn:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-score__form { display: flex; flex-direction: column; gap: 10px; padding: 16px; background: var(--cis-surface-1); border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); }
.m-score__form-row { display: flex; gap: 8px; }
.m-score__form-row--split > * { flex: 1; }
.m-score__form-row--actions { gap: 10px; }
.m-score__form-row--actions .m-score__btn { flex: 1; height: 44px; }
.m-score__select, .m-score__number, .m-score__reason { width: 100%; }
.m-score__quick-title, .m-score__today-title { margin: 0 0 8px; color: var(--cis-text-tertiary); }
.m-score__quick-scroll { display: flex; gap: 8px; overflow-x: auto; -webkit-overflow-scrolling: touch; padding: 2px; scrollbar-width: none; }
.m-score__quick-scroll::-webkit-scrollbar { display: none; }
.m-score__chip { display: inline-flex; align-items: center; gap: 6px; min-height: 36px; padding: 0 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); font-size: 13px; font-weight: 500; font-family: inherit; cursor: pointer; flex-shrink: 0; transition: border-color var(--cis-transition-fast), color var(--cis-transition-fast); }
.m-score__chip:active { transform: scale(var(--cis-press-scale)); }
.m-score__chip.is-plus { color: var(--cis-success); border-color: rgba(21, 128, 61, 0.3); }
.m-score__chip.is-minus { color: var(--cis-accent); border-color: rgba(185, 28, 28, 0.3); }
.m-score__chip-value { font-weight: 700; }
.m-score__today-list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-score__today-row { display: grid; grid-template-columns: 1fr auto; grid-template-areas: 'name score' 'reason time'; gap: 2px 8px; padding: 10px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-score__today-row:last-child { border-bottom: none; }
.m-score__today-name { grid-area: name; font-size: 14px; font-weight: 500; }
.m-score__today-score { grid-area: score; font-size: 14px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-score__today-score.is-plus { color: var(--cis-success); }
.m-score__today-score.is-minus { color: var(--cis-accent); }
.m-score__today-reason { grid-area: reason; font-size: 12px; color: var(--cis-text-tertiary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-score__today-time { grid-area: time; font-size: 11px; color: var(--cis-text-tertiary); }

.m-score__fab, .m-score__fab-batch { position: fixed; right: 16px; z-index: 100; border: none; border-radius: 9999px; display: flex; align-items: center; justify-content: center; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-score__fab { bottom: calc(80px + env(safe-area-inset-bottom, 0)); width: 56px; height: 56px; background: var(--cis-primary); color: #fff; box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3); }
.m-score__fab:active { transform: scale(var(--cis-press-scale-strong)); }
.m-score__fab-batch { bottom: calc(148px + env(safe-area-inset-bottom, 0)); width: 44px; height: 44px; background: var(--cis-surface-1); color: var(--cis-primary); border: 1px solid var(--cis-border); }
.m-score__fab-batch:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); }

.m-score__sheet-hint { margin: 0 0 12px; font-size: 13px; color: var(--cis-text-tertiary); }
.m-score__sheet-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-score__sheet-tile { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px; min-height: 72px; padding: 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-score__sheet-tile:active { transform: scale(var(--cis-press-scale)); }
.m-score__sheet-tile.is-plus { border-color: rgba(21, 128, 61, 0.4); background: var(--cis-success-tint); color: var(--cis-success); }
.m-score__sheet-tile.is-minus { border-color: rgba(185, 28, 28, 0.4); background: var(--cis-accent-tint); color: var(--cis-accent); }
.m-score__sheet-tile-name { font-size: 13px; font-weight: 600; }
.m-score__sheet-tile-value { font-size: 18px; font-weight: 700; font-variant-numeric: tabular-nums; }

.m-score__batch-form { display: flex; flex-direction: column; gap: 4px; }
.m-score__batch-select, .m-score__batch-group, .m-score__batch-number, .m-score__batch-reason { width: 100%; }
.m-score__batch-actions { display: flex; gap: 8px; justify-content: flex-end; margin-top: 12px; }
</style>
