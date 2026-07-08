<template>
  <div class="student-profile" v-loading="loading">
    <!-- 返回按钮 -->
    <div class="student-profile__back">
      <el-button text @click="goBack" aria-label="返回学生列表">
        <el-icon aria-hidden="true"><ArrowLeft /></el-icon>
        返回学生列表
      </el-button>
    </div>

    <div class="student-profile__content" v-if="student">
      <!-- 学生信息卡片 -->
      <el-card class="student-profile__card student-profile__info-card">
        <div class="profile-info">
          <div class="profile-info__avatar" :style="avatarStyle" aria-hidden="true">
            <img v-if="student.avatar" :src="student.avatar" :alt="student.name" />
            <span v-else class="profile-info__initial">{{ student.name.charAt(0) }}</span>
          </div>
          <div class="profile-info__details">
            <h2 id="student-profile-name" class="profile-info__name">{{ student.name }}</h2>
            <div class="profile-info__meta">
              <span v-if="student.studentNumber">学号: {{ student.studentNumber }}</span>
              <span>当前积分: <strong :class="scoreClass" style="font-variant-numeric: tabular-nums">{{ student.score }}</strong></span>
            </div>
            <div class="profile-info__pet" v-if="hasPet">
              <span class="profile-info__pet-emoji" aria-hidden="true">{{ petEmoji }}</span>
              <span>{{ petName }} Lv.{{ petLevel }} ({{ levelTitle }})</span>
              <div class="profile-info__pet-exp" role="progressbar" :aria-valuenow="levelProgress.percentage" aria-valuemin="0" aria-valuemax="100" :aria-label="`宠物经验 ${expText}`">
                <div class="profile-info__pet-exp-track">
                  <div class="profile-info__pet-exp-bar" :style="expBarStyle"></div>
                </div>
                <span class="profile-info__pet-exp-text" aria-live="polite">{{ expText }}</span>
              </div>
            </div>
            <div class="profile-info__pet" v-else>
              <span class="profile-info__pet-emoji" aria-hidden="true">❓</span>
              <span style="color: var(--cis-text-tertiary)">尚未领养宠物</span>
            </div>
          </div>
          <div class="profile-info__pet-image" v-if="hasPet">
            <img :src="petImagePath" :alt="petName" @error="onPetImageError" />
          </div>
        </div>
      </el-card>

      <!-- 积分统计卡片 -->
      <el-card class="student-profile__card">
        <template #header>
          <span class="card-title">积分统计</span>
        </template>
        <div class="stats-grid">
          <div class="stats-item">
            <span class="stats-item__value stats-item__value--primary" style="font-variant-numeric: tabular-nums" aria-live="polite">{{ stats.totalChanges }}</span>
            <span class="stats-item__label">总变动</span>
          </div>
          <div class="stats-item">
            <span class="stats-item__value stats-item__value--success" style="font-variant-numeric: tabular-nums" aria-live="polite">{{ stats.positiveChanges }}</span>
            <span class="stats-item__label">加分</span>
          </div>
          <div class="stats-item">
            <span class="stats-item__value stats-item__value--danger" style="font-variant-numeric: tabular-nums" aria-live="polite">{{ stats.negativeChanges }}</span>
            <span class="stats-item__label">减分</span>
          </div>
          <div class="stats-item">
            <span class="stats-item__value stats-item__value--primary" style="font-variant-numeric: tabular-nums" aria-live="polite">{{ stats.netChange > 0 ? '+' : '' }}{{ stats.netChange }}</span>
            <span class="stats-item__label">净变动</span>
          </div>
        </div>
      </el-card>

      <!-- 积分趋势图 -->
      <el-card class="student-profile__card">
        <template #header>
          <span class="card-title">积分趋势</span>
        </template>
        <div class="trend-chart" ref="trendChartRef" role="img" aria-label="最近积分趋势图">
          <div v-if="trendData.length === 0" class="trend-chart__empty">暂无趋势数据</div>
          <svg v-else :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="trend-chart__svg" aria-hidden="true">
            <!-- 网格线 -->
            <line v-for="i in 4" :key="'grid-'+i"
              :x1="chartPadding" :y1="chartPadding + (i-1) * (chartHeight - 2*chartPadding) / 3"
              :x2="chartWidth - chartPadding" :y2="chartPadding + (i-1) * (chartHeight - 2*chartPadding) / 3"
              stroke="var(--cis-border-color-light)" stroke-width="1" stroke-dasharray="4,4"
            />
            <!-- 渐变定义 -->
            <defs>
              <linearGradient id="trendGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="var(--cis-primary)" stop-opacity="0.2" />
                <stop offset="100%" stop-color="var(--cis-primary)" stop-opacity="0" />
              </linearGradient>
            </defs>
            <!-- 渐变填充区域 -->
            <polygon
              :points="trendAreaPoints"
              fill="url(#trendGradient)"
            />
            <!-- 趋势线 -->
            <polyline
              :points="trendLinePoints"
              fill="none"
              stroke="var(--cis-primary)"
              stroke-width="2.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
            <!-- 数据点 -->
            <circle v-for="(point, i) in trendPoints" :key="'point-'+i"
              :cx="point.x" :cy="point.y" r="4"
              fill="var(--cis-card-bg)" stroke="var(--cis-primary)" stroke-width="2"
            />
            <!-- 日期标签 -->
            <text v-for="(point, i) in trendPoints" :key="'label-'+i"
              :x="point.x" :y="chartHeight - 4"
              text-anchor="middle" font-size="10"
              fill="var(--cis-text-tertiary)"
            >{{ point.label }}</text>
          </svg>
        </div>
      </el-card>

      <!-- 最近积分记录 -->
      <el-card class="student-profile__card">
        <template #header>
          <span class="card-title">最近记录</span>
        </template>
        <ol class="recent-records" aria-label="最近积分记录">
          <li v-for="record in recentRecords" :key="record.id" class="recent-record">
            <span class="recent-record__change" :class="record.scoreChange > 0 ? 'recent-record__change--positive' : 'recent-record__change--negative'" style="font-variant-numeric: tabular-nums">
              {{ record.scoreChange > 0 ? '+' : '' }}{{ record.scoreChange }}
            </span>
            <span class="recent-record__reason">{{ record.reason }}</span>
            <span class="recent-record__time">{{ formatTime(record.createdAt) }}</span>
          </li>
          <li v-if="recentRecords.length === 0" class="recent-records__empty">
            <el-empty description="暂无积分记录" :image-size="60" />
          </li>
        </ol>
      </el-card>
    </div>

    <el-empty v-else-if="!loading" description="未找到该学生" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import { useStudentStore } from '@/stores/student'
import { useScoreStore } from '@/stores/score'
import type { Student, ScoreRecord } from '@/types'
import {
  calculateLevel,
  getLevelProgress,
  getLevelTitle,
  getLevelGradient,
  getPetTypeInfo,
  getPetEmoji,
  getPetImagePath,
} from '@/utils/petSystem'

const route = useRoute()
const router = useRouter()
const studentStore = useStudentStore()
const scoreStore = useScoreStore()

const loading = ref(true)
const student = ref<Student | null>(null)
const recentRecords = ref<ScoreRecord[]>([])
const trendData = ref<Array<{ date: string; score: number; change: number }>>([])

const chartWidth = 600
const chartHeight = 200
const chartPadding = 30

const hasPet = computed(() => !!student.value?.petType)
const petLevel = computed(() => student.value ? calculateLevel(student.value.petExp) : 1)
const levelTitle = computed(() => getLevelTitle(petLevel.value))
const petInfo = computed(() => getPetTypeInfo(student.value?.petType ?? ''))
const petName = computed(() => petInfo.value?.name ?? '未领养')
const petEmoji = computed(() => getPetEmoji(student.value?.petType))
const levelProgress = computed(() => getLevelProgress(student.value?.petExp ?? 0))
const gradient = computed(() => getLevelGradient(petLevel.value))

const petImagePath = computed(() => getPetImagePath(student.value?.petType, petLevel.value))

const avatarStyle = computed(() => {
  if (hasPet.value) {
    return {
      background: `linear-gradient(135deg, ${gradient.value.start}, ${gradient.value.end})`,
    }
  }
  return { background: 'var(--cis-gradient-primary)' }
})

const scoreClass = computed(() => {
  if (!student.value) return ''
  if (student.value.score > 0) return 'score-positive'
  if (student.value.score < 0) return 'score-negative'
  return ''
})

const expBarStyle = computed(() => {
  const percentage = levelProgress.value.isMaxLevel ? 100 : levelProgress.value.percentage
  return {
    width: `${percentage}%`,
    background: `linear-gradient(90deg, ${gradient.value.start}, ${gradient.value.end})`,
  }
})

const expText = computed(() => {
  if (levelProgress.value.isMaxLevel) return 'MAX'
  return `${levelProgress.value.current}/${levelProgress.value.required}`
})

const stats = computed(() => {
  const validRecords = recentRecords.value.filter(r => !r.isReverted)
  return {
    totalChanges: validRecords.length,
    positiveChanges: validRecords.filter(r => r.scoreChange > 0).length,
    negativeChanges: validRecords.filter(r => r.scoreChange < 0).length,
    netChange: validRecords.reduce((sum, r) => sum + r.scoreChange, 0),
  }
})

const trendPoints = computed(() => {
  if (trendData.value.length === 0) return []
  const data = trendData.value
  const minScore = Math.min(...data.map(d => d.score))
  const maxScore = Math.max(...data.map(d => d.score))
  const scoreRange = maxScore - minScore || 1
  const xStep = (chartWidth - 2 * chartPadding) / Math.max(data.length - 1, 1)

  return data.map((d, i) => ({
    x: chartPadding + i * xStep,
    y: chartPadding + (1 - (d.score - minScore) / scoreRange) * (chartHeight - 2 * chartPadding),
    label: d.date.slice(5),
  }))
})

const trendLinePoints = computed(() => trendPoints.value.map(p => `${p.x},${p.y}`).join(' '))
const trendAreaPoints = computed(() => {
  if (trendPoints.value.length === 0) return ''
  const linePoints = trendPoints.value.map(p => `${p.x},${p.y}`).join(' ')
  const lastPoint = trendPoints.value[trendPoints.value.length - 1]
  const firstPoint = trendPoints.value[0]
  return `${linePoints} ${lastPoint.x},${chartHeight - chartPadding} ${firstPoint.x},${chartHeight - chartPadding}`
})

onMounted(async () => {
  const studentId = route.params.id as string
  if (!studentId) {
    loading.value = false
    return
  }

  try {
    // 获取学生信息
    const s = studentStore.getStudentById(studentId)
    if (s) {
      student.value = s
    } else {
      await studentStore.fetchStudents()
      student.value = studentStore.getStudentById(studentId) ?? null
    }

    // 获取积分记录
    await scoreStore.fetchRecords()
    const allRecords = scoreStore.recentRecords
    recentRecords.value = allRecords.filter(r => r.studentId === studentId)

    // 计算趋势数据（按日汇总）
    calculateTrend()
  } finally {
    loading.value = false
  }
})

function calculateTrend() {
  const validRecords = recentRecords.value
    .filter(r => !r.isReverted)
    .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())

  if (validRecords.length === 0) {
    trendData.value = []
    return
  }

  // 按日期分组
  const dailyMap = new Map<string, { change: number; score: number }>()
  let cumulative = 0

  for (const record of validRecords) {
    const dateKey = new Date(record.createdAt).toISOString().slice(0, 10)
    cumulative += record.scoreChange
    const existing = dailyMap.get(dateKey)
    if (existing) {
      existing.change += record.scoreChange
      existing.score = cumulative
    } else {
      dailyMap.set(dateKey, { change: record.scoreChange, score: cumulative })
    }
  }

  trendData.value = Array.from(dailyMap.entries()).map(([date, data]) => ({
    date,
    score: data.score,
    change: data.change,
  }))
}

function goBack() {
  router.push('/admin/students')
}

const profileDateFormatter = new Intl.DateTimeFormat('zh-CN', { month: '2-digit', day: '2-digit' })
const profileTimeFormatter = new Intl.DateTimeFormat('zh-CN', { hour: '2-digit', minute: '2-digit', hour12: false })

function formatTime(dateStr: string): string {
  const date = new Date(dateStr)
  return `${profileDateFormatter.format(date)} ${profileTimeFormatter.format(date)}`
}

function onPetImageError() {
  // Fallback handled by petSystem
}
</script>

<style scoped>
.student-profile {
  max-width: 720px;
}

.student-profile__back {
  margin-bottom: 16px;
}

.student-profile__content {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.student-profile__card {
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.student-profile__card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.card-title {
  font-family: var(--cis-font-family-display);
  font-size: 16px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

/* 学生信息卡片 */
.profile-info {
  display: flex;
  align-items: center;
  gap: 20px;
}

.profile-info__avatar {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: var(--cis-shadow-glow);
}

.profile-info__avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.profile-info__initial {
  font-size: 30px;
  font-weight: 600;
  color: #fff;
}

.profile-info__details {
  flex: 1;
  min-width: 0;
}

.profile-info__name {
  margin: 0;
  font-family: var(--cis-font-family-display);
  font-size: 22px;
  font-weight: 700;
  color: var(--cis-text-primary);
  scroll-margin-top: 80px;
}

.profile-info__meta {
  display: flex;
  gap: 16px;
  font-size: 13px;
  color: var(--cis-text-secondary);
  margin-top: 4px;
}

.score-positive { color: var(--cis-success); font-weight: 700; }
.score-negative { color: var(--cis-danger); font-weight: 700; }

.profile-info__pet {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 6px;
  font-size: 13px;
  color: var(--cis-text-secondary);
}

.profile-info__pet-emoji {
  font-size: 18px;
}

.profile-info__pet-exp {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-left: 4px;
}

.profile-info__pet-exp-track {
  height: 4px;
  width: 60px;
  border-radius: 2px;
  background: rgba(128, 128, 128, 0.2);
  overflow: hidden;
}

.profile-info__pet-exp-bar {
  height: 100%;
  border-radius: 2px;
  transition: width var(--cis-transition-normal);
}

.profile-info__pet-exp-text {
  font-size: 10px;
  color: var(--cis-text-tertiary);
}

.profile-info__pet-image {
  width: 80px;
  height: 80px;
  flex-shrink: 0;
}

.profile-info__pet-image img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* 积分统计 */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

.stats-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.stats-item__value {
  font-size: 28px;
  font-weight: 700;
  font-family: var(--cis-font-family-display);
}

.stats-item__value--primary { color: var(--cis-primary); }
.stats-item__value--success { color: var(--cis-success); }
.stats-item__value--danger { color: var(--cis-danger); }

.stats-item__label {
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

/* 趋势图 */
.trend-chart {
  width: 100%;
  min-height: 200px;
}

.trend-chart__empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 200px;
  color: var(--cis-text-tertiary);
  font-size: 14px;
}

.trend-chart__svg {
  width: 100%;
  height: auto;
}

/* 最近记录 */
.recent-records {
  display: flex;
  flex-direction: column;
  list-style: none;
  margin: 0;
  padding: 0;
}

.recent-record {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 0;
  border-bottom: 1px solid var(--cis-border-color-light);
  list-style: none;
}

.recent-record:last-child {
  border-bottom: none;
}

.recent-records__empty {
  list-style: none;
}

.recent-record__change {
  font-weight: 700;
  font-size: 14px;
  min-width: 50px;
}

.recent-record__change--positive { color: var(--cis-success); }
.recent-record__change--negative { color: var(--cis-danger); }

.recent-record__reason {
  flex: 1;
  font-size: 13px;
  color: var(--cis-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.recent-record__time {
  font-size: 12px;
  color: var(--cis-text-tertiary);
  white-space: nowrap;
}

@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}
</style>
