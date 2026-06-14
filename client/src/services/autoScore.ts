import { invoke } from './tauri'
import type { AutoEvaluationConfig } from '@/types'

interface RustAutoEvalConfig {
  id: number
  name: string
  trigger_type: string
  trigger_time: string | null
  day_of_week: number | null
  day_of_month: number | null
  evaluation_item_id: number | null
  score_change: number | null
  reason: string | null
  target_type: string | null
  target_group_id: number | null
  target_student_id: number | null
  is_enabled: boolean
  created_at: string
  updated_at: string
}

function toAutoEvalConfig(r: RustAutoEvalConfig): AutoEvaluationConfig {
  return {
    id: String(r.id),
    name: r.name,
    triggerType: r.trigger_type as AutoEvaluationConfig['triggerType'],
    triggerTime: r.trigger_time ?? '',
    dayOfWeek: r.day_of_week,
    dayOfMonth: r.day_of_month,
    evaluationItemId: r.evaluation_item_id,
    scoreChange: r.score_change,
    reason: r.reason ?? '',
    targetType: (r.target_type ?? undefined) as AutoEvaluationConfig['targetType'],
    targetGroupId: r.target_group_id,
    targetStudentId: r.target_student_id,
    isEnabled: r.is_enabled,
    createdAt: r.created_at,
  }
}

export const autoScoreApi = {
  async getAll() {
    const rules = await invoke<RustAutoEvalConfig[]>('auto_score_get_rules')
    return { data: { data: rules.map(toAutoEvalConfig) } }
  },

  async addRule(config: Record<string, unknown>) {
    const result = await invoke<RustAutoEvalConfig>('auto_score_add_rule', {
      input: {
        name: config.name,
        trigger_type: config.triggerType,
        trigger_time: config.triggerTime || null,
        day_of_week: config.dayOfWeek,
        day_of_month: config.dayOfMonth,
        evaluation_item_id: config.evaluationItemId,
        score_change: config.scoreChange,
        reason: config.reason || null,
        target_type: config.targetType ?? null,
        target_group_id: config.targetGroupId,
        target_student_id: config.targetStudentId,
      }
    })
    return { data: { data: toAutoEvalConfig(result) } }
  },

  async updateRule(id: string, config: Record<string, unknown>) {
    const result = await invoke<RustAutoEvalConfig>('auto_score_update_rule', {
      id: Number(id),
      input: {
        name: config.name,
        trigger_type: config.triggerType,
        trigger_time: config.triggerTime || null,
        day_of_week: config.dayOfWeek,
        day_of_month: config.dayOfMonth,
        evaluation_item_id: config.evaluationItemId,
        score_change: config.scoreChange,
        reason: config.reason || null,
        target_type: config.targetType ?? null,
        target_group_id: config.targetGroupId,
        target_student_id: config.targetStudentId,
      }
    })
    return { data: { data: toAutoEvalConfig(result) } }
  },

  async deleteRule(id: string) {
    await invoke('auto_score_delete_rule', { id: Number(id) })
    return { data: { data: undefined } }
  },

  async toggleRule(id: string, isEnabled: boolean) {
    await invoke('auto_score_toggle_rule', { id: Number(id), isEnabled })
    return { data: { data: undefined } }
  },
}
