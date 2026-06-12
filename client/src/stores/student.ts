import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Student } from '@/types'
import { studentApi } from '@/services/student'

export const useStudentStore = defineStore('student', () => {
  const students = ref<Student[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const studentCount = computed(() => students.value.length)

  const studentMap = computed(() => {
    const map = new Map<string, Student>()
    students.value.forEach((s) => map.set(s.id, s))
    return map
  })

  async function fetchStudents() {
    loading.value = true
    error.value = null
    try {
      const response = await studentApi.getAll()
      students.value = response.data.data
    } catch (err) {
      error.value = err instanceof Error ? err.message : '获取学生列表失败'
    } finally {
      loading.value = false
    }
  }

  async function createStudent(student: Partial<Student>) {
    loading.value = true
    error.value = null
    try {
      const response = await studentApi.create(student)
      students.value.push(response.data.data)
      return response.data.data
    } catch (err) {
      error.value = err instanceof Error ? err.message : '创建学生失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateStudent(id: string, student: Partial<Student>) {
    loading.value = true
    error.value = null
    try {
      const response = await studentApi.update(id, student)
      const index = students.value.findIndex((s) => s.id === id)
      if (index !== -1) {
        students.value[index] = response.data.data
      }
      return response.data.data
    } catch (err) {
      error.value = err instanceof Error ? err.message : '更新学生失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deleteStudent(id: string) {
    loading.value = true
    error.value = null
    try {
      await studentApi.delete(id)
      students.value = students.value.filter((s) => s.id !== id)
    } catch (err) {
      error.value = err instanceof Error ? err.message : '删除学生失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function batchCreateStudent(studentList: Partial<Student>[]) {
    loading.value = true
    error.value = null
    try {
      const response = await studentApi.batchCreate(studentList)
      students.value.push(...response.data.data)
      return response.data.data
    } catch (err) {
      error.value = err instanceof Error ? err.message : '批量创建学生失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  function getStudentById(id: string): Student | undefined {
    return studentMap.value.get(id)
  }

  return {
    students,
    loading,
    error,
    studentCount,
    studentMap,
    fetchStudents,
    createStudent,
    updateStudent,
    deleteStudent,
    batchCreateStudent,
    getStudentById,
  }
})
