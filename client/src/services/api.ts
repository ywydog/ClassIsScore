/**
 * 浏览器开发模式下的 HTTP 客户端（axios）
 *
 * Tauri 环境下业务代码不应直接 import 这个文件，
 * 请走 `@/services/tauri` 的 `invoke()` 函数。
 *
 * 这里的 axios 只在 `tauri.ts` 浏览器 fallback 路径里被动态 import。
 */

import axios from 'axios'
import type { ApiResponse } from '@/types'
import { ElMessage } from 'element-plus'

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

// 后端未就绪标记：HTTP 后端没起来时不要每请求都弹错
let backendReady = true
let lastNetworkErrorTime = 0
const NETWORK_ERROR_THROTTLE = 5000 // 5 秒内只弹一次

api.interceptors.request.use(
  (config) => {
    if (!backendReady) {
      return Promise.reject(new Error('BACKEND_NOT_READY'))
    }
    return config
  },
  (error) => Promise.reject(error)
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
    if (error.message === 'BACKEND_NOT_READY') {
      return Promise.reject(error)
    }
    if (error.message === 'Network Error' || error.code === 'ERR_NETWORK') {
      const now = Date.now()
      if (now - lastNetworkErrorTime > NETWORK_ERROR_THROTTLE) {
        lastNetworkErrorTime = now
        ElMessage.error('无法连接到服务器，请确认后端服务已启动')
      }
      backendReady = false
    } else {
      const message = error.response?.data?.message || error.message || '网络请求失败'
      ElMessage.error(message)
    }
    return Promise.reject(error)
  }
)

export default api
