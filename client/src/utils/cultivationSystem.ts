/**
 * 修仙体系核心逻辑
 * 包含修为计算、境界判定、突破判定、渡劫逻辑
 */

import { CULTIVATION_LEVELS, TRIBULATION_THRESHOLD, type CultivationLevel } from '@/themes/xianxia/cultivationLevels'

/** 仙宠等级对应的加成百分比 */
const PET_LEVEL_BONUS_PERCENT: Record<number, number> = {
  1: 0.03,
  2: 0.06,
  3: 0.09,
  4: 0.12,
  5: 0.15,
  6: 0.18,
  7: 0.21,
  8: 0.24,
}

/** 仙宠等级对应的固定修为加成 */
const PET_LEVEL_FLAT_BONUS = 5

/**
 * 计算道友的修为
 * 修为 = 学期总积分 × (1 + 仙宠加成%) + 仙宠等级 × 5
 */
export function calculateCultivation(
  semesterScore: number,
  petLevel: number
): number {
  const bonusPercent = PET_LEVEL_BONUS_PERCENT[petLevel] || 0
  return Math.floor(semesterScore * (1 + bonusPercent) + petLevel * PET_LEVEL_FLAT_BONUS)
}

/**
 * 根据修为判定当前境界
 * 返回修为对应的最高境界
 */
export function getCultivationLevel(cultivation: number): CultivationLevel {
  let result = CULTIVATION_LEVELS[0]
  for (const level of CULTIVATION_LEVELS) {
    if (cultivation >= level.exp) {
      result = level
    } else {
      break
    }
  }
  return result
}

/**
 * 获取下一个境界
 * 如果已是最高境界，返回 null
 */
export function getNextCultivationLevel(currentLevelId: number): CultivationLevel | null {
  const nextIndex = CULTIVATION_LEVELS.findIndex(l => l.levelId === currentLevelId) + 1
  if (nextIndex >= CULTIVATION_LEVELS.length) return null
  return CULTIVATION_LEVELS[nextIndex]
}

/**
 * 判断境界突破是否需要渡劫
 * level_id >= 41 需要渡劫
 */
export function isTribulationRequired(targetLevelId: number): boolean {
  return targetLevelId >= TRIBULATION_THRESHOLD
}

/**
 * 计算道友渡劫突破的成功率
 * @param targetLevelId 目标境界ID
 * @param cultivation 当前修为
 * @param targetExp 目标境界的经验阈值
 * @returns 成功率（0-1之间）
 */
export function calculateBreakthroughSuccessRate(
  targetLevelId: number,
  cultivation: number,
  targetExp: number
): number {
  if (targetLevelId === TRIBULATION_THRESHOLD) {
    // 无妄真劫境：基础50% + 修为超出部分加成
    const baseRate = 0.5
    const nextLevel = getNextCultivationLevel(targetLevelId)
    const nextExp = nextLevel ? nextLevel.exp : targetExp * 2
    const overflow = Math.max(0, cultivation - targetExp)
    const bonusRate = Math.min(0.3, (overflow / (nextExp - targetExp)) * 0.3)
    return Math.min(0.8, baseRate + bonusRate)
  }

  if (targetLevelId > TRIBULATION_THRESHOLD) {
    // 地仙及以上：成功率随境界递减
    const baseRate = Math.max(0.3, 0.8 - (targetLevelId - TRIBULATION_THRESHOLD) * 0.01)
    const nextLevel = getNextCultivationLevel(targetLevelId)
    const nextExp = nextLevel ? nextLevel.exp : targetExp * 2
    const overflow = Math.max(0, cultivation - targetExp)
    const bonusRate = Math.min(0.2, (overflow / (nextExp - targetExp)) * 0.2)
    return Math.min(0.8, baseRate + bonusRate)
  }

  return 1.0 // 不需要渡劫的境界，确定性成功
}

/**
 * 渡劫失败惩罚（阶梯式）
 * 返回 { penaltyRate: 扣除修为比例, description: 描述文案 }
 */
export function getBreakthroughFailurePenalty(): { penaltyRate: number; description: string } {
  const roll = Math.random()

  if (roll > 0.9) {
    return { penaltyRate: 0.4, description: '天劫降世，道基崩碎！' }
  } else if (roll > 0.7) {
    return { penaltyRate: 0.2, description: '雷劫反噬，修为受损' }
  } else if (roll > 0.5) {
    return { penaltyRate: 0.1, description: '心魔侵扰，略有损耗' }
  } else if (roll > 0.3) {
    return { penaltyRate: 0.05, description: '劫雷擦身，轻微反噬' }
  } else {
    return { penaltyRate: 0, description: '天劫未成，全身而退' }
  }
}

/**
 * 执行渡劫突破判定
 * @returns success: 是否成功, penaltyRate: 失败惩罚比例, description: 描述
 */
export function executeBreakthroughTribulation(
  targetLevelId: number,
  cultivation: number,
  targetExp: number
): { success: boolean; penaltyRate: number; description: string } {
  const successRate = calculateBreakthroughSuccessRate(targetLevelId, cultivation, targetExp)
  const roll = Math.random()

  if (roll <= successRate) {
    return { success: true, penaltyRate: 0, description: '突破成功！' }
  }

  const penalty = getBreakthroughFailurePenalty()
  return { success: false, penaltyRate: penalty.penaltyRate, description: penalty.description }
}

/**
 * 格式化大数字显示
 * 如 1000000000 → "10.0亿"
 */
export function formatCultivationNumber(num: number): string {
  if (num >= 1e12) return (num / 1e12).toFixed(1) + '万亿'
  if (num >= 1e8) return (num / 1e8).toFixed(1) + '亿'
  if (num >= 1e4) return (num / 1e4).toFixed(1) + '万'
  return num.toString()
}
