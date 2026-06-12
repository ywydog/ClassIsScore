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

  revertScore(recordId: string) {
    return api.post<ApiResponse<void>>(`/api/scores/${recordId}/revert`)
  },

  getRecentRecords(limit: number = 50) {
    return api.get<ApiResponse<ScoreRecord[]>>('/api/scores/recent', { params: { limit } })
  },
}
