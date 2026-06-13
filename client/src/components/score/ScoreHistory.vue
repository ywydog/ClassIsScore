<template>
  <div class="score-history">
    <div class="score-history__header">
      <span class="score-history__title">积分记录</span>
      <span class="score-history__count">共 {{ filteredRecords.length }} 条</span>
    </div>

    <div class="score-history__filters">
      <el-input
        v-model="searchText"
        placeholder="搜索学生..."
        size="small"
        clearable
        class="score-history__search"
        :prefix-icon="Search"
      />
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="开始日期"
        end-placeholder="结束日期"
        size="small"
        clearable
        class="score-history__date-picker"
      />
    </div>

    <div class="score-history__list">
      <el-skeleton v-if="loading" :loading="loading" :rows="5" animated />
      <template v-else-if="paginatedRecords.length > 0">
        <ScoreCard
          v-for="record in paginatedRecords"
          :key="record.id"
          :id="record.id"
          :student-name="record.studentName"
          :score-change="record.scoreChange"
          :reason="record.reason"
          :created-at="record.createdAt"
          :can-quick-revert="record.canQuickRevert && !record.isReverted"
          :needs-admin-revert="record.needsAdminRevert && !record.isReverted && !record.canQuickRevert"
          :is-reverted="record.isReverted"
          :category-color="getCategoryColor(record.reason)"
          @revert="$emit('revert', $event)"
          @admin-revert="$emit('adminRevert', $event)"
        />
      </template>
      <el-empty v-else description="暂无积分记录" :image-size="80" />
    </div>

    <div v-if="filteredRecords.length > pageSize" class="score-history__pagination">
      <el-pagination
        v-model:current-page="currentPage"
        :page-size="pageSize"
        :total="filteredRecords.length"
        layout="prev, pager, next"
        small
        background
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Search } from '@element-plus/icons-vue'
import type { ScoreRecord, EvaluationItem } from '@/types'
import ScoreCard from './ScoreCard.vue'

const props = defineProps<{
  records: ScoreRecord[]
  evaluationItems?: EvaluationItem[]
  loading?: boolean
}>()

defineEmits<{
  revert: [id: string]
  adminRevert: [id: string]
}>()

function getCategoryColor(reason: string): string | undefined {
  if (!props.evaluationItems) return undefined
  const item = props.evaluationItems.find(i => i.name === reason && i.color)
  return item?.color
}

const searchText = ref('')
const dateRange = ref<[Date, Date] | null>(null)
const currentPage = ref(1)
const pageSize = 10

const filteredRecords = computed(() => {
  let result = props.records

  // 学生名搜索过滤
  if (searchText.value) {
    const keyword = searchText.value.toLowerCase()
    result = result.filter(r => r.studentName.toLowerCase().includes(keyword))
  }

  // 日期范围过滤
  if (dateRange.value) {
    const [start, end] = dateRange.value
    result = result.filter(r => {
      const date = new Date(r.createdAt)
      return date >= start && date <= end
    })
  }

  return result
})

const paginatedRecords = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filteredRecords.value.slice(start, start + pageSize)
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

.score-history__count {
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.score-history__filters {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.score-history__search {
  width: 160px;
}

.score-history__date-picker {
  width: 240px;
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

.score-history__pagination {
  display: flex;
  justify-content: center;
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px solid var(--cis-border-color-light);
}
</style>
