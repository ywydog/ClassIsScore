import { computed } from 'vue'
import { TERMINOLOGY_MAP, type TermKey } from './terminology'
import { useSettingsStore } from '@/stores/settings'

/**
 * 术语映射 composable
 * 根据当前主题模式返回对应的术语文字
 */
export function useTerminology() {
  const settingsStore = useSettingsStore()

  const isXianxia = computed(() => settingsStore.settings.themeMode === 'xianxia')

  /**
   * 获取术语文字
   * @param key 术语键
   * @returns 当前主题模式下的术语文字
   */
  function t(key: TermKey): string {
    const entry = TERMINOLOGY_MAP[key]
    if (!entry) return key
    return isXianxia.value ? entry.xianxia : entry.default
  }

  /**
   * 获取仙宠等级称号
   */
  function getPetLevelTitle(level: number): string {
    if (!isXianxia.value) {
      // 默认模式
      if (level >= 8) return '已毕业'
      if (level >= 7) return '史诗级'
      if (level >= 5) return '稀有'
      if (level >= 3) return '优秀'
      return '成长中'
    }
    // 修仙模式
    const xianxiaTitles: Record<number, string> = {
      1: '仙胎',
      2: '仙仔',
      3: '仙兽',
      4: '仙道',
      5: '仙灵',
      6: '仙王',
      7: '仙帝',
      8: '仙尊',
    }
    return xianxiaTitles[level] || '仙胎'
  }

  /**
   * 获取仙宠等级边框颜色
   */
  function getPetLevelBorderColor(level: number): string {
    if (!isXianxia.value) {
      // 默认模式颜色
      const colors: Record<number, string> = {
        1: '#E0E0E0',
        2: '#B0B0B0',
        3: '#4488FF',
        4: '#00C8FF',
        5: '#8844FF',
        6: '#FF4488',
        7: '#FF2244',
        8: '#FFCC00',
      }
      return colors[level] || '#E0E0E0'
    }
    // 修仙模式颜色
    const xianxiaColors: Record<number, string> = {
      1: '#D4D4D4',   // 灰白
      2: '#87CEEB',   // 浅蓝
      3: '#4488FF',   // 蓝色
      4: '#00CED1',   // 青色
      5: '#9B59B6',   // 紫色
      6: '#FF69B4',   // 粉红
      7: '#DC143C',   // 红色
      8: '#FFD700',   // 金色
    }
    return xianxiaColors[level] || '#D4D4D4'
  }

  return {
    isXianxia,
    t,
    getPetLevelTitle,
    getPetLevelBorderColor,
  }
}
