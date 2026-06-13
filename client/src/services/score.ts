import api from './api'
import type { ApiResponse, ScoreRecord } from '@/types'

interface AddScoreRequest {
  studentId: string
  scoreChange: number
  reason: string
}

interface BatchAddScoreRequest {
  studentIds: string[]
  scoreChange: number
  reason: string
}

interface RevertScoreRequest {
  adminPassword?: string
}

interface CanRevertResponse {
  canQuickRevert: boolean
  needsAdminVerification: boolean
}

export const scoreApi = {
  getRecords(studentId?: string) {
    const params = studentId ? { studentId } : {}
    return api.get<ApiResponse<ScoreRecord[]>>('/api/scores', { params })
  },

  addScore(data: AddScoreRequest) {
    return api.post<ApiResponse<ScoreRecord>>('/api/scores', data)
  },

  batchAddScore(data: BatchAddScoreRequest) {
    return api.post<ApiResponse<ScoreRecord[]>>('/api/scores/batch', data)
  },

  revertScore(recordId: string, adminPassword?: string) {
    const data: RevertScoreRequest = {}
    if (adminPassword) {
      data.adminPassword = adminPassword
    }
    return api.post<ApiResponse<void>>(`/api/scores/${recordId}/revert`, data)
  },

  canRevert(recordId: string) {
    return api.get<ApiResponse<CanRevertResponse>>(`/api/scores/${recordId}/can-revert`)
  },

  getRecentRecords(limit: number = 50) {
    return api.get<ApiResponse<ScoreRecord[]>>('/api/scores/recent', { params: { limit } })
  },
}
