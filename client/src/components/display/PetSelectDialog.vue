<template>
  <el-dialog
    :model-value="visible"
    :title="t('selectPet')"
    width="520px"
    class="pet-select-dialog"
    append-to-body
    @update:model-value="emit('update:visible', $event)"
    @close="emit('update:visible', false)"
  >
    <div class="pet-select-grid" role="list">
      <div class="pet-select-grid__section">
        <div class="pet-select-grid__section-title" id="pet-select-normal-title">普通动物</div>
        <div class="pet-select-grid__items" role="group" aria-labelledby="pet-select-normal-title">
          <button
            v-for="pet in normalPets"
            :key="pet.id"
            type="button"
            class="pet-select-item"
            :class="{ 'pet-select-item--active': pet.id === student?.petType }"
            :aria-label="`选择 ${pet.name}`"
            :aria-pressed="pet.id === student?.petType"
            @click="emit('select-pet', pet.id)"
          >
            <span class="pet-select-item__emoji" aria-hidden="true">{{ pet.emoji }}</span>
            <span class="pet-select-item__name">{{ pet.name }}</span>
          </button>
        </div>
      </div>
      <div class="pet-select-grid__section">
        <div class="pet-select-grid__section-title" id="pet-select-mythical-title">神兽</div>
        <div class="pet-select-grid__items" role="group" aria-labelledby="pet-select-mythical-title">
          <button
            v-for="pet in mythicalPets"
            :key="pet.id"
            type="button"
            class="pet-select-item"
            :class="{ 'pet-select-item--active': pet.id === student?.petType }"
            :aria-label="`选择 ${pet.name}`"
            :aria-pressed="pet.id === student?.petType"
            @click="emit('select-pet', pet.id)"
          >
            <span class="pet-select-item__emoji" aria-hidden="true">{{ pet.emoji }}</span>
            <span class="pet-select-item__name">{{ pet.name }}</span>
          </button>
        </div>
      </div>
    </div>
    <template #footer>
      <el-button @click="emit('update:visible', false)" aria-label="取消">取消</el-button>
      <el-button type="danger" plain aria-label="移除仙宠" @click="emit('select-pet', '')">{{ t('removePet') }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Student } from '@/types'
import { PetCategory } from '@/types'
import { ALL_PET_TYPES } from '@/utils/petSystem'
import { useTerminology } from '@/themes/xianxia/useTerminology'

defineProps<{
  visible: boolean
  student: Student | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  'select-pet': [petTypeId: string]
}>()

const { t } = useTerminology()

const normalPets = computed(() => ALL_PET_TYPES.filter(p => p.category === PetCategory.Normal))
const mythicalPets = computed(() => ALL_PET_TYPES.filter(p => p.category === PetCategory.Mythical))
</script>

<style scoped>
.pet-select-grid {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.pet-select-grid__section-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--cis-text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 10px;
}

.pet-select-grid__items {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(90px, 1fr));
  gap: 8px;
}

.pet-select-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 10px 6px;
  border-radius: var(--cis-radius-md, 8px);
  border: 1px solid var(--cis-border-color-light);
  cursor: pointer;
  transition: border-color 0.15s ease, background-color 0.15s ease, transform 0.15s ease;
  background: var(--cis-card-bg);
  font: inherit;
  color: inherit;
}

.pet-select-item:hover {
  border-color: rgba(13, 148, 136, 0.4);
  background: rgba(13, 148, 136, 0.06);
  transform: translateY(-1px);
}

.pet-select-item:focus-visible {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
}

.pet-select-item--active {
  border-color: #0d9488;
  background: rgba(13, 148, 136, 0.1);
  box-shadow: 0 0 0 1px #0d9488;
}

.pet-select-item__emoji {
  font-size: 28px;
  line-height: 1;
}

.pet-select-item__name {
  font-size: 12px;
  font-weight: 500;
  color: var(--cis-text-primary);
  text-align: center;
}

@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
</style>
