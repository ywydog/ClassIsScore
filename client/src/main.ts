import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import App from './App.vue'
import router from './router'
import { isTauri } from './services/tauri'
import './themes/variables.css'
import './themes/light.css'
import './themes/dark.css'
import './themes/xianxia/styles.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(ElementPlus)

for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

// 前端日志转发：将 Vue 错误和全局未捕获错误转发到 Rust 日志系统
if (isTauri) {
  import('./services/log').then(({ logApi }) => {
    app.config.errorHandler = (err, _instance, info) => {
      const message = `[Vue] ${info}: ${err instanceof Error ? err.message : String(err)}`
      logApi.write('error', message).catch(() => {})
    }

    window.addEventListener('error', (event) => {
      const message = `[Window] ${event.message} at ${event.filename}:${event.lineno}:${event.colno}`
      logApi.write('error', message).catch(() => {})
    })

    window.addEventListener('unhandledrejection', (event) => {
      const message = `[Promise] Unhandled rejection: ${event.reason instanceof Error ? event.reason.message : String(event.reason)}`
      logApi.write('error', message).catch(() => {})
    })
  })
}

app.mount('#app')
