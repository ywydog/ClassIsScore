<template>
  <div class="m-profile">
    <header class="m-profile__head">
      <button type="button" class="m-profile__back" aria-label="返回学生列表" @click="$router.back()">
        <el-icon :size="20" aria-hidden="true"><ArrowLeft /></el-icon>
      </button>
      <h1 class="cis-display m-profile__name">{{ student?.name || '学生详情' }}</h1>
    </header>

    <section v-if="student" class="m-profile__hero cis-hairline">
      <div class="m-profile__hero-top">
        <button
          type="button"
          class="m-profile__hero-edit"
          aria-label="编辑学生"
          @click="openEditDialog"
        >
          <el-icon :size="16" aria-hidden="true"><Edit /></el-icon>
        </button>
      </div>
      <div class="m-profile__avatar" aria-hidden="true">
        <span class="m-profile__avatar-text">{{ student.name.slice(0, 1) }}</span>
      </div>
      <span class="m-profile__hero-name">{{ student.name }}</span>
      <span class="m-profile__hero-id cis-mono">
        {{ student.studentNumber || '—' }} · {{ groupName(student.groupId) || '未分组' }}
      </span>
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

    <button
      type="button"
      class="m-profile__delete"
      aria-label="删除学生"
      @click="handleDelete"
    >
      <el-icon :size="16" aria-hidden="true"><Delete /></el-icon>
      <span>删除学生</span>
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

    <el-dialog
      v-model="editOpen"
      title="编辑学生"
      width="320px"
      :close-on-click-modal="false"
      destroy-on-close
    >
      <el-form label-position="top" @submit.prevent="handleEditSubmit">
        <el-form-item label="姓名" required>
          <el-input
            v-model="form.name"
            placeholder="请输入学生姓名…"
            maxlength="20"
            show-word-limit
            autocomplete="off"
            aria-label="姓名"
          />
        </el-form-item>
        <el-form-item label="学号">
          <el-input
            v-model="form.studentNumber"
            placeholder="例：20240001（选填）"
            maxlength="20"
            autocomplete="off"
            aria-label="学号"
          />
        </el-form-item>
        <el-form-item label="分组">
          <el-select
            v-model="form.groupId"
            placeholder="未分组"
            clearable
            class="m-profile__group-select"
            aria-label="所属分组"
          >
            <el-option
              v-for="g in groups"
              :key="g.id"
              :label="g.name"
              :value="g.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="m-profile__dialog-actions">
          <el-button @click="editOpen = false">取消</el-button>
          <el-button
            type="primary"
            :loading="submitting"
            :disabled="!form.name.trim()"
            @click="handleEditSubmit"
          >保存</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, Edit, Delete } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { useStudentStore } from '@/stores/student'
import { useScoreStore } from '@/stores/score'
import { invoke } from '@/services/tauri'
import { groupApi } from '@/services/group'
import type { EvaluationItem, StudentGroup } from '@/types'

const route = useRoute()
const router = useRouter()
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
const editOpen = ref(false)
const submitting = ref(false)
const groups = ref<StudentGroup[]>([])
const form = reactive({ name: '', studentNumber: '', groupId: '' })

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

function groupName(groupId?: string): string {
  if (!groupId) return ''
  return groups.value.find(g => g.id === groupId)?.name || ''
}

function applyEvaluationItem(item: EvaluationItem) {
  if (!student.value) return
  const value = Math.abs(item.scoreChange)
  scoreStore.addScore(student.value.id, item.isPositive ? value : -value, item.name)
    .then(() => ElMessage.success('已记录'))
    .catch(() => ElMessage.error('记录失败'))
}

function openEditDialog() {
  if (!student.value) return
  form.name = student.value.name
  form.studentNumber = student.value.studentNumber || ''
  form.groupId = student.value.groupId || ''
  editOpen.value = true
}

async function handleEditSubmit() {
  if (!student.value) return
  const name = form.name.trim()
  if (!name) {
    ElMessage.warning('请输入学生姓名')
    return
  }
  submitting.value = true
  try {
    await studentStore.updateStudent(student.value.id, {
      name,
      studentNumber: form.studentNumber.trim() || undefined,
      groupId: form.groupId || undefined,
    })
    ElMessage.success('已保存')
    editOpen.value = false
  } catch { /* store handled */ }
  finally {
    submitting.value = false
  }
}

async function handleDelete() {
  if (!student.value) return
  try {
    await ElMessageBox.confirm(
      `确定删除学生"${student.value.name}"吗？此操作不可恢复。`,
      '确认删除',
      { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  try {
    await studentStore.deleteStudent(student.value.id)
    ElMessage.success('已删除')
    router.replace('/m/students')
  } catch { /* store handled */ }
}

onMounted(async () => {
  await Promise.all([
    studentStore.fetchStudents(),
    scoreStore.fetchRecords(),
    fetchEvaluationItems(),
    fetchGroups(),
  ])
})

async function fetchEvaluationItems() {
  try { evaluationItems.value = await invoke<EvaluationItem[]>('evaluation_list', {}) }
  catch { evaluationItems.value = [] }
}

async function fetchGroups() {
  try {
    const res = await groupApi.getAll()
    groups.value = res.data.data
  } catch { groups.value = [] }
}
</script>

<style scoped>
.m-profile { display: flex; flex-direction: column; gap: 16px; padding-bottom: 120px; }
.m-profile__head { display: flex; align-items: center; gap: 4px; }
.m-profile__back { display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px; border: none; border-radius: var(--cis-radius-btn); background: transparent; color: var(--cis-text-secondary); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; }
.m-profile__back:active { background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-profile__name { font-size: 20px; margin: 0; font-weight: 600; }
.m-profile__hero { position: relative; display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 24px 16px; background: var(--cis-surface-1); border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); text-align: center; }
.m-profile__hero-top { position: absolute; top: 8px; right: 8px; }
.m-profile__hero-edit { display: inline-flex; align-items: center; justify-content: center; width: 36px; height: 36px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; }
.m-profile__hero-edit:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); color: var(--cis-primary); }
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
.m-profile__cta { position: fixed; right: 16px; left: 16px; bottom: calc(132px + env(safe-area-inset-bottom, 0)); z-index: 100; height: 48px; border: none; border-radius: var(--cis-radius-btn); background: var(--cis-primary); color: #fff; font-size: 15px; font-weight: 600; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3); }
.m-profile__cta:active { transform: scale(var(--cis-press-scale-strong)); }
.m-profile__delete { position: fixed; right: 16px; left: 16px; bottom: calc(76px + env(safe-area-inset-bottom, 0)); z-index: 100; height: 44px; border: 1px solid var(--cis-accent); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-accent); font-size: 14px; font-weight: 500; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; display: inline-flex; align-items: center; justify-content: center; gap: 4px; }
.m-profile__delete:active { transform: scale(var(--cis-press-scale)); background: var(--cis-accent-tint); }
.m-profile__sheet-hint { margin: 0 0 12px; font-size: 13px; color: var(--cis-text-tertiary); }
.m-profile__sheet-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-profile__sheet-tile { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px; min-height: 72px; padding: 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); background: var(--cis-surface-1); font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-profile__sheet-tile:active { transform: scale(var(--cis-press-scale)); }
.m-profile__sheet-tile.is-plus { border-color: rgba(21, 128, 61, 0.4); background: var(--cis-success-tint); color: var(--cis-success); }
.m-profile__sheet-tile.is-minus { border-color: rgba(185, 28, 28, 0.4); background: var(--cis-accent-tint); color: var(--cis-accent); }
.m-profile__sheet-tile-name { font-size: 13px; font-weight: 600; }
.m-profile__sheet-tile-value { font-size: 18px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-profile__dialog-actions { display: flex; gap: 8px; justify-content: flex-end; width: 100%; }
.m-profile__group-select { width: 100%; }
</style>
