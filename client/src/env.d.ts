/// <reference types="vite/client" />

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<object, object, unknown>
  export default component
}

interface Window {
  electronAPI?: {
    getServerUrl: () => Promise<string>
    getAppInfo: () => Promise<{ version: string; platform: string }>
    invokeServer: (channel: string, ...args: unknown[]) => Promise<unknown>
    onScoreUpdate: (callback: (data: unknown) => void) => void
    removeScoreUpdateListener: () => void
    openDisplayWindow: () => Promise<void>
    openFloatingWindow: () => Promise<void>
    closeDisplayWindow: () => Promise<void>
    closeFloatingWindow: () => Promise<void>
    openWindow: (name: string) => Promise<void>
    openPath: (path: string) => Promise<void>
  }
}
