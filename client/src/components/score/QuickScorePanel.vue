<template>
  <div class="quick-score-panel">
    <div class="quick-score-panel__header">
      <span class="quick-score-panel__title">快捷积分</span>
    </div>
    <div class="quick-score-panel__student">
      <el-select
        v-model="selectedStudentId"
        placeholder="选择学生"
        filterable
        size="default"
        class="quick-score-panel__student-select"
      >
        <el-option
          v-for="s in students"
          :key="s.id"
          :label="s.name"
          :value="s.id"
        >
          <span>{{ s.name }}</span>
          <span style="float: right; color: var(--cis-text-tertiary); font-size: 12px">{{ s.score }}分</span>
        </el-option>
      </el-select>
    </div>
    <div v-if="selectedStudentId" class="quick-score-panel__items">
      <div class="quick-score-panel__group">
        <div class="quick-score-panel__group-label">加分项</div>
        <div class="quick-score-panel__group-items">
          <div
            v-for="item in positiveItems"
            :key="item.id"
            class="quick-score-panel__item quick-score-panel__item--positive"
            @click="handleScore(item)"
          >
            <span class="quick-score-panel__item-name">{{ item.name }}</span>
            <span class="quick-score-panel__item-value">+{{ item.scoreChange }}</span>
          </div>
        </div>
      </div>
      <div class="quick-score-panel__group">
        <div class="quick-score-panel__group-label">扣分项</div>
        <div class="quick-score-panel__group-items">
          <div
            v-for="item in negativeItems"
            :key="item.id"
            class="quick-score-panel__item quick-score-panel__item--negative"
            @click="handleScore(item)"
          >
            <span class="quick-score-panel__item-name">{{ item.name }}</span>
            <span class="quick-score-panel__item-value">{{ item.scoreChange }}</span>
          </div>
        </div>
      </div>
    </div>
    <div v-else class="quick-score-panel__hint">
      请先选择学生
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { EvaluationItem, Student } from '@/types'

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
  background-color: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
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
  font-size: 12px;
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
  transition: transform 0.15s, box-shadow 0.15s;
  user-select: none;
}

.quick-score-panel__item:hover {
  transform: translateY(-1px);
  box-shadow: var(--cis-shadow-sm);
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

.quick-score-panel__hint {
  text-align: center;
  padding: 24px 0;
  color: var(--cis-text-tertiary);
  font-size: 13px;
}
</style>
