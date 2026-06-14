import { contextBridge, ipcRenderer } from 'electron'

contextBridge.exposeInMainWorld('electronAPI', {
  getServerUrl: () => ipcRenderer.invoke('get-server-url'),
  getAppInfo: () => ipcRenderer.invoke('get-app-info'),
  isBackendReady: () => ipcRenderer.invoke('is-backend-ready'),
  onBackendReady: (callback: () => void) => {
    ipcRenderer.on('backend-ready', () => callback())
  },
  removeBackendReadyListener: () => {
    ipcRenderer.removeAllListeners('backend-ready')
  },
  invokeServer: (channel: string, ...args: unknown[]) => ipcRenderer.invoke(channel, ...args),
  onScoreUpdate: (callback: (data: unknown) => void) => {
    ipcRenderer.on('score-update', (_event, data) => callback(data))
  },
  removeScoreUpdateListener: () => {
    ipcRenderer.removeAllListeners('score-update')
  },
  openDisplayWindow: () => ipcRenderer.invoke('open-display-window'),
  openFloatingWindow: () => ipcRenderer.invoke('open-floating-window'),
  closeDisplayWindow: () => ipcRenderer.invoke('close-display-window'),
  closeFloatingWindow: () => ipcRenderer.invoke('close-floating-window'),
  openWindow: (name: string) => {
    if (name === 'display') return ipcRenderer.invoke('open-display-window')
    if (name === 'floating') return ipcRenderer.invoke('open-floating-window')
    if (name === 'main') return ipcRenderer.invoke('show-main-window')
    return Promise.resolve()
  },
  relaunchApp: () => ipcRenderer.invoke('relaunch-app'),
})
