<template>
  <div class="battle-result">
    <div class="battle-result__banner" :class="{ 'battle-result__banner--draw': battleResult?.isDraw }">
      <span v-if="battleResult?.isDraw">平局</span>
      <span v-else-if="battleResult && challenger && battleResult.winnerId === challenger.id">胜利！</span>
      <span v-else>战败...</span>
    </div>

    <div class="battle-result__summary">
      <div class="battle-result__fighter" :class="{ 'battle-result__fighter--winner': challenger && battleResult?.winnerId === challenger.id }">
        <div class="battle-result__pet">{{ getPetEmoji(challenger?.petType) }}</div>
        <div class="battle-result__name">{{ combatantA?.name }}</div>
        <div class="battle-result__stats">
          <div>剩余血量 {{ battleResult?.winnerId === challenger?.id ? battleResult?.winnerFinalHp : 0 }}</div>
        </div>
      </div>

      <div class="battle-result__vs">VS</div>

      <div class="battle-result__fighter" :class="{ 'battle-result__fighter--winner': selectedOpponent && battleResult?.winnerId === selectedOpponent.id }">
        <div class="battle-result__pet">{{ getPetEmoji(selectedOpponent?.petType) }}</div>
        <div class="battle-result__name">{{ combatantB?.name }}</div>
        <div class="battle-result__stats">
          <div>剩余血量 {{ battleResult?.winnerId === selectedOpponent?.id ? battleResult?.winnerFinalHp : 0 }}</div>
        </div>
      </div>
    </div>

    <div class="battle-result__info">
      <div class="battle-result__info-row">
        <span>总回合数</span>
        <span>{{ battleResult?.totalRounds }}</span>
      </div>
      <div class="battle-result__info-row">
        <span>胜者</span>
        <span>{{ battleResult?.winnerName }}</span>
      </div>
      <div class="battle-result__info-row">
        <span>败者</span>
        <span>{{ battleResult?.loserName }}</span>
      </div>
    </div>

    <!-- 战斗过程 -->
    <div class="battle-result__log">
      <div class="battle-result__log-title">切磋过程</div>
      <div class="battle-result__log-list">
        <div
          v-for="(log, idx) in battleResult?.battleLog"
          :key="idx"
          class="battle-result__log-item"
          :class="{
            'battle-result__log-item--header': isLogHeader(log),
            'battle-result__log-item--round': isLogRound(log),
            'battle-result__log-item--crit': isLogCrit(log),
            'battle-result__log-item--dodge': isLogDodge(log),
            'battle-result__log-item--end': isLogEnd(log),
          }"
        >{{ log }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Student } from '@/types'
import type { BattleResult, Combatant } from '@/utils/combatSystem'
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
  battleResult: BattleResult | null
  combatantA: Combatant | null
  combatantB: Combatant | null
}>()
</script>

<style scoped>
.battle-result {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.battle-result__banner {
  text-align: center;
  padding: 16px;
  background: linear-gradient(135deg, rgba(201, 168, 76, 0.2) 0%, rgba(201, 168, 76, 0.05) 100%);
  border-radius: 12px;
  border: 1px solid rgba(201, 168, 76, 0.3);
}

.battle-result__banner span {
  font-size: 28px;
  font-weight: 900;
  color: #C9A84C;
  text-shadow: 0 0 20px rgba(201, 168, 76, 0.5);
}

.battle-result__banner--draw span {
  color: rgba(255, 255, 255, 0.7);
}

.battle-result__summary {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  background: rgba(255, 255, 255, 0.03);
  border-radius: 12px;
}

.battle-result__fighter {
  flex: 1;
  text-align: center;
  padding: 12px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.02);
  transition: all 0.3s ease;
}

.battle-result__fighter--winner {
  background: rgba(201, 168, 76, 0.15);
  border: 1px solid rgba(201, 168, 76, 0.4);
  box-shadow: 0 0 20px rgba(201, 168, 76, 0.2);
}

.battle-result__pet {
  font-size: 40px;
  margin-bottom: 8px;
}

.battle-result__name {
  font-size: 16px;
  font-weight: 700;
  color: #fff;
  margin-bottom: 6px;
}

.battle-result__stats {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.6);
  line-height: 1.5;
}

.battle-result__vs {
  font-size: 18px;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.3);
  letter-spacing: 2px;
}

.battle-result__info {
  background: rgba(0, 0, 0, 0.15);
  border-radius: 10px;
  padding: 12px 16px;
}

.battle-result__info-row {
  display: flex;
  justify-content: space-between;
  padding: 6px 0;
  font-size: 13px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

.battle-result__info-row:last-child {
  border-bottom: none;
}

.battle-result__info-row span:first-child {
  color: rgba(255, 255, 255, 0.5);
}

.battle-result__info-row span:last-child {
  color: #fff;
  font-weight: 600;
}

.battle-result__log {
  background: rgba(0, 0, 0, 0.2);
  border-radius: 8px;
  padding: 12px;
  max-height: 200px;
  overflow-y: auto;
}

.battle-result__log-title {
  font-size: 13px;
  font-weight: 600;
  color: #C9A84C;
  margin-bottom: 8px;
}

.battle-result__log-item {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
  line-height: 1.9;
  padding: 3px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.03);
}

.battle-result__log-item--header {
  color: #C9A84C;
  font-weight: 600;
  text-align: center;
  font-size: 13px;
  padding: 6px 0;
  border-bottom: none;
}

.battle-result__log-item--round {
  color: rgba(255, 255, 255, 0.9);
  font-weight: 600;
  margin-top: 6px;
  border-bottom: none;
}

.battle-result__log-item--crit {
  color: #f59e0b;
  font-weight: 500;
}

.battle-result__log-item--dodge {
  color: #60a5fa;
  font-style: italic;
}

.battle-result__log-item--end {
  color: #f87171;
  font-weight: 600;
  margin-top: 6px;
  border-bottom: none;
}
</style>
