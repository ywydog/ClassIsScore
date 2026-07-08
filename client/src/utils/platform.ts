/**
 * 平台检测工具
 */

/** 是否在 Tauri 环境中 */
export const isTauri = (): boolean => {
  if (typeof window === 'undefined') return false
  return '__TAURI_INTERNALS__' in window || '__TAURI__' in window
}

/** 是否为 Android 平台 */
export const isAndroid = (): boolean => {
  if (typeof navigator === 'undefined') return false
  return /android/i.test(navigator.userAgent)
}

/** 是否为 iOS 平台 */
export const isIOS = (): boolean => {
  if (typeof navigator === 'undefined') return false
  return /iphone|ipad|ipod/i.test(navigator.userAgent)
}

/** 是否为移动端 */
export const isMobile = (): boolean => isAndroid() || isIOS()

/** 是否为桌面端 */
export const isDesktop = (): boolean => isTauri() && !isMobile()

/** 视口宽度断点（dp） */
export const breakpoints = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
} as const

/** 当前视口断点 */
export const currentBreakpoint = (): 'sm' | 'md' | 'lg' | 'xl' => {
  if (typeof window === 'undefined') return 'lg'
  const w = window.innerWidth
  if (w < breakpoints.sm) return 'sm'
  if (w < breakpoints.md) return 'md'
  if (w < breakpoints.lg) return 'lg'
  return 'xl'
}

/** 是否为平板及以上 */
export const isTabletOrLarger = (): boolean => {
  if (typeof window === 'undefined') return false
  return window.innerWidth >= breakpoints.md
}
