<template>
  <div class="battle-screen">
    <div class="battle-screen__arena" role="group" aria-label="战斗双方">
      <!-- 挑战者 -->
      <div
        class="battle-fighter"
        :class="{ 'battle-fighter--active': currentAttackerId === combatantA?.id }"
        :aria-current="currentAttackerId === combatantA?.id ? 'true' : 'false'"
      >
        <div class="battle-fighter__pet" :class="{ 'battle-fighter__pet--attacking': currentAttackerId === combatantA?.id && isAttacking }" aria-hidden="true">
          {{ getPetEmoji(challenger?.petType) }}
        </div>
        <div class="battle-fighter__name">{{ combatantA?.name }}</div>
        <div class="battle-fighter__level">{{ combatantA?.cultivationName }}</div>
        <div class="battle-fighter__hp-bar" role="progressbar" :aria-valuenow="combatantAHpPercent" aria-valuemin="0" aria-valuemax="100" :aria-label="`${combatantA?.name} 剩余血量`">
          <div
            class="battle-fighter__hp-fill"
            :style="{ width: combatantAHpPercent + '%' }"
            :class="{ 'battle-fighter__hp-fill--low': combatantAHpPercent < 30 }"
          ></div>
          <span class="battle-fighter__hp-text">{{ currentRoundAHp }} / {{ combatantA?.maxHp }}</span>
        </div>
      </div>

      <!-- VS -->
      <div class="battle-screen__vs" aria-hidden="true">
        <span class="battle-screen__vs-text">⚔️</span>
      </div>

      <!-- 对手 -->
      <div
        class="battle-fighter"
        :class="{ 'battle-fighter--active': currentAttackerId === combatantB?.id }"
        :aria-current="currentAttackerId === combatantB?.id ? 'true' : 'false'"
      >
        <div class="battle-fighter__pet" :class="{ 'battle-fighter__pet--attacking': currentAttackerId === combatantB?.id && isAttacking }" aria-hidden="true">
          {{ getPetEmoji(selectedOpponent?.petType) }}
        </div>
        <div class="battle-fighter__name">{{ combatantB?.name }}</div>
        <div class="battle-fighter__level">{{ combatantB?.cultivationName }}</div>
        <div class="battle-fighter__hp-bar" role="progressbar" :aria-valuenow="combatantBHpPercent" aria-valuemin="0" aria-valuemax="100" :aria-label="`${combatantB?.name} 剩余血量`">
          <div
            class="battle-fighter__hp-fill"
            :style="{ width: combatantBHpPercent + '%' }"
            :class="{ 'battle-fighter__hp-fill--low': combatantBHpPercent < 30 }"
          ></div>
          <span class="battle-fighter__hp-text">{{ currentRoundBHp }} / {{ combatantB?.maxHp }}</span>
        </div>
      </div>
    </div>

    <!-- 当前回合描述 -->
    <div class="battle-screen__round-desc" aria-live="polite">
      <span v-if="currentRoundDescription" class="battle-screen__round-text">{{ currentRoundDescription }}</span>
      <span v-else class="battle-screen__round-text battle-screen__round-text--start">战斗开始！</span>
    </div>

    <!-- 战斗日志 -->
    <div class="battle-screen__log" ref="logRef" aria-live="polite" aria-label="战斗日志">
      <div
        v-for="(log, idx) in displayedLogs"
        :key="idx"
        class="battle-screen__log-item"
        :class="{
          'battle-screen__log-item--header': isLogHeader(log),
          'battle-screen__log-item--round': isLogRound(log),
          'battle-screen__log-item--crit': isLogCrit(log),
          'battle-screen__log-item--dodge': isLogDodge(log),
          'battle-screen__log-item--end': isLogEnd(log),
        }"
      >
        {{ log }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { Student } from '@/types'
import type { Combatant } from '@/utils/combatSystem'
import {
  getPetEmoji,
  isLogHeader,
  isLogRound,
  isLogCrit,
  isLogDodge,
  isLogEnd,
} from '@/composables/useBattleHelpers'

defineProps<{
  challenger: Student | null
  selectedOpponent: Student | null
  combatantA: Combatant | null
  combatantB: Combatant | null
  combatantAHpPercent: number
  combatantBHpPercent: number
  currentRoundAHp: number
  currentRoundBHp: number
  currentRoundDescription: string
  currentAttackerId: string
  isAttacking: boolean
  displayedLogs: string[]
}>()

const logRef = ref<HTMLElement | null>(null)

defineExpose({
  logRef,
})
</script>

<style scoped>
.battle-screen {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.battle-screen__arena {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24px 16px;
  background: linear-gradient(180deg, rgba(201, 168, 76, 0.05) 0%, rgba(124, 58, 237, 0.03) 100%);
  border-radius: 16px;
  border: 1px solid rgba(201, 168, 76, 0.1);
}

.battle-fighter {
  flex: 1;
  text-align: center;
  transition: transform 0.3s ease;
}

.battle-fighter--active {
  transform: scale(1.05);
}

.battle-fighter__pet {
  font-size: 56px;
  width: 80px;
  height: 80px;
  margin: 0 auto 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 16px;
  border: 2px solid rgba(201, 168, 76, 0.3);
  transition: transform 0.3s ease, box-shadow 0.3s ease, border-color 0.3s ease;
}

.battle-fighter__pet--attacking {
  transform: scale(1.1);
  box-shadow: 0 0 30px rgba(201, 168, 76, 0.6);
  border-color: #C9A84C;
  animation: attack-shake 0.4s ease;
}

@keyframes attack-shake {
  0%, 100% { transform: scale(1.1) translateX(0); }
  25% { transform: scale(1.15) translateX(-4px); }
  75% { transform: scale(1.15) translateX(4px); }
}

.battle-fighter__name {
  font-size: 18px;
  font-weight: 700;
  color: #fff;
  margin-bottom: 4px;
}

.battle-fighter__level {
  font-size: 12px;
  color: #C9A84C;
  margin-bottom: 12px;
}

.battle-fighter__hp-bar {
  position: relative;
  height: 20px;
  background: rgba(0, 0, 0, 0.3);
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.battle-fighter__hp-fill {
  height: 100%;
  background: linear-gradient(90deg, #4ade80 0%, #22c55e 100%);
  transition: width 0.5s ease;
  border-radius: 10px;
}

.battle-fighter__hp-fill--low {
  background: linear-gradient(90deg, #f87171 0%, #dc2626 100%);
  animation: low-hp-pulse 1s infinite;
}

@keyframes low-hp-pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.7; }
}

.battle-fighter__hp-text {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 600;
  color: #fff;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5);
}

.battle-screen__vs {
  padding: 0 16px;
}

.battle-screen__vs-text {
  font-size: 36px;
}

.battle-screen__round-desc {
  min-height: 40px;
  padding: 12px 16px;
  background: rgba(201, 168, 76, 0.08);
  border-radius: 10px;
  border-left: 3px solid #C9A84C;
}

.battle-screen__round-text {
  font-size: 14px;
  color: #fff;
  font-weight: 500;
}

.battle-screen__round-text--start {
  color: #C9A84C;
  font-weight: 600;
}

.battle-screen__log {
  max-height: 180px;
  overflow-y: auto;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 8px;
  padding: 12px;
}

.battle-screen__log-item {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
  line-height: 1.8;
  padding: 2px 0;
}

.battle-screen__log-item--header {
  color: #C9A84C;
  font-weight: 600;
  text-align: center;
}

.battle-screen__log-item--round {
  color: rgba(255, 255, 255, 0.9);
  font-weight: 600;
}

.battle-screen__log-item--crit {
  color: #f59e0b;
  font-weight: 500;
}

.battle-screen__log-item--dodge {
  color: #60a5fa;
  font-style: italic;
}

.battle-screen__log-item--end {
  color: #f87171;
  font-weight: 600;
}

@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
</style>
