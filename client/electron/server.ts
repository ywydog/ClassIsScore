import { spawn, ChildProcess } from 'child_process'
import http from 'http'
import path from 'path'
import { app } from 'electron'

const SERVER_PORT = 18888
const HEALTH_CHECK_INTERVAL = 3000
const HEALTH_CHECK_TIMEOUT = 60000

let serverProcess: ChildProcess | null = null
let healthCheckTimer: ReturnType<typeof setInterval> | null = null

export function getServerUrl(): string {
  return `http://localhost:${SERVER_PORT}`
}

export async function startServer(): Promise<void> {
  const jarPath = findServerJar()
  if (!jarPath) {
    console.warn('未找到 Spring Boot JAR 文件，跳过后端启动')
    return
  }

  return new Promise((resolve, reject) => {
    serverProcess = spawn('java', ['-jar', jarPath, `--server.port=${SERVER_PORT}`], {
      stdio: ['ignore', 'pipe', 'pipe'],
    })

    serverProcess.stdout?.on('data', (data: Buffer) => {
      console.log(`[Spring Boot] ${data.toString().trim()}`)
    })

    serverProcess.stderr?.on('data', (data: Buffer) => {
      console.error(`[Spring Boot] ${data.toString().trim()}`)
    })

    serverProcess.on('error', (err) => {
      console.error('启动 Spring Boot 失败:', err)
      reject(err)
    })

    serverProcess.on('exit', (code) => {
      console.log(`Spring Boot 进程退出，代码: ${code}`)
      serverProcess = null
    })

    const startTime = Date.now()

    healthCheckTimer = setInterval(async () => {
      try {
        const isHealthy = await checkHealth()
        if (isHealthy) {
          if (healthCheckTimer) clearInterval(healthCheckTimer)
          healthCheckTimer = null
          console.log('Spring Boot 后端已就绪')
          resolve()
          return
        }
      } catch {
        // 健康检查失败，继续轮询
      }

      if (Date.now() - startTime > HEALTH_CHECK_TIMEOUT) {
        if (healthCheckTimer) clearInterval(healthCheckTimer)
        healthCheckTimer = null
        reject(new Error('Spring Boot 启动超时'))
      }
    }, HEALTH_CHECK_INTERVAL)
  })
}

export async function stopServer(): Promise<void> {
  if (healthCheckTimer) {
    clearInterval(healthCheckTimer)
    healthCheckTimer = null
  }

  if (!serverProcess || serverProcess.exitCode !== null) {
    return
  }

  return new Promise((resolve) => {
    const shutdownUrl = `${getServerUrl()}/actuator/shutdown`
    const req = http.request(
      shutdownUrl,
      {
        method: 'POST',
        timeout: 5000,
      },
      () => {
        console.log('已发送优雅停止请求')
      }
    )

    req.on('error', () => {
      // 优雅停止失败，强制杀死进程
      if (serverProcess) {
        serverProcess.kill('SIGTERM')
      }
    })

    req.end()

    const forceKillTimer = setTimeout(() => {
      if (serverProcess && serverProcess.exitCode === null) {
        serverProcess.kill('SIGKILL')
      }
    }, 10000)

    serverProcess.on('exit', () => {
      clearTimeout(forceKillTimer)
      serverProcess = null
      resolve()
    })

    // 如果进程已经退出
    if (serverProcess.exitCode !== null) {
      clearTimeout(forceKillTimer)
      serverProcess = null
      resolve()
    }
  })
}

function findServerJar(): string | null {
  const resourcesDir = path.join(app.getAppPath(), 'resources')
  const fs = require('fs')
  if (!fs.existsSync(resourcesDir)) {
    return null
  }

  const files = fs.readdirSync(resourcesDir)
  const jarFile = files.find((f: string) => f.endsWith('.jar') && f.includes('classisscore'))
  return jarFile ? path.join(resourcesDir, jarFile) : null
}

function checkHealth(): Promise<boolean> {
  return new Promise((resolve) => {
    const req = http.get(`${getServerUrl()}/actuator/health`, (res) => {
      let data = ''
      res.on('data', (chunk: Buffer) => {
        data += chunk.toString()
      })
      res.on('end', () => {
        try {
          const parsed = JSON.parse(data)
          resolve(parsed.status === 'UP')
        } catch {
          resolve(false)
        }
      })
    })

    req.on('error', () => resolve(false))
    req.setTimeout(3000, () => {
      req.destroy()
      resolve(false)
    })
  })
}
