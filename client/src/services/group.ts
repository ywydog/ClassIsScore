import api from './api'
import type { ApiResponse, StudentGroup } from '@/types'

export const groupApi = {
  getAll() {
    return api.get<ApiResponse<StudentGroup[]>>('/api/groups')
  },

  getById(id: string) {
    return api.get<ApiResponse<StudentGroup>>(`/api/groups/${id}`)
  },

  create(group: Partial<StudentGroup>) {
    return api.post<ApiResponse<StudentGroup>>('/api/groups', group)
  },

  update(id: string, group: Partial<StudentGroup>) {
    return api.put<ApiResponse<StudentGroup>>(`/api/groups/${id}`, group)
  },

  delete(id: string) {
    return api.delete<ApiResponse<void>>(`/api/groups/${id}`)
  },

  addStudent(groupId: string, studentId: string) {
    return api.post<ApiResponse<void>>(`/api/groups/${groupId}/students/${studentId}`)
  },

  removeStudent(groupId: string, studentId: string) {
    return api.delete<ApiResponse<void>>(`/api/groups/${groupId}/students/${studentId}`)
  },
}
