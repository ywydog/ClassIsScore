<template>
  <div
    class="score-card"
    :class="{
      'score-card--positive': scoreChange > 0,
      'score-card--negative': scoreChange < 0,
      'score-card--reverted': isReverted
    }"
  >
    <div class="score-card__header">
      <span class="score-card__name">{{ studentName }}</span>
      <span class="score-card__change" :class="scoreChange > 0 ? 'score-card__change--up' : 'score-card__change--down'">
        {{ scoreChange > 0 ? '+' : '' }}{{ scoreChange }}
      </span>
    </div>
    <div class="score-card__reason">{{ reason }}</div>
    <div class="score-card__footer">
      <span class="score-card__time">{{ formatTime(createdAt) }}</span>
      <el-tag v-if="isReverted" type="info" size="small">已撤销</el-tag>
      <el-button
        v-else-if="canQuickRevert"
        type="danger"
        size="small"
        text
        @click="$emit('revert', id)"
      >
        撤销
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  id: string
  studentName: string
  scoreChange: number
  reason: string
  createdAt: string
  canQuickRevert: boolean
  isReverted?: boolean
}>()

defineEmits<{
  revert: [id: string]
}>()

function formatTime(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}
</script>

<style scoped>
.score-card {
  background-color: var(--cis-card-bg);
  border-radius: var(--cis-radius-md);
  padding: 12px 16px;
  border: 1px solid var(--cis-border-color);
  transition: box-shadow 0.2s, opacity 0.2s;
}

.score-card:hover {
  box-shadow: var(--cis-shadow-sm);
}

.score-card--reverted {
  opacity: 0.5;
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
  font-size: 16px;
}

.score-card__change--up {
  color: var(--el-color-success);
}

.score-card__change--down {
  color: var(--el-color-danger);
}

.score-card__reason {
  font-size: 13px;
  color: var(--cis-text-secondary);
  margin-bottom: 8px;
}

.score-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.score-card__time {
  font-size: 12px;
  color: var(--cis-text-tertiary);
}
</style>
