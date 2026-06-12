import axios from 'axios'
import type { ApiResponse } from '@/types'
import { ElMessage } from 'element-plus'

function getBaseUrl(): string {
  if (window.electronAPI) {
    return 'http://localhost:18888'
  }
  return 'http://localhost:18888'
}

const api = axios.create({
  baseURL: getBaseUrl(),
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use(
  (config) => {
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
    const message = error.response?.data?.message || error.message || '网络请求失败'
    ElMessage.error(message)
    return Promise.reject(error)
  }
)

export default api
