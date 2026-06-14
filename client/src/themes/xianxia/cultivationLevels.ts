/**
 * 道友境界体系 - 64级
 * 直接搬诸天修仙练气境界，使用原始阈值
 */

export interface CultivationLevel {
  levelId: number
  name: string
  exp: number  // 经验阈值（修为达到此值即可突破到此境界）
}

export const CULTIVATION_LEVELS: CultivationLevel[] = [
  { levelId: 1, name: '凡人', exp: 500 },
  { levelId: 2, name: '虚妄境初期', exp: 5000 },
  { levelId: 3, name: '虚妄境中期', exp: 10000 },
  { levelId: 4, name: '虚妄境后期', exp: 20000 },
  { levelId: 5, name: '虚妄境巅峰', exp: 40000 },
  { levelId: 6, name: '虚妄境圆满', exp: 50000 },
  { levelId: 7, name: '浮生境初期', exp: 55000 },
  { levelId: 8, name: '浮生境中期', exp: 60000 },
  { levelId: 9, name: '浮生境后期', exp: 65000 },
  { levelId: 10, name: '浮生境巅峰', exp: 70000 },
  { levelId: 11, name: '浮生境圆满', exp: 75000 },
  { levelId: 12, name: '驻念境初期', exp: 80000 },
  { levelId: 13, name: '驻念境中期', exp: 100000 },
  { levelId: 14, name: '驻念境后期', exp: 120000 },
  { levelId: 15, name: '驻念境巅峰', exp: 140000 },
  { levelId: 16, name: '驻念境圆满', exp: 170000 },
  { levelId: 17, name: '归演境初期', exp: 200000 },
  { levelId: 18, name: '归演境中期', exp: 220000 },
  { levelId: 19, name: '归演境后期', exp: 240000 },
  { levelId: 20, name: '归演境巅峰', exp: 270000 },
  { levelId: 21, name: '归演境圆满', exp: 300000 },
  { levelId: 22, name: '破虚境初期', exp: 320000 },
  { levelId: 23, name: '破虚境中期', exp: 340000 },
  { levelId: 24, name: '破虚境后期', exp: 370000 },
  { levelId: 25, name: '破虚境巅峰', exp: 400000 },
  { levelId: 26, name: '破虚境圆满', exp: 420000 },
  { levelId: 27, name: '洞照境初期', exp: 440000 },
  { levelId: 28, name: '洞照境中期', exp: 470000 },
  { levelId: 29, name: '洞照境后期', exp: 500000 },
  { levelId: 30, name: '洞照境巅峰', exp: 520000 },
  { levelId: 31, name: '洞照境圆满', exp: 540000 },
  { levelId: 32, name: '法身境初期', exp: 600000 },
  { levelId: 33, name: '法身境中期', exp: 680000 },
  { levelId: 34, name: '法身境后期', exp: 900000 },
  { levelId: 35, name: '法身境巅峰', exp: 1500000 },
  { levelId: 36, name: '法身境圆满', exp: 2000000 },
  { levelId: 37, name: '真界境初期', exp: 4000000 },
  { levelId: 38, name: '真界境中期', exp: 7000000 },
  { levelId: 39, name: '真界境后期', exp: 8000000 },
  { levelId: 40, name: '真界境巅峰', exp: 10000000 },
  { levelId: 41, name: '无妄真劫境', exp: 20000000 },
  { levelId: 42, name: '地仙', exp: 30000000 },
  { levelId: 43, name: '天仙', exp: 60000000 },
  { levelId: 44, name: '灵仙', exp: 70000000 },
  { levelId: 45, name: '玄仙', exp: 90000000 },
  { levelId: 46, name: '金仙', exp: 100000000 },
  { levelId: 47, name: '大罗金仙', exp: 150000000 },
  { levelId: 48, name: '大道真仙', exp: 180000000 },
  { levelId: 49, name: '不朽真仙', exp: 200000000 },
  { levelId: 50, name: '究极仙王', exp: 500000000 },
  { levelId: 51, name: '无极仙帝', exp: 1000000000 },
  { levelId: 52, name: '无上仙帝', exp: 1500000000 },
  { levelId: 53, name: '超凡入圣', exp: 2000000000 },
  { levelId: 54, name: '一介凡人', exp: 2500000000 },
  { levelId: 55, name: '万界道祖', exp: 5000000000 },
  { levelId: 56, name: '混元者', exp: 6000000000 },
  { levelId: 57, name: '无极者', exp: 7000000000 },
  { levelId: 58, name: '混元无极大罗金仙', exp: 8000000000 },
  { levelId: 59, name: '准圣', exp: 9000000000 },
  { levelId: 60, name: '洪荒圣人', exp: 9000000000 },
  { levelId: 61, name: '天道圣人', exp: 9500000000 },
  { levelId: 62, name: '太始之源', exp: 9600000000 },
  { levelId: 63, name: '永恒境', exp: 35000000000 },
  { levelId: 64, name: '无境', exp: 100000000000 },
]

/** 最大境界等级 */
export const MAX_CULTIVATION_LEVEL = 64

/** 需要渡劫的境界（level_id >= 41） */
export const TRIBULATION_THRESHOLD = 41

/**
 * 根据积分/修为判定当前境界
 * @param score 学生积分（修为）
 * @returns 当前境界信息
 */
export function getCultivationLevel(score: number): { level: number; name: string; exp: number } {
  const absScore = Math.max(0, score)

  let currentLevel = CULTIVATION_LEVELS[0]
  for (const level of CULTIVATION_LEVELS) {
    if (absScore >= level.exp) {
      currentLevel = level
    } else {
      break
    }
  }

  return {
    level: currentLevel.levelId,
    name: currentLevel.name,
    exp: currentLevel.exp,
  }
}

/**
 * 计算学生的修为值（用于战斗）
 * 修为 = 积分 * (1 + 仙宠加成%) + 仙宠等级 * 5
 */
export function calculateCultivationFromScore(
  score: number,
  petLevel: number
): number {
  const bonusPercent = 0.03 * petLevel
  return Math.floor(score * (1 + bonusPercent) + petLevel * 5)
}
