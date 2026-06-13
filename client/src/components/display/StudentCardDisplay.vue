<template>
  <div class="student-card-display" @click="$emit('click', student)">
    <div class="student-card-display__avatar">
      <img v-if="student.avatar" :src="student.avatar" :alt="student.name" />
      <span v-else class="student-card-display__initial">{{ student.name.charAt(0) }}</span>
    </div>
    <div class="student-card-display__info">
      <span class="student-card-display__name">{{ student.name }}</span>
      <span class="student-card-display__score">{{ student.score }}</span>
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
}>()
</script>

<style scoped>
.student-card-display {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 12px;
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  cursor: pointer;
  transition: all var(--cis-transition-fast);
  border: 1px solid var(--cis-border-color-light);
}

.student-card-display:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-2px);
}

.student-card-display__avatar {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  overflow: hidden;
  background: var(--cis-gradient-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 10px;
  flex-shrink: 0;
}

.student-card-display__avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.student-card-display__initial {
  font-size: 24px;
  font-weight: 600;
  color: #fff;
}

.student-card-display__info {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  gap: 8px;
}

.student-card-display__name {
  font-size: 14px;
  font-weight: 500;
  color: var(--cis-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.student-card-display__score {
  font-size: 16px;
  font-weight: 700;
  color: var(--cis-primary);
  flex-shrink: 0;
}
</style>
