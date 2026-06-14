<template>
  <el-dialog
    v-model="visible"
    :title="dialogTitle"
    width="680px"
    :close-on-click-modal="false"
    :close-on-press-escape="battleInProgress === false"
    :show-close="battleInProgress === false"
    class="battle-dialog"
    @close="handleClose"
  >
    <!-- Step 1: 选择对手 -->
    <div v-if="step === 'select'" class="battle-select">
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
            @click="selectOpponent(opponent)"
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

    <!-- Step 2: 战斗动画 -->
    <div v-else-if="step === 'battle'" class="battle-screen">
      <div class="battle-screen__arena">
        <!-- 挑战者 -->
        <div class="battle-fighter" :class="{ 'battle-fighter--active': currentAttackerId === combatantA?.id }">
          <div class="battle-fighter__pet" :class="{ 'battle-fighter__pet--attacking': currentAttackerId === combatantA?.id && isAttacking }">
            {{ getPetEmoji(challenger?.petType) }}
          </div>
          <div class="battle-fighter__name">{{ combatantA?.name }}</div>
          <div class="battle-fighter__level">{{ combatantA?.cultivationName }}</div>
          <div class="battle-fighter__hp-bar">
            <div
              class="battle-fighter__hp-fill"
              :style="{ width: combatantAHpPercent + '%' }"
              :class="{ 'battle-fighter__hp-fill--low': combatantAHpPercent < 30 }"
            ></div>
            <span class="battle-fighter__hp-text">{{ currentRoundAHp }} / {{ combatantA?.maxHp }}</span>
          </div>
        </div>

        <!-- VS -->
        <div class="battle-screen__vs">
          <span class="battle-screen__vs-text">⚔️</span>
        </div>

        <!-- 对手 -->
        <div class="battle-fighter" :class="{ 'battle-fighter--active': currentAttackerId === combatantB?.id }">
          <div class="battle-fighter__pet" :class="{ 'battle-fighter__pet--attacking': currentAttackerId === combatantB?.id && isAttacking }">
            {{ getPetEmoji(selectedOpponent?.petType) }}
          </div>
          <div class="battle-fighter__name">{{ combatantB?.name }}</div>
          <div class="battle-fighter__level">{{ combatantB?.cultivationName }}</div>
          <div class="battle-fighter__hp-bar">
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
      <div class="battle-screen__round-desc">
        <span v-if="currentRoundDescription" class="battle-screen__round-text">{{ currentRoundDescription }}</span>
        <span v-else class="battle-screen__round-text battle-screen__round-text--start">战斗开始！</span>
      </div>

      <!-- 战斗日志 -->
      <div class="battle-screen__log" ref="logRef">
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

    <!-- Step 3: 战斗结果 -->
    <div v-else-if="step === 'result'" class="battle-result">
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

    <!-- Footer -->
    <template #footer>
      <template v-if="step === 'select'">
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" :disabled="!selectedOpponent" @click="startBattle">
          开始切磋
        </el-button>
      </template>
      <template v-else-if="step === 'battle'">
        <el-button :disabled="battleInProgress" @click="skipBattle" v-if="!isBattleFinished">
          跳过动画
        </el-button>
      </template>
      <template v-else-if="step === 'result'">
        <el-button @click="backToSelect" v-if="!battleInProgress">重新选择</el-button>
        <el-button type="primary" @click="handleClose">关闭</el-button>
      </template>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, watch } from 'vue'
import type { Student } from '@/types'
import {
  simulateBattle,
  calculateCombatant,
  calculatePowerIndex,
  PET_TYPE_INFO,
  type BattleResult,
  type Combatant,
  type BattleRound,
} from '@/utils/combatSystem'
import { calculateLevel } from '@/utils/petSystem'
import { getCultivationLevel } from '@/themes/xianxia/cultivationLevels'

const props = defineProps<{
  modelValue: boolean
  challenger?: Student | null
  opponents: Student[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val),
})

const dialogTitle = computed(() => {
  if (step.value === 'select') return '道友切磋 - 选择对手'
  if (step.value === 'battle') return '道友切磋 - 战斗中'
  return '道友切磋 - 战斗结果'
})

const step = ref<'select' | 'battle' | 'result'>('select')
const selectedOpponent = ref<Student | null>(null)
const battleResult = ref<BattleResult | null>(null)
const battleInProgress = ref(false)
const isBattleFinished = ref(false)

// 战斗状态
const combatantA = ref<Combatant | null>(null)
const combatantB = ref<Combatant | null>(null)
const currentRoundAHp = ref(0)
const currentRoundBHp = ref(0)
const currentRoundDescription = ref('')
const currentRoundIndex = ref(0)
const currentAttackerId = ref('')
const isAttacking = ref(false)
const displayedLogs = ref<string[]>([])
const battleRounds = ref<BattleRound[]>([])
const logRef = ref<HTMLElement | null>(null)

const combatantAHpPercent = computed(() => {
  if (!combatantA.value) return 0
  return Math.max(0, Math.min(100, (currentRoundAHp.value / combatantA.value.maxHp) * 100))
})

const combatantBHpPercent = computed(() => {
  if (!combatantB.value) return 0
  return Math.max(0, Math.min(100, (currentRoundBHp.value / combatantB.value.maxHp) * 100))
})

// 监听dialog关闭重置
watch(visible, (val) => {
  if (!val) {
    step.value = 'select'
    selectedOpponent.value = null
    battleResult.value = null
    battleInProgress.value = false
    isBattleFinished.value = false
    displayedLogs.value = []
  }
})

function selectOpponent(opponent: Student) {
  selectedOpponent.value = opponent
}

function getPetEmoji(petType: string | undefined): string {
  if (!petType) return '🐾'
  return PET_TYPE_INFO.get(petType)?.emoji || petType.charAt(0) === petType.charAt(0).toUpperCase() ? petType.charAt(0) : '🐾'
}

function getCultivationName(score: number): string {
  return getCultivationLevel(score).name
}

function getPetLevel(petExp: number): number {
  return calculateLevel(petExp)
}

function getPowerIndex(student: Student): number {
  const combatant = calculateCombatant(student, PET_TYPE_INFO)
  return calculatePowerIndex(combatant)
}

async function startBattle() {
  if (!props.challenger || !selectedOpponent.value) return

  // 预计算战斗结果
  const result = simulateBattle(
    props.challenger,
    selectedOpponent.value,
    PET_TYPE_INFO
  )
  battleRounds.value = result.rounds

  // 初始化战斗展示状态
  combatantA.value = calculateCombatant(props.challenger, PET_TYPE_INFO)
  combatantB.value = calculateCombatant(selectedOpponent.value, PET_TYPE_INFO)
  currentRoundAHp.value = combatantA.value.maxHp
  currentRoundBHp.value = combatantB.value.maxHp
  currentRoundDescription.value = ''
  currentRoundIndex.value = 0
  displayedLogs.value = [
    `【切磋开始】`,
    `${combatantA.value.name}（${combatantA.value.cultivationName}·${combatantA.value.petLevel}级仙宠）`,
    ` VS `,
    `${combatantB.value.name}（${combatantB.value.cultivationName}·${combatantB.value.petLevel}级仙宠）`,
  ]
  battleInProgress.value = true
  isBattleFinished.value = false
  step.value = 'battle'

  await nextTick()
  playBattleAnimation()
}

async function playBattleAnimation() {
  battleInProgress.value = true

  for (let i = 0; i < battleRounds.value.length; i++) {
    const round = battleRounds.value[i]
    currentRoundIndex.value = i + 1

    // 更新攻击者标识
    currentAttackerId.value = round.attackerId
    isAttacking.value = true

    // 显示当前回合描述
    currentRoundDescription.value = round.description

    // 更新血量
    if (round.attackerId === combatantA.value?.id) {
      currentRoundBHp.value = Math.max(0, round.defenderRemainingHp)
    } else {
      currentRoundAHp.value = Math.max(0, round.defenderRemainingHp)
    }

    // 添加到日志
    displayedLogs.value.push(`【第${round.roundNumber}回合】${round.description}`)

    // 滚动日志到底部
    await nextTick()
    if (logRef.value) {
      logRef.value.scrollTop = logRef.value.scrollHeight
    }

    // 暂停攻击动画
    await new Promise((resolve) => setTimeout(resolve, 400))
    isAttacking.value = false

    // 回合间隔
    await new Promise((resolve) => setTimeout(resolve, 300))
  }

  // 战斗结束
  battleInProgress.value = false
  isBattleFinished.value = true
  currentRoundDescription.value = '战斗结束'

  await new Promise((resolve) => setTimeout(resolve, 800))
  showResult()
}

function skipBattle() {
  // 直接显示最终结果
  if (combatantA.value && combatantB.value) {
    const allRounds = battleRounds.value
    if (allRounds.length > 0) {
      const lastRound = allRounds[allRounds.length - 1]
      currentRoundAHp.value = lastRound.attackerId === combatantA.value.id
        ? lastRound.attackerRemainingHp
        : lastRound.defenderRemainingHp
      currentRoundBHp.value = lastRound.attackerId === combatantB.value.id
        ? lastRound.attackerRemainingHp
        : lastRound.defenderRemainingHp
      // 重新找 - 根据战斗结果中的胜者
      // 使用一个简单的算法：找到胜者和败者，并显示血量
      // 战斗在最后一个回合结束，败者HP为0
      if (battleRounds.value.length > 0) {
        // 遍历所有回合，找胜者和败者的最终HP
        // 简化处理：让胜者保留HP，败者为0
        const result = simulateBattle(
          props.challenger!,
          selectedOpponent.value!,
          PET_TYPE_INFO
        )
        if (result.winnerId === combatantA.value.id) {
          currentRoundAHp.value = result.winnerFinalHp
          currentRoundBHp.value = 0
        } else {
          currentRoundAHp.value = 0
          currentRoundBHp.value = result.winnerFinalHp
        }
      }
    }
  }

  // 将所有日志显示
  displayedLogs.value = []
  for (const round of battleRounds.value) {
    displayedLogs.value.push(`【第${round.roundNumber}回合】${round.description}`)
  }

  battleInProgress.value = false
  isBattleFinished.value = true
  showResult()
}

function showResult() {
  battleResult.value = simulateBattle(
    props.challenger!,
    selectedOpponent.value!,
    PET_TYPE_INFO
  )
  step.value = 'result'
}

function backToSelect() {
  step.value = 'select'
  battleResult.value = null
  battleRounds.value = []
  displayedLogs.value = []
  combatantA.value = null
  combatantB.value = null
  isBattleFinished.value = false
  battleInProgress.value = false
  currentRoundDescription.value = ''
}

function handleClose() {
  visible.value = false
}

// 日志分类函数（用于样式高亮）
function isLogHeader(log: string): boolean {
  return log.includes('═══') || log.includes('切磋开始') || log.includes('VS')
}

function isLogRound(log: string): boolean {
  return log.startsWith('【第') && log.includes('回合】')
}

function isLogCrit(log: string): boolean {
  return log.includes('暴击') || log.includes('重创') || log.includes('轰飞')
}

function isLogDodge(log: string): boolean {
  return log.includes('闪避') || log.includes('躲开') || log.includes('避开') || log.includes('化解')
}

function isLogEnd(log: string): boolean {
  return log.includes('战斗结束') || log.includes('切磋结束') || log.includes('平局') || log.includes('已倒下') || log.includes('剩余血量')
}
</script>

<style scoped>
.battle-dialog {
  background: linear-gradient(135deg, #1a1a2e 0%, #2d1f3f 100%);
}

/* ===== 选择对手 ===== */
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

/* ===== 战斗屏幕 ===== */
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
  transition: all 0.3s ease;
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

/* ===== 战斗结果 ===== */
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
