/**
 * Tauri IPC 调用封装层
 *
 * Tauri 环境下：直接走 `@tauri-apps/api/core` 的 `invoke`
 * 浏览器开发环境：通过 axios 调本地 HTTP 后端，路径从 `ipc-routes.ts` 查表
 */

import { isTauri } from '@/utils/platform'
import { cmdToPath, type IpcRoute } from './ipc-routes'

// 延迟加载的 invoke 函数
let _invoke: ((cmd: string, args?: Record<string, unknown>) => Promise<unknown>) | null = null

async function getInvoke() {
  if (_invoke) return _invoke

  if (isTauri()) {
    const tauriApi = await import('@tauri-apps/api/core')
    _invoke = tauriApi.invoke
  } else {
    // 浏览器开发环境：把 IPC 命令翻译成 HTTP 请求
    const { default: api } = await import('./api')

    _invoke = async (cmd: string, args?: Record<string, unknown>): Promise<unknown> => {
      const mapping: IpcRoute | undefined = cmdToPath[cmd]
      if (!mapping) {
        throw new Error(`未知的 IPC 命令: ${cmd}`)
      }

      // 替换路径占位符
      let path = mapping.path
      if (args?.id !== undefined) {
        path = path.replace('{id}', String(args.id))
      }
      if (args?.key !== undefined) {
        path = path.replace('{key}', String(args.key))
      }

      // GET 走 query，其余走 body；并把"用于占位符替换"的 id/key 从参数里抠掉
      const isGet = mapping.method === 'GET'
      const stripped: Record<string, unknown> = {}
      for (const [k, v] of Object.entries(args ?? {})) {
        if (k === 'id' || k === 'key') continue
        stripped[k] = v
      }

      const response = await api.request({
        method: mapping.method as 'GET' | 'POST' | 'PUT' | 'DELETE',
        url: path,
        data: isGet ? undefined : stripped,
        params: isGet ? stripped : undefined,
      })

      // 后端统一返回 { code, data, message }，提取 data 给调用方
      return (response as { data?: { data?: unknown } }).data?.data
    }
  }

  return _invoke
}

export async function invoke<T>(cmd: string, args?: Record<string, unknown>): Promise<T> {
  const fn = await getInvoke()
  return fn(cmd, args) as Promise<T>
}

export { cmdToPath } from './ipc-routes'
