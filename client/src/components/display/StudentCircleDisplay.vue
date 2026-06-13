<template>
  <div class="student-circle-display" @click="$emit('click', student)">
    <div class="student-circle-display__avatar-wrapper">
      <div class="student-circle-display__avatar">
        <img v-if="student.avatar" :src="student.avatar" :alt="student.name" />
        <span v-else class="student-circle-display__initial">{{ student.name.charAt(0) }}</span>
      </div>
      <div class="student-circle-display__score-badge">
        {{ student.score }}
      </div>
    </div>
    <span class="student-circle-display__name">{{ student.name }}</span>
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
.student-circle-display {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 8px;
  transition: transform var(--cis-transition-fast);
}

.student-circle-display:hover {
  transform: scale(1.05);
}

.student-circle-display__avatar-wrapper {
  position: relative;
  width: 80px;
  height: 80px;
}

.student-circle-display__avatar {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  overflow: hidden;
  background: var(--cis-gradient-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: var(--cis-shadow-md);
}

.student-circle-display__avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.student-circle-display__initial {
  font-size: 32px;
  font-weight: 600;
  color: #fff;
}

.student-circle-display__score-badge {
  position: absolute;
  bottom: -2px;
  left: 50%;
  transform: translateX(-50%);
  background: rgba(0, 0, 0, 0.75);
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  padding: 2px 10px;
  border-radius: 10px;
  white-space: nowrap;
}

.student-circle-display__name {
  font-size: 13px;
  font-weight: 500;
  color: var(--cis-text-primary);
  max-width: 100px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: center;
}
</style>
