import api from './api'
import type { ApiResponse, Student } from '@/types'

export const studentApi = {
  getAll() {
    return api.get<ApiResponse<Student[]>>('/api/students')
  },

  getById(id: string) {
    return api.get<ApiResponse<Student>>(`/api/students/${id}`)
  },

  create(student: Partial<Student>) {
    return api.post<ApiResponse<Student>>('/api/students', student)
  },

  update(id: string, student: Partial<Student>) {
    return api.put<ApiResponse<Student>>(`/api/students/${id}`, student)
  },

  delete(id: string) {
    return api.delete<ApiResponse<void>>(`/api/students/${id}`)
  },

  batchCreate(students: Partial<Student>[]) {
    return api.post<ApiResponse<Student[]>>('/api/students/batch', students)
  },
}
