import { invoke } from './tauri'
import type { SettlementRecord } from '@/types'

interface RustSettlement {
  id: number
  name: string
  period: string | null
  snapshot_data: string | null
  status: number
  created_at: string
}

function toSettlementRecord(r: RustSettlement): SettlementRecord {
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
    const records = await invoke<RustSettlement[]>('settlement_list')
    return { data: { data: records.map(toSettlementRecord) } }
  },

  async create(input: { name: string; period?: string }) {
    const result = await invoke<RustSettlement>('settlement_create', {
      input: {
        name: input.name,
        period: input.period ?? null,
      }
    })
    return { data: { data: toSettlementRecord(result) } }
  },

  async revert(id: string) {
    await invoke('settlement_rollback', { id: Number(id) })
    return { data: { data: undefined } }
  },
}
