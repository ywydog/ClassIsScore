/**
 * 修仙主题术语映射表
 * 键为术语ID，值为 { default: 默认模式文字, xianxia: 修仙模式文字 }
 */

export interface TermEntry {
  default: string
  xianxia: string
}

export const TERMINOLOGY_MAP: Record<string, TermEntry> = {
  student: { default: '学生', xianxia: '道友' },
  students: { default: '学生管理', xianxia: '道友管理' },
  group: { default: '小组', xianxia: '宗门' },
  groups: { default: '小组管理', xianxia: '宗门管理' },
  groupMember: { default: '组员', xianxia: '弟子' },
  groupLeader: { default: '组长', xianxia: '掌门' },
  pet: { default: '宠物', xianxia: '仙宠' },
  score: { default: '积分', xianxia: '灵力' },
  scorePlus: { default: '加分', xianxia: '悟道' },
  scoreMinus: { default: '减分', xianxia: '魔障' },
  netScore: { default: '净积分', xianxia: '修为' },
  leaderboard: { default: '排行榜', xianxia: '仙榜' },
  display: { default: '大屏展示', xianxia: '仙界展示' },
  scoreManagement: { default: '积分管理', xianxia: '修炼管理' },
  quickEvaluation: { default: '快捷评价', xianxia: '天机指引' },
  exportReport: { default: '导出报表', xianxia: '仙册录录' },
  settings: { default: '设置', xianxia: '洞府' },
  generalSettings: { default: '通用设置', xianxia: '道法设置' },
}

export type TermKey = keyof typeof TERMINOLOGY_MAP
