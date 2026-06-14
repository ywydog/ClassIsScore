import fs from 'fs'
import path from 'path'
import { app } from 'electron'

const LOG_DIR = path.join(app.getAppPath(), 'logs')
const MAX_LOG_FILES = 10
const MAX_LOG_SIZE = 5 * 1024 * 1024 // 5MB

let currentLogFile: string
let logStream: fs.WriteStream | null = null

function getLogFileName(): string {
  const now = new Date()
  const date = now.toISOString().slice(0, 10)
  return `app-${date}.log`
}

function ensureLogDir(): void {
  if (!fs.existsSync(LOG_DIR)) {
    fs.mkdirSync(LOG_DIR, { recursive: true })
  }
}

function openLogStream(): void {
  ensureLogDir()
  currentLogFile = path.join(LOG_DIR, getLogFileName())
  logStream = fs.createWriteStream(currentLogFile, { flags: 'a' })
}

function rotateIfNeeded(): void {
  if (!logStream || !currentLogFile) return
  try {
    const stats = fs.statSync(currentLogFile)
    if (stats.size >= MAX_LOG_SIZE) {
      logStream.end()
      logStream = null
      openLogStream()
    }
  } catch {
    // 文件不存在，重新打开
    openLogStream()
  }
}

function cleanOldLogs(): void {
  ensureLogDir()
  const files = fs.readdirSync(LOG_DIR)
  const logFiles = files
    .filter(f => f.startsWith('app-') && f.endsWith('.log'))
    .sort()
    .reverse()

  // 删除超过数量的旧日志
  for (let i = MAX_LOG_FILES; i < logFiles.length; i++) {
    try {
      fs.unlinkSync(path.join(LOG_DIR, logFiles[i]))
    } catch {
      // 忽略删除失败
    }
  }
}

function formatMessage(level: string, ...args: unknown[]): string {
  const timestamp = new Date().toISOString()
  const message = args.map(a => {
    if (a instanceof Error) return `${a.message}\n${a.stack}`
    if (typeof a === 'object') {
      try { return JSON.stringify(a) } catch { return String(a) }
    }
    return String(a)
  }).join(' ')
  return `[${timestamp}] [${level}] ${message}\n`
}

function writeLog(level: string, ...args: unknown[]): void {
  const line = formatMessage(level, ...args)
  // 仍然输出到控制台
  if (level === 'ERROR') {
    process.stderr.write(line)
  } else {
    process.stdout.write(line)
  }
  // 写入文件
  if (!logStream) openLogStream()
  rotateIfNeeded()
  logStream?.write(line)
}

export function initLogger(): void {
  openLogStream()
  cleanOldLogs()

  // 覆盖 console 方法，同时输出到文件
  const origLog = console.log
  const origWarn = console.warn
  const origError = console.error

  console.log = (...args: unknown[]) => {
    origLog.apply(console, args)
    writeLog('INFO', ...args)
  }

  console.warn = (...args: unknown[]) => {
    origWarn.apply(console, args)
    writeLog('WARN', ...args)
  }

  console.error = (...args: unknown[]) => {
    origError.apply(console, args)
    writeLog('ERROR', ...args)
  }

  // 捕获未处理的异常和Promise拒绝
  process.on('uncaughtException', (err) => {
    writeLog('ERROR', 'Uncaught Exception:', err)
  })

  process.on('unhandledRejection', (reason) => {
    writeLog('ERROR', 'Unhandled Rejection:', reason)
  })

  console.log('日志系统已初始化，日志目录:', LOG_DIR)
}

export function getLogDir(): string {
  return LOG_DIR
}
