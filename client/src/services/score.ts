import { invoke } from './tauri'
import type { ScoreRecord } from '@/types'

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

export const scoreApi = {
  async getRecords(studentId?: string, limit?: number) {
    const records = await invoke<RustScoreRecord[]>('score_list', {
      student_id: studentId ? Number(studentId) : null,
      limit: limit ?? null,
    })
    return { data: { data: records.map(toScoreRecord) } }
  },

  async addScore(data: { studentId: string; scoreChange: number; reason: string; category?: string; operatorId?: number }) {
    const result = await invoke<RustScoreRecord>('score_add', {
      input: {
        student_id: Number(data.studentId),
        score_change: data.scoreChange,
        reason: data.reason || null,
        category: data.category ?? null,
        operator_id: data.operatorId ?? null,
      }
    })
    return { data: { data: toScoreRecord(result) } }
  },

  async batchAddScore(data: { studentIds: string[]; scoreChange: number; reason: string; category?: string; operatorId?: number }) {
    const results = await invoke<RustScoreRecord[]>('score_batch_add', {
      input: {
        student_ids: data.studentIds.map(Number),
        score_change: data.scoreChange,
        reason: data.reason || null,
        category: data.category ?? null,
        operator_id: data.operatorId ?? null,
      }
    })
    return { data: { data: results.map(toScoreRecord) } }
  },

  async revertScore(recordId: string, _adminPassword?: string) {
    await invoke('score_revert', { id: Number(recordId) })
    return { data: { data: undefined } }
  },

  async canRevert(recordId: string) {
    try {
      const records = await invoke<RustScoreRecord[]>('score_list', { limit: 1 })
      const record = records.find(r => String(r.id) === recordId)
      if (record) {
        return {
          data: {
            data: {
              canQuickRevert: record.can_quick_revert && !record.reverted,
              needsAdminVerification: !record.can_quick_revert && !record.reverted,
            }
          }
        }
      }
    } catch { /* fall through */ }
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

  async getStats(studentId: string) {
    const stats = await invoke<{ total_positive: number; total_negative: number; count: number }>('score_stats', {
      student_id: Number(studentId),
    })
    return { data: { data: { ...stats } } }
  },
}
