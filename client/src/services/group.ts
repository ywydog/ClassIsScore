import { invoke } from './tauri'
import type { StudentGroup } from '@/types'

interface RustGroup {
  id: number
  name: string
  description: string | null
  created_at: string
}

function toGroup(r: RustGroup): StudentGroup {
  return {
    id: String(r.id),
    name: r.name,
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
    const groups = await invoke<RustGroup[]>('group_list', {})
    const group = groups.find(g => String(g.id) === id)
    if (!group) throw new Error('小组不存在')
    return { data: { data: toGroup(group) } }
  },

  async create(group: Partial<StudentGroup>) {
    const result = await invoke<RustGroup>('group_create', {
      input: {
        name: group.name ?? '',
        description: null as string | null,
      }
    })
    return { data: { data: toGroup(result) } }
  },

  async update(id: string, group: Partial<StudentGroup>) {
    const result = await invoke<RustGroup>('group_update', {
      input: {
        id: Number(id),
        name: group.name,
        description: null as string | null,
      }
    })
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
