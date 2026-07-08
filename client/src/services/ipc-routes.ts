/**
 * IPC 命令 ↔ HTTP 路径映射
 *
 * 这张表同时服务两个方向：
 * 1. 浏览器开发模式：`invoke('student_list')` → `api.get('/api/students')`
 * 2. Tauri 模式：直接调 Tauri IPC，不需要这张表
 *
 * 因此新加 IPC 命令时，必须同时在两边都登记。
 */

export interface IpcRoute {
  method: 'GET' | 'POST' | 'PUT' | 'DELETE'
  /** 路径里 `{id}` / `{key}` 会被 args.id / args.key 替换。 */
  path: string
}

export const cmdToPath: Record<string, IpcRoute> = {
  // 学生
  student_list: { method: 'GET', path: '/api/students' },
  student_get: { method: 'GET', path: '/api/students/{id}' },
  student_create: { method: 'POST', path: '/api/students' },
  student_update: { method: 'PUT', path: '/api/students/{id}' },
  student_delete: { method: 'DELETE', path: '/api/students/{id}' },
  student_batch_create: { method: 'POST', path: '/api/students/batch' },
  student_reset_scores: { method: 'POST', path: '/api/students/{id}/reset-scores' },

  // 积分
  score_list: { method: 'GET', path: '/api/scores' },
  score_add: { method: 'POST', path: '/api/scores' },
  score_batch_add: { method: 'POST', path: '/api/scores/batch' },
  score_revert: { method: 'POST', path: '/api/scores/{id}/revert' },
  score_recent: { method: 'GET', path: '/api/scores/recent' },
  score_stats: { method: 'GET', path: '/api/scores/stats' },
  score_today_count: { method: 'GET', path: '/api/scores/today-count' },
  score_trend: { method: 'GET', path: '/api/scores/trend' },

  // 分组
  group_list: { method: 'GET', path: '/api/groups' },
  group_get: { method: 'GET', path: '/api/groups/{id}' },
  group_create: { method: 'POST', path: '/api/groups' },
  group_update: { method: 'PUT', path: '/api/groups/{id}' },
  group_delete: { method: 'DELETE', path: '/api/groups/{id}' },

  // 评估项
  evaluation_list: { method: 'GET', path: '/api/evaluation/items' },
  evaluation_create: { method: 'POST', path: '/api/evaluation/items' },
  evaluation_update: { method: 'PUT', path: '/api/evaluation/items/{id}' },
  evaluation_delete: { method: 'DELETE', path: '/api/evaluation/items/{id}' },

  // 自动评估规则
  auto_score_get_rules: { method: 'GET', path: '/api/auto-evaluation-configs' },
  auto_score_add_rule: { method: 'POST', path: '/api/auto-evaluation-configs' },
  auto_score_update_rule: { method: 'PUT', path: '/api/auto-evaluation-configs/{id}' },
  auto_score_delete_rule: { method: 'DELETE', path: '/api/auto-evaluation-configs/{id}' },
  auto_score_toggle_rule: { method: 'PUT', path: '/api/auto-evaluation-configs/{id}/toggle' },

  // 排行榜
  leaderboard_query: { method: 'GET', path: '/api/leaderboard' },
  leaderboard_by_group: { method: 'GET', path: '/api/leaderboard/group/{id}' },
  leaderboard_individual: { method: 'GET', path: '/api/leaderboard/individual' },
  leaderboard_all_groups: { method: 'GET', path: '/api/leaderboard/all-groups' },

  // 结算
  settlement_list: { method: 'GET', path: '/api/settlements' },
  settlement_create: { method: 'POST', path: '/api/settlements' },
  settlement_complete: { method: 'POST', path: '/api/settlements/{id}/complete' },
  settlement_rollback: { method: 'POST', path: '/api/settlements/{id}/revert' },

  // 设置
  settings_get_all: { method: 'GET', path: '/api/settings' },
  settings_get: { method: 'GET', path: '/api/settings/{key}' },
  settings_set: { method: 'PUT', path: '/api/settings' },
  settings_export: { method: 'GET', path: '/api/settings/export' },
  settings_import: { method: 'POST', path: '/api/settings/import' },
  settings_data_path: { method: 'GET', path: '/api/settings/data-path' },

  // 认证
  auth_login: { method: 'POST', path: '/api/admin/login' },
  auth_change_password: { method: 'POST', path: '/api/admin/password' },
  auth_verify: { method: 'POST', path: '/api/admin/verify' },
  auth_get_info: { method: 'GET', path: '/api/admin/info' },
  auth_set_passwords: { method: 'POST', path: '/api/admin/passwords' },
  admin_reset: { method: 'POST', path: '/api/admin/reset' },

  // 主题
  theme_list: { method: 'GET', path: '/api/themes' },
  theme_get: { method: 'GET', path: '/api/themes/{id}' },
  theme_install: { method: 'POST', path: '/api/themes/install' },
  theme_toggle: { method: 'PUT', path: '/api/themes/{id}/toggle' },
  theme_delete: { method: 'DELETE', path: '/api/themes/{id}' },

  // 插件
  plugin_list: { method: 'GET', path: '/api/plugins' },
  plugin_get: { method: 'GET', path: '/api/plugins/{id}' },
  plugin_install: { method: 'POST', path: '/api/plugins/install' },
  plugin_toggle: { method: 'PUT', path: '/api/plugins/{id}/toggle' },
  plugin_delete: { method: 'DELETE', path: '/api/plugins/{id}' },

  // 日志
  log_query: { method: 'GET', path: '/api/logs' },
  log_clear: { method: 'POST', path: '/api/logs/clear' },
  log_set_level: { method: 'POST', path: '/api/logs/level' },
  log_write: { method: 'POST', path: '/api/logs/write' },

  // 应用控制
  restart_app: { method: 'POST', path: '/api/app/restart' },
  open_path: { method: 'POST', path: '/api/app/open-path' },
  open_display_window: { method: 'POST', path: '/api/app/open-display' },
}
