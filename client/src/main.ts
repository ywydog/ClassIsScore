import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
// 主题样式
// 字体必须在 variables.css 之前声明（variables.css 里 --cis-font-* 引用字体名）
import './themes/fonts.css'
import './themes/element-overrides.css'

import App from './App.vue'
import router from './router'
import './themes/variables.css'
import './themes/light.css'
import './themes/dark.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(ElementPlus)

for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

// 监听 Tauri 后端事件
async function setupTauriListeners() {
  if (typeof window === 'undefined') return
  if (!('__TAURI_INTERNALS__' in window)) return
  try {
    const { listen } = await import('@tauri-apps/api/event')
    // 后端 emit('navigate', '/display') 时跳转到对应路由
    await listen<string>('navigate', (event) => {
      const path = event.payload
      if (typeof path === 'string' && path.startsWith('/')) {
        router.push(path).catch(() => {
          // 忽略重复导航
        })
      }
    })
  } catch (e) {
    console.warn('Tauri 事件监听初始化失败:', e)
  }
}

setupTauriListeners()

app.mount('#app')

