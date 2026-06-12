import { Tray, Menu, nativeImage, app, BrowserWindow } from 'electron'
import path from 'path'

let tray: Tray | null = null

export function createTray(
  mainWindow: BrowserWindow | null,
  displayWindow: BrowserWindow | null,
  floatingWindow: BrowserWindow | null
): Tray {
  const iconPath = path.join(__dirname, '../resources/icon.png')
  const icon = nativeImage.createFromPath(iconPath)

  tray = new Tray(icon.resize({ width: 16, height: 16 }))
  tray.setToolTip('ClassIsScore')

  const contextMenu = Menu.buildFromTemplate([
    {
      label: '显示主窗口',
      click: () => {
        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.show()
          mainWindow.focus()
        }
      },
    },
    {
      label: '显示大屏',
      click: () => {
        if (displayWindow && !displayWindow.isDestroyed()) {
          displayWindow.show()
          displayWindow.focus()
        }
      },
    },
    { type: 'separator' },
    {
      label: '退出',
      click: () => {
        app.quit()
      },
    },
  ])

  tray.setContextMenu(contextMenu)

  tray.on('double-click', () => {
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.show()
      mainWindow.focus()
    }
  })

  return tray
}

export function destroyTray(): void {
  if (tray) {
    tray.destroy()
    tray = null
  }
}
