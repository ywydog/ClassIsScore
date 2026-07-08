<template>
  <div
    class="score-card"
    :class="{
      'score-card--positive': scoreChange > 0,
      'score-card--negative': scoreChange < 0,
      'score-card--reverted': isReverted
    }"
  >
    <div class="score-card__accent"></div>
    <div class="score-card__body">
      <div class="score-card__header">
        <span class="score-card__name">{{ studentName }}</span>
        <span
          class="score-card__change"
          :class="scoreChange > 0 ? 'score-card__change--up' : 'score-card__change--down'"
          :aria-label="`积分变化 ${scoreChange > 0 ? '+' : ''}${scoreChange}`"
        >
          {{ scoreChange > 0 ? '+' : '' }}{{ scoreChange }}
        </span>
      </div>
      <div class="score-card__reason">
        <span v-if="categoryColor" class="score-card__color-dot" :style="{ backgroundColor: categoryColor }" aria-hidden="true"></span>
        {{ reason }}
      </div>
      <div class="score-card__footer">
        <span class="score-card__time" :aria-label="`时间 ${formatTime(createdAt)}`">
          <time :datetime="createdAt">{{ formatTime(createdAt) }}</time>
        </span>
        <el-tag v-if="isReverted" type="info" size="small">已撤销</el-tag>
        <el-button
          v-else-if="canQuickRevert"
          type="danger"
          size="small"
          text
          aria-label="撤销积分"
          @click="handleRevert"
        >
          撤销
        </el-button>
        <el-button
          v-else-if="needsAdminRevert"
          type="warning"
          size="small"
          text
          aria-label="申请撤销积分"
          @click="handleAdminRevert"
        >
          申请撤销
        </el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ElMessageBox } from 'element-plus'

const props = defineProps<{
  id: string
  studentName: string
  scoreChange: number
  reason: string
  createdAt: string
  canQuickRevert: boolean
  needsAdminRevert?: boolean
  isReverted?: boolean
  categoryColor?: string
}>()

const emit = defineEmits<{
  revert: [id: string]
  adminRevert: [id: string]
}>()

const timeFormatter = new Intl.DateTimeFormat('zh-CN', {
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
})

function formatTime(dateStr: string): string {
  const date = new Date(dateStr)
  return timeFormatter.format(date)
}

async function handleRevert() {
  try {
    await ElMessageBox.confirm(
      `确定要撤销「${props.studentName}」的这次积分变更（${props.scoreChange > 0 ? '+' : ''}${props.scoreChange} 分）吗？`,
      '撤销确认',
      {
        type: 'warning',
        confirmButtonText: '撤销',
        cancelButtonText: '取消',
      }
    )
    emit('revert', props.id)
  } catch {
    // 用户取消
  }
}

async function handleAdminRevert() {
  try {
    await ElMessageBox.confirm(
      `确定要申请撤销「${props.studentName}」的这次积分变更（${props.scoreChange > 0 ? '+' : ''}${props.scoreChange} 分）吗？此操作将提交给管理员审核。`,
      '申请撤销确认',
      {
        type: 'warning',
        confirmButtonText: '申请撤销',
        cancelButtonText: '取消',
      }
    )
    emit('adminRevert', props.id)
  } catch {
    // 用户取消
  }
}
</script>

<style scoped>
.score-card {
  display: flex;
  background: var(--cis-card-bg);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  border-radius: var(--cis-radius-lg);
  border: 1px solid var(--cis-border-color-light);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-normal), transform var(--cis-transition-fast);
  overflow: hidden;
}

.score-card:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-1px);
}

.score-card--reverted {
  opacity: 0.5;
}

.score-card--reverted .score-card__name,
.score-card--reverted .score-card__reason {
  text-decoration: line-through;
}

.score-card__accent {
  width: 4px;
  flex-shrink: 0;
  border-radius: 4px 0 0 4px;
  background: var(--cis-border-color);
  transition: background var(--cis-transition-fast);
}

.score-card--positive .score-card__accent {
  background: var(--cis-gradient-success);
}

.score-card--negative .score-card__accent {
  background: linear-gradient(180deg, var(--cis-danger), #f87171);
}

.score-card__body {
  flex: 1;
  padding: 12px 16px;
  min-width: 0;
}

.score-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.score-card__name {
  font-weight: 600;
  font-size: 14px;
  color: var(--cis-text-primary);
}

.score-card__change {
  font-weight: 700;
  font-size: 18px;
  font-family: var(--cis-font-family-display);
  flex-shrink: 0;
  margin-left: 8px;
  font-variant-numeric: tabular-nums;
}

.score-card__change--up {
  color: var(--cis-success);
  text-shadow: 0 0 8px rgba(34, 197, 94, 0.2);
}

.score-card__change--down {
  color: var(--cis-danger);
  text-shadow: 0 0 8px rgba(239, 68, 68, 0.2);
}

.score-card__reason {
  font-size: 13px;
  color: var(--cis-text-secondary);
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  gap: 6px;
}

.score-card__color-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.score-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.score-card__time {
  font-size: 12px;
  color: var(--cis-text-tertiary);
  font-variant-numeric: tabular-nums;
}

@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
</style>
