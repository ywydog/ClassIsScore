<template>
  <div class="score-history">
    <div class="score-history__header">
      <span class="score-history__title">积分历史</span>
    </div>
    <div class="score-history__list">
      <ScoreCard
        v-for="record in records"
        :key="record.id"
        :id="record.id"
        :student-name="record.studentName"
        :score-change="record.scoreChange"
        :reason="record.reason"
        :created-at="record.createdAt"
        :can-quick-revert="record.canQuickRevert"
        @revert="$emit('revert', $event)"
      />
      <el-empty v-if="records.length === 0" description="暂无积分记录" />
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ScoreRecord } from '@/types'
import ScoreCard from './ScoreCard.vue'

defineProps<{
  records: ScoreRecord[]
}>()

defineEmits<{
  revert: [id: string]
}>()
</script>

<style scoped>
.score-history {
  background-color: var(--cis-card-bg);
  border-radius: 8px;
  padding: 16px;
  border: 1px solid var(--cis-border-color);
}

.score-history__header {
  margin-bottom: 12px;
}

.score-history__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.score-history__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 400px;
  overflow-y: auto;
}
</style>
