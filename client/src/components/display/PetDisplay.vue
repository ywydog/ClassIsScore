<template>
  <div
    class="pet-display"
    :style="{ borderColor: levelBorderColor }"
    @click="$emit('click', student)"
  >
    <!-- 宠物图片区域 + 等级徽章 -->
    <div class="pet-display__top">
      <div
        class="pet-display__image-border"
        :style="imageBorderStyle"
      >
        <img
          :src="petImagePath"
          :alt="petName"
          class="pet-display__image"
          :class="{ 'pet-display__image--unowned': !hasPet }"
          @error="onImageError"
        />
      </div>
      <div class="pet-display__level-badge" :style="{ background: levelBorderColor }">
        Lv.{{ petLevel }}
      </div>
    </div>

    <!-- 毕业标记 -->
    <div v-if="isGraduated" class="pet-display__graduated">已毕业</div>

    <!-- 经验进度条 -->
    <div class="pet-display__exp-section">
      <div class="pet-display__exp-track">
        <div
          class="pet-display__exp-bar"
          :style="expBarStyle"
        ></div>
      </div>
      <span class="pet-display__exp-text">{{ expText }}</span>
    </div>

    <!-- 学生姓名 + 积分 -->
    <div class="pet-display__info">
      <span class="pet-display__name">{{ student.name }}</span>
      <span class="pet-display__score">{{ student.score }}</span>
      <span class="pet-display__pet-name">
        {{ hasPet ? petName : '未领养' }}
        <template v-if="hasPet"> · {{ levelTitle }}</template>
      </span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Student } from '@/types'
import {
  calculateLevel,
  getLevelProgress,
  getLevelBorderColor,
  getLevelTitle,
  getLevelGradient,
  getPetTypeInfo,
  getPetImagePath,
} from '@/utils/petSystem'

const props = defineProps<{
  student: Student
}>()

defineEmits<{
  click: [student: Student]
}>()

const imageLoadFailed = ref(false)

const hasPet = computed(() => !!props.student.petType)
const petLevel = computed(() => calculateLevel(props.student.petExp))
const levelProgress = computed(() => getLevelProgress(props.student.petExp))
const levelBorderColor = computed(() => getLevelBorderColor(petLevel.value))
const levelTitle = computed(() => getLevelTitle(petLevel.value))
const gradient = computed(() => getLevelGradient(petLevel.value))
const petInfo = computed(() => getPetTypeInfo(props.student.petType ?? ''))
const petName = computed(() => petInfo.value?.name ?? '未领养')
const isGraduated = computed(() => petLevel.value >= 8)

const petImagePath = computed(() => {
  if (imageLoadFailed.value) return '/pets/cat/lv1.png'
  return getPetImagePath(props.student.petType, petLevel.value)
})

const imageBorderStyle = computed(() => {
  if (!hasPet.value) {
    return { background: '#AAAAAA' }
  }
  return {
    background: `linear-gradient(135deg, ${gradient.value.start}, ${gradient.value.end})`,
  }
})

const expBarStyle = computed(() => {
  const percentage = levelProgress.value.isMaxLevel ? 100 : levelProgress.value.percentage
  return {
    width: `${percentage}%`,
    background: `linear-gradient(90deg, ${gradient.value.start}, ${gradient.value.end})`,
  }
})

const expText = computed(() => {
  if (levelProgress.value.isMaxLevel) return 'MAX'
  return `${levelProgress.value.current}/${levelProgress.value.required}`
})

function onImageError() {
  imageLoadFailed.value = true
}
</script>

<style scoped>
.pet-display {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px;
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  cursor: pointer;
  transition: all var(--cis-transition-fast);
  border: 2px solid var(--cis-border-color-light);
  width: 180px;
}

.pet-display:hover {
  box-shadow: var(--cis-shadow-card-hover);
  transform: translateY(-2px);
}

.pet-display__top {
  position: relative;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  width: 100%;
  margin-bottom: 4px;
}

.pet-display__image-border {
  width: 96px;
  height: 96px;
  border-radius: var(--cis-radius-lg);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 3px;
}

.pet-display__image {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: calc(var(--cis-radius-lg) - 3px);
  background: var(--cis-card-bg);
  transition: opacity var(--cis-transition-fast);
}

.pet-display__image--unowned {
  opacity: 0.3;
}

.pet-display__level-badge {
  position: absolute;
  top: 0;
  right: 8px;
  padding: 2px 8px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
  color: #fff;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.pet-display__graduated {
  background: linear-gradient(135deg, #FFD700, #FF8C00);
  color: #333;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 10px;
  border-radius: 4px;
  margin-bottom: 4px;
}

.pet-display__exp-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  margin-bottom: 4px;
  width: 100%;
}

.pet-display__exp-track {
  width: 120px;
  height: 6px;
  background: rgba(128, 128, 128, 0.2);
  border-radius: 3px;
  overflow: hidden;
}

.pet-display__exp-bar {
  height: 100%;
  border-radius: 3px;
  transition: width var(--cis-transition-normal);
}

.pet-display__exp-text {
  font-size: 10px;
  color: var(--cis-text-tertiary);
}

.pet-display__info {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.pet-display__name {
  font-size: 14px;
  font-weight: 600;
  color: var(--cis-text-primary);
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pet-display__score {
  font-size: 18px;
  font-weight: 700;
  color: var(--cis-primary);
}

.pet-display__pet-name {
  font-size: 11px;
  color: var(--cis-text-tertiary);
}
</style>
