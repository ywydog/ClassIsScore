import { invoke } from './tauri'
import type { EvaluationItem } from '@/types'

interface RustEvaluationItem {
  id: number
  name: string
  score_change: number
  category: string | null
  is_quick_access: boolean
  created_at: string
}

function toEvaluationItem(r: RustEvaluationItem): EvaluationItem {
  return {
    id: String(r.id),
    name: r.name,
    scoreChange: r.score_change,
    isPositive: r.score_change >= 0,
    createdAt: r.created_at,
  }
}

export const evaluationApi = {
  async getAll() {
    const items = await invoke<RustEvaluationItem[]>('evaluation_list')
    return { data: { data: items.map(toEvaluationItem) } }
  },

  async create(item: { name: string; scoreChange: number; category?: string; isQuickAccess?: boolean }) {
    const result = await invoke<RustEvaluationItem>('evaluation_create', {
      input: {
        name: item.name,
        score_change: item.scoreChange,
        category: item.category ?? null,
        is_quick_access: item.isQuickAccess ?? null,
      }
    })
    return { data: { data: toEvaluationItem(result) } }
  },

  async update(id: string, item: { name?: string; scoreChange?: number; category?: string; isQuickAccess?: boolean }) {
    const input: Record<string, unknown> = {
      id: Number(id),
    }
    if (item.name !== undefined) input.name = item.name
    if (item.scoreChange !== undefined) input.score_change = item.scoreChange
    if (item.category !== undefined) input.category = item.category ?? null
    if (item.isQuickAccess !== undefined) input.is_quick_access = item.isQuickAccess

    const result = await invoke<RustEvaluationItem>('evaluation_update', { input })
    return { data: { data: toEvaluationItem(result) } }
  },

  async delete(id: string) {
    await invoke('evaluation_delete', { id: Number(id) })
    return { data: { data: undefined } }
  },
}
