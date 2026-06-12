import { contextBridge, ipcRenderer } from 'electron'

contextBridge.exposeInMainWorld('electronAPI', {
  getServerUrl: () => ipcRenderer.invoke('get-server-url'),
  getAppInfo: () => ipcRenderer.invoke('get-app-info'),
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
})
