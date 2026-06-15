import { invoke } from './tauri'
import type { SettlementRecord } from '@/types'

interface RustSettlement {
  id: number
  name: string
  period: string | null
  snapshot_data: string | null
  status: number
  created_at: string
  updated_at: string
}

function toSettlement(r: RustSettlement): SettlementRecord {
  return {
    id: String(r.id),
    name: r.name,
    period: r.period ?? undefined,
    snapshotData: r.snapshot_data ?? undefined,
    status: r.status,
    settledAt: r.created_at,
    createdAt: r.created_at,
    studentCount: 0,
    totalScore: 0,
    backupFilePath: '',
    isReverted: r.status === 2,
  }
}

export const settlementApi = {
  async getAll() {
    const records = await invoke<RustSettlement[]>('settlement_list', {})
    return { data: { data: records.map(toSettlement) } }
  },

  async create(data: { name: string; period?: string }) {
    const result = await invoke<RustSettlement>('settlement_create', {
      input: {
        name: data.name,
        period: data.period ?? null,
      }
    })
    return { data: { data: toSettlement(result) } }
  },

  async complete(id: string) {
    const result = await invoke<RustSettlement>('settlement_complete', { id: Number(id) })
    return { data: { data: toSettlement(result) } }
  },

  async rollback(id: string) {
    const result = await invoke<RustSettlement>('settlement_rollback', { id: Number(id) })
    return { data: { data: toSettlement(result) } }
  },
}