/**
 * 道友切磋战斗系统
 * 参考诸天修仙战斗机制，实现回合制战斗
 */

import type { Student } from '@/types'
import { calculateLevel } from '@/utils/petSystem'
import { getCultivationLevel } from '@/themes/xianxia/cultivationLevels'

/**
 * 战斗角色状态
 */
export interface Combatant {
  id: string
  name: string
  student: Student
  // 基础属性
  maxHp: number
  currentHp: number
  attack: number
  defense: number
  critRate: number
  critDamage: number
  dodgeRate: number
  // 境界信息
  cultivationLevel: number
  cultivationName: string
  // 仙宠信息
  petLevel: number
  petName: string
  petEmoji: string
  // 战斗动态
  isAlive: boolean
}

/**
 * 单回合战斗记录
 */
export interface BattleRound {
  roundNumber: number
  attackerId: string
  attackerName: string
  defenderId: string
  defenderName: string
  damage: number
  isCritical: boolean
  isDodge: boolean
  description: string
  attackerRemainingHp: number
  defenderRemainingHp: number
}

/**
 * 战斗结果
 */
export interface BattleResult {
  rounds: BattleRound[]
  winnerId: string
  winnerName: string
  loserId: string
  loserName: string
  winnerFinalHp: number
  winnerMaxHp: number
  totalRounds: number
  battleLog: string[]
  isDraw: boolean
}

/**
 * 格式化数字（处理大数显示）
 */
function formatNumber(num: number): string {
  if (num >= 1_0000_0000) return `${(num / 1_0000_0000).toFixed(2)}亿`
  if (num >= 10000) return `${(num / 10000).toFixed(1)}万`
  return Math.floor(num).toString()
}

/**
 * 从学生数据计算战斗属性
 * 基于诸天修仙的属性计算逻辑，简化适配教室积分系统
 */
export function calculateCombatant(student: Student, allPets?: Map<string, { name: string, emoji: string }>): Combatant {
  const score = student.score
  const petLevel = calculateLevel(student.petExp)
  const cultivation = getCultivationLevel(score)

  // 基础属性计算（参考诸天修仙的血量/攻击/防御公式）
  const baseHp = Math.max(100, 100 + Math.abs(score) * 2)
  const maxHp = Math.floor(baseHp * (1 + petLevel * 0.1))
  const attack = Math.max(5, Math.floor(10 + Math.abs(score) * 0.5 + petLevel * 3))
  const defense = Math.max(2, Math.floor(5 + Math.abs(score) * 0.2 + petLevel * 2))
  const critRate = Math.min(0.3, 0.10 + petLevel * 0.02)
  const critDamage = 1.5
  const dodgeRate = Math.min(0.25, 0.05 + petLevel * 0.01)

  // 仙宠信息
  const petType = student.petType || 'Unknown'
  const petInfo = allPets?.get(petType)
  const petName = petInfo?.name || petType
  const petEmoji = petInfo?.emoji || '🐾'

  return {
    id: student.id,
    name: student.name,
    student,
    maxHp,
    currentHp: maxHp,
    attack,
    defense,
    critRate,
    critDamage,
    dodgeRate,
    cultivationLevel: cultivation.level,
    cultivationName: cultivation.name,
    petLevel,
    petName,
    petEmoji,
    isAlive: true,
  }
}

/**
 * 计算先手顺序
 * 基于境界和仙宠等级，高者先手
 */
function determineFirstAttacker(attackerA: Combatant, attackerB: Combatant): Combatant {
  // 境界优先，境界高者先手
  const levelDiff = attackerA.cultivationLevel - attackerB.cultivationLevel
  if (levelDiff !== 0) {
    return levelDiff > 0 ? attackerA : attackerB
  }
  // 仙宠等级作次判
  const petDiff = attackerA.petLevel - attackerB.petLevel
  if (petDiff !== 0) {
    return petDiff > 0 ? attackerA : attackerB
  }
  // 随机先手
  return Math.random() < 0.5 ? attackerA : attackerB
}

/**
 * 计算单回合伤害
 * 参考诸天修仙：攻击-防御，带随机波动和暴击
 */
function calculateDamage(attacker: Combatant, defender: Combatant): { damage: number; isCritical: boolean; isDodge: boolean } {
  // 闪避判定
  const dodgeRoll = Math.random()
  if (dodgeRoll < defender.dodgeRate) {
    return { damage: 0, isCritical: false, isDodge: true }
  }

  // 基础伤害：攻击 * 系数 - 防御 * 系数
  const baseDamage = attacker.attack * (0.8 + Math.random() * 0.4)
  const defenseReduction = defender.defense * 0.3
  let rawDamage = Math.max(1, baseDamage - defenseReduction)

  // 暴击判定
  const critRoll = Math.random()
  let isCritical = false
  if (critRoll < attacker.critRate) {
    rawDamage = rawDamage * attacker.critDamage
    isCritical = true
  }

  return { damage: Math.floor(rawDamage), isCritical, isDodge: false }
}

/**
 * 生成战斗描述文字
 * 参考诸天修仙的战斗描述风格，加入血量变化和修仙术语
 */
function generateBattleDescription(
  attacker: Combatant,
  defender: Combatant,
  damage: number,
  isCritical: boolean,
  isDodge: boolean
): string {
  // 修仙风格攻击技能名
  const attackSkills = [
    '灵力冲击', '法力凝拳', '御气斩', '灵光一击', '真元掌',
    '玄气破', '道韵一指', '灵压碾压', '法印轰击', '仙力横扫',
  ]
  const critSkills = [
    '天雷诀', '破灭一击', '灭世神拳', '万剑归宗', '天罡正气',
    '九天玄雷', '混沌一击', '太虚神掌', '乾坤碎裂', '星辰坠落',
  ]
  const dodgeSkills = [
    '幻影步', '虚空遁', '凌波微步', '瞬息万变', '影遁术',
  ]

  const skill = attackSkills[Math.floor(Math.random() * attackSkills.length)]
  const critSkill = critSkills[Math.floor(Math.random() * critSkills.length)]
  const dodgeSkill = dodgeSkills[Math.floor(Math.random() * dodgeSkills.length)]

  const attackerHpPercent = Math.round((attacker.currentHp / attacker.maxHp) * 100)
  const defenderHpPercent = Math.round((defender.currentHp / defender.maxHp) * 100)

  if (isDodge) {
    const dodgeTexts = [
      `${attacker.name}施展【${skill}】，但${defender.name}以【${dodgeSkill}】闪避！`,
      `${attacker.name}发动【${skill}】，${defender.name}身形飘忽，巧妙避开！`,
      `${attacker.name}一记【${skill}】打出，${defender.name}施展【${dodgeSkill}】躲开！`,
      `${defender.name}以【${dodgeSkill}】化解了${attacker.name}的【${skill}】！`,
    ]
    const text = dodgeTexts[Math.floor(Math.random() * dodgeTexts.length)]
    return `${text}（${attacker.name} ${attackerHpPercent}% | ${defender.name} ${defenderHpPercent}%）`
  }

  if (isCritical) {
    const critTexts = [
      `${attacker.name}施展【${critSkill}】！暴击！${defender.name}受到${formatNumber(damage)}点重创！`,
      `轰！${attacker.name}的【${critSkill}】正中${defender.name}要害！造成${formatNumber(damage)}点伤害！`,
      `${attacker.name}怒喝一声，【${critSkill}】爆发！${defender.name}被轰飞，损失${formatNumber(damage)}点血！`,
      `暴击！${attacker.name}的【${critSkill}】势不可挡，${defender.name}遭受${formatNumber(damage)}点重创！`,
    ]
    const text = critTexts[Math.floor(Math.random() * critTexts.length)]
    return `${text}（${attacker.name} ${attackerHpPercent}% | ${defender.name} ${defenderHpPercent}%）`
  }

  const normalTexts = [
    `${attacker.name}施展【${skill}】，对${defender.name}造成${formatNumber(damage)}点伤害。`,
    `${attacker.name}以【${skill}】攻向${defender.name}，造成${formatNumber(damage)}点伤害。`,
    `${attacker.name}一记【${skill}】命中${defender.name}，${defender.name}损失${formatNumber(damage)}点血。`,
    `${attacker.name}催动灵力，【${skill}】击中${defender.name}，造成${formatNumber(damage)}点伤害。`,
  ]
  const text = normalTexts[Math.floor(Math.random() * normalTexts.length)]
  return `${text}（${attacker.name} ${attackerHpPercent}% | ${defender.name} ${defenderHpPercent}%）`
}

/**
 * 模拟战斗
 */
export function simulateBattle(
  challenger: Student,
  opponent: Student,
  allPets?: Map<string, { name: string; emoji: string }>
): BattleResult {
  const combatantA = calculateCombatant(challenger, allPets)
  const combatantB = calculateCombatant(opponent, allPets)

  const firstAttacker = determineFirstAttacker(combatantA, combatantB)
  let currentAttacker = firstAttacker === combatantA ? combatantA : combatantB
  let currentDefender = currentAttacker === combatantA ? combatantB : combatantA

  const rounds: BattleRound[] = []
  const battleLog: string[] = []
  let roundNumber = 1
  const maxRounds = 50 // 防止无限循环

  // 开场消息
  battleLog.push(`═══════════ 切磋开始 ═══════════`)
  battleLog.push(`${combatantA.name}（${combatantA.cultivationName}·${combatantA.petLevel}级${combatantA.petName}）血量 ${combatantA.maxHp}`)
  battleLog.push(`VS`)
  battleLog.push(`${combatantB.name}（${combatantB.cultivationName}·${combatantB.petLevel}级${combatantB.petName}）血量 ${combatantB.maxHp}`)
  battleLog.push(`${firstAttacker.name}先手出击！`)
  battleLog.push(`─────────────────────────────`)

  while (
    combatantA.isAlive &&
    combatantB.isAlive &&
    roundNumber <= maxRounds
  ) {
    // 计算本回合伤害
    const { damage, isCritical, isDodge } = calculateDamage(currentAttacker, currentDefender)

    if (!isDodge) {
      currentDefender.currentHp = Math.max(0, currentDefender.currentHp - damage)
    }

    // 记录回合
    const description = generateBattleDescription(currentAttacker, currentDefender, damage, isCritical, isDodge)
    const round: BattleRound = {
      roundNumber,
      attackerId: currentAttacker.id,
      attackerName: currentAttacker.name,
      defenderId: currentDefender.id,
      defenderName: currentDefender.name,
      damage,
      isCritical,
      isDodge,
      description,
      attackerRemainingHp: currentAttacker.currentHp,
      defenderRemainingHp: currentDefender.currentHp,
    }
    rounds.push(round)
    battleLog.push(`【第${roundNumber}回合】${description}`)

    // 检查生死
    if (currentDefender.currentHp <= 0) {
      currentDefender.isAlive = false
      break
    }

    // 交换攻守
    const temp = currentAttacker
    currentAttacker = currentDefender
    currentDefender = temp

    roundNumber++
  }

  // 判断结果
  let winner: Combatant
  let loser: Combatant
  let isDraw = false

  if (combatantA.isAlive && !combatantB.isAlive) {
    winner = combatantA
    loser = combatantB
  } else if (combatantB.isAlive && !combatantA.isAlive) {
    winner = combatantB
    loser = combatantA
  } else if (combatantA.isAlive && combatantB.isAlive && roundNumber > maxRounds) {
    // 超过最大回合，按血量判定
    if (combatantA.currentHp > combatantB.currentHp) {
      winner = combatantA
      loser = combatantB
    } else if (combatantB.currentHp > combatantA.currentHp) {
      winner = combatantB
      loser = combatantA
    } else {
      // 真的平局
      winner = combatantA
      loser = combatantB
      isDraw = true
    }
  } else {
    // 都死了（理论上不可能）
    winner = combatantA
    loser = combatantB
    isDraw = true
  }

  if (isDraw) {
    battleLog.push(`─────────────────────────────`)
    battleLog.push(`【平局】双方势均力敌，鏖战${roundNumber}回合，不分胜负！`)
    battleLog.push(`${combatantA.name} 剩余血量：${combatantA.currentHp} / ${combatantA.maxHp}`)
    battleLog.push(`${combatantB.name} 剩余血量：${combatantB.currentHp} / ${combatantB.maxHp}`)
  } else {
    battleLog.push(`─────────────────────────────`)
    battleLog.push(`【战斗结束】${winner.name}击败${loser.name}，获得胜利！`)
    battleLog.push(`${winner.name}（${winner.cultivationName}）剩余血量：${winner.currentHp} / ${winner.maxHp}`)
    battleLog.push(`${loser.name}（${loser.cultivationName}）已倒下`)
    battleLog.push(`═══════════ 切磋结束 ═══════════`)
  }

  return {
    rounds,
    winnerId: winner.id,
    winnerName: winner.name,
    loserId: loser.id,
    loserName: loser.name,
    winnerFinalHp: winner.currentHp,
    winnerMaxHp: winner.maxHp,
    totalRounds: roundNumber,
    battleLog,
    isDraw,
  }
}

/**
 * 计算战力指数（用于展示）
 */
export function calculatePowerIndex(combatant: Combatant): number {
  return Math.floor(
    combatant.maxHp * 0.3 +
    combatant.attack * 5 +
    combatant.defense * 3 +
    combatant.cultivationLevel * 200 +
    combatant.petLevel * 50
  )
}

/**
 * 获取仙宠类型映射
 */
export const PET_TYPE_INFO: Map<string, { name: string; emoji: string }> = new Map([
  ['cat', { name: '灵猫', emoji: '🐱' }],
  ['dog', { name: '灵兽犬', emoji: '🐕' }],
  ['fox', { name: '灵狐', emoji: '🦊' }],
  ['rabbit', { name: '玉兔', emoji: '🐰' }],
  ['panda', { name: '灵熊', emoji: '🐼' }],
  ['tiger', { name: '白虎', emoji: '🐅' }],
  ['dragon', { name: '青龙', emoji: '🐉' }],
  ['phoenix', { name: '朱雀', emoji: '🦅' }],
  ['unicorn', { name: '麒麟', emoji: '🦄' }],
  ['wolf', { name: '天狼', emoji: '🐺' }],
  ['owl', { name: '夜枭', emoji: '🦉' }],
  ['snake', { name: '玄蛇', emoji: '🐍' }],
  ['turtle', { name: '玄武', emoji: '🐢' }],
  ['fish', { name: '灵鲤', emoji: '🐟' }],
  ['bird', { name: '灵鸟', emoji: '🐦' }],
  ['dinosaur', { name: '古兽', emoji: '🦖' }],
  ['elephant', { name: '巨象', emoji: '🐘' }],
  ['lion', { name: '金狮', emoji: '🦁' }],
  ['bear', { name: '灵熊', emoji: '🐻' }],
  ['monkey', { name: '灵猴', emoji: '🐒' }],
  ['Unknown', { name: '仙宠', emoji: '🐾' }],
])
