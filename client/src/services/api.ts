import axios from 'axios'
import type { ApiResponse } from '@/types'
import { ElMessage } from 'element-plus'
import { isTauri } from '@/utils/platform'

function getBaseUrl(): string {
  return 'http://localhost:18888'
}

const api = axios.create({
  baseURL: getBaseUrl(),
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Tauri 模式下后端内嵌进程，默认就绪
let backendReady = true

// Network Error 节流：避免后端未就绪时疯狂弹窗
let lastNetworkErrorTime = 0
const NETWORK_ERROR_THROTTLE = 5000 // 5秒内只弹一次

// 请求拦截：后端未就绪 / Tauri 环境 都静默拦截请求
api.interceptors.request.use(
  (config) => {
    if (!backendReady) {
      return Promise.reject(new Error('BACKEND_NOT_READY'))
    }
    // Tauri 环境下 Rust 后端通过 IPC 暴露命令，并没有 HTTP 服务
    // （http://localhost:18888 在 Tauri 里连不通）。
    // 业务代码应该走 @/services/tauri 的 invoke()，而不是这里的 axios。
    // 这里的实现是给浏览器开发模式连独立 HTTP 后端用的。
    // 如果在 Tauri 环境下被误用，直接 reject 一个特殊错误，
    // 避免在 response 拦截器里弹出"无法连接到服务器"提示。
    if (isTauri()) {
      return Promise.reject(new Error('TAURI_USE_INVOKE'))
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

api.interceptors.response.use(
  (response) => {
    const data = response.data as ApiResponse
    if (data.code !== 0 && data.code !== 200) {
      ElMessage.error(data.message || '请求失败')
      return Promise.reject(new Error(data.message))
    }
    return response
  },
  (error) => {
    // 后端未就绪的静默请求，不弹任何提示
    if (error.message === 'BACKEND_NOT_READY' || error.message === 'TAURI_USE_INVOKE') {
      return Promise.reject(error)
    }

    if (error.message === 'Network Error' || error.code === 'ERR_NETWORK') {
      const now = Date.now()
      if (now - lastNetworkErrorTime > NETWORK_ERROR_THROTTLE) {
        lastNetworkErrorTime = now
        ElMessage.error('无法连接到服务器，请确认后端服务已启动')
      }
      // 标记后端未就绪，后续请求静默
      backendReady = false
    } else {
      const message = error.response?.data?.message || error.message || '网络请求失败'
      ElMessage.error(message)
    }
    return Promise.reject(error)
  }
)

export default api
