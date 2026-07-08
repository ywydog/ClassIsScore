import type { Student } from '@/types'
import {
  calculateCombatant,
  calculatePowerIndex,
  PET_TYPE_INFO,
} from '@/utils/combatSystem'
import { calculateLevel } from '@/utils/petSystem'
import { getCultivationLevel } from '@/themes/xianxia/cultivationLevels'

export function getPetEmoji(petType: string | undefined): string {
  if (!petType) return '🐾'
  return PET_TYPE_INFO.get(petType)?.emoji || petType.charAt(0) === petType.charAt(0).toUpperCase() ? petType.charAt(0) : '🐾'
}

export function getCultivationName(score: number): string {
  return getCultivationLevel(score).name
}

export function getPetLevel(petExp: number): number {
  return calculateLevel(petExp)
}

export function getPowerIndex(student: Student): number {
  const combatant = calculateCombatant(student, PET_TYPE_INFO)
  return calculatePowerIndex(combatant)
}

// 日志分类函数
export function isLogHeader(log: string): boolean {
  return log.includes('═══') || log.includes('切磋开始') || log.includes('VS')
}

export function isLogRound(log: string): boolean {
  return log.startsWith('【第') && log.includes('回合】')
}

export function isLogCrit(log: string): boolean {
  return log.includes('暴击') || log.includes('重创') || log.includes('轰飞')
}

export function isLogDodge(log: string): boolean {
  return log.includes('闪避') || log.includes('躲开') || log.includes('避开') || log.includes('化解')
}

export function isLogEnd(log: string): boolean {
  return log.includes('战斗结束') || log.includes('切磋结束') || log.includes('平局') || log.includes('已倒下') || log.includes('剩余血量')
}
