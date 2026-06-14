import type { ScoreUpdateEvent } from '@/types'
import { isTauri } from './tauri'

interface WebSocketCallbacks {
  onScoreUpdate?: (data: ScoreUpdateEvent) => void
  onConnect?: () => void
  onDisconnect?: () => void
}

let callbacks: WebSocketCallbacks = {}

// Tauri 模式：使用事件系统
let tauriUnlisten: (() => void) | null = null

async function connectTauri(cbs: WebSocketCallbacks) {
  callbacks = cbs
  try {
    const { listen } = await import('@tauri-apps/api/event')
    tauriUnlisten = await listen<ScoreUpdateEvent>('score-update', (event) => {
      callbacks.onScoreUpdate?.(event.payload)
    })
    callbacks.onConnect?.()
  } catch (err) {
    console.error('[Tauri Event] 监听失败:', err)
  }
}

function disconnectTauri() {
  if (tauriUnlisten) {
    tauriUnlisten()
    tauriUnlisten = null
  }
}

// 非 Tauri 模式：使用 WebSocket（开发兼容）
let ws: WebSocket | null = null
let reconnectTimer: ReturnType<typeof setTimeout> | null = null
const RECONNECT_DELAY = 3000

function getWsUrl(): string {
  return 'ws://localhost:18888/ws'
}

function connectWs(cbs: WebSocketCallbacks): void {
  callbacks = cbs

  if (ws && (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING)) {
    return
  }

  try {
    ws = new WebSocket(getWsUrl())

    ws.onopen = () => {
      console.log('[WebSocket] 已连接')
      callbacks.onConnect?.()
    }

    ws.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data)
        if (data.type === 'SCORE_UPDATE' && callbacks.onScoreUpdate) {
          callbacks.onScoreUpdate(data.payload as ScoreUpdateEvent)
        }
      } catch (err) {
        console.error('[WebSocket] 解析消息失败:', err)
      }
    }

    ws.onclose = () => {
      console.log('[WebSocket] 连接关闭')
      callbacks.onDisconnect?.()
      scheduleReconnect()
    }

    ws.onerror = () => {
      ws?.close()
    }
  } catch (err) {
    console.error('[WebSocket] 创建连接失败:', err)
    scheduleReconnect()
  }
}

function disconnectWs(): void {
  if (reconnectTimer) {
    clearTimeout(reconnectTimer)
    reconnectTimer = null
  }

  if (ws) {
    ws.onclose = null
    ws.close()
    ws = null
  }
}

function scheduleReconnect(): void {
  if (reconnectTimer) return

  reconnectTimer = setTimeout(() => {
    reconnectTimer = null
    console.log('[WebSocket] 尝试重连...')
    connectWs(callbacks)
  }, RECONNECT_DELAY)
}

// 统一导出
export function connectWebSocket(cbs: WebSocketCallbacks): void {
  if (isTauri) {
    connectTauri(cbs)
  } else {
    connectWs(cbs)
  }
}

export function disconnectWebSocket(): void {
  if (isTauri) {
    disconnectTauri()
  } else {
    disconnectWs()
  }
}

export function sendWebSocketMessage(_type: string, _payload: unknown): void {
  // Tauri 模式下通过 IPC 命令发送，非 Tauri 模式通过 WebSocket
  if (!isTauri && ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify({ type: _type, payload: _payload }))
  }
}
