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
          <h1 class="score-display__title">积分排行榜</h1>
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
            <el-radio-button value="personal">个人</el-radio-button>
            <el-radio-button value="group">小组</el-radio-button>
          </el-radio-group>
          <el-radio-group v-model="displayMode" size="small" class="score-display__display-toggle">
            <el-radio-button value="leaderboard">排行</el-radio-button>
            <el-radio-button value="Card">卡片</el-radio-button>
            <el-radio-button value="Circle">圆形</el-radio-button>
            <el-radio-button value="Pet">宠物</el-radio-button>
          </el-radio-group>
          <!-- 设置齿轮按钮 -->
          <button class="score-display__settings-btn" @click="showSettings = !showSettings" title="显示设置">
            <el-icon :size="18"><Setting /></el-icon>
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
          批量评分
        </el-button>
      </div>

      <!-- 排行榜模式：领奖台 + 列表 -->
      <template v-if="displayMode === 'leaderboard'">
        <div v-if="limitedTopThree.length > 0" class="score-display__podium">
          <div
            class="score-display__podium-item score-display__podium--2"
            :class="{ 'score-display__podium-item--animated': mounted }"
            style="--podium-delay: 0.2s"
            v-if="limitedTopThree.length >= 2"
          >
            <div class="score-display__podium-avatar">
              <el-avatar :size="72">{{ limitedTopThree[1].name.charAt(0) }}</el-avatar>
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
              <el-avatar :size="96">{{ limitedTopThree[0].name.charAt(0) }}</el-avatar>
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
              <el-avatar :size="60">{{ limitedTopThree[2].name.charAt(0) }}</el-avatar>
              <div class="score-display__medal score-display__medal--bronze" v-if="displaySettings.showRank">3</div>
            </div>
            <div class="score-display__podium-name">{{ limitedTopThree[2].name }}</div>
            <div class="score-display__podium-score" v-if="displaySettings.showScore">{{ limitedTopThree[2].score }}</div>
          </div>
        </div>

        <div class="score-display__list" v-if="limitedRestEntries.length > 0">
          <div
            v-for="(entry, idx) in limitedRestEntries"
            :key="entry.rank"
            class="score-display__item"
            :class="{ 'score-display__item--animated': mounted }"
            :style="{ '--item-delay': `${idx * 50}ms` }"
          >
            <span class="score-display__rank" v-if="displaySettings.showRank">{{ entry.rank }}</span>
            <span class="score-display__name">{{ entry.name }}</span>
            <span class="score-display__period-stats" v-if="displaySettings.showScore">
              <span class="score-display__period-stat score-display__period-stat--day" :title="`今日 +${getEntryStats(entry)?.dayPlus || 0} / ${getEntryStats(entry)?.dayMinus || 0}`">
                日{{ formatNet(getEntryStats(entry)?.dayNet) }}
              </span>
              <span class="score-display__period-stat score-display__period-stat--week" :title="`本周 +${getEntryStats(entry)?.weekPlus || 0} / ${getEntryStats(entry)?.weekMinus || 0}`">
                周{{ formatNet(getEntryStats(entry)?.weekNet) }}
              </span>
              <span class="score-display__period-stat score-display__period-stat--month" :title="`本月 +${getEntryStats(entry)?.monthPlus || 0} / ${getEntryStats(entry)?.monthMinus || 0}`">
                月{{ formatNet(getEntryStats(entry)?.monthNet) }}
              </span>
            </span>
            <span class="score-display__score" v-if="displaySettings.showScore">{{ entry.score }}</span>
          </div>
        </div>
      </template>

      <!-- 卡片模式 -->
      <div v-else-if="displayMode === 'Card'" class="score-display__grid score-display__grid--card">
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
        </div>
      </div>

      <!-- 圆形模式 -->
      <div v-else-if="displayMode === 'Circle'" class="score-display__grid score-display__grid--circle">
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
      <div v-else-if="displayMode === 'Pet'" class="score-display__grid score-display__grid--pet">
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

    <!-- 分数变化浮动动画 -->
    <transition-group name="score-float" tag="div" class="score-display__float-container">
      <div
        v-for="anim in scoreAnimations"
        :key="anim.id"
        class="score-display__float-anim"
        :class="anim.change > 0 ? 'score-display__float-anim--pos' : 'score-display__float-anim--neg'"
      >
        {{ anim.change > 0 ? '+' : '' }}{{ anim.change }}
      </div>
    </transition-group>

    <!-- 显示设置面板 -->
    <transition name="settings-slide">
      <div v-if="showSettings" class="score-display__settings-panel">
        <div class="score-display__settings-header">
          <h3>显示设置</h3>
          <button class="score-display__settings-close" @click="showSettings = false">
            <el-icon :size="16"><Close /></el-icon>
          </button>
        </div>

        <div class="score-display__settings-body">
          <!-- 背景主题 -->
          <div class="score-display__settings-section">
            <div class="score-display__settings-label">背景主题</div>
            <el-radio-group v-model="displaySettings.background" size="small" @change="saveSettings">
              <el-radio-button value="deepblue">深蓝</el-radio-button>
              <el-radio-button value="pureblack">纯黑</el-radio-button>
              <el-radio-button value="warmgray">暖灰</el-radio-button>
              <el-radio-button value="custom">自定义</el-radio-button>
            </el-radio-group>
            <div v-if="displaySettings.background === 'custom'" class="score-display__settings-color-picker">
              <input type="color" v-model="displaySettings.customColor" @input="saveSettings" />
            </div>
          </div>

          <!-- 字体大小 -->
          <div class="score-display__settings-section">
            <div class="score-display__settings-label">字体大小</div>
            <el-radio-group v-model="displaySettings.fontSize" size="small" @change="saveSettings">
              <el-radio-button value="small">小</el-radio-button>
              <el-radio-button value="medium">中</el-radio-button>
              <el-radio-button value="large">大</el-radio-button>
              <el-radio-button value="xlarge">特大</el-radio-button>
            </el-radio-group>
          </div>

          <!-- 排行榜条目数 -->
          <div class="score-display__settings-section">
            <div class="score-display__settings-label">排行榜条目数</div>
            <el-radio-group v-model="displaySettings.maxItems" size="small" @change="saveSettings">
              <el-radio-button :value="5">5</el-radio-button>
              <el-radio-button :value="10">10</el-radio-button>
              <el-radio-button :value="15">15</el-radio-button>
              <el-radio-button :value="20">20</el-radio-button>
            </el-radio-group>
          </div>

          <!-- 自动刷新间隔 -->
          <div class="score-display__settings-section">
            <div class="score-display__settings-label">自动刷新间隔</div>
            <el-radio-group v-model="displaySettings.refreshInterval" size="small" @change="onRefreshIntervalChange">
              <el-radio-button :value="10">10s</el-radio-button>
              <el-radio-button :value="30">30s</el-radio-button>
              <el-radio-button :value="60">60s</el-radio-button>
              <el-radio-button :value="0">关闭</el-radio-button>
            </el-radio-group>
          </div>

          <!-- 显示/隐藏开关 -->
          <div class="score-display__settings-section">
            <div class="score-display__settings-label">显示元素</div>
            <div class="score-display__settings-switches">
              <label class="score-display__settings-switch">
                <span>时钟</span>
                <input type="checkbox" v-model="displaySettings.showClock" @change="saveSettings" />
              </label>
              <label class="score-display__settings-switch">
                <span>排名数字</span>
                <input type="checkbox" v-model="displaySettings.showRank" @change="saveSettings" />
              </label>
              <label class="score-display__settings-switch">
                <span>分数</span>
                <input type="checkbox" v-model="displaySettings.showScore" @change="saveSettings" />
              </label>
            </div>
          </div>

          <!-- 全屏按钮 -->
          <div class="score-display__settings-section">
            <button class="score-display__settings-fullscreen-btn" @click="toggleFullscreen">
              {{ isFullscreen ? '退出全屏' : '进入全屏' }}
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 快速评分底部栏 -->
    <transition name="quick-score-slide">
      <div v-if="quickScoreStudent && !multiSelectMode" class="quick-score-bar">
        <div class="quick-score-bar__inner">
          <div class="quick-score-bar__student">
            <span class="quick-score-bar__student-name">{{ quickScoreStudent.name }}</span>
            <span class="quick-score-bar__student-score">当前 {{ quickScoreStudent.score }} 分</span>
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
      title="批量评分"
      width="480px"
      :close-on-click-modal="false"
      class="score-display__batch-dialog"
      append-to-body
    >
      <div class="batch-score-form">
        <div class="batch-score-form__info">
          已选择 <strong>{{ selectedStudentIds.length }}</strong> 名学生
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
        <el-button type="primary" :disabled="!batchScoreChange" @click="submitBatchScore">确认评分</el-button>
      </template>
    </el-dialog>

    <!-- 宠物选择对话框 -->
    <el-dialog
      v-model="showPetDialog"
      title="选择宠物"
      width="520px"
      class="score-display__pet-dialog"
      append-to-body
    >
      <div class="pet-select-grid">
        <div class="pet-select-grid__section">
          <div class="pet-select-grid__section-title">普通动物</div>
          <div class="pet-select-grid__items">
            <div
              v-for="pet in normalPets"
              :key="pet.id"
              class="pet-select-item"
              :class="{ 'pet-select-item--active': pet.id === petDialogStudent?.petType }"
              @click="selectPet(pet.id)"
            >
              <span class="pet-select-item__emoji">{{ pet.emoji }}</span>
              <span class="pet-select-item__name">{{ pet.name }}</span>
            </div>
          </div>
        </div>
        <div class="pet-select-grid__section">
          <div class="pet-select-grid__section-title">神兽</div>
          <div class="pet-select-grid__items">
            <div
              v-for="pet in mythicalPets"
              :key="pet.id"
              class="pet-select-item"
              :class="{ 'pet-select-item--active': pet.id === petDialogStudent?.petType }"
              @click="selectPet(pet.id)"
            >
              <span class="pet-select-item__emoji">{{ pet.emoji }}</span>
              <span class="pet-select-item__name">{{ pet.name }}</span>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="showPetDialog = false">取消</el-button>
        <el-button type="danger" plain @click="selectPet('')">移除宠物</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, reactive } from 'vue'
import { Trophy, Check, Close, Setting } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { LeaderboardEntry, Student, EvaluationItem, ScoreUpdateEvent, StudentScoreStats } from '@/types'
import { PetCategory } from '@/types'
import api from '@/services/api'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'
import { studentApi } from '@/services/student'
import { scoreApi } from '@/services/score'
import { useSettingsStore } from '@/stores/settings'
import { ALL_PET_TYPES } from '@/utils/petSystem'
import StudentCardDisplay from '@/components/display/StudentCardDisplay.vue'
import StudentCircleDisplay from '@/components/display/StudentCircleDisplay.vue'
import PetDisplay from '@/components/display/PetDisplay.vue'

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
const displayMode = ref<'leaderboard' | 'Card' | 'Circle' | 'Pet'>('leaderboard')
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

let timeTimer: ReturnType<typeof setInterval> | null = null
let refreshTimer: ReturnType<typeof setInterval> | null = null

const settingsStore = useSettingsStore()

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

const normalPets = computed(() => ALL_PET_TYPES.filter(p => p.category === PetCategory.Normal))
const mythicalPets = computed(() => ALL_PET_TYPES.filter(p => p.category === PetCategory.Mythical))

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
    const endpoint = mode.value === 'personal' ? '/api/leaderboard/personal' : '/api/leaderboard/group'
    const response = await api.get<{ data: LeaderboardEntry[] }>(endpoint)
    leaderboard.value = response.data.data
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
    const response = await api.get<{ data: EvaluationItem[] }>('/api/evaluation/items')
    evaluationItems.value = response.data.data || []
  } catch {
    // silent
  }
}

async function fetchScoreStats() {
  try {
    const semesterStartDate = settingsStore.settings.semesterStartDate
    const response = await scoreApi.getStats(semesterStartDate)
    scoreStats.value = response.data.data || []
  } catch {
    // silent
  }
}

function getStudentStats(studentId: string | number): StudentScoreStats | undefined {
  return scoreStats.value.find(s => String(s.studentId) === String(studentId))
}

function getEntryStats(entry: LeaderboardEntry): StudentScoreStats | undefined {
  // 通过名字匹配（排行榜没有 studentId）
  return scoreStats.value.find(s => s.studentName === entry.name)
}

function formatNet(val: number | undefined): string {
  if (val === undefined || val === 0) return '0'
  return val > 0 ? `+${val}` : `${val}`
}
</script>

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

.score-display__period-stats {
  display: flex;
  gap: 6px;
  align-items: center;
}

.score-display__period-stat {
  font-size: 11px;
  font-weight: 600;
  padding: 2px 6px;
  border-radius: 4px;
  white-space: nowrap;
}

.score-display__period-stat--day {
  background: rgba(59, 130, 246, 0.12);
  color: #60a5fa;
}

.score-display__period-stat--week {
  background: rgba(168, 85, 247, 0.12);
  color: #c084fc;
}

.score-display__period-stat--month {
  background: rgba(245, 158, 11, 0.12);
  color: #fbbf24;
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

/* ===== 宠物选择对话框 ===== */
.pet-select-grid {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.pet-select-grid__section-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 10px;
}

.pet-select-grid__items {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(90px, 1fr));
  gap: 8px;
}

.pet-select-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 10px 6px;
  border-radius: var(--cis-radius-md, 8px);
  border: 1px solid var(--cis-border-color-light);
  cursor: pointer;
  transition: all 0.15s ease;
  background: var(--cis-card-bg);
}

.pet-select-item:hover {
  border-color: rgba(13, 148, 136, 0.4);
  background: rgba(13, 148, 136, 0.06);
  transform: translateY(-1px);
}

.pet-select-item--active {
  border-color: #0d9488;
  background: rgba(13, 148, 136, 0.1);
  box-shadow: 0 0 0 1px #0d9488;
}

.pet-select-item__emoji {
  font-size: 28px;
  line-height: 1;
}

.pet-select-item__name {
  font-size: 12px;
  font-weight: 500;
  color: var(--cis-text-primary);
  text-align: center;
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

/* ===== 显示设置面板 ===== */
.score-display__settings-panel {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  width: 320px;
  z-index: 150;
  background: rgba(10, 22, 40, 0.88);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border-left: 1px solid rgba(13, 148, 136, 0.2);
  box-shadow: -8px 0 32px rgba(0, 0, 0, 0.4);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.score-display__settings-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.score-display__settings-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #fff;
}

.score-display__settings-close {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
}

.score-display__settings-close:hover {
  background: rgba(255, 255, 255, 0.12);
  color: #fff;
}

.score-display__settings-body {
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.score-display__settings-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.score-display__settings-label {
  font-size: 13px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.5);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.score-display__settings-section :deep(.el-radio-button__inner) {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.12);
  color: rgba(255, 255, 255, 0.5);
  font-size: 12px;
  padding: 6px 12px;
}

.score-display__settings-section :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  border-color: #0d9488;
  color: #fff;
}

.score-display__settings-color-picker {
  margin-top: 4px;
}

.score-display__settings-color-picker input[type="color"] {
  width: 48px;
  height: 32px;
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  padding: 2px;
}

.score-display__settings-switches {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.score-display__settings-switch {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.8);
}

.score-display__settings-switch input[type="checkbox"] {
  width: 40px;
  height: 22px;
  appearance: none;
  -webkit-appearance: none;
  background: rgba(255, 255, 255, 0.12);
  border-radius: 11px;
  position: relative;
  transition: background 0.2s ease;
  cursor: pointer;
}

.score-display__settings-switch input[type="checkbox"]::after {
  content: '';
  position: absolute;
  top: 2px;
  left: 2px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.6);
  transition: transform 0.2s ease, background 0.2s ease;
}

.score-display__settings-switch input[type="checkbox"]:checked {
  background: rgba(13, 148, 136, 0.6);
}

.score-display__settings-switch input[type="checkbox"]:checked::after {
  transform: translateX(18px);
  background: #2dd4bf;
}

.score-display__settings-fullscreen-btn {
  width: 100%;
  padding: 10px 0;
  border-radius: 10px;
  border: 1px solid rgba(13, 148, 136, 0.3);
  background: rgba(13, 148, 136, 0.1);
  color: #2dd4bf;
  font-size: 14px;
  font-weight: 600;
  transition: all 0.2s ease;
}

.score-display__settings-fullscreen-btn:hover {
  background: rgba(13, 148, 136, 0.2);
  border-color: rgba(13, 148, 136, 0.5);
  box-shadow: 0 2px 12px rgba(13, 148, 136, 0.2);
}

/* 设置面板滑入动画 */
.settings-slide-enter-active,
.settings-slide-leave-active {
  transition: transform 0.35s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.35s ease;
}

.settings-slide-enter-from,
.settings-slide-leave-to {
  transform: translateX(100%);
  opacity: 0;
}
</style>
