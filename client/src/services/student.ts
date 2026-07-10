import { invoke } from './tauri'
import type { Student } from '@/types'

interface RustStudent {
  id: number
  name: string
  student_number: string | null
  group_id: number | null
  total_score: number
  avatar: string | null
  pet_type: string | null
  pet_name: string | null
  pet_exp: number
  created_at: string
  updated_at: string
}

function toStudent(r: RustStudent): Student {
  return {
    id: String(r.id),
    name: r.name,
    studentNumber: r.student_number ?? undefined,
    groupId: r.group_id ? String(r.group_id) : undefined,
    score: r.total_score,
    avatar: r.avatar ?? undefined,
    petType: r.pet_type ?? undefined,
    petName: r.pet_name ?? undefined,
    petExp: r.pet_exp,
    createdAt: r.created_at,
    updatedAt: r.updated_at,
  }
}

export const studentApi = {
  async getAll() {
    const students = await invoke<RustStudent[]>('student_list', {})
    return { data: { data: students.map(toStudent) } }
  },

  async getById(id: string) {
    const student = await invoke<RustStudent>('student_get', { id: Number(id) })
    return { data: { data: toStudent(student) } }
  },

  async create(student: Partial<Student>) {
    const result = await invoke<RustStudent>('student_create', {
      input: {
        name: student.name ?? '',
        student_number: student.studentNumber ?? null,
        group_id: student.groupId ? Number(student.groupId) : null,
        avatar: student.avatar ?? null,
        pet_type: student.petType ?? null,
        pet_name: student.petName ?? null,
      }
    })
    return { data: { data: toStudent(result) } }
  },

  async update(id: string, student: Partial<Student>) {
    const input: Record<string, unknown> = {
      id: Number(id),
    }
    if (student.name !== undefined) input.name = student.name
    if (student.studentNumber !== undefined) input.student_number = student.studentNumber ?? null
    if (student.groupId !== undefined) input.group_id = student.groupId ? Number(student.groupId) : null
    if (student.avatar !== undefined) input.avatar = student.avatar ?? null
    if (student.petType !== undefined) input.pet_type = student.petType ?? null
    if (student.petName !== undefined) input.pet_name = student.petName ?? null
    if (student.petExp !== undefined) input.pet_exp = student.petExp
    const result = await invoke<RustStudent>('student_update', { input })
    return { data: { data: toStudent(result) } }
  },

  async delete(id: string) {
    await invoke('student_delete', { id: Number(id) })
    return { data: { data: undefined } }
  },

  async batchCreate(students: Partial<Student>[]) {
    const inputs = students.map(s => ({
      name: s.name ?? '',
      student_number: s.studentNumber ?? null,
      group_id: s.groupId ? Number(s.groupId) : null,
      avatar: s.avatar ?? null,
      pet_type: s.petType ?? null,
      pet_name: s.petName ?? null,
    }))
    const results = await invoke<RustStudent[]>('student_batch_create', { students: inputs })
    return { data: { data: results.map(toStudent) } }
  },
}
