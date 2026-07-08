<template>
  <div class="battle-select">
    <div v-if="challenger" class="battle-select__challenger">
      <div class="battle-select__challenger-title">挑战方</div>
      <div class="battle-combatant-card battle-combatant-card--challenger">
        <div class="battle-combatant-card__pet">{{ getPetEmoji(challenger.petType) }}</div>
        <div class="battle-combatant-card__info">
          <div class="battle-combatant-card__name">{{ challenger.name }}</div>
          <div class="battle-combatant-card__level">{{ getCultivationName(challenger.score) }}</div>
          <div class="battle-combatant-card__stats">
            <span>积分 {{ challenger.score }}</span>
            <span>仙宠 {{ getPetLevel(challenger.petExp) }}级</span>
            <span>战力 {{ getPowerIndex(challenger) }}</span>
          </div>
        </div>
      </div>
    </div>

    <div class="battle-select__vs">
      <span class="battle-select__vs-text">VS</span>
    </div>

    <div class="battle-select__opponents">
      <div class="battle-select__opponents-title">选择对手</div>
      <div class="battle-select__opponents-list">
        <div
          v-for="opponent in opponents"
          :key="opponent.id"
          class="battle-select__opponent-card"
          :class="{ 'battle-select__opponent-card--selected': selectedOpponent?.id === opponent.id }"
          @click="emit('select-opponent', opponent)"
        >
          <div class="battle-select__opponent-card-left">
            <div class="battle-select__opponent-pet">{{ getPetEmoji(opponent.petType) }}</div>
            <div class="battle-select__opponent-info">
              <div class="battle-select__opponent-name">{{ opponent.name }}</div>
              <div class="battle-select__opponent-sub">
                {{ getCultivationName(opponent.score) }} · {{ getPetLevel(opponent.petExp) }}级仙宠
              </div>
            </div>
          </div>
          <div class="battle-select__opponent-card-right">
            <div class="battle-select__opponent-score">{{ opponent.score }}</div>
            <div class="battle-select__opponent-power">战力 {{ getPowerIndex(opponent) }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Student } from '@/types'
import {
  getPetEmoji,
  getCultivationName,
  getPetLevel,
  getPowerIndex,
} from '@/composables/useBattleHelpers'

defineProps<{
  challenger: Student | null
  opponents: Student[]
  selectedOpponent: Student | null
}>()

const emit = defineEmits<{
  'select-opponent': [opponent: Student]
}>()
</script>

<style scoped>
.battle-select {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.battle-select__challenger-title,
.battle-select__opponents-title {
  font-size: 12px;
  color: #C9A84C;
  font-weight: 600;
  margin-bottom: 8px;
  text-transform: uppercase;
}

.battle-combatant-card {
  background: rgba(201, 168, 76, 0.08);
  border: 1px solid rgba(201, 168, 76, 0.2);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 16px;
}

.battle-combatant-card__pet {
  font-size: 40px;
  width: 60px;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(201, 168, 76, 0.1);
  border-radius: 12px;
  border: 2px solid rgba(201, 168, 76, 0.3);
}

.battle-combatant-card__info {
  flex: 1;
}

.battle-combatant-card__name {
  font-size: 18px;
  font-weight: 700;
  color: #fff;
  margin-bottom: 4px;
}

.battle-combatant-card__level {
  font-size: 13px;
  color: #C9A84C;
  margin-bottom: 6px;
}

.battle-combatant-card__stats {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
}

.battle-select__vs {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 4px 0;
}

.battle-select__vs-text {
  font-size: 24px;
  font-weight: 900;
  color: #C9A84C;
  text-shadow: 0 0 20px rgba(201, 168, 76, 0.5);
  letter-spacing: 4px;
}

.battle-select__opponents-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 320px;
  overflow-y: auto;
}

.battle-select__opponent-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.battle-select__opponent-card:hover {
  background: rgba(201, 168, 76, 0.08);
  border-color: rgba(201, 168, 76, 0.3);
}

.battle-select__opponent-card--selected {
  background: rgba(201, 168, 76, 0.15);
  border-color: #C9A84C;
  box-shadow: 0 0 20px rgba(201, 168, 76, 0.2);
}

.battle-select__opponent-card-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.battle-select__opponent-pet {
  font-size: 28px;
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 8px;
}

.battle-select__opponent-name {
  font-size: 15px;
  font-weight: 600;
  color: #fff;
  margin-bottom: 2px;
}

.battle-select__opponent-sub {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.5);
}

.battle-select__opponent-card-right {
  text-align: right;
}

.battle-select__opponent-score {
  font-size: 18px;
  font-weight: 700;
  color: #C9A84C;
}

.battle-select__opponent-power {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.5);
  margin-top: 2px;
}
</style>
