import { invoke } from './tauri'
import type { AutoEvaluationConfig } from '@/types'

interface RustAutoEval {
  // 安全最佳实践：使用 UUID public_id 而非自增 id 作为外部标识
  id: number
  public_id: string
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
  last_executed_at: string | null
  created_at: string
  updated_at: string
}

function toAutoEval(r: RustAutoEval): AutoEvaluationConfig {
  return {
    // 暴露给前端的 id 使用 public_id（UUID），后端数据库自增 id 不外泄
    id: r.public_id,
    name: r.name,
    triggerType: r.trigger_type as AutoEvaluationConfig['triggerType'],
    triggerTime: r.trigger_time ?? '',
    dayOfWeek: r.day_of_week,
    dayOfMonth: r.day_of_month,
    evaluationItemId: r.evaluation_item_id,
    scoreChange: r.score_change,
    reason: r.reason ?? '',
    targetType: (r.target_type ?? 'AllStudents') as AutoEvaluationConfig['targetType'],
    targetGroupId: r.target_group_id,
    targetStudentId: r.target_student_id,
    isEnabled: r.is_enabled,
    createdAt: r.created_at,
  }
}

export const autoScoreApi = {
  async getAll() {
    const rules = await invoke<RustAutoEval[]>('auto_score_get_rules', {})
    return { data: { data: rules.map(toAutoEval) } }
  },

  async create(rule: Partial<AutoEvaluationConfig>) {
    const result = await invoke<RustAutoEval>('auto_score_add_rule', {
      input: {
        name: rule.name ?? '',
        trigger_type: rule.triggerType ?? 'Daily',
        trigger_time: rule.triggerTime ?? null,
        day_of_week: rule.dayOfWeek ?? null,
        day_of_month: rule.dayOfMonth ?? null,
        evaluation_item_id: rule.evaluationItemId ?? null,
        score_change: rule.scoreChange ?? null,
        reason: rule.reason ?? null,
        target_type: rule.targetType ?? 'AllStudents',
        target_group_id: rule.targetGroupId ?? null,
        target_student_id: rule.targetStudentId ?? null,
      }
    })
    return { data: { data: toAutoEval(result) } }
  },

  // 安全最佳实践：使用 public_id（UUID 字符串）作为外部 ID
  async update(id: string, rule: Partial<AutoEvaluationConfig>) {
    const result = await invoke<RustAutoEval>('auto_score_update_rule', {
      public_id: id,
      input: {
        name: rule.name ?? '',
        trigger_type: rule.triggerType ?? 'Daily',
        trigger_time: rule.triggerTime ?? null,
        day_of_week: rule.dayOfWeek ?? null,
        day_of_month: rule.dayOfMonth ?? null,
        evaluation_item_id: rule.evaluationItemId ?? null,
        score_change: rule.scoreChange ?? null,
        reason: rule.reason ?? null,
        target_type: rule.targetType ?? 'AllStudents',
        target_group_id: rule.targetGroupId ?? null,
        target_student_id: rule.targetStudentId ?? null,
      }
    })
    return { data: { data: toAutoEval(result) } }
  },

  async delete(id: string) {
    await invoke('auto_score_delete_rule', { public_id: id })
    return { data: { data: undefined } }
  },

  async toggle(id: string, enabled: boolean) {
    const result = await invoke<RustAutoEval>('auto_score_toggle_rule', { public_id: id, is_enabled: enabled })
    return { data: { data: toAutoEval(result) } }
  },
}