import { app, BrowserWindow, ipcMain } from 'electron'
import { createMainWindow, createDisplayWindow, createFloatingWindow } from './windows'
import { createTray } from './tray'
import { startServer, stopServer, getServerUrl } from './server'

let mainWindow: BrowserWindow | null = null
let displayWindow: BrowserWindow | null = null
let floatingWindow: BrowserWindow | null = null

app.whenReady().then(async () => {
  // 尝试启动后端，失败不阻止前端打开
  try {
    await startServer()
  } catch (err) {
    console.warn('后端启动失败，前端仍可使用:', err)
  }

  mainWindow = createMainWindow()
  createTray(mainWindow, displayWindow, floatingWindow)

  ipcMain.handle('get-server-url', () => getServerUrl())

  ipcMain.handle('open-display-window', () => {
    if (displayWindow) {
      displayWindow.focus()
      return
    }
    displayWindow = createDisplayWindow()
    displayWindow.on('closed', () => {
      displayWindow = null
    })
  })

  ipcMain.handle('open-floating-window', () => {
    if (floatingWindow) {
      floatingWindow.focus()
      return
    }
    floatingWindow = createFloatingWindow()
    floatingWindow.on('closed', () => {
      floatingWindow = null
    })
  })

  ipcMain.handle('close-display-window', () => {
    if (displayWindow && !displayWindow.isDestroyed()) {
      displayWindow.close()
    }
  })

  ipcMain.handle('close-floating-window', () => {
    if (floatingWindow && !floatingWindow.isDestroyed()) {
      floatingWindow.close()
    }
  })

  ipcMain.handle('show-main-window', () => {
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.show()
      mainWindow.focus()
    }
  })

  ipcMain.handle('relaunch-app', () => {
    app.relaunch()
    app.exit(0)
  })

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      mainWindow = createMainWindow()
    } else if (mainWindow) {
      mainWindow.show()
    }
  })
})

app.on('window-all-closed', () => {
  // 不退出应用，等同 ShutdownMode.OnExplicitShutdown
})

app.on('before-quit', async () => {
  await stopServer()
})
