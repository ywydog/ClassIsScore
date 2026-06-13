<template>
  <div class="student-card" @click="$emit('click', student)">
    <div class="student-card__avatar">
      <div class="student-card__avatar-wrapper">
        <el-avatar :size="40" :src="student.avatar">
          {{ student.name.charAt(0) }}
        </el-avatar>
      </div>
    </div>
    <div class="student-card__info">
      <div class="student-card__name">{{ student.name }}</div>
      <div class="student-card__meta">
        <span v-if="student.studentNumber" class="student-card__number">{{ student.studentNumber }}</span>
        <el-tag v-if="groupName" size="small" type="info">{{ groupName }}</el-tag>
      </div>
    </div>
    <div class="student-card__score-badge" :class="scoreClass">
      {{ student.score }}
    </div>
    <div class="student-card__actions">
      <el-button type="primary" size="small" text @click.stop="$emit('edit', student)">
        编辑
      </el-button>
      <el-button type="danger" size="small" text @click.stop="$emit('delete', student.id)">
        删除
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Student } from '@/types'

const props = defineProps<{
  student: Student
  groupName?: string
}>()

defineEmits<{
  click: [student: Student]
  edit: [student: Student]
  delete: [id: string]
}>()

const scoreClass = computed(() => {
  if (props.student.score > 0) return 'student-card__score-badge--positive'
  if (props.student.score < 0) return 'student-card__score-badge--negative'
  return 'student-card__score-badge--zero'
})
</script>

<style scoped>
.student-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: var(--cis-card-bg);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  border-radius: var(--cis-radius-lg);
  border: 1px solid var(--cis-border-color-light);
  box-shadow: var(--cis-shadow-card);
  cursor: pointer;
  transition: box-shadow var(--cis-transition-normal), transform var(--cis-transition-fast);
}

.student-card:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-2px);
}

.student-card__avatar-wrapper {
  position: relative;
  border-radius: var(--cis-radius-full);
  padding: 2px;
  background: var(--cis-gradient-primary);
}

.student-card__avatar-wrapper :deep(.el-avatar) {
  border: 2px solid var(--cis-card-bg);
}

.student-card__info {
  flex: 1;
  min-width: 0;
}

.student-card__name {
  font-weight: 600;
  font-size: 14px;
  color: var(--cis-text-primary);
}

.student-card__meta {
  display: flex;
  gap: 8px;
  font-size: 12px;
  color: var(--cis-text-secondary);
  margin-top: 2px;
  align-items: center;
}

.student-card__score-badge {
  font-family: var(--cis-font-family-display);
  font-size: 20px;
  font-weight: 700;
  min-width: 48px;
  text-align: center;
  padding: 4px 10px;
  border-radius: var(--cis-radius-md);
  flex-shrink: 0;
}

.student-card__score-badge--positive {
  color: var(--cis-success);
  background: rgba(34, 197, 94, 0.1);
}

.student-card__score-badge--negative {
  color: var(--cis-danger);
  background: rgba(239, 68, 68, 0.1);
}

.student-card__score-badge--zero {
  color: var(--cis-text-tertiary);
  background: var(--cis-bg-secondary);
}

.student-card__actions {
  display: flex;
  gap: 4px;
}
</style>
