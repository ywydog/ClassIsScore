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
        <span class="student-card__score">积分: {{ student.score }}</span>
      </div>
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
import type { Student } from '@/types'

defineProps<{
  student: Student
}>()

defineEmits<{
  click: [student: Student]
  edit: [student: Student]
  delete: [id: string]
}>()
</script>

<style scoped>
.student-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background-color: var(--cis-card-bg);
  border-radius: 8px;
  border: 1px solid var(--cis-border-color);
  cursor: pointer;
  transition: box-shadow 0.2s;
}

.student-card:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
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
  gap: 12px;
  font-size: 12px;
  color: var(--cis-text-secondary);
  margin-top: 2px;
}

.student-card__actions {
  display: flex;
  gap: 4px;
}
</style>
