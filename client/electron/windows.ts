import { BrowserWindow, screen, nativeImage } from 'electron'
import path from 'path'

export function createMainWindow(): BrowserWindow {
  const iconPath = path.join(__dirname, '../resources/icon.png')
  const mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    center: true,
    title: 'ClassIsScore',
    icon: nativeImage.createFromPath(iconPath),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  })

  if (process.env.VITE_DEV_SERVER_URL) {
    mainWindow.loadURL(process.env.VITE_DEV_SERVER_URL)
  } else {
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'))
  }

  mainWindow.on('close', (event) => {
    event.preventDefault()
    mainWindow.hide()
  })

  return mainWindow
}

export function createDisplayWindow(): BrowserWindow {
  const primaryDisplay = screen.getPrimaryDisplay()
  const { width, height } = primaryDisplay.workAreaSize
  const iconPath = path.join(__dirname, '../resources/icon.png')

  const displayWindow = new BrowserWindow({
    width,
    height,
    fullscreen: true,
    focusable: false,
    skipTaskbar: true,
    title: 'ClassIsScore - 大屏展示',
    icon: nativeImage.createFromPath(iconPath),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  })

  if (process.env.VITE_DEV_SERVER_URL) {
    displayWindow.loadURL(`${process.env.VITE_DEV_SERVER_URL}#/display`)
  } else {
    displayWindow.loadFile(path.join(__dirname, '../dist/index.html'), {
      hash: '/display',
    })
  }

  return displayWindow
}

export function createFloatingWindow(): BrowserWindow {
  const floatingWindow = new BrowserWindow({
    width: 400,
    height: 60,
    frame: false,
    alwaysOnTop: true,
    focusable: false,
    resizable: false,
    skipTaskbar: true,
    transparent: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  })

  if (process.env.VITE_DEV_SERVER_URL) {
    floatingWindow.loadURL(`${process.env.VITE_DEV_SERVER_URL}#/floating`)
  } else {
    floatingWindow.loadFile(path.join(__dirname, '../dist/index.html'), {
      hash: '/floating',
    })
  }

  return floatingWindow
}
