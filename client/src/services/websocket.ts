import type { ScoreUpdateEvent } from '@/types'

interface WebSocketCallbacks {
  onScoreUpdate?: (data: ScoreUpdateEvent) => void
  onConnect?: () => void
  onDisconnect?: () => void
}

let ws: WebSocket | null = null
let reconnectTimer: ReturnType<typeof setTimeout> | null = null
let callbacks: WebSocketCallbacks = {}
const RECONNECT_DELAY = 3000

function getWsUrl(): string {
  return 'ws://localhost:18888/ws'
}

export function connectWebSocket(cbs: WebSocketCallbacks): void {
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

    ws.onerror = (err) => {
      console.error('[WebSocket] 连接错误:', err)
      ws?.close()
    }
  } catch (err) {
    console.error('[WebSocket] 创建连接失败:', err)
    scheduleReconnect()
  }
}

export function disconnectWebSocket(): void {
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
    connectWebSocket(callbacks)
  }, RECONNECT_DELAY)
}

export function sendWebSocketMessage(type: string, payload: unknown): void {
  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify({ type, payload }))
  }
}
