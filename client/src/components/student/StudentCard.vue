<template>
  <div class="student-card" @click="$emit('click', student)">
    <div class="student-card__avatar">
      <el-avatar :size="40" :src="student.avatar">
        {{ student.name.charAt(0) }}
      </el-avatar>
    </div>
    <div class="student-card__info">
      <div class="student-card__name">{{ student.name }}</div>
      <div class="student-card__meta">
        <span v-if="student.studentNumber" class="student-card__number">{{ student.studentNumber }}</span>
        <el-tag v-if="groupName" size="small" type="info">{{ groupName }}</el-tag>
      </div>
    </div>
    <div class="student-card__score" :class="scoreClass">
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
  if (props.student.score > 0) return 'student-card__score--positive'
  if (props.student.score < 0) return 'student-card__score--negative'
  return 'student-card__score--zero'
})
</script>

<style scoped>
.student-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background-color: var(--cis-card-bg);
  border-radius: var(--cis-radius-md);
  border: 1px solid var(--cis-border-color);
  cursor: pointer;
  transition: box-shadow 0.2s;
}

.student-card:hover {
  box-shadow: var(--cis-shadow-sm);
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

.student-card__score {
  font-size: 20px;
  font-weight: 700;
  min-width: 48px;
  text-align: right;
}

.student-card__score--positive {
  color: var(--el-color-success);
}

.student-card__score--negative {
  color: var(--el-color-danger);
}

.student-card__score--zero {
  color: var(--cis-text-tertiary);
}

.student-card__actions {
  display: flex;
  gap: 4px;
}
</style>
