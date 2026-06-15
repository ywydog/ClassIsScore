import { invoke } from './tauri'
import type { StudentGroup } from '@/types'

interface RustGroup {
  id: number
  name: string
  description: string | null
  created_at: string
  updated_at: string
}

function toGroup(r: RustGroup): StudentGroup {
  return {
    id: String(r.id),
    name: r.name,
    description: r.description ?? undefined,
    studentIds: [],
    createdAt: r.created_at,
  }
}

export const groupApi = {
  async getAll() {
    const groups = await invoke<RustGroup[]>('group_list', {})
    return { data: { data: groups.map(toGroup) } }
  },

  async getById(id: string) {
    const group = await invoke<RustGroup>('group_get', { id: Number(id) })
    return { data: { data: toGroup(group) } }
  },

  async create(group: Partial<StudentGroup>) {
    const result = await invoke<RustGroup>('group_create', {
      input: {
        name: group.name ?? '',
        description: group.description ?? null,
      }
    })
    return { data: { data: toGroup(result) } }
  },

  async update(id: string, group: Partial<StudentGroup>) {
    const input: Record<string, unknown> = {
      id: Number(id),
    }
    if (group.name !== undefined) input.name = group.name
    if (group.description !== undefined) input.description = group.description ?? null
    const result = await invoke<RustGroup>('group_update', { input })
    return { data: { data: toGroup(result) } }
  },

  async delete(id: string) {
    await invoke('group_delete', { id: Number(id) })
    return { data: { data: undefined } }
  },

  async addStudent(_groupId: string, _studentId: string) {
    return { data: { data: undefined } }
  },

  async removeStudent(_groupId: string, _studentId: string) {
    return { data: { data: undefined } }
  },
}
