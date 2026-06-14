import { PetCategory, type PetTypeInfo, type LevelProgress } from '@/types'

/** 等级经验阈值 */
export const LEVEL_THRESHOLDS = [40, 60, 80, 100, 120, 140, 160]

/** 最大等级 */
export const MAX_LEVEL = 8

/** 所有可用宠物类型 */
export const ALL_PET_TYPES: PetTypeInfo[] = [
  // 普通动物
  { id: 'cat', name: '猫咪', category: PetCategory.Normal, emoji: '🐱', description: '温顺可爱的小猫' },
  { id: 'orange-cat', name: '橘猫', category: PetCategory.Normal, emoji: '🍊', description: '十只橘猫九只胖' },
  { id: 'ragdoll', name: '布偶猫', category: PetCategory.Normal, emoji: '🧸', description: '优雅的猫中仙女' },
  { id: 'dog', name: '小狗', category: PetCategory.Normal, emoji: '🐶', description: '忠诚的好伙伴' },
  { id: 'shiba', name: '柴犬', category: PetCategory.Normal, emoji: '🐕', description: '微笑天使' },
  { id: 'corgi', name: '柯基', category: PetCategory.Normal, emoji: '🦊', description: '短腿小电臀' },
  { id: 'golden', name: '金毛', category: PetCategory.Normal, emoji: '🦮', description: '温暖的大暖男' },
  { id: 'husky', name: '哈士奇', category: PetCategory.Normal, emoji: '🐺', description: '拆家小能手' },
  { id: 'rabbit', name: '兔子', category: PetCategory.Normal, emoji: '🐰', description: '软萌小兔子' },
  { id: 'hamster', name: '仓鼠', category: PetCategory.Normal, emoji: '🐹', description: '腮帮子鼓鼓的' },
  { id: 'duck', name: '柯尔鸭', category: PetCategory.Normal, emoji: '🦆', description: '嘎嘎嘎' },
  { id: 'panda', name: '熊猫', category: PetCategory.Normal, emoji: '🐼', description: '国宝级萌物' },
  { id: 'red-panda', name: '小熊猫', category: PetCategory.Normal, emoji: '🦝', description: '不是小浣熊' },
  { id: 'alpaca', name: '羊驼', category: PetCategory.Normal, emoji: '🦙', description: '会吐口水的萌物' },
  { id: 'fox', name: '狐狸', category: PetCategory.Normal, emoji: '🦊', description: '聪明的小狐狸' },
  // 神兽
  { id: 'white-tiger', name: '白虎', category: PetCategory.Mythical, emoji: '🐯', description: '传说中的神兽' },
  { id: 'unicorn', name: '独角兽', category: PetCategory.Mythical, emoji: '🦄', description: '纯洁的象征' },
  { id: 'dragon', name: '青龙', category: PetCategory.Mythical, emoji: '🐉', description: '东方神龙' },
  { id: 'phoenix', name: '朱雀', category: PetCategory.Mythical, emoji: '🔥', description: '浴火重生' },
  { id: 'pixiu', name: '貔貅', category: PetCategory.Mythical, emoji: '🦁', description: '招财进宝' },
]

/** 根据经验值计算等级 */
export function calculateLevel(exp: number): number {
  let level = 1
  let total = 0
  for (const threshold of LEVEL_THRESHOLDS) {
    total += threshold
    if (exp >= total) level++
    else break
  }
  return Math.min(level, MAX_LEVEL)
}

/** 获取当前等级进度信息 */
export function getLevelProgress(exp: number): LevelProgress {
  if (exp <= 0) {
    return { current: 0, required: LEVEL_THRESHOLDS[0], percentage: 0, isMaxLevel: false }
  }

  let total = 0
  for (let i = 0; i < LEVEL_THRESHOLDS.length; i++) {
    const levelTotal = total + LEVEL_THRESHOLDS[i]
    if (exp < levelTotal) {
      const current = exp - total
      return {
        current: Math.floor(current),
        required: LEVEL_THRESHOLDS[i],
        percentage: Math.floor((current / LEVEL_THRESHOLDS[i]) * 100),
        isMaxLevel: false,
      }
    }
    total = levelTotal
  }

  const maxExp = LEVEL_THRESHOLDS.reduce((a, b) => a + b, 0)
  return { current: Math.floor(exp), required: maxExp, percentage: 100, isMaxLevel: true }
}

/** 获取等级对应的边框颜色 */
export function getLevelBorderColor(level: number): string {
  const colors: Record<number, string> = {
    1: '#E0E0E0',
    2: '#B0B0B0',
    3: '#4488FF',
    4: '#00C8FF',
    5: '#8844FF',
    6: '#FF4488',
    7: '#FF2244',
    8: '#FFCC00',
  }
  return colors[level] || '#E0E0E0'
}

/** 获取等级对应的称号 */
export function getLevelTitle(level: number): string {
  if (level >= 8) return '已毕业'
  if (level >= 7) return '史诗级'
  if (level >= 5) return '稀有'
  if (level >= 3) return '优秀'
  return '成长中'
}

/** 获取等级对应的背景渐变色 */
export function getLevelGradient(level: number): { start: string; end: string } {
  if (level >= 8) return { start: '#FFD700', end: '#FF8C00' }
  if (level >= 7) return { start: '#FF4488', end: '#FF2244' }
  if (level >= 5) return { start: '#8844FF', end: '#4488FF' }
  if (level >= 3) return { start: '#4488FF', end: '#00C8FF' }
  return { start: '#AAAAAA', end: '#888888' }
}

/** 获取宠物类型信息 */
export function getPetTypeInfo(petTypeId: string): PetTypeInfo | undefined {
  return ALL_PET_TYPES.find(p => p.id === petTypeId)
}

/** 获取宠物emoji */
export function getPetEmoji(petTypeId?: string): string {
  return getPetTypeInfo(petTypeId ?? '')?.emoji ?? '❓'
}

/** 获取宠物图片路径（根据宠物类型和等级） */
export function getPetImagePath(petTypeId?: string, level: number = 1): string {
  if (!petTypeId) return '/pets/cat/lv1.png'
  const validLevel = Math.max(1, Math.min(level, MAX_LEVEL))
  return `/pets/${petTypeId}/lv${validLevel}.png`
}

/** 渡劫相关常量 */
export const TRIBULATION_LEVELS = [5, 8] // 需要渡劫的等级

/** 判断指定等级是否需要渡劫 */
export function isTribulationLevel(level: number): boolean {
  return TRIBULATION_LEVELS.includes(level)
}

/**
 * 计算仙宠渡劫成功率
 * 基础成功率 = 50%（5级渡劫）/ 30%（8级渡劫）
 * 经验加成 = (当前经验 / 升级所需经验) × 30%
 * 最终成功率 = min(基础成功率 + 经验加成, 80%)
 */
export function calculatePetTribulationSuccessRate(
  targetLevel: number,
  currentExp: number,
  requiredExp: number
): number {
  const baseRate = targetLevel === 5 ? 0.5 : 0.3
  const expRatio = requiredExp > 0 ? Math.min(1, currentExp / requiredExp) : 0
  const expBonus = expRatio * 0.3
  return Math.min(0.8, baseRate + expBonus)
}

/** 渡劫失败惩罚结果 */
export interface TribulationPenalty {
  penaltyRate: number  // 扣除经验比例
  description: string  // 描述文案
}

/**
 * 获取渡劫失败惩罚（阶梯式）
 * 参考诸天修仙的阶梯式惩罚机制
 */
export function getPetTribulationPenalty(): TribulationPenalty {
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
 * 执行仙宠渡劫判定
 * @returns success: 是否成功, penaltyRate: 失败惩罚比例, description: 描述
 */
export function executePetTribulation(
  targetLevel: number,
  currentExp: number,
  requiredExp: number
): { success: boolean; penaltyRate: number; description: string } {
  const successRate = calculatePetTribulationSuccessRate(targetLevel, currentExp, requiredExp)
  const roll = Math.random()

  if (roll <= successRate) {
    return { success: true, penaltyRate: 0, description: '渡劫成功！仙宠突破！' }
  }

  const penalty = getPetTribulationPenalty()
  return { success: false, penaltyRate: penalty.penaltyRate, description: penalty.description }
}
