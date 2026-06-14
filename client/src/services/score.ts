import { invoke } from './tauri'
import type { ScoreRecord, StudentScoreStats } from '@/types'

interface RustScoreRecord {
  id: number
  student_id: number
  score_change: number
  reason: string | null
  category: string | null
  operator_id: number | null
  can_quick_revert: boolean
  reverted: boolean
  created_at: string
}

interface RustStudentScoreStats {
  student_id: number
  student_name: string
  total_score: number
  day_plus: number
  day_minus: number
  day_net: number
  week_plus: number
  week_minus: number
  week_net: number
  month_plus: number
  month_minus: number
  month_net: number
}

function toScoreRecord(r: RustScoreRecord): ScoreRecord {
  return {
    id: String(r.id),
    studentId: String(r.student_id),
    studentName: '',
    scoreChange: r.score_change,
    reason: r.reason ?? '',
    operator: r.operator_id ? String(r.operator_id) : undefined,
    createdAt: r.created_at,
    isReverted: r.reverted,
    canQuickRevert: r.can_quick_revert,
    needsAdminRevert: !r.reverted && !r.can_quick_revert,
  }
}

function toStudentScoreStats(r: RustStudentScoreStats): StudentScoreStats {
  return {
    studentId: r.student_id,
    studentName: r.student_name,
    totalScore: r.total_score,
    dayPlus: r.day_plus,
    dayMinus: r.day_minus,
    dayNet: r.day_net,
    weekPlus: r.week_plus,
    weekMinus: r.week_minus,
    weekNet: r.week_net,
    monthPlus: r.month_plus,
    monthMinus: r.month_minus,
    monthNet: r.month_net,
  }
}

export const scoreApi = {
  async getRecords(studentId?: string) {
    const records = await invoke<RustScoreRecord[]>('score_list', {
      studentId: studentId ? Number(studentId) : null,
    })
    return { data: { data: records.map(toScoreRecord) } }
  },

  async addScore(data: { studentId: string; scoreChange: number; reason: string }) {
    const result = await invoke<RustScoreRecord>('score_add', {
      input: {
        student_id: Number(data.studentId),
        score_change: data.scoreChange,
        reason: data.reason,
      }
    })
    return { data: { data: toScoreRecord(result) } }
  },

  async batchAddScore(data: { studentIds: string[]; scoreChange: number; reason: string }) {
    const results = await invoke<RustScoreRecord[]>('score_batch_add', {
      input: {
        student_ids: data.studentIds.map(Number),
        score_change: data.scoreChange,
        reason: data.reason,
      }
    })
    return { data: { data: results.map(toScoreRecord) } }
  },

  async revertScore(recordId: string, _adminPassword?: string) {
    await invoke('score_revert', { id: Number(recordId) })
    return { data: { data: undefined } }
  },

  async canRevert(_recordId: string) {
    return {
      data: {
        data: {
          canQuickRevert: false,
          needsAdminVerification: true,
        }
      }
    }
  },

  async getRecentRecords(limit: number = 50) {
    const records = await invoke<RustScoreRecord[]>('score_recent', { limit })
    return { data: { data: records.map(toScoreRecord) } }
  },

  async getStats(_semesterStartDate?: string) {
    const stats = await invoke<RustStudentScoreStats[]>('score_stats_all')
    return { data: { data: stats.map(toStudentScoreStats) } }
  },
}
