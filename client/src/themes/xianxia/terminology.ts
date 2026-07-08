/**
 * 修仙主题术语映射表
 * 键为术语ID，值为 { default: 默认模式文字, xianxia: 修仙模式文字 }
 */

export interface TermEntry {
  default: string
  xianxia: string
}

export const TERMINOLOGY_MAP: Record<string, TermEntry> = {
  // 核心概念
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
  scoreUnit: { default: '分', xianxia: '灵力' },
  scoreChange: { default: '积分变动', xianxia: '灵力变动' },
  scorePlusItem: { default: '加分项', xianxia: '悟道项' },
  scoreMinusItem: { default: '扣分项', xianxia: '魔障项' },
  scoreStats: { default: '积分统计', xianxia: '灵力统计' },
  scoreColumn: { default: '积分列', xianxia: '灵力列' },

  // 页面/功能名称
  leaderboard: { default: '排行榜', xianxia: '仙榜' },
  leaderboardTitle: { default: '积分排行榜', xianxia: '仙榜' },
  display: { default: '大屏展示', xianxia: '仙界展示' },
  scoreManagement: { default: '积分管理', xianxia: '修炼管理' },
  quickEvaluation: { default: '快捷评价', xianxia: '天机指引' },
  quickLabel: { default: '快捷', xianxia: '天机' },
  exportReport: { default: '导出报表', xianxia: '仙册录录' },
  settings: { default: '设置', xianxia: '洞府' },
  generalSettings: { default: '通用设置', xianxia: '道法设置' },
  autoEvaluation: { default: '自动评估', xianxia: '天道评估' },
  evaluationItem: { default: '评估项目', xianxia: '天道项目' },
  editEvaluationItem: { default: '编辑评估项', xianxia: '编辑天道项' },
  addEvaluationItem: { default: '添加评估项', xianxia: '添加天道项' },

  // 操作
  batchScore: { default: '批量积分', xianxia: '批量悟道' },
  batchOperation: { default: '批量操作', xianxia: '批量悟道' },
  batchScoreAction: { default: '批量评分', xianxia: '批量悟道' },
  addScore: { default: '加分', xianxia: '悟道' },
  subtractScore: { default: '减分', xianxia: '魔障' },
  confirmScore: { default: '确认评分', xianxia: '确认悟道' },
  importScore: { default: '从表格导入积分', xianxia: '从表格导入灵力' },

  // 选择/目标
  selectStudent: { default: '选择学生', xianxia: '选择道友' },
  targetStudent: { default: '目标学生', xianxia: '目标道友' },
  allStudents: { default: '所有学生', xianxia: '所有道友' },
  selectPet: { default: '选择宠物', xianxia: '选择仙宠' },
  removePet: { default: '移除宠物', xianxia: '移除仙宠' },

  // 大屏展示设置
  displaySettings: { default: '展示设置', xianxia: '仙界设置' },
  displaySettingsHint: { default: '调整排行榜的展示效果', xianxia: '调整仙榜的展示效果' },
  sortBy: { default: '排序方式', xianxia: '排序方式' },
  sortByRank: { default: '按排名', xianxia: '按榜位' },
  sortByNumber: { default: '按学号', xianxia: '按道号' },
  privacyMode: { default: '隐私模式', xianxia: '隐私模式' },
  showRealName: { default: '真名', xianxia: '道名' },
  showAlias: { default: '昵称', xianxia: '别名' },
  showNumber: { default: '学号', xianxia: '道号' },
  displayItems: { default: '显示项', xianxia: '显示项' },
  showPet: { default: '显示宠物', xianxia: '显示仙宠' },
  showGroup: { default: '显示小组', xianxia: '显示宗门' },
  showTrend: { default: '显示趋势', xianxia: '显示变化' },
  refreshInterval: { default: '刷新间隔', xianxia: '刷新间隔' },
  advancedSettings: { default: '更多设置', xianxia: '更多设置' },
  multiSelect: { default: '多选', xianxia: '点选' },
  periodStats: { default: '周期统计', xianxia: '周期统计' },
  reasonPlaceholder: { default: '备注（可选）', xianxia: '备注（可选）' },
  noLeaderboardData: { default: '暂无排行榜数据', xianxia: '仙榜尚空' },

  // 提示消息
  selectStudentFirst: { default: '请先选择学生', xianxia: '请先选择道友' },
  selectStudentAndReason: { default: '请选择学生并填写原因', xianxia: '请选择道友并填写原因' },
  addScoreRequired: { default: '加分分数必须大于0', xianxia: '悟道灵力必须大于0' },
  subtractScoreRequired: { default: '减分分数必须大于0', xianxia: '魔障灵力必须大于0' },
}

export type TermKey = keyof typeof TERMINOLOGY_MAP
