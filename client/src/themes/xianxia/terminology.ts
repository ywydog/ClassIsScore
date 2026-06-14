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
  netScore: { default: '净积分', xianxia: '净灵力' },
  cultivation: { default: '修为', xianxia: '修为' },
  leaderboard: { default: '排行榜', xianxia: '仙榜' },
  display: { default: '大屏展示', xianxia: '仙界展示' },
  scoreManagement: { default: '积分管理', xianxia: '修炼管理' },
  quickEvaluation: { default: '快捷评价', xianxia: '天机指引' },
  exportReport: { default: '导出报表', xianxia: '仙册录录' },
  settings: { default: '设置', xianxia: '洞府' },
  generalSettings: { default: '通用设置', xianxia: '道法设置' },
  scoreChange: { default: '积分变动', xianxia: '灵力变动' },
  scorePlusItem: { default: '加分项', xianxia: '悟道项' },
  scoreMinusItem: { default: '扣分项', xianxia: '魔障项' },
  batchScore: { default: '批量积分', xianxia: '批量悟道' },
  addScore: { default: '加分', xianxia: '悟道' },
  subtractScore: { default: '减分', xianxia: '魔障' },
  scoreStats: { default: '积分统计', xianxia: '灵力统计' },
  autoEvaluation: { default: '自动评估', xianxia: '天道评估' },
  evaluationItem: { default: '评估项目', xianxia: '天道项目' },
  selectStudent: { default: '选择学生', xianxia: '选择道友' },
  allStudents: { default: '所有学生', xianxia: '所有道友' },
}

export type TermKey = keyof typeof TERMINOLOGY_MAP
