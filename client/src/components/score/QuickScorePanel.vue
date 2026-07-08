<template>
  <div class="quick-score-panel">
    <div class="quick-score-panel__header">
      <span class="quick-score-panel__title">{{ t('quickEvaluation') }}</span>
    </div>
    <div class="quick-score-panel__student">
      <el-select
        v-model="selectedStudentId"
        :placeholder="t('selectStudent')"
        filterable
        size="default"
        class="quick-score-panel__student-select"
        aria-label="选择学生"
      >
        <el-option
          v-for="s in students"
          :key="s.id"
          :label="s.name"
          :value="s.id"
        >
          <span>{{ s.name }}</span>
          <span style="float: right; color: var(--cis-text-tertiary); font-size: 12px">{{ s.score }}{{ t('scoreUnit') }}</span>
        </el-option>
      </el-select>
    </div>
    <div v-if="selectedStudentId" class="quick-score-panel__items">
      <div class="quick-score-panel__group">
        <div class="quick-score-panel__group-label">{{ t('scorePlusItem') }}</div>
        <div class="quick-score-panel__group-items">
          <button
            v-for="item in positiveItems"
            :key="item.id"
            type="button"
            class="quick-score-panel__item quick-score-panel__item--positive"
            :aria-label="`${item.name}，加 ${item.scoreChange} 分`"
            @click="handleScore(item)"
          >
            <span class="quick-score-panel__item-name">{{ item.name }}</span>
            <span class="quick-score-panel__item-value" aria-hidden="true">+{{ item.scoreChange }}</span>
          </button>
        </div>
      </div>
      <div class="quick-score-panel__group">
        <div class="quick-score-panel__group-label">{{ t('scoreMinusItem') }}</div>
        <div class="quick-score-panel__group-items">
          <button
            v-for="item in negativeItems"
            :key="item.id"
            type="button"
            class="quick-score-panel__item quick-score-panel__item--negative"
            :aria-label="`${item.name}，减 ${Math.abs(item.scoreChange)} 分`"
            @click="handleScore(item)"
          >
            <span class="quick-score-panel__item-name">{{ item.name }}</span>
            <span class="quick-score-panel__item-value" aria-hidden="true">{{ item.scoreChange }}</span>
          </button>
        </div>
      </div>
    </div>
    <div v-else class="quick-score-panel__hint" aria-live="polite">
      {{ t('selectStudentFirst') }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { EvaluationItem, Student } from '@/types'
import { useTerminology } from '@/themes/xianxia/useTerminology'

const { t } = useTerminology()

const props = defineProps<{
  evaluationItems: EvaluationItem[]
  students: Student[]
}>()

const emit = defineEmits<{
  score: [studentId: string, item: EvaluationItem]
}>()

const selectedStudentId = ref<string>('')

const positiveItems = computed(() => props.evaluationItems.filter(i => i.isPositive))
const negativeItems = computed(() => props.evaluationItems.filter(i => !i.isPositive))

function handleScore(item: EvaluationItem) {
  if (!selectedStudentId.value) return
  emit('score', selectedStudentId.value, item)
}
</script>

<style scoped>
.quick-score-panel {
  background: var(--cis-card-bg);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-radius: var(--cis-radius-lg);
  padding: 16px;
  border: 1px solid var(--cis-border-color-light);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-normal);
}

.quick-score-panel:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.quick-score-panel__header {
  margin-bottom: 12px;
}

.quick-score-panel__title {
  font-family: var(--cis-font-family-display);
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.quick-score-panel__student {
  margin-bottom: 16px;
}

.quick-score-panel__student-select {
  width: 100%;
}

.quick-score-panel__group {
  margin-bottom: 12px;
}

.quick-score-panel__group:last-child {
  margin-bottom: 0;
}

.quick-score-panel__group-label {
  font-size: 11px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.quick-score-panel__group-items {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.quick-score-panel__item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: var(--cis-radius-md);
  cursor: pointer;
  font-size: 13px;
  transition: transform var(--cis-transition-fast), box-shadow var(--cis-transition-fast);
  user-select: none;
  position: relative;
  overflow: hidden;
  font: inherit;
  color: inherit;
}

.quick-score-panel__item::after {
  content: '';
  position: absolute;
  inset: 0;
  background: rgba(255, 255, 255, 0);
  transition: background-color var(--cis-transition-fast);
}

.quick-score-panel__item:hover {
  transform: translateY(-1px);
  box-shadow: var(--cis-shadow-card);
}

.quick-score-panel__item:active {
  transform: translateY(0) scale(0.97);
}

.quick-score-panel__item:active::after {
  background: rgba(255, 255, 255, 0.15);
}

.quick-score-panel__item:focus-visible {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
}

.quick-score-panel__item--positive {
  background: linear-gradient(135deg, rgba(34, 197, 94, 0.08), rgba(34, 197, 94, 0.15));
  color: var(--cis-success);
  border: 1px solid rgba(34, 197, 94, 0.2);
}

.quick-score-panel__item--positive:hover {
  background: linear-gradient(135deg, rgba(34, 197, 94, 0.12), rgba(34, 197, 94, 0.2));
  border-color: rgba(34, 197, 94, 0.35);
  box-shadow: 0 2px 8px rgba(34, 197, 94, 0.15);
}

.quick-score-panel__item--negative {
  background: linear-gradient(135deg, rgba(239, 68, 68, 0.08), rgba(239, 68, 68, 0.15));
  color: var(--cis-danger);
  border: 1px solid rgba(239, 68, 68, 0.2);
}

.quick-score-panel__item--negative:hover {
  background: linear-gradient(135deg, rgba(239, 68, 68, 0.12), rgba(239, 68, 68, 0.2));
  border-color: rgba(239, 68, 68, 0.35);
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.15);
}

.quick-score-panel__item-name {
  font-weight: 500;
}

.quick-score-panel__item-value {
  font-weight: 700;
  font-size: 14px;
  font-variant-numeric: tabular-nums;
}

.quick-score-panel__hint {
  text-align: center;
  padding: 24px 0;
  color: var(--cis-text-tertiary);
  font-size: 13px;
}

@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
</style>
