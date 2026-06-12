<template>
  <div class="quick-score-panel">
    <div class="quick-score-panel__header">
      <span class="quick-score-panel__title">快捷操作</span>
    </div>
    <div class="quick-score-panel__items">
      <div
        v-for="item in evaluationItems"
        :key="item.id"
        class="quick-score-panel__item"
        :class="{ 'quick-score-panel__item--positive': item.isPositive, 'quick-score-panel__item--negative': !item.isPositive }"
        @click="$emit('score', item)"
      >
        <span class="quick-score-panel__item-name">{{ item.name }}</span>
        <span class="quick-score-panel__item-value">
          {{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { EvaluationItem } from '@/types'

defineProps<{
  evaluationItems: EvaluationItem[]
}>()

defineEmits<{
  score: [item: EvaluationItem]
}>()
</script>

<style scoped>
.quick-score-panel {
  background-color: var(--cis-card-bg);
  border-radius: 8px;
  padding: 16px;
  border: 1px solid var(--cis-border-color);
}

.quick-score-panel__header {
  margin-bottom: 12px;
}

.quick-score-panel__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.quick-score-panel__items {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.quick-score-panel__item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 13px;
  transition: transform 0.15s, box-shadow 0.15s;
  user-select: none;
}

.quick-score-panel__item:hover {
  transform: translateY(-1px);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}

.quick-score-panel__item:active {
  transform: translateY(0);
}

.quick-score-panel__item--positive {
  background-color: var(--el-color-success-light-9);
  color: var(--el-color-success);
  border: 1px solid var(--el-color-success-light-7);
}

.quick-score-panel__item--negative {
  background-color: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
  border: 1px solid var(--el-color-danger-light-7);
}

.quick-score-panel__item-name {
  font-weight: 500;
}

.quick-score-panel__item-value {
  font-weight: 700;
}
</style>
