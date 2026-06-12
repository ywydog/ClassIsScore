<template>
  <div class="score-history">
    <div class="score-history__header">
      <span class="score-history__title">积分记录</span>
      <el-input
        v-model="searchText"
        placeholder="搜索学生..."
        size="small"
        clearable
        class="score-history__search"
        :prefix-icon="Search"
      />
    </div>
    <div class="score-history__list">
      <template v-if="filteredRecords.length > 0">
        <ScoreCard
          v-for="record in filteredRecords"
          :key="record.id"
          :id="record.id"
          :student-name="record.studentName"
          :score-change="record.scoreChange"
          :reason="record.reason"
          :created-at="record.createdAt"
          :can-quick-revert="record.canQuickRevert && !record.isReverted"
          :is-reverted="record.isReverted"
          @revert="$emit('revert', $event)"
        />
      </template>
      <el-empty v-else description="暂无积分记录" :image-size="80" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Search } from '@element-plus/icons-vue'
import type { ScoreRecord } from '@/types'
import ScoreCard from './ScoreCard.vue'

const props = defineProps<{
  records: ScoreRecord[]
}>()

defineEmits<{
  revert: [id: string]
}>()

const searchText = ref('')

const filteredRecords = computed(() => {
  if (!searchText.value) return props.records
  const keyword = searchText.value.toLowerCase()
  return props.records.filter(r => r.studentName.toLowerCase().includes(keyword))
})
</script>

<style scoped>
.score-history {
  background-color: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  padding: 16px;
  border: 1px solid var(--cis-border-color);
}

.score-history__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.score-history__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.score-history__search {
  width: 180px;
}

.score-history__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 500px;
  overflow-y: auto;
}

.score-history__list::-webkit-scrollbar {
  width: 4px;
}

.score-history__list::-webkit-scrollbar-thumb {
  background-color: var(--cis-border-color);
  border-radius: 2px;
}
</style>
