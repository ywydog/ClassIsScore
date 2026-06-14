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

// 后端是否已就绪（Electron环境下由主进程通知）
let backendReady = !window.electronAPI // 非Electron环境默认就绪

// Network Error 节流：避免后端未就绪时疯狂弹窗
let lastNetworkErrorTime = 0
const NETWORK_ERROR_THROTTLE = 5000 // 5秒内只弹一次

// 监听后端就绪事件（Electron环境）
if (window.electronAPI) {
  window.electronAPI.onBackendReady(() => {
    backendReady = true
    console.log('前端收到后端就绪通知')
  })
}

// 请求拦截：后端未就绪时静默拦截请求
api.interceptors.request.use(
  (config) => {
    if (!backendReady) {
      return Promise.reject(new Error('BACKEND_NOT_READY'))
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
    if (error.message === 'BACKEND_NOT_READY') {
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
      // 重新监听后端就绪
      if (window.electronAPI) {
        window.electronAPI.isBackendReady()?.then((ready) => {
          if (ready) backendReady = true
        })
      }
    } else {
      const message = error.response?.data?.message || error.message || '网络请求失败'
      ElMessage.error(message)
    }
    return Promise.reject(error)
  }
)

export default api
