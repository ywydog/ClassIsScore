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
    <BattleSelectStep
      v-if="step === 'select'"
      :challenger="challenger"
      :opponents="opponents"
      :selected-opponent="selectedOpponent"
      @select-opponent="selectOpponent"
    />

    <!-- Step 2: 战斗动画 -->
    <BattleAnimationStep
      v-else-if="step === 'battle'"
      ref="battleAnimationRef"
      :challenger="challenger"
      :selected-opponent="selectedOpponent"
      :combatant-a="combatantA"
      :combatant-b="combatantB"
      :combatant-a-hp-percent="combatantAHpPercent"
      :combatant-b-hp-percent="combatantBHpPercent"
      :current-round-a-hp="currentRoundAHp"
      :current-round-b-hp="currentRoundBHp"
      :current-round-description="currentRoundDescription"
      :current-attacker-id="currentAttackerId"
      :is-attacking="isAttacking"
      :displayed-logs="displayedLogs"
    />

    <!-- Step 3: 战斗结果 -->
    <BattleResultStep
      v-else-if="step === 'result'"
      :challenger="challenger"
      :selected-opponent="selectedOpponent"
      :battle-result="battleResult"
      :combatant-a="combatantA"
      :combatant-b="combatantB"
    />

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
  PET_TYPE_INFO,
  type BattleResult,
  type Combatant,
  type BattleRound,
} from '@/utils/combatSystem'
import BattleSelectStep from './BattleSelectStep.vue'
import BattleAnimationStep from './BattleAnimationStep.vue'
import BattleResultStep from './BattleResultStep.vue'

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
const battleAnimationRef = ref<InstanceType<typeof BattleAnimationStep> | null>(null)

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
    const logEl = battleAnimationRef.value?.logRef
    if (logEl) {
      logEl.scrollTop = logEl.scrollHeight
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
      // 使用一个简单的算法：找到胜者和败者，并显示血量
      if (battleRounds.value.length > 0) {
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
</script>

<style scoped>
.battle-dialog {
  background: linear-gradient(135deg, #1a1a2e 0%, #2d1f3f 100%);
}
</style>
