/**
 * Tauri IPC 调用封装层
 * 替代原来的 HTTP API (api.ts)，直接通过 Tauri IPC 调用 Rust 后端命令
 */

// 检测是否在 Tauri 环境中运行
export const isTauri = typeof window !== 'undefined' && '__TAURI_INTERNALS__' in window

// 延迟加载的 invoke 函数
let _invoke: ((cmd: string, args?: Record<string, unknown>) => Promise<unknown>) | null = null

async function getInvoke() {
  if (_invoke) return _invoke

  if (isTauri) {
    const tauriApi = await import('@tauri-apps/api/core')
    _invoke = tauriApi.invoke
  } else {
    // 非 Tauri 环境：回退到 HTTP API
    const { default: api } = await import('./api')
    _invoke = async (cmd: string, args?: Record<string, unknown>): Promise<unknown> => {
      const cmdToPath: Record<string, { method: string; path: string }> = {
        student_list: { method: 'GET', path: '/api/students' },
        student_get: { method: 'GET', path: '/api/students/{id}' },
        student_create: { method: 'POST', path: '/api/students' },
        student_update: { method: 'PUT', path: '/api/students/{id}' },
        student_delete: { method: 'DELETE', path: '/api/students/{id}' },
        student_batch_create: { method: 'POST', path: '/api/students/batch' },
        score_list: { method: 'GET', path: '/api/scores' },
        score_add: { method: 'POST', path: '/api/scores' },
        score_batch_add: { method: 'POST', path: '/api/scores/batch' },
        score_revert: { method: 'POST', path: '/api/scores/{id}/revert' },
        score_recent: { method: 'GET', path: '/api/scores/recent' },
        group_list: { method: 'GET', path: '/api/groups' },
        group_create: { method: 'POST', path: '/api/groups' },
        group_update: { method: 'PUT', path: '/api/groups/{id}' },
        group_delete: { method: 'DELETE', path: '/api/groups/{id}' },
        evaluation_list: { method: 'GET', path: '/api/evaluations' },
        evaluation_create: { method: 'POST', path: '/api/evaluations' },
        evaluation_update: { method: 'PUT', path: '/api/evaluations/{id}' },
        evaluation_delete: { method: 'DELETE', path: '/api/evaluations/{id}' },
        settings_get_all: { method: 'GET', path: '/api/settings' },
        settings_get: { method: 'GET', path: '/api/settings/{key}' },
        settings_set: { method: 'PUT', path: '/api/settings' },
        auth_login: { method: 'POST', path: '/api/admin/login' },
        auth_verify: { method: 'POST', path: '/api/admin/verify' },
      }

      const mapping = cmdToPath[cmd]
      if (!mapping) {
        throw new Error(`未知的 IPC 命令: ${cmd}`)
      }

      let path = mapping.path
      if (args?.id) {
        path = path.replace('{id}', String(args.id))
      }
      if (args?.key) {
        path = path.replace('{key}', String(args.key))
      }

      const response = await api.request({
        method: mapping.method as 'GET' | 'POST' | 'PUT' | 'DELETE',
        url: path,
        data: mapping.method !== 'GET' ? args : undefined,
        params: mapping.method === 'GET' ? args : undefined,
      })

      return (response as any).data?.data
    }
  }

  return _invoke
}

export async function invoke<T>(cmd: string, args?: Record<string, unknown>): Promise<T> {
  const fn = await getInvoke()
  return fn(cmd, args) as Promise<T>
}
