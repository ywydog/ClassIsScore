<template>
  <div
    class="score-display"
    :class="{ 'score-display--fullscreen': isFullscreen, 'score-display--settings-open': showSettings }"
    :style="rootStyle"
    @mousemove="onMouseMove"
  >
    <div class="score-display__bg" :style="bgStyle">
      <div class="score-display__orb" v-for="i in 5" :key="i" :style="orbStyle(i)"></div>
    </div>
    <div class="score-display__content">
      <div class="score-display__header" :class="{ 'score-display__header--hidden': isFullscreen && !showGearOnFullscreen }">
        <div class="score-display__brand">
          <div class="score-display__brand-icon">
            <el-icon :size="20"><Trophy /></el-icon>
          </div>
          <h1 class="score-display__title">{{ t('leaderboardTitle') }}</h1>
        </div>
        <div class="score-display__time" v-if="displaySettings.showClock">{{ currentTime }}</div>
        <div class="score-display__toggles">
          <!-- 多选模式切换 -->
          <el-button
            :type="multiSelectMode ? 'primary' : 'default'"
            size="small"
            @click="toggleMultiSelect"
            class="score-display__multi-btn"
          >
            <el-icon style="margin-right: 4px"><Check /></el-icon>
            {{ multiSelectMode ? '退出多选' : '多选' }}
          </el-button>
          <el-radio-group v-model="mode" size="small" @change="fetchLeaderboard">
            <el-radio-button value="personal">{{ t('student') }}</el-radio-button>
            <el-radio-button value="group">{{ t('group') }}</el-radio-button>
          </el-radio-group>
          <!-- 设置齿轮按钮 -->
          <button class="score-display__settings-btn" @click="showSettings = !showSettings" title="显示设置">
            <el-icon :size="18"><Setting /></el-icon>
          </button>
          <!-- 退出按钮 -->
          <button class="score-display__close-btn" @click="closeDisplay" title="关闭大屏">
            <el-icon :size="18"><Close /></el-icon>
          </button>
        </div>
      </div>

      <!-- 多选工具栏 -->
      <div v-if="multiSelectMode" class="score-display__multi-toolbar">
        <el-button size="small" @click="selectAll">全选</el-button>
        <el-button size="small" @click="clearSelection">清除</el-button>
        <span class="score-display__multi-count">已选 {{ selectedStudentIds.length }} 人</span>
        <el-button
          type="primary"
          size="small"
          :disabled="selectedStudentIds.length === 0"
          @click="showBatchScorePanel = true"
        >
          {{ t('batchScoreAction') }}
        </el-button>
        <el-button
          v-if="isXianxia"
          type="warning"
          size="small"
          :disabled="selectedStudentIds.length === 0 || students.length < 2"
          @click="openBattleDialog"
        >
          ⚔️ 道友切磋
        </el-button>
      </div>

      <div class="score-display__body">
        <!-- 主体区域：卡片/圆形/宠物 -->
        <div class="score-display__main">
          <!-- 卡片模式 -->
          <div v-if="effectiveDisplayMode === 'Card'" class="score-display__grid score-display__grid--card">
            <div
              v-for="student in students"
              :key="student.id"
              class="score-display__selectable-wrapper"
              :class="{ 'score-display__selectable-wrapper--selected': isSelected(student.id) }"
              @click="handleStudentClick(student)"
            >
              <div v-if="multiSelectMode" class="score-display__select-check">
                <el-icon v-if="isSelected(student.id)" :size="18"><Check /></el-icon>
              </div>
              <StudentCardDisplay :student="student" />
              <div v-if="isXianxia" class="score-display__cultivation-info">
                <span class="score-display__cultivation-level">{{ getCultivationLevel(calculateCultivation(student.score, calculateLevel(student.petExp))).name }}</span>
                <span class="score-display__cultivation-score">修为 {{ formatCultivationNumber(calculateCultivation(student.score, calculateLevel(student.petExp))) }}</span>
              </div>
            </div>
          </div>

          <!-- 圆形模式 -->
          <div v-else-if="effectiveDisplayMode === 'Circle'" class="score-display__grid score-display__grid--circle">
            <div
              v-for="student in students"
              :key="student.id"
              class="score-display__selectable-wrapper score-display__selectable-wrapper--circle"
              :class="{ 'score-display__selectable-wrapper--selected': isSelected(student.id) }"
              @click="handleStudentClick(student)"
            >
              <div v-if="multiSelectMode" class="score-display__select-check">
                <el-icon v-if="isSelected(student.id)" :size="18"><Check /></el-icon>
              </div>
              <StudentCircleDisplay :student="student" />
            </div>
          </div>

          <!-- 宠物模式 -->
          <div v-else-if="effectiveDisplayMode === 'Pet'" class="score-display__grid score-display__grid--pet">
            <div
              v-for="student in students"
              :key="student.id"
              class="score-display__selectable-wrapper score-display__selectable-wrapper--pet"
              :class="{ 'score-display__selectable-wrapper--selected': isSelected(student.id) }"
              @click="handleStudentClick(student)"
              @contextmenu.prevent="onPetRightClick(student)"
            >
              <div v-if="multiSelectMode" class="score-display__select-check">
                <el-icon v-if="isSelected(student.id)" :size="18"><Check /></el-icon>
              </div>
              <PetDisplay :student="student" />
            </div>
          </div>
        </div>

        <!-- 侧边栏：排行榜 -->
        <div class="score-display__sidebar">
          <div class="score-display__sidebar-header">
            <h3 class="score-display__sidebar-title">{{ t('leaderboardTitle') }}</h3>
            <el-radio-group v-model="mode" size="small" @change="fetchLeaderboard">
              <el-radio-button value="personal">{{ t('student') }}</el-radio-button>
              <el-radio-button value="group">{{ t('group') }}</el-radio-button>
            </el-radio-group>
          </div>

          <!-- 领奖台 -->
          <div v-if="limitedTopThree.length > 0" class="score-display__podium">
            <div
              class="score-display__podium-item score-display__podium--2"
              :class="{ 'score-display__podium-item--animated': mounted }"
              style="--podium-delay: 0.2s"
              v-if="limitedTopThree.length >= 2"
            >
              <div class="score-display__podium-avatar">
                <el-avatar :size="48">{{ limitedTopThree[1].name.charAt(0) }}</el-avatar>
                <div class="score-display__medal score-display__medal--silver" v-if="displaySettings.showRank">2</div>
              </div>
              <div class="score-display__podium-name">{{ limitedTopThree[1].name }}</div>
              <div class="score-display__podium-score" v-if="displaySettings.showScore">{{ limitedTopThree[1].score }}</div>
            </div>
            <div
              class="score-display__podium-item score-display__podium--1"
              :class="{ 'score-display__podium-item--animated': mounted }"
              style="--podium-delay: 0.4s"
              v-if="limitedTopThree.length >= 1"
            >
              <div class="score-display__podium-crown">👑</div>
              <div class="score-display__podium-avatar">
                <el-avatar :size="64">{{ limitedTopThree[0].name.charAt(0) }}</el-avatar>
                <div class="score-display__medal score-display__medal--gold" v-if="displaySettings.showRank">1</div>
              </div>
              <div class="score-display__podium-name">{{ limitedTopThree[0].name }}</div>
              <div class="score-display__podium-score" v-if="displaySettings.showScore">{{ limitedTopThree[0].score }}</div>
            </div>
            <div
              class="score-display__podium-item score-display__podium--3"
              :class="{ 'score-display__podium-item--animated': mounted }"
              style="--podium-delay: 0s"
              v-if="limitedTopThree.length >= 3"
            >
              <div class="score-display__podium-avatar">
                <el-avatar :size="40">{{ limitedTopThree[2].name.charAt(0) }}</el-avatar>
                <div class="score-display__medal score-display__medal--bronze" v-if="displaySettings.showRank">3</div>
              </div>
              <div class="score-display__podium-name">{{ limitedTopThree[2].name }}</div>
              <div class="score-display__podium-score" v-if="displaySettings.showScore">{{ limitedTopThree[2].score }}</div>
            </div>
          </div>

          <!-- 列表 -->
          <div class="score-display__sidebar-list" v-if="limitedRestEntries.length > 0">
            <div
              v-for="(entry, idx) in limitedRestEntries"
              :key="entry.rank"
              class="score-display__item"
              :class="{ 'score-display__item--animated': mounted }"
              :style="{ '--item-delay': `${idx * 50}ms` }"
            >
              <span class="score-display__rank" v-if="displaySettings.showRank">{{ entry.rank }}</span>
              <span class="score-display__name">{{ entry.name }}</span>
              <span class="score-display__score" v-if="displaySettings.showScore">{{ entry.score }}</span>
            </div>
          </div>
          <div v-else class="score-display__sidebar-empty">暂无排行数据</div>
        </div>
      </div>
    </div>

    <!-- 周期积分面板（右下角） -->
    <div class="score-display__period-panel">
      <div class="score-display__period-panel__header">
        <span class="score-display__period-panel__title">{{ t('scoreStats') }}</span>
        <div class="score-display__period-panel__toggles">
          <button
            v-for="p in periodOptions"
            :key="p.key"
            :class="['score-display__period-panel__toggle', { 'score-display__period-panel__toggle--active': activePeriod === p.key }]"
            @click="activePeriod = p.key"
          >{{ p.label }}</button>
        </div>
      </div>
      <div class="score-display__period-panel__body">
        <div
          v-for="stat in topPeriodStats"
          :key="stat.studentId"
          class="score-display__period-panel__row"
        >
          <span class="score-display__period-panel__name">{{ stat.studentName }}</span>
          <span class="score-display__period-panel__detail">
            <span class="score-display__period-panel__plus">+{{ stat.plus }}</span>
            <span class="score-display__period-panel__slash">/</span>
            <span class="score-display__period-panel__minus">{{ stat.minus }}</span>
          </span>
          <span class="score-display__period-panel__net" :class="stat.net > 0 ? 'score-display__period-panel__net--pos' : stat.net < 0 ? 'score-display__period-panel__net--neg' : ''">
            {{ formatNet(stat.net) }}
          </span>
        </div>
        <div v-if="topPeriodStats.length === 0" class="score-display__period-panel__empty">
          暂无数据
        </div>
      </div>
    </div>

    <!-- 显示设置面板 -->
    <DisplaySettingsPanel
      v-model:visible="showSettings"
      :settings="displaySettings"
      :display-mode="displayMode"
      :is-fullscreen="isFullscreen"
      @save-settings="saveSettings"
      @update:display-mode="onDisplayModeChange"
      @toggle-fullscreen="toggleFullscreen"
      @update:refresh-interval="onRefreshIntervalChange"
    />

    <!-- 快速评分底部栏 -->
    <transition name="quick-score-slide">
      <div v-if="quickScoreStudent && !multiSelectMode" class="quick-score-bar">
        <div class="quick-score-bar__inner">
          <div class="quick-score-bar__student">
            <span class="quick-score-bar__student-name">{{ quickScoreStudent.name }}</span>
            <span class="quick-score-bar__student-score">当前 {{ quickScoreStudent.score }} {{ t('scoreUnit') }}</span>
          </div>
          <div class="quick-score-bar__actions">
            <!-- 评价项预设 -->
            <div v-if="evaluationItems.length > 0" class="quick-score-bar__eval-presets">
              <div v-if="positiveEvalItems.length > 0" class="quick-score-bar__eval-group">
                <button
                  v-for="item in positiveEvalItems.slice(0, 6)"
                  :key="item.id"
                  class="quick-score-bar__btn quick-score-bar__btn--eval-pos"
                  @click="applyQuickPreset(item)"
                  :title="item.name + ' +' + item.scoreChange"
                >
                  {{ item.name }}
                </button>
              </div>
              <div v-if="negativeEvalItems.length > 0" class="quick-score-bar__eval-group">
                <button
                  v-for="item in negativeEvalItems.slice(0, 4)"
                  :key="item.id"
                  class="quick-score-bar__btn quick-score-bar__btn--eval-neg"
                  @click="applyQuickPreset(item)"
                  :title="item.name + ' ' + item.scoreChange"
                >
                  {{ item.name }}
                </button>
              </div>
            </div>
            <div class="quick-score-bar__divider" v-if="evaluationItems.length > 0"></div>
            <!-- 快捷分值 -->
            <div class="quick-score-bar__preset-btns">
              <button class="quick-score-bar__btn quick-score-bar__btn--pos" @click="quickScore(1)">+1</button>
              <button class="quick-score-bar__btn quick-score-bar__btn--pos" @click="quickScore(2)">+2</button>
              <button class="quick-score-bar__btn quick-score-bar__btn--pos" @click="quickScore(5)">+5</button>
              <button class="quick-score-bar__btn quick-score-bar__btn--neg" @click="quickScore(-1)">-1</button>
              <button class="quick-score-bar__btn quick-score-bar__btn--neg" @click="quickScore(-2)">-2</button>
              <button class="quick-score-bar__btn quick-score-bar__btn--neg" @click="quickScore(-5)">-5</button>
            </div>
            <div class="quick-score-bar__custom">
              <button class="quick-score-bar__btn quick-score-bar__btn--neg" @click="customScoreChange--">−</button>
              <input
                v-model.number="customScoreChange"
                type="number"
                class="quick-score-bar__input"
                placeholder="自定义"
              />
              <button class="quick-score-bar__btn quick-score-bar__btn--pos" @click="customScoreChange++">+</button>
              <button
                class="quick-score-bar__btn quick-score-bar__btn--apply"
                :disabled="!customScoreChange"
                @click="quickScore(customScoreChange)"
              >
                应用
              </button>
            </div>
            <div class="quick-score-bar__reason">
              <input
                v-model="quickScoreReason"
                type="text"
                class="quick-score-bar__reason-input"
                placeholder="原因（可选）"
              />
            </div>
          </div>
          <button class="quick-score-bar__close" @click="closeQuickScore">
            <el-icon :size="18"><Close /></el-icon>
          </button>
        </div>
      </div>
    </transition>

    <!-- 批量评分对话框 -->
    <el-dialog
      v-model="showBatchScorePanel"
      :title="t('batchScoreAction')"
      width="480px"
      :close-on-click-modal="false"
      class="score-display__batch-dialog"
      append-to-body
    >
      <div class="batch-score-form">
        <div class="batch-score-form__info">
          已选择 <strong>{{ selectedStudentIds.length }}</strong> 名{{ t('student') }}
        </div>
        <!-- 评价项预设 -->
        <div v-if="evaluationItems.length > 0" class="batch-score-form__eval-section">
          <div class="batch-score-form__eval-label">常用评价</div>
          <div class="batch-score-form__eval-groups">
            <div v-if="positiveEvalItems.length > 0" class="batch-score-form__eval-group">
              <button
                v-for="item in positiveEvalItems"
                :key="item.id"
                class="quick-score-bar__btn quick-score-bar__btn--eval-pos"
                @click="applyBatchPreset(item)"
              >
                {{ item.name }} +{{ item.scoreChange }}
              </button>
            </div>
            <div v-if="negativeEvalItems.length > 0" class="batch-score-form__eval-group">
              <button
                v-for="item in negativeEvalItems"
                :key="item.id"
                class="quick-score-bar__btn quick-score-bar__btn--eval-neg"
                @click="applyBatchPreset(item)"
              >
                {{ item.name }} {{ item.scoreChange }}
              </button>
            </div>
          </div>
        </div>
        <el-divider v-if="evaluationItems.length > 0" style="margin: 8px 0" />
        <!-- 快捷分值 -->
        <div class="batch-score-form__presets">
          <button class="quick-score-bar__btn quick-score-bar__btn--pos" @click="batchScoreChange = 1">+1</button>
          <button class="quick-score-bar__btn quick-score-bar__btn--pos" @click="batchScoreChange = 2">+2</button>
          <button class="quick-score-bar__btn quick-score-bar__btn--pos" @click="batchScoreChange = 5">+5</button>
          <button class="quick-score-bar__btn quick-score-bar__btn--neg" @click="batchScoreChange = -1">-1</button>
          <button class="quick-score-bar__btn quick-score-bar__btn--neg" @click="batchScoreChange = -2">-2</button>
          <button class="quick-score-bar__btn quick-score-bar__btn--neg" @click="batchScoreChange = -5">-5</button>
        </div>
        <el-input-number
          v-model="batchScoreChange"
          :step="1"
          controls-position="right"
          style="width: 100%"
        />
        <el-input
          v-model="batchScoreReason"
          placeholder="原因（可选，选择预设后可修改）"
          style="margin-top: 12px"
        />
      </div>
      <template #footer>
        <el-button @click="showBatchScorePanel = false">取消</el-button>
        <el-button type="primary" :disabled="!batchScoreChange" @click="submitBatchScore">{{ t('confirmScore') }}</el-button>
      </template>
    </el-dialog>

    <!-- 宠物选择对话框 -->
    <PetSelectDialog
      v-model:visible="showPetDialog"
      :student="petDialogStudent"
      @select-pet="selectPet"
    />

    <!-- 道友切磋对话框 -->
    <BattleDialog
      v-if="isXianxia"
      v-model="showBattleDialog"
      :challenger="battleChallenger"
      :opponents="battleOpponents"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, reactive } from 'vue'
import { Trophy, Check, Close, Setting } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { LeaderboardEntry, Student, EvaluationItem, ScoreUpdateEvent, StudentScoreStats } from '@/types'
import { leaderboardApi } from '@/services/leaderboard'
import { evaluationApi } from '@/services/evaluation'
import { Window } from '@tauri-apps/api/window'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'
import { studentApi } from '@/services/student'
import { scoreApi } from '@/services/score'
import { calculateLevel } from '@/utils/petSystem'
import { useTerminology } from '@/themes/xianxia/useTerminology'
import { calculateCultivation, getCultivationLevel, formatCultivationNumber } from '@/utils/cultivationSystem'
import StudentCardDisplay from '@/components/display/StudentCardDisplay.vue'
import StudentCircleDisplay from '@/components/display/StudentCircleDisplay.vue'
import PetDisplay from '@/components/display/PetDisplay.vue'
import BattleDialog from '@/components/xianxia/BattleDialog.vue'
import DisplaySettingsPanel from '@/components/display/DisplaySettingsPanel.vue'
import PetSelectDialog from '@/components/display/PetSelectDialog.vue'

// ===== 显示设置 =====
interface DisplaySettings {
  background: 'deepblue' | 'pureblack' | 'warmgray' | 'custom'
  customColor: string
  fontSize: 'small' | 'medium' | 'large' | 'xlarge'
  maxItems: number
  refreshInterval: number
  showClock: boolean
  showRank: boolean
  showScore: boolean
}

const DEFAULT_SETTINGS: DisplaySettings = {
  background: 'deepblue',
  customColor: '#1a1a2e',
  fontSize: 'medium',
  maxItems: 10,
  refreshInterval: 30,
  showClock: true,
  showRank: true,
  showScore: true,
}

function loadSettings(): DisplaySettings {
  try {
    const raw = localStorage.getItem('displaySettings')
    if (raw) {
      const parsed = JSON.parse(raw)
      return { ...DEFAULT_SETTINGS, ...parsed }
    }
  } catch { /* ignore */ }
  return { ...DEFAULT_SETTINGS }
}

const displaySettings = reactive<DisplaySettings>(loadSettings())

function saveSettings() {
  localStorage.setItem('displaySettings', JSON.stringify(displaySettings))
}

// ===== 全屏模式 =====
const isFullscreen = ref(false)
const showGearOnFullscreen = ref(true)
let fullscreenHideTimer: ReturnType<typeof setTimeout> | null = null

function toggleFullscreen() {
  if (!document.fullscreenElement) {
    document.documentElement.requestFullscreen().then(() => {
      isFullscreen.value = true
    }).catch(() => {})
  } else {
    document.exitFullscreen().then(() => {
      isFullscreen.value = false
    }).catch(() => {})
  }
}

function onFullscreenChange() {
  isFullscreen.value = !!document.fullscreenElement
}

function onMouseMove(e: MouseEvent) {
  if (!isFullscreen.value) return
  if (e.clientY < 60) {
    showGearOnFullscreen.value = true
    if (fullscreenHideTimer) clearTimeout(fullscreenHideTimer)
    fullscreenHideTimer = setTimeout(() => {
      showGearOnFullscreen.value = false
    }, 3000)
  }
}

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

// ===== 设置面板 =====
const showSettings = ref(false)

// ===== 分数变化动画 =====
interface ScoreAnimation {
  id: number
  change: number
}

const scoreAnimations = ref<ScoreAnimation[]>([])
let animIdCounter = 0

function addScoreAnimation(change: number) {
  const id = ++animIdCounter
  scoreAnimations.value.push({ id, change })
  setTimeout(() => {
    scoreAnimations.value = scoreAnimations.value.filter(a => a.id !== id)
  }, 1500)
}

// ===== 排行榜入场动画 =====
const mounted = ref(false)

// ===== 核心数据 =====
const leaderboard = ref<LeaderboardEntry[]>([])
const students = ref<Student[]>([])
const evaluationItems = ref<EvaluationItem[]>([])
const scoreStats = ref<StudentScoreStats[]>([])
const mode = ref<'personal' | 'group'>('personal')
const displayMode = ref<'leaderboard' | 'Card' | 'Circle' | 'Pet'>('Card')

const effectiveDisplayMode = computed(() => {
  if (isXianxia.value && mode.value === 'personal') {
    return 'Card' // 修仙模式下个人页强制卡片模式
  }
  return displayMode.value
})
const currentTime = ref('')

// 多选模式
const multiSelectMode = ref(false)
const selectedStudentIds = ref<string[]>([])

// 快速评分
const quickScoreStudent = ref<Student | null>(null)
const customScoreChange = ref<number>(0)
const quickScoreReason = ref('')

// 批量评分
const showBatchScorePanel = ref(false)
const batchScoreChange = ref<number>(1)
const batchScoreReason = ref('')

// 宠物选择
const showPetDialog = ref(false)
const petDialogStudent = ref<Student | null>(null)

// 道友切磋
const showBattleDialog = ref(false)
const battleChallenger = computed(() => {
  if (selectedStudentIds.value.length === 0) return null
  const id = selectedStudentIds.value[0]
  return students.value.find(s => s.id === id) || null
})
const battleOpponents = computed(() => {
  const selectedIds = new Set(selectedStudentIds.value)
  return students.value.filter(s => !selectedIds.has(s.id))
})

function openBattleDialog() {
  if (students.value.length < 2) {
    ElMessage.warning('至少需要两名道友才能切磋')
    return
  }
  showBattleDialog.value = true
}

let timeTimer: ReturnType<typeof setInterval> | null = null
let refreshTimer: ReturnType<typeof setInterval> | null = null

const { isXianxia, t } = useTerminology()

// ===== 周期积分面板 =====
type PeriodKey = 'day' | 'week' | 'month' | 'semester'
const activePeriod = ref<PeriodKey>('week')
const periodOptions = computed(() => {
  const opts: { key: PeriodKey; label: string }[] = [
    { key: 'day', label: '日' },
    { key: 'week', label: '周' },
    { key: 'month', label: '月' },
  ]
  if (scoreStats.value.some(s => s.semesterNet !== undefined)) {
    opts.push({ key: 'semester', label: '学期' })
  }
  return opts
})

interface PeriodStatRow {
  studentId: number
  studentName: string
  plus: number
  minus: number
  net: number
}

const topPeriodStats = computed<PeriodStatRow[]>(() => {
  return scoreStats.value
    .map(s => {
      let plus = 0, minus = 0, net = 0
      switch (activePeriod.value) {
        case 'day': plus = s.dayPlus; minus = s.dayMinus; net = s.dayNet; break
        case 'week': plus = s.weekPlus; minus = s.weekMinus; net = s.weekNet; break
        case 'month': plus = s.monthPlus; minus = s.monthMinus; net = s.monthNet; break
        case 'semester': plus = s.semesterPlus || 0; minus = s.semesterMinus || 0; net = s.semesterNet || 0; break
      }
      return { studentId: s.studentId, studentName: s.studentName, plus, minus, net }
    })
    .sort((a, b) => b.net - a.net)
    .slice(0, 10)
})

// ===== 计算属性 =====
const fontSizeMap: Record<string, string> = {
  small: '14px',
  medium: '16px',
  large: '20px',
  xlarge: '24px',
}

const rootStyle = computed(() => ({
  '--display-font-size': fontSizeMap[displaySettings.fontSize] || '16px',
}))

const bgStyle = computed(() => {
  switch (displaySettings.background) {
    case 'pureblack':
      return { background: '#000' }
    case 'warmgray':
      return { background: 'linear-gradient(160deg, #2a2520 0%, #3d3530 40%, #332d28 70%, #252018 100%)' }
    case 'custom':
      return { background: displaySettings.customColor }
    default: // deepblue
      return {}
  }
})

const limitedLeaderboard = computed(() => leaderboard.value.slice(0, displaySettings.maxItems))
const limitedTopThree = computed(() => limitedLeaderboard.value.slice(0, 3))
const limitedRestEntries = computed(() => limitedLeaderboard.value.slice(3))



const positiveEvalItems = computed(() => evaluationItems.value.filter(i => i.isPositive))
const negativeEvalItems = computed(() => evaluationItems.value.filter(i => !i.isPositive))

function updateTime() {
  currentTime.value = new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function orbStyle(i: number) {
  const size = 120 + i * 80
  const x = (i * 19 + 10) % 90
  const y = (i * 13 + 5) % 80
  const delay = i * 3
  return {
    width: `${size}px`,
    height: `${size}px`,
    left: `${x}%`,
    top: `${y}%`,
    animationDelay: `${delay}s`,
    animationDuration: `${18 + i * 2}s`,
  }
}

// 刷新间隔变更
function onRefreshIntervalChange() {
  saveSettings()
  restartRefreshTimer()
}

// 显示模式变更
function onDisplayModeChange(value: string) {
  if (value === 'Card' || value === 'Circle' || value === 'Pet' || value === 'leaderboard') {
    displayMode.value = value
  }
}

function restartRefreshTimer() {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
  if (displaySettings.refreshInterval > 0) {
    refreshTimer = setInterval(() => {
      fetchLeaderboard()
      fetchStudents()
      fetchScoreStats()
    }, displaySettings.refreshInterval * 1000)
  }
}

// 学生点击处理
function handleStudentClick(student: Student) {
  if (multiSelectMode.value) {
    toggleStudentSelection(student.id)
  } else {
    quickScoreStudent.value = student
    customScoreChange.value = 0
    quickScoreReason.value = ''
  }
}

// 评价项预设选择（快速评分栏）
function applyQuickPreset(item: EvaluationItem) {
  customScoreChange.value = item.scoreChange
  quickScoreReason.value = item.name
}

// 评价项预设选择（批量评分对话框）
function applyBatchPreset(item: EvaluationItem) {
  batchScoreChange.value = item.scoreChange
  batchScoreReason.value = item.name
}

// 多选相关
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

function isSelected(id: string): boolean {
  return selectedStudentIds.value.includes(id)
}

function selectAll() {
  selectedStudentIds.value = students.value.map(s => s.id)
}

function clearSelection() {
  selectedStudentIds.value = []
}

// 快速评分
async function quickScore(change: number) {
  if (!quickScoreStudent.value || change === 0) return
  try {
    await scoreApi.addScore({
      studentId: quickScoreStudent.value.id,
      scoreChange: change,
      reason: quickScoreReason.value || (change > 0 ? `加分${change}` : `扣分${Math.abs(change)}`),
    })
    ElMessage.success(`${quickScoreStudent.value.name} ${change > 0 ? '+' : ''}${change} 分`)
    quickScoreStudent.value = null
    await fetchLeaderboard()
    await fetchStudents()
  } catch {
    // error handled by interceptor
  }
}

function closeQuickScore() {
  quickScoreStudent.value = null
}

// 批量评分
async function submitBatchScore() {
  if (selectedStudentIds.value.length === 0 || !batchScoreChange.value) return
  try {
    await scoreApi.batchAddScore({
      studentIds: selectedStudentIds.value,
      scoreChange: batchScoreChange.value,
      reason: batchScoreReason.value || (batchScoreChange.value > 0 ? `批量加分${batchScoreChange.value}` : `批量扣分${Math.abs(batchScoreChange.value)}`),
    })
    ElMessage.success(`已为 ${selectedStudentIds.value.length} 名学生评分`)
    showBatchScorePanel.value = false
    selectedStudentIds.value = []
    await fetchLeaderboard()
    await fetchStudents()
  } catch {
    // error handled by interceptor
  }
}

// 宠物右键
function onPetRightClick(student: Student) {
  petDialogStudent.value = student
  showPetDialog.value = true
}

async function selectPet(petTypeId: string) {
  if (!petDialogStudent.value) return
  try {
    await studentApi.update(petDialogStudent.value.id, { petType: petTypeId || undefined })
    ElMessage.success(petTypeId ? `已为 ${petDialogStudent.value.name} 选择宠物` : `已移除 ${petDialogStudent.value.name} 的宠物`)
    showPetDialog.value = false
    petDialogStudent.value = null
    await fetchStudents()
  } catch {
    // error handled by interceptor
  }
}

onMounted(async () => {
  updateTime()
  timeTimer = setInterval(updateTime, 1000)
  await Promise.all([fetchLeaderboard(), fetchStudents(), fetchEvaluationItems(), fetchScoreStats()])
  // 触发入场动画
  requestAnimationFrame(() => {
    mounted.value = true
  })
  connectWebSocket({
    onScoreUpdate: (data: ScoreUpdateEvent) => {
      if (data.scoreChange !== undefined && data.scoreChange !== 0) {
        addScoreAnimation(data.scoreChange)
      }
      fetchLeaderboard()
      fetchStudents()
      fetchScoreStats()
    },
  })
  restartRefreshTimer()
  document.addEventListener('fullscreenchange', onFullscreenChange)
})

onUnmounted(() => {
  disconnectWebSocket()
  if (timeTimer) clearInterval(timeTimer)
  if (refreshTimer) clearInterval(refreshTimer)
  if (fullscreenHideTimer) clearTimeout(fullscreenHideTimer)
  document.removeEventListener('fullscreenchange', onFullscreenChange)
})

async function fetchLeaderboard() {
  try {
    leaderboard.value = await leaderboardApi.query()
  } catch {
    // silent
  }
}

async function fetchStudents() {
  try {
    const response = await studentApi.getAll()
    students.value = response.data.data
  } catch {
    // silent
  }
}

async function fetchEvaluationItems() {
  try {
    const response = await evaluationApi.getAll()
    evaluationItems.value = response.data.data || []
  } catch {
    // silent
  }
}

async function fetchScoreStats() {
  try {
    scoreStats.value = []
  } catch {
    // silent
  }
}

function formatNet(val: number | undefined): string {
  if (val === undefined || val === 0) return '0'
  return val > 0 ? `+${val}` : `${val}`
}
</script>

<style src="@/themes/xianxia/styles.css"></style>

<style scoped>
/* ===== Focus-less CSS ===== */
.score-display * {
  user-select: none;
  -webkit-user-select: none;
  cursor: default;
}
.score-display *:focus {
  outline: none;
}
.score-display *:focus-visible {
  outline: none;
}
/* 设置面板打开时恢复交互元素光标 */
.score-display--settings-open .score-display__settings-panel * {
  cursor: auto;
}
.score-display--settings-open .score-display__settings-panel button,
.score-display--settings-open .score-display__settings-panel input,
.score-display--settings-open .score-display__settings-panel label {
  cursor: pointer;
}

.score-display {
  width: 100vw;
  height: 100vh;
  background: linear-gradient(160deg, #0a1628 0%, #0d2137 40%, #0a2a3c 70%, #061a2e 100%);
  color: #fff;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
  font-size: var(--display-font-size, 16px);
}

/* 背景光球 */
.score-display__bg {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.score-display__orb {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(13, 148, 136, 0.12) 0%, transparent 70%);
  animation: orb-float 20s infinite ease-in-out;
  filter: blur(2px);
}

@keyframes orb-float {
  0%, 100% { transform: translate(0, 0) scale(1); opacity: 0.4; }
  33% { transform: translate(20px, -30px) scale(1.1); opacity: 0.6; }
  66% { transform: translate(-15px, 20px) scale(0.95); opacity: 0.5; }
}

.score-display__content {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  padding: 32px 48px;
  height: 100%;
}

.score-display__header {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 36px;
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.score-display__header--hidden {
  opacity: 0;
  transform: translateY(-20px);
  pointer-events: none;
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
  background: var(--cis-gradient-primary, linear-gradient(135deg, #0d9488, #14b8a6));
  border-radius: var(--cis-radius-md, 8px);
  color: #fff;
}

.score-display__title {
  font-family: var(--cis-font-family-display, serif);
  font-size: 28px;
  font-weight: 700;
  margin: 0;
  background: linear-gradient(135deg, #2dd4bf, #5eead4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.score-display__time {
  font-size: 20px;
  font-weight: 300;
  color: rgba(255, 255, 255, 0.45);
  font-variant-numeric: tabular-nums;
  letter-spacing: 2px;
}

.score-display__toggles {
  margin-left: auto;
  display: flex;
  gap: 12px;
  align-items: center;
}

.score-display__multi-btn {
  border-color: rgba(255, 255, 255, 0.15);
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.7);
}

.score-display__multi-btn:hover {
  border-color: rgba(13, 148, 136, 0.4);
  background: rgba(13, 148, 136, 0.1);
  color: #fff;
}

.score-display__toggles :deep(.el-radio-button__inner) {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.12);
  color: rgba(255, 255, 255, 0.5);
  font-size: 12px;
  padding: 6px 16px;
}

.score-display__toggles :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  border-color: #0d9488;
  color: #fff;
  box-shadow: 0 0 16px rgba(13, 148, 136, 0.3);
}

/* 设置齿轮按钮 */
.score-display__settings-btn {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  flex-shrink: 0;
}

.score-display__settings-btn:hover {
  background: rgba(13, 148, 136, 0.15);
  border-color: rgba(13, 148, 136, 0.4);
  color: #2dd4bf;
  transform: rotate(45deg);
}

/* 关闭按钮 */
.score-display__close-btn {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  flex-shrink: 0;
  cursor: pointer;
}

.score-display__close-btn:hover {
  background: rgba(239, 68, 68, 0.2);
  border-color: rgba(239, 68, 68, 0.4);
  color: #f87171;
}

/* 多选工具栏 */
.score-display__multi-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
  padding: 10px 16px;
  background: rgba(13, 148, 136, 0.1);
  border: 1px solid rgba(13, 148, 136, 0.25);
  border-radius: var(--cis-radius-lg, 12px);
}

.score-display__multi-toolbar :deep(.el-button) {
  border-color: rgba(255, 255, 255, 0.15);
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.8);
}

.score-display__multi-toolbar :deep(.el-button:hover) {
  border-color: rgba(255, 255, 255, 0.3);
  background: rgba(255, 255, 255, 0.1);
  color: #fff;
}

.score-display__multi-count {
  margin-left: auto;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.6);
}

/* 可选择包装器 */
.score-display__selectable-wrapper {
  position: relative;
  cursor: pointer;
  transition: all var(--cis-transition-fast, 0.15s ease);
}

.score-display__selectable-wrapper--selected {
  outline: 2px solid #0d9488;
  outline-offset: 2px;
  border-radius: var(--cis-radius-lg, 12px);
}

.score-display__selectable-wrapper--selected :deep(.student-card-display),
.score-display__selectable-wrapper--selected :deep(.pet-display) {
  background: rgba(13, 148, 136, 0.12);
  border-color: rgba(13, 148, 136, 0.4);
}

.score-display__selectable-wrapper--circle.score-display__selectable-wrapper--selected :deep(.student-circle-display__avatar) {
  box-shadow: 0 0 0 3px #0d9488;
}

.score-display__select-check {
  position: absolute;
  top: 6px;
  right: 6px;
  z-index: 2;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: rgba(13, 148, 136, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
}

/* 领奖台 */
.score-display__podium {
  display: flex;
  justify-content: center;
  align-items: flex-end;
  gap: 40px;
  margin-bottom: 36px;
  padding: 20px 0;
}

.score-display__podium-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  opacity: 0;
  transform: translateY(30px);
  transition: opacity 0.6s ease, transform 0.6s ease;
}

.score-display__podium-item--animated {
  opacity: 1;
  transform: translateY(0);
  transition-delay: var(--podium-delay, 0s);
}

.score-display__podium--1 { order: 2; }
.score-display__podium--2 { order: 1; }
.score-display__podium--3 { order: 3; }

.score-display__podium-crown {
  font-size: 28px;
  animation: crown-bounce 2s infinite ease-in-out;
}

@keyframes crown-bounce {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-4px); }
}

.score-display__podium-avatar {
  position: relative;
}

.score-display__podium-avatar :deep(.el-avatar) {
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
  font-size: 28px;
  font-weight: 700;
  border: 3px solid rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.score-display__medal {
  position: absolute;
  bottom: -2px;
  right: -2px;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 700;
  color: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}

.score-display__medal--gold {
  background: linear-gradient(135deg, #ffd700, #ffb700);
  box-shadow: 0 0 16px rgba(255, 215, 0, 0.4);
}
.score-display__medal--silver {
  background: linear-gradient(135deg, #e8e8e8, #b0b0b0);
  box-shadow: 0 0 12px rgba(192, 192, 192, 0.3);
}
.score-display__medal--bronze {
  background: linear-gradient(135deg, #cd7f32, #a0622a);
  box-shadow: 0 0 12px rgba(205, 127, 50, 0.3);
}

.score-display__podium-name {
  font-size: 18px;
  font-weight: 600;
  letter-spacing: 0.5px;
}

.score-display__podium-score {
  font-size: 36px;
  font-weight: 700;
  background: linear-gradient(135deg, #2dd4bf, #5eead4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

/* 列表 */
.score-display__list {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 10px;
  align-content: start;
  overflow-y: auto;
}

.score-display__item {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px 18px;
  background: rgba(255, 255, 255, 0.04);
  border-radius: var(--cis-radius-lg, 12px);
  border: 1px solid rgba(255, 255, 255, 0.06);
  transition: all var(--cis-transition-fast, 0.12s ease);
  opacity: 0;
  transform: translateX(20px);
}

.score-display__item--animated {
  opacity: 1;
  transform: translateX(0);
  transition-delay: var(--item-delay, 0ms);
}

.score-display__item:hover {
  background: rgba(255, 255, 255, 0.08);
  border-color: rgba(13, 148, 136, 0.2);
}

.score-display__rank {
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-weight: 700;
  font-size: 13px;
  background: rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.5);
}

.score-display__name {
  flex: 1;
  font-size: 15px;
  font-weight: 500;
}

.score-display__score {
  font-size: 20px;
  font-weight: 700;
  background: linear-gradient(135deg, #2dd4bf, #5eead4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

/* 网格展示模式 */
.score-display__grid {
  flex: 1;
  overflow-y: auto;
  align-content: start;
}

.score-display__grid--card {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 16px;
}

.score-display__grid--circle {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  gap: 12px;
  justify-items: center;
}

.score-display__grid--pet {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
  gap: 16px;
  justify-items: center;
}

/* 深色背景下调整子组件样式 */
.score-display__grid :deep(.student-card-display) {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.1);
}

.score-display__grid :deep(.student-card-display:hover) {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(13, 148, 136, 0.3);
}

.score-display__grid :deep(.student-card-display__name) {
  color: rgba(255, 255, 255, 0.9);
}

.score-display__grid :deep(.student-card-display__score) {
  color: #2dd4bf;
}

.score-display__grid :deep(.student-circle-display__name) {
  color: rgba(255, 255, 255, 0.85);
}

.score-display__grid :deep(.pet-display) {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.1);
}

.score-display__grid :deep(.pet-display:hover) {
  background: rgba(255, 255, 255, 0.1);
}

.score-display__grid :deep(.pet-display__name) {
  color: rgba(255, 255, 255, 0.9);
}

.score-display__grid :deep(.pet-display__score) {
  color: #2dd4bf;
}

.score-display__grid :deep(.pet-display__pet-name) {
  color: rgba(255, 255, 255, 0.5);
}

.score-display__grid :deep(.pet-display__exp-text) {
  color: rgba(255, 255, 255, 0.4);
}

/* ===== 快速评分底部栏 ===== */
.quick-score-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 100;
  padding: 0 32px 24px;
  pointer-events: none;
}

.quick-score-bar__inner {
  pointer-events: auto;
  display: flex;
  align-items: center;
  gap: 20px;
  padding: 16px 24px;
  background: rgba(10, 22, 40, 0.85);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border: 1px solid rgba(13, 148, 136, 0.25);
  border-radius: 16px;
  box-shadow: 0 -4px 32px rgba(0, 0, 0, 0.4), 0 0 24px rgba(13, 148, 136, 0.1);
}

.quick-score-bar__student {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 100px;
}

.quick-score-bar__student-name {
  font-size: 16px;
  font-weight: 600;
  color: #fff;
}

.quick-score-bar__student-score {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.45);
}

.quick-score-bar__actions {
  display: flex;
  align-items: center;
  gap: 16px;
  flex: 1;
}

.quick-score-bar__preset-btns {
  display: flex;
  gap: 6px;
}

.quick-score-bar__btn {
  padding: 6px 14px;
  border-radius: 8px;
  border: 1px solid transparent;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
  user-select: none;
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.7);
}

.quick-score-bar__btn:hover {
  transform: translateY(-1px);
}

.quick-score-bar__btn:active {
  transform: translateY(0) scale(0.97);
}

.quick-score-bar__btn--pos {
  background: rgba(34, 197, 94, 0.12);
  color: #4ade80;
  border-color: rgba(34, 197, 94, 0.25);
}

.quick-score-bar__btn--pos:hover {
  background: rgba(34, 197, 94, 0.2);
  border-color: rgba(34, 197, 94, 0.4);
  box-shadow: 0 2px 8px rgba(34, 197, 94, 0.15);
}

.quick-score-bar__btn--neg {
  background: rgba(239, 68, 68, 0.12);
  color: #f87171;
  border-color: rgba(239, 68, 68, 0.25);
}

.quick-score-bar__btn--neg:hover {
  background: rgba(239, 68, 68, 0.2);
  border-color: rgba(239, 68, 68, 0.4);
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.15);
}

.quick-score-bar__btn--apply {
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  color: #fff;
  border-color: transparent;
}

.quick-score-bar__btn--apply:hover {
  box-shadow: 0 2px 12px rgba(13, 148, 136, 0.35);
}

.quick-score-bar__btn--apply:disabled {
  opacity: 0.4;
  cursor: not-allowed;
  transform: none;
}

/* 评价项预设按钮 */
.quick-score-bar__eval-presets {
  display: flex;
  flex-direction: column;
  gap: 4px;
  max-width: 320px;
}

.quick-score-bar__eval-group {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
}

.quick-score-bar__btn--eval-pos {
  background: rgba(34, 197, 94, 0.08);
  color: #4ade80;
  border-color: rgba(34, 197, 94, 0.18);
  font-size: 12px;
  padding: 4px 10px;
}

.quick-score-bar__btn--eval-pos:hover {
  background: rgba(34, 197, 94, 0.16);
  border-color: rgba(34, 197, 94, 0.35);
}

.quick-score-bar__btn--eval-neg {
  background: rgba(239, 68, 68, 0.08);
  color: #f87171;
  border-color: rgba(239, 68, 68, 0.18);
  font-size: 12px;
  padding: 4px 10px;
}

.quick-score-bar__btn--eval-neg:hover {
  background: rgba(239, 68, 68, 0.16);
  border-color: rgba(239, 68, 68, 0.35);
}

.quick-score-bar__divider {
  width: 1px;
  height: 40px;
  background: rgba(255, 255, 255, 0.1);
  flex-shrink: 0;
}

.quick-score-bar__custom {
  display: flex;
  align-items: center;
  gap: 4px;
}

.quick-score-bar__input {
  width: 60px;
  text-align: center;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  padding: 6px 4px;
  outline: none;
  transition: border-color 0.15s;
}

.quick-score-bar__input:focus {
  border-color: rgba(13, 148, 136, 0.5);
}

.quick-score-bar__input::-webkit-inner-spin-button,
.quick-score-bar__input::-webkit-outer-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.quick-score-bar__input[type='number'] {
  -moz-appearance: textfield;
}

.quick-score-bar__reason {
  flex: 1;
  min-width: 120px;
}

.quick-score-bar__reason-input {
  width: 100%;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  color: #fff;
  font-size: 13px;
  padding: 6px 12px;
  outline: none;
  transition: border-color 0.15s;
}

.quick-score-bar__reason-input::placeholder {
  color: rgba(255, 255, 255, 0.3);
}

.quick-score-bar__reason-input:focus {
  border-color: rgba(13, 148, 136, 0.5);
}

.quick-score-bar__close {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.5);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
  flex-shrink: 0;
}

.quick-score-bar__close:hover {
  background: rgba(255, 255, 255, 0.12);
  color: #fff;
}

/* 底部栏动画 */
.quick-score-slide-enter-active,
.quick-score-slide-leave-active {
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.3s ease;
}

.quick-score-slide-enter-from,
.quick-score-slide-leave-to {
  transform: translateY(100%);
  opacity: 0;
}

/* ===== 批量评分对话框 ===== */
.batch-score-form {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.batch-score-form__info {
  font-size: 14px;
  color: var(--cis-text-secondary);
}

.batch-score-form__presets {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.batch-score-form__eval-section {
  margin-bottom: 4px;
}

.batch-score-form__eval-label {
  font-size: 12px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.5);
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.batch-score-form__eval-groups {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.batch-score-form__eval-group {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}



/* 对话框深色主题适配 */
.score-display :deep(.el-dialog) {
  background: #0d2137;
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.score-display :deep(.el-dialog__title) {
  color: #fff;
}

.score-display :deep(.el-dialog__headerbtn .el-dialog__close) {
  color: rgba(255, 255, 255, 0.5);
}

.score-display :deep(.el-dialog__body) {
  color: rgba(255, 255, 255, 0.8);
}

.score-display :deep(.el-input-number .el-input__wrapper) {
  background: rgba(255, 255, 255, 0.06);
  box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.12) inset;
}

.score-display :deep(.el-input-number .el-input__inner) {
  color: #fff;
}

.score-display :deep(.el-input__wrapper) {
  background: rgba(255, 255, 255, 0.06);
  box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.12) inset;
}

.score-display :deep(.el-input__inner) {
  color: #fff;
}

.score-display :deep(.el-input__inner::placeholder) {
  color: rgba(255, 255, 255, 0.3);
}

/* ===== 排行榜入场动画关键帧 ===== */
@keyframes slide-up-fade {
  from {
    transform: translateY(30px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

@keyframes slide-right-fade {
  from {
    transform: translateX(20px);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

/* ===== 分数变化浮动动画 ===== */
.score-display__float-container {
  position: fixed;
  bottom: 80px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 200;
  display: flex;
  flex-direction: column-reverse;
  align-items: center;
  gap: 4px;
  pointer-events: none;
}

.score-display__float-anim {
  font-size: 24px;
  font-weight: 700;
  animation: score-rise 1.5s ease-out forwards;
  text-shadow: 0 2px 8px rgba(0, 0, 0, 0.5);
  white-space: nowrap;
}

.score-display__float-anim--pos {
  color: #4ade80;
}

.score-display__float-anim--neg {
  color: #f87171;
}

@keyframes score-rise {
  0% {
    transform: translateY(0);
    opacity: 1;
  }
  100% {
    transform: translateY(-40px);
    opacity: 0;
  }
}

.score-float-enter-active {
  animation: score-rise 1.5s ease-out forwards;
}

.score-float-leave-active {
  transition: opacity 0.3s ease;
}

.score-float-leave-to {
  opacity: 0;
}

/* ===== 周期积分面板（右下角） ===== */
.score-display__period-panel {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 120;
  width: 260px;
  background: rgba(10, 22, 40, 0.82);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid rgba(13, 148, 136, 0.2);
  border-radius: 14px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.35), 0 0 16px rgba(13, 148, 136, 0.08);
  overflow: hidden;
}

.score-display__period-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 14px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.score-display__period-panel__title {
  font-size: 13px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.7);
}

.score-display__period-panel__toggles {
  display: flex;
  gap: 3px;
}

.score-display__period-panel__toggle {
  padding: 3px 10px;
  border-radius: 6px;
  border: none;
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.45);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}

.score-display__period-panel__toggle:hover {
  background: rgba(255, 255, 255, 0.1);
  color: rgba(255, 255, 255, 0.7);
}

.score-display__period-panel__toggle--active {
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  color: #fff;
  box-shadow: 0 2px 8px rgba(13, 148, 136, 0.3);
}

.score-display__period-panel__body {
  padding: 8px 0;
  max-height: 320px;
  overflow-y: auto;
}

.score-display__period-panel__body::-webkit-scrollbar {
  width: 3px;
}

.score-display__period-panel__body::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 2px;
}

.score-display__period-panel__row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 14px;
  transition: background 0.12s;
}

.score-display__period-panel__row:hover {
  background: rgba(255, 255, 255, 0.04);
}

.score-display__period-panel__name {
  flex: 1;
  font-size: 13px;
  font-weight: 500;
  color: rgba(255, 255, 255, 0.85);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-display__period-panel__detail {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: 11px;
}

.score-display__period-panel__plus {
  color: #4ade80;
  font-weight: 600;
}

.score-display__period-panel__slash {
  color: rgba(255, 255, 255, 0.25);
}

.score-display__period-panel__minus {
  color: #f87171;
  font-weight: 600;
}

.score-display__period-panel__net {
  font-size: 14px;
  font-weight: 700;
  min-width: 36px;
  text-align: right;
  color: rgba(255, 255, 255, 0.6);
}

.score-display__period-panel__net--pos {
  color: #4ade80;
}

.score-display__period-panel__net--neg {
  color: #f87171;
}

.score-display__period-panel__empty {
  padding: 16px;
  text-align: center;
  font-size: 12px;
  color: rgba(255, 255, 255, 0.3);
}



/* ===== 修仙模式：境界信息 ===== */
.score-display__cultivation-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  margin-top: 4px;
}

.score-display__cultivation-level {
  font-size: 11px;
  font-weight: 600;
  color: #C9A84C;
}

.score-display__cultivation-score {
  font-size: 10px;
  color: rgba(201, 168, 76, 0.6);
}
</style>
