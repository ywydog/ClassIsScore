<template>
  <div class="score-display" :class="{ 'score-display--multi-select': multiSelectMode }">
    <!-- 顶栏 -->
    <header class="score-display__topbar">
      <div class="score-display__brand">
        <div class="score-display__brand-icon">
          <el-icon><Trophy /></el-icon>
        </div>
        <div>
          <h1 class="score-display__title">{{ t('leaderboard') }}</h1>
          <p class="score-display__subtitle">{{ currentDate }} · {{ currentTime }}</p>
        </div>
      </div>

      <div class="score-display__topbar-actions">
        <!-- 多选模式指示器 -->
        <div v-if="multiSelectMode" class="score-display__multi-bar">
          <span class="score-display__multi-count">已选 {{ selectedStudentIds.length }}</span>
          <button class="score-display__mini-btn" @click="selectAll">全选</button>
          <button class="score-display__mini-btn" @click="clearSelection">清空</button>
          <button class="score-display__mini-btn score-display__mini-btn--primary" :disabled="!selectedStudentIds.length" @click="openBatchScore">批量评分</button>
          <button class="score-display__mini-btn" @click="toggleMultiSelect">退出多选</button>
        </div>
        <template v-else>
          <button class="score-display__icon-btn" :title="t('multiSelect')" @click="toggleMultiSelect">
            <el-icon><Check /></el-icon>
          </button>
          <button class="score-display__icon-btn" :title="t('settings')" @click="openSettings">
            <el-icon><Setting /></el-icon>
          </button>
          <button class="score-display__icon-btn score-display__icon-btn--close" title="关闭" @click="closeDisplay">
            <el-icon><Close /></el-icon>
          </button>
        </template>
      </div>
    </header>

    <!-- 主体 -->
    <main class="score-display__main">
      <!-- 领奖台 -->
      <section v-if="topThree.length >= 3" class="score-display__podium">
        <div
          v-for="(entry, idx) in [topThree[1], topThree[0], topThree[2]]"
          :key="entry.id"
          class="score-display__podium-item"
          :class="`score-display__podium-item--${idx}`"
          :style="{ animationDelay: `${idx * 80}ms` }"
        >
          <div class="score-display__podium-medal">
            <el-icon v-if="idx === 1"><Trophy /></el-icon>
            <span v-else>{{ idx === 0 ? '🥈' : '🥉' }}</span>
          </div>
          <div class="score-display__podium-avatar" :style="{ background: avatarColor(entry.id) }">
            <span>{{ nameInitial(entry.name) }}</span>
          </div>
          <div class="score-display__podium-name">{{ entry.name }}</div>
          <div class="score-display__podium-score">{{ entry.score }}</div>
        </div>
      </section>

      <!-- 卡片网格 -->
      <section class="score-display__grid-wrap">
        <transition-group name="card" tag="div" class="score-display__grid">
          <div
            v-for="entry in restEntries"
            :key="entry.id"
            class="score-display__card"
            :class="{
              'score-display__card--selected': isSelected(entry.id),
              'score-display__card--expanded': expandedStudentId === entry.id,
            }"
            :style="{ animationDelay: `${Math.min(entry.rank, 30) * 30}ms` }"
            @click="onCardClick(entry)"
          >
            <!-- 卡片默认视图 -->
            <template v-if="expandedStudentId !== entry.id">
              <div class="score-display__card-rank">#{{ entry.rank }}</div>
              <div class="score-display__card-avatar" :style="{ background: avatarColor(entry.id) }">
                <span>{{ nameInitial(entry.name) }}</span>
              </div>
              <div class="score-display__card-name">{{ entry.name }}</div>
              <div class="score-display__card-score">{{ entry.score }}</div>
              <transition name="score-pop">
                <div
                  v-for="anim in activeScoreAnimations(entry.id)"
                  :key="anim.id"
                  class="score-display__score-anim"
                  :class="anim.change > 0 ? 'is-plus' : 'is-minus'"
                >
                  {{ anim.change > 0 ? '+' : '' }}{{ anim.change }}
                </div>
              </transition>
            </template>

            <!-- 卡片展开视图：快速评分 -->
            <template v-else>
              <div class="score-display__card-expanded">
                <div class="score-display__card-expanded-header">
                  <button class="score-display__back-btn" @click.stop="closeQuickScore" title="返回">
                    <el-icon><ArrowLeft /></el-icon>
                  </button>
                  <div class="score-display__card-avatar score-display__card-avatar--lg" :style="{ background: avatarColor(entry.id) }">
                    <span>{{ nameInitial(entry.name) }}</span>
                  </div>
                  <div class="score-display__card-expanded-name">{{ entry.name }}</div>
                  <div class="score-display__card-expanded-score">{{ entry.score }}</div>
                </div>

                <div class="score-display__quick-items">
                  <button
                    v-for="item in positiveEvalItems.slice(0, 4)"
                    :key="`p-${item.id}`"
                    class="score-display__quick-item score-display__quick-item--plus"
                    @click.stop="quickScore(entry, item.scoreChange, item.name)"
                  >
                    <span class="score-display__quick-item-value">+{{ item.scoreChange }}</span>
                    <span class="score-display__quick-item-name">{{ item.name }}</span>
                  </button>
                  <button
                    v-for="item in negativeEvalItems.slice(0, 4)"
                    :key="`n-${item.id}`"
                    class="score-display__quick-item score-display__quick-item--minus"
                    @click.stop="quickScore(entry, item.scoreChange, item.name)"
                  >
                    <span class="score-display__quick-item-value">{{ item.scoreChange }}</span>
                    <span class="score-display__quick-item-name">{{ item.name }}</span>
                  </button>
                </div>

                <div class="score-display__custom-score" @click.stop>
                  <button class="score-display__step-btn" @click="bumpCustom(-1)" title="减 1">-1</button>
                  <button class="score-display__step-btn score-display__step-btn--lg" @click="bumpCustom(-5)" title="减 5">-5</button>
                  <input
                    v-model.number="customScoreChange"
                    type="number"
                    class="score-display__custom-input"
                    placeholder="0"
                    @keyup.enter="quickScore(entry)"
                  />
                  <button class="score-display__step-btn score-display__step-btn--lg" @click="bumpCustom(5)" title="加 5">+5</button>
                  <button class="score-display__step-btn" @click="bumpCustom(1)" title="加 1">+1</button>
                  <input
                    v-model="customScoreReason"
                    type="text"
                    class="score-display__custom-reason"
                    :placeholder="t('reasonPlaceholder')"
                    maxlength="20"
                  />
                  <button
                    class="score-display__confirm-btn"
                    :disabled="!customScoreChange"
                    @click="quickScore(entry)"
                  >
                    {{ t('confirmScore') }}
                  </button>
                </div>
              </div>
            </template>
          </div>
        </transition-group>

        <div v-if="!leaderboard.length" class="score-display__empty">
          <p>{{ t('noLeaderboardData') }}</p>
        </div>
      </section>
    </main>

    <!-- 底部抽屉 -->
    <footer class="score-display__drawer" :class="{ 'is-open': drawerOpen }">
      <button class="score-display__drawer-handle" @click="drawerOpen = !drawerOpen">
        <el-icon><component :is="drawerOpen ? ArrowDown : ArrowUp" /></el-icon>
        <span>{{ t('periodStats') }}</span>
      </button>
      <transition name="drawer">
        <div v-show="drawerOpen" class="score-display__drawer-body">
          <div class="score-display__drawer-tabs">
            <button
              v-for="opt in periodOptions"
              :key="opt.key"
              class="score-display__drawer-tab"
              :class="{ 'is-active': activePeriod === opt.key }"
              @click="activePeriod = opt.key"
            >
              {{ opt.label }}
            </button>
          </div>
          <div class="score-display__drawer-list">
            <div
              v-for="(row, idx) in topPeriodStats"
              :key="row.studentId"
              class="score-display__drawer-row"
            >
              <span class="score-display__drawer-row-rank">#{{ idx + 1 }}</span>
              <span class="score-display__drawer-row-name">{{ row.studentName }}</span>
              <span class="score-display__drawer-row-net" :class="row.net >= 0 ? 'is-plus' : 'is-minus'">
                {{ row.net >= 0 ? '+' : '' }}{{ row.net }}
              </span>
              <span class="score-display__drawer-row-detail">
                +{{ row.plus }} / {{ row.minus }}
              </span>
            </div>
          </div>
        </div>
      </transition>
    </footer>

    <!-- 设置弹层 -->
    <el-dialog
      v-model="showAdvanced"
      :title="t('advancedSettings')"
      width="520"
      align-center
      :show-close="true"
      class="score-display__advanced-dialog"
    >
      <DisplaySettingsPanel
        :sort-by="sortBy"
        :privacy="privacy"
        :show-pet="showPet"
        :show-group="showGroup"
        :show-trend="showTrend"
        :refresh-interval="refreshInterval"
        @update:sort-by="sortBy = $event"
        @update:privacy="privacy = $event"
        @update:show-pet="showPet = $event"
        @update:show-group="showGroup = $event"
        @update:show-trend="showTrend = $event"
        @update:refresh-interval="refreshInterval = $event"
      />
    </el-dialog>

    <!-- 批量评分 -->
    <el-dialog
      v-model="showBatchScorePanel"
      :title="t('batchScoreAction')"
      width="420"
      align-center
    >
      <div class="score-display__batch">
        <el-input-number v-model="batchScoreChange" :min="-100" :max="100" :step="1" size="large" class="score-display__batch-input" />
        <el-input v-model="batchScoreReason" :placeholder="t('reasonPlaceholder')" maxlength="20" size="large" />
        <div class="score-display__batch-presets">
          <button
            v-for="item in evaluationItems.slice(0, 6)"
            :key="`bp-${item.id}`"
            class="score-display__quick-item"
            :class="item.isPositive ? 'score-display__quick-item--plus' : 'score-display__quick-item--minus'"
            @click="applyBatchPreset(item)"
          >
            <span class="score-display__quick-item-value">{{ item.scoreChange > 0 ? '+' : '' }}{{ item.scoreChange }}</span>
            <span class="score-display__quick-item-name">{{ item.name }}</span>
          </button>
        </div>
      </div>
      <template #footer>
        <el-button @click="showBatchScorePanel = false">取消</el-button>
        <el-button type="primary" :disabled="!batchScoreChange || !selectedStudentIds.length" @click="submitBatchScore">
          确认 ({{ selectedStudentIds.length }})
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, reactive, watch } from 'vue'
import { Trophy, Check, Close, Setting, ArrowLeft, ArrowUp, ArrowDown } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { LeaderboardEntry, Student, EvaluationItem, ScoreUpdateEvent } from '@/types'
import { leaderboardApi } from '@/services/leaderboard'
import { evaluationApi } from '@/services/evaluation'
import { Window } from '@tauri-apps/api/window'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'
import { studentApi } from '@/services/student'
import { scoreApi } from '@/services/score'
import { useTerminology } from '@/themes/xianxia/useTerminology'
import DisplaySettingsPanel from '@/components/display/DisplaySettingsPanel.vue'

const { t } = useTerminology()

// ===== 时间 =====
const currentTime = ref('')
const currentDate = ref('')
let timeTimer: ReturnType<typeof setInterval> | null = null

function updateTime() {
  const now = new Date()
  currentTime.value = now.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  currentDate.value = `${now.getMonth() + 1}月${now.getDate()}日`
}

// ===== 数据 =====
interface DisplayEntry {
  id: string
  rank: number
  name: string
  score: number
  student: Student | null
}

const leaderboard = ref<LeaderboardEntry[]>([])
const students = ref<Student[]>([])
const evaluationItems = ref<EvaluationItem[]>([])

const studentMap = computed(() => {
  const m = new Map<string, Student>()
  for (const s of students.value) m.set(s.id, s)
  return m
})

const displayEntries = computed<DisplayEntry[]>(() => {
  if (sortBy.value === 'studentNumber') {
    return [...leaderboard.value]
      .map(e => ({
        id: e.name,
        rank: 0,
        name: e.name,
        score: e.score,
        student: null,
      }))
      .sort((a, b) => a.name.localeCompare(b.name, 'zh-Hans-CN'))
      .map((e, i) => ({ ...e, rank: i + 1 }))
  }
  return leaderboard.value.map(e => ({
    id: e.name,
    rank: e.rank,
    name: e.name,
    score: e.score,
    student: studentMap.value.get(
      students.value.find(s => s.name === e.name)?.id ?? ''
    ) ?? null,
  }))
})

const topThree = computed<DisplayEntry[]>(() => displayEntries.value.slice(0, 3))
const restEntries = computed<DisplayEntry[]>(() => displayEntries.value.slice(3))

// ===== 设置 =====
const sortBy = ref<'rank' | 'studentNumber'>(
  (localStorage.getItem('displaySortBy') as 'rank' | 'studentNumber') || 'rank'
)
const privacy = ref<'name' | 'alias' | 'number'>(
  (localStorage.getItem('displayPrivacy') as 'name' | 'alias' | 'number') || 'name'
)
const showPet = ref(localStorage.getItem('displayShowPet') !== 'false')
const showGroup = ref(localStorage.getItem('displayShowGroup') !== 'false')
const showTrend = ref(localStorage.getItem('displayShowTrend') !== 'false')
const refreshInterval = ref<number>(Number(localStorage.getItem('displayRefreshInterval')) || 15)

function persistSettings() {
  localStorage.setItem('displaySortBy', sortBy.value)
  localStorage.setItem('displayPrivacy', privacy.value)
  localStorage.setItem('displayShowPet', String(showPet.value))
  localStorage.setItem('displayShowGroup', String(showGroup.value))
  localStorage.setItem('displayShowTrend', String(showTrend.value))
  localStorage.setItem('displayRefreshInterval', String(refreshInterval.value))
}

const showAdvanced = ref(false)

function openSettings() {
  showAdvanced.value = true
}

// ===== 评价项 =====
const positiveEvalItems = computed(() => evaluationItems.value.filter(i => i.isPositive))
const negativeEvalItems = computed(() => evaluationItems.value.filter(i => !i.isPositive))

// ===== 快速评分 =====
const expandedStudentId = ref<string | null>(null)
const customScoreChange = ref<number>(0)
const customScoreReason = ref('')

function bumpCustom(delta: number) {
  customScoreChange.value = (customScoreChange.value || 0) + delta
}

function onCardClick(entry: DisplayEntry) {
  if (multiSelectMode.value) {
    toggleStudentSelection(entry.id)
    return
  }
  if (expandedStudentId.value === entry.id) return
  expandedStudentId.value = entry.id
  customScoreChange.value = 0
  customScoreReason.value = ''
}

function closeQuickScore() {
  expandedStudentId.value = null
  customScoreChange.value = 0
  customScoreReason.value = ''
}

async function quickScore(entry: DisplayEntry, change?: number, reason?: string) {
  const student = entry.student
  if (!student) {
    ElMessage.warning('未找到该学生')
    return
  }
  const delta = change ?? customScoreChange.value
  if (!delta) return
  try {
    await scoreApi.addScore({
      studentId: student.id,
      scoreChange: delta,
      reason: (reason ?? customScoreReason.value) || (delta > 0 ? `${t('addScore')}${delta}` : `${t('subtractScore')}${Math.abs(delta)}`),
    })
    ElMessage.success(`${entry.name} ${delta > 0 ? '+' : ''}${delta}`)
    addScoreAnimation(entry.id, delta)
    closeQuickScore()
    await fetchLeaderboard()
    await fetchStudents()
  } catch {
    // error handled by interceptor
  }
}

// ===== 多选 =====
const multiSelectMode = ref(false)
const selectedStudentIds = ref<string[]>([])

function toggleMultiSelect() {
  multiSelectMode.value = !multiSelectMode.value
  if (!multiSelectMode.value) {
    selectedStudentIds.value = []
  }
  closeQuickScore()
}

function toggleStudentSelection(id: string) {
  const idx = selectedStudentIds.value.indexOf(id)
  if (idx >= 0) {
    selectedStudentIds.value.splice(idx, 1)
  } else {
    selectedStudentIds.value.push(id)
  }
}

function isSelected(id: string) {
  return selectedStudentIds.value.includes(id)
}

function selectAll() {
  selectedStudentIds.value = displayEntries.value.map(e => e.id)
}

function clearSelection() {
  selectedStudentIds.value = []
}

// ===== 批量评分 =====
const showBatchScorePanel = ref(false)
const batchScoreChange = ref<number>(1)
const batchScoreReason = ref('')

function openBatchScore() {
  if (!selectedStudentIds.value.length) {
    ElMessage.warning(t('selectStudentFirst'))
    return
  }
  batchScoreChange.value = 1
  batchScoreReason.value = ''
  showBatchScorePanel.value = true
}

function applyBatchPreset(item: EvaluationItem) {
  batchScoreChange.value = item.scoreChange
  batchScoreReason.value = item.name
}

async function submitBatchScore() {
  const ids = selectedStudentIds.value
    .map(displayName => students.value.find(s => s.name === displayName)?.id)
    .filter((id): id is string => Boolean(id))
  if (!ids.length || !batchScoreChange.value) return
  try {
    await scoreApi.batchAddScore({
      studentIds: ids,
      scoreChange: batchScoreChange.value,
      reason: batchScoreReason.value || (batchScoreChange.value > 0 ? `批量${t('addScore')}${batchScoreChange.value}` : `批量${t('subtractScore')}${Math.abs(batchScoreChange.value)}`),
    })
    ElMessage.success(`已为 ${ids.length} 名${t('student')}评分`)
    showBatchScorePanel.value = false
    selectedStudentIds.value = []
    multiSelectMode.value = false
    await fetchLeaderboard()
    await fetchStudents()
  } catch {
    // error handled by interceptor
  }
}

// ===== 动画 =====
interface ScoreAnimation {
  id: number
  studentId: string
  change: number
}

const scoreAnimations = reactive<ScoreAnimation[]>([])
let animIdCounter = 0

function addScoreAnimation(studentId: string, change: number) {
  const id = ++animIdCounter
  scoreAnimations.push({ id, studentId, change })
  setTimeout(() => {
    const idx = scoreAnimations.findIndex(a => a.id === id)
    if (idx >= 0) scoreAnimations.splice(idx, 1)
  }, 1500)
}

function activeScoreAnimations(studentId: string) {
  return scoreAnimations.filter(a => a.studentId === studentId)
}

// ===== 周期统计 =====
type PeriodKey = 'day' | 'week' | 'month' | 'semester'
const activePeriod = ref<PeriodKey>('week')
const drawerOpen = ref(false)

const periodOptions: { key: PeriodKey; label: string }[] = [
  { key: 'day', label: '日' },
  { key: 'week', label: '周' },
  { key: 'month', label: '月' },
]

const topPeriodStats = computed<{ studentId: string; studentName: string; plus: number; minus: number; net: number }[]>(() => {
  return displayEntries.value
    .map(e => {
      // 简易占位：从总积分反推；真实统计需要 scoreStats 接口
      return {
        studentId: e.id,
        studentName: e.name,
        plus: 0,
        minus: 0,
        net: e.score,
      }
    })
    .sort((a, b) => b.net - a.net)
    .slice(0, 10)
})

// ===== 工具 =====
function nameInitial(name: string) {
  return name?.slice(0, 1) ?? '?'
}

function avatarColor(id: string) {
  // 基于 id 哈希生成稳定颜色
  const palette = [
    'linear-gradient(135deg, #0D7C5F, #3FB28F)',
    'linear-gradient(135deg, #1E40AF, #60A5FA)',
    'linear-gradient(135deg, #C2410C, #FB923C)',
    'linear-gradient(135deg, #7C3AED, #A78BFA)',
    'linear-gradient(135deg, #BE185D, #F472B6)',
    'linear-gradient(135deg, #0E7490, #67E8F9)',
    'linear-gradient(135deg, #15803D, #86EFAC)',
    'linear-gradient(135deg, #B45309, #FCD34D)',
  ]
  let hash = 0
  for (let i = 0; i < id.length; i++) {
    hash = (hash << 5) - hash + id.charCodeAt(i)
    hash |= 0
  }
  return palette[Math.abs(hash) % palette.length]
}

// ===== 关闭 =====
async function closeDisplay() {
  try {
    const win = await Window.getByLabel('display')
    if (win) {
      await win.close()
    } else {
      window.close()
    }
  } catch {
    window.close()
  }
}

// ===== 刷新 =====
let refreshTimer: ReturnType<typeof setInterval> | null = null

function restartRefreshTimer() {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
  if (refreshInterval.value > 0) {
    refreshTimer = setInterval(() => {
      fetchLeaderboard()
      fetchStudents()
    }, refreshInterval.value * 1000)
  }
}

watch(refreshInterval, () => {
  persistSettings()
  restartRefreshTimer()
})

watch([sortBy, privacy, showPet, showGroup, showTrend], () => {
  persistSettings()
})

// ===== 生命周期 =====
onMounted(async () => {
  updateTime()
  timeTimer = setInterval(updateTime, 1000)
  await Promise.all([fetchLeaderboard(), fetchStudents(), fetchEvaluationItems()])
  connectWebSocket({
    onScoreUpdate: (data: ScoreUpdateEvent) => {
      if (data.scoreChange !== undefined && data.scoreChange !== 0) {
        addScoreAnimation(data.studentName, data.scoreChange)
      }
      fetchLeaderboard()
      fetchStudents()
    },
  })
  restartRefreshTimer()
})

onUnmounted(() => {
  disconnectWebSocket()
  if (timeTimer) clearInterval(timeTimer)
  if (refreshTimer) clearInterval(refreshTimer)
})

async function fetchLeaderboard() {
  try {
    leaderboard.value = await leaderboardApi.query()
  } catch { /* silent */ }
}

async function fetchStudents() {
  try {
    const response = await studentApi.getAll()
    students.value = response.data.data
  } catch { /* silent */ }
}

async function fetchEvaluationItems() {
  try {
    const response = await evaluationApi.getAll()
    evaluationItems.value = response.data.data || []
  } catch { /* silent */ }
}
</script>

<style scoped>
.score-display {
  width: 100vw;
  height: 100vh;
  background: var(--cis-bg);
  color: var(--cis-text-primary);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  font-family: var(--cis-font-family);
  font-size: var(--cis-font-size-base);
}

/* ===== 顶栏 ===== */
.score-display__topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 24px;
  border-bottom: 1px solid var(--cis-border-color-light);
  background: var(--cis-header-bg);
  flex-shrink: 0;
  height: 60px;
}

.score-display__brand {
  display: flex;
  align-items: center;
  gap: 12px;
}

.score-display__brand-icon {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--cis-gradient-primary);
  border-radius: var(--cis-radius-md);
  color: #fff;
  font-size: 18px;
}

.score-display__title {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: var(--cis-text-primary);
  line-height: 1.2;
}

.score-display__subtitle {
  margin: 2px 0 0;
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.score-display__topbar-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.score-display__icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: none;
  border-radius: var(--cis-radius-md);
  background: transparent;
  color: var(--cis-text-secondary);
  cursor: pointer;
  font-size: 16px;
  transition: all var(--cis-transition-fast);
}

.score-display__icon-btn:hover {
  background: var(--cis-bg-secondary);
  color: var(--cis-text-primary);
}

.score-display__icon-btn--close:hover {
  background: rgba(220, 38, 38, 0.1);
  color: var(--cis-danger);
}

.score-display__multi-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 8px;
  border-radius: var(--cis-radius-md);
  background: var(--cis-primary-light-9);
}

.score-display__multi-count {
  font-size: 13px;
  font-weight: 600;
  color: var(--cis-primary);
  padding: 0 4px;
}

.score-display__mini-btn {
  display: inline-flex;
  align-items: center;
  height: 30px;
  padding: 0 12px;
  border: 1px solid var(--cis-border-color);
  border-radius: var(--cis-radius-sm);
  background: var(--cis-card-bg);
  color: var(--cis-text-primary);
  font-size: 12px;
  cursor: pointer;
  transition: all var(--cis-transition-fast);
}

.score-display__mini-btn:hover:not(:disabled) {
  border-color: var(--cis-primary);
  color: var(--cis-primary);
}

.score-display__mini-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.score-display__mini-btn--primary {
  background: var(--cis-primary);
  color: #fff;
  border-color: var(--cis-primary);
}

.score-display__mini-btn--primary:hover:not(:disabled) {
  background: var(--cis-primary-dark);
  border-color: var(--cis-primary-dark);
  color: #fff;
}

/* ===== 主体 ===== */
.score-display__main {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 20px 24px;
  gap: 16px;
}

/* ===== 领奖台 ===== */
.score-display__podium {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 16px;
  flex-shrink: 0;
}

.score-display__podium-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 12px 20px;
  background: var(--cis-card-bg);
  border: 1px solid var(--cis-border-color-light);
  border-radius: var(--cis-radius-lg);
  position: relative;
  animation: podium-rise 0.5s cubic-bezier(0.4, 0, 0.2, 1) backwards;
}

.score-display__podium-item--0 {
  border-color: #FFD70080;
  box-shadow: 0 4px 12px rgba(255, 215, 0, 0.15);
}

.score-display__podium-item--1 {
  border-color: #C0C0C080;
  transform: translateY(8px);
  box-shadow: 0 4px 12px rgba(192, 192, 192, 0.15);
}

.score-display__podium-item--2 {
  border-color: #CD7F3280;
  transform: translateY(16px);
  box-shadow: 0 4px 12px rgba(205, 127, 50, 0.15);
}

@keyframes podium-rise {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.score-display__podium-medal {
  font-size: 24px;
  color: #FFD700;
  margin-bottom: 4px;
}

.score-display__podium-item--0 .score-display__podium-medal {
  color: #FFD700;
}

.score-display__podium-item--1 .score-display__podium-medal {
  font-size: 20px;
  color: #C0C0C0;
}

.score-display__podium-item--2 .score-display__podium-medal {
  font-size: 20px;
  color: #CD7F32;
}

.score-display__podium-avatar,
.score-display__card-avatar {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  color: #fff;
  font-size: 18px;
  font-weight: 600;
  flex-shrink: 0;
}

.score-display__podium-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--cis-text-primary);
  margin-top: 8px;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-display__podium-score {
  font-size: 28px;
  font-weight: 700;
  color: var(--cis-primary);
  font-variant-numeric: tabular-nums;
  margin-top: 4px;
  line-height: 1;
}

/* ===== 卡片网格 ===== */
.score-display__grid-wrap {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
}

.score-display__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 12px;
  padding-bottom: 8px;
}

.score-display__card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 8px;
  background: var(--cis-card-bg);
  border: 1px solid var(--cis-border-color-light);
  border-radius: var(--cis-radius-md);
  cursor: pointer;
  transition: all var(--cis-transition-fast);
  animation: card-enter 0.4s cubic-bezier(0.4, 0, 0.2, 1) backwards;
  user-select: none;
  overflow: hidden;
  min-height: 140px;
}

@keyframes card-enter {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.score-display__card:hover:not(.score-display__card--expanded) {
  border-color: var(--cis-primary);
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-2px);
}

.score-display__card--selected {
  border-color: var(--cis-primary);
  background: var(--cis-primary-light-9);
}

.score-display__card--expanded {
  grid-column: span 2;
  grid-row: span 2;
  background: var(--cis-card-bg);
  border-color: var(--cis-primary);
  box-shadow: var(--cis-shadow-card-hover);
  cursor: default;
  padding: 0;
  min-height: 320px;
}

.score-display__card-rank {
  position: absolute;
  top: 6px;
  left: 8px;
  font-size: 11px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  font-variant-numeric: tabular-nums;
}

.score-display__card-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--cis-text-primary);
  margin-top: 8px;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: center;
}

.score-display__card-score {
  font-size: 22px;
  font-weight: 700;
  color: var(--cis-primary);
  font-variant-numeric: tabular-nums;
  margin-top: 4px;
  line-height: 1;
}

.score-display__score-anim {
  position: absolute;
  top: 20%;
  right: 8px;
  font-size: 18px;
  font-weight: 700;
  pointer-events: none;
  animation: score-pop 1.4s ease-out forwards;
}

.score-display__score-anim.is-plus {
  color: var(--cis-success);
}

.score-display__score-anim.is-minus {
  color: var(--cis-danger);
}

@keyframes score-pop {
  0% { opacity: 0; transform: translateY(0); }
  20% { opacity: 1; }
  100% { opacity: 0; transform: translateY(-30px); }
}

/* ===== 卡片展开（快速评分） ===== */
.score-display__card-expanded {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 12px;
}

.score-display__card-expanded-header {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.score-display__back-btn {
  position: absolute;
  top: 0;
  left: 0;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: var(--cis-radius-sm);
  background: var(--cis-bg-secondary);
  color: var(--cis-text-secondary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  transition: all var(--cis-transition-fast);
}

.score-display__back-btn:hover {
  background: var(--cis-primary-light-9);
  color: var(--cis-primary);
}

.score-display__card-avatar--lg {
  width: 56px;
  height: 56px;
  font-size: 22px;
}

.score-display__card-expanded-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--cis-text-primary);
  margin-top: 8px;
}

.score-display__card-expanded-score {
  font-size: 32px;
  font-weight: 700;
  color: var(--cis-primary);
  font-variant-numeric: tabular-nums;
  line-height: 1;
  margin-top: 4px;
}

.score-display__quick-items {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 6px;
  padding: 12px 0;
}

.score-display__quick-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 10px;
  border: 1px solid;
  border-radius: var(--cis-radius-sm);
  background: transparent;
  cursor: pointer;
  font-size: 12px;
  font-family: inherit;
  transition: all var(--cis-transition-fast);
  min-height: 36px;
  text-align: left;
}

.score-display__quick-item--plus {
  border-color: rgba(22, 163, 74, 0.3);
  color: var(--cis-success);
  background: rgba(22, 163, 74, 0.05);
}

.score-display__quick-item--plus:hover {
  border-color: var(--cis-success);
  background: rgba(22, 163, 74, 0.1);
}

.score-display__quick-item--minus {
  border-color: rgba(220, 38, 38, 0.3);
  color: var(--cis-danger);
  background: rgba(220, 38, 38, 0.05);
}

.score-display__quick-item--minus:hover {
  border-color: var(--cis-danger);
  background: rgba(220, 38, 38, 0.1);
}

.score-display__quick-item-value {
  font-weight: 700;
  font-size: 14px;
  font-variant-numeric: tabular-nums;
}

.score-display__quick-item-name {
  font-size: 12px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.score-display__custom-score {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
  margin-top: auto;
  padding-top: 8px;
}

.score-display__step-btn {
  height: 36px;
  min-width: 36px;
  padding: 0 10px;
  border: 1px solid var(--cis-border-color);
  border-radius: var(--cis-radius-sm);
  background: var(--cis-card-bg);
  color: var(--cis-text-primary);
  font-size: 13px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: all var(--cis-transition-fast);
}

.score-display__step-btn:hover {
  border-color: var(--cis-primary);
  color: var(--cis-primary);
}

.score-display__step-btn--lg {
  min-width: 44px;
}

.score-display__custom-input {
  width: 64px;
  height: 36px;
  border: 1px solid var(--cis-border-color);
  border-radius: var(--cis-radius-sm);
  background: var(--cis-card-bg);
  color: var(--cis-text-primary);
  font-size: 16px;
  font-weight: 600;
  text-align: center;
  font-family: inherit;
  font-variant-numeric: tabular-nums;
}

.score-display__custom-input:focus {
  outline: none;
  border-color: var(--cis-primary);
  box-shadow: var(--cis-shadow-glow);
}

.score-display__custom-reason {
  flex: 1;
  min-width: 100px;
  height: 36px;
  border: 1px solid var(--cis-border-color);
  border-radius: var(--cis-radius-sm);
  background: var(--cis-card-bg);
  color: var(--cis-text-primary);
  font-size: 13px;
  padding: 0 10px;
  font-family: inherit;
}

.score-display__custom-reason:focus {
  outline: none;
  border-color: var(--cis-primary);
  box-shadow: var(--cis-shadow-glow);
}

.score-display__confirm-btn {
  height: 36px;
  padding: 0 16px;
  border: none;
  border-radius: var(--cis-radius-sm);
  background: var(--cis-primary);
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: all var(--cis-transition-fast);
}

.score-display__confirm-btn:hover:not(:disabled) {
  background: var(--cis-primary-dark);
}

.score-display__confirm-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

/* ===== 空状态 ===== */
.score-display__empty {
  text-align: center;
  padding: 60px 20px;
  color: var(--cis-text-tertiary);
}

/* ===== 底部抽屉 ===== */
.score-display__drawer {
  border-top: 1px solid var(--cis-border-color-light);
  background: var(--cis-header-bg);
  flex-shrink: 0;
}

.score-display__drawer-handle {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  width: 100%;
  height: 32px;
  border: none;
  background: transparent;
  color: var(--cis-text-secondary);
  font-size: 12px;
  cursor: pointer;
  font-family: inherit;
  transition: all var(--cis-transition-fast);
}

.score-display__drawer-handle:hover {
  background: var(--cis-bg-secondary);
  color: var(--cis-text-primary);
}

.score-display__drawer-body {
  border-top: 1px solid var(--cis-border-color-light);
  padding: 12px 24px 16px;
  max-height: 280px;
  overflow-y: auto;
}

.score-display__drawer-tabs {
  display: flex;
  gap: 4px;
  margin-bottom: 8px;
}

.score-display__drawer-tab {
  padding: 4px 12px;
  border: none;
  border-radius: var(--cis-radius-sm);
  background: transparent;
  color: var(--cis-text-secondary);
  font-size: 12px;
  font-family: inherit;
  cursor: pointer;
  transition: all var(--cis-transition-fast);
}

.score-display__drawer-tab.is-active {
  background: var(--cis-primary);
  color: #fff;
}

.score-display__drawer-tab:hover:not(.is-active) {
  background: var(--cis-bg-secondary);
  color: var(--cis-text-primary);
}

.score-display__drawer-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.score-display__drawer-row {
  display: grid;
  grid-template-columns: 36px 1fr 80px 100px;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border-radius: var(--cis-radius-sm);
  font-size: 13px;
  transition: background var(--cis-transition-fast);
}

.score-display__drawer-row:hover {
  background: var(--cis-bg-secondary);
}

.score-display__drawer-row-rank {
  font-size: 12px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
}

.score-display__drawer-row-name {
  color: var(--cis-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-display__drawer-row-net {
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  text-align: right;
}

.score-display__drawer-row-net.is-plus { color: var(--cis-success); }
.score-display__drawer-row-net.is-minus { color: var(--cis-danger); }

.score-display__drawer-row-detail {
  font-size: 11px;
  color: var(--cis-text-tertiary);
  text-align: right;
  font-variant-numeric: tabular-nums;
}

/* ===== 抽屉动画 ===== */
.drawer-enter-active,
.drawer-leave-active {
  transition: max-height 0.3s ease, opacity 0.3s ease;
  overflow: hidden;
}

.drawer-enter-from,
.drawer-leave-to {
  max-height: 0;
  opacity: 0;
}

.drawer-enter-to,
.drawer-leave-from {
  max-height: 280px;
  opacity: 1;
}

/* ===== 批量评分 ===== */
.score-display__batch {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.score-display__batch-input {
  width: 100%;
}

.score-display__batch-presets {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 6px;
  margin-top: 4px;
}

/* ===== 多选模式高亮 ===== */
.score-display--multi-select .score-display__card {
  cursor: pointer;
}

.score-display--multi-select .score-display__card--selected {
  background: var(--cis-primary-light-9);
  border-color: var(--cis-primary);
  border-width: 2px;
}
</style>

<style src="@/themes/xianxia/styles.css"></style>
