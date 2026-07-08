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
    <div class="pet-select-grid">
      <div class="pet-select-grid__section">
        <div class="pet-select-grid__section-title">普通动物</div>
        <div class="pet-select-grid__items">
          <div
            v-for="pet in normalPets"
            :key="pet.id"
            class="pet-select-item"
            :class="{ 'pet-select-item--active': pet.id === student?.petType }"
            @click="emit('select-pet', pet.id)"
          >
            <span class="pet-select-item__emoji">{{ pet.emoji }}</span>
            <span class="pet-select-item__name">{{ pet.name }}</span>
          </div>
        </div>
      </div>
      <div class="pet-select-grid__section">
        <div class="pet-select-grid__section-title">神兽</div>
        <div class="pet-select-grid__items">
          <div
            v-for="pet in mythicalPets"
            :key="pet.id"
            class="pet-select-item"
            :class="{ 'pet-select-item--active': pet.id === student?.petType }"
            @click="emit('select-pet', pet.id)"
          >
            <span class="pet-select-item__emoji">{{ pet.emoji }}</span>
            <span class="pet-select-item__name">{{ pet.name }}</span>
          </div>
        </div>
      </div>
    </div>
    <template #footer>
      <el-button @click="emit('update:visible', false)">取消</el-button>
      <el-button type="danger" plain @click="emit('select-pet', '')">{{ t('removePet') }}</el-button>
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
  transition: all 0.15s ease;
  background: var(--cis-card-bg);
}

.pet-select-item:hover {
  border-color: rgba(13, 148, 136, 0.4);
  background: rgba(13, 148, 136, 0.06);
  transform: translateY(-1px);
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
</style>
