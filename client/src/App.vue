<template>
  <div v-if="waitingBackend" class="backend-waiting">
    <div class="waiting-content">
      <div class="waiting-spinner"></div>
      <p>正在连接服务器...</p>
    </div>
  </div>
  <router-view v-else v-slot="{ Component }">
    <transition name="fade-slide" mode="out-in">
      <component :is="Component" />
    </transition>
  </router-view>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useAppStore } from '@/stores/app'

const appStore = useAppStore()
const waitingBackend = ref(false)

onMounted(async () => {
  // Electron环境下等待后端就绪
  if (window.electronAPI) {
    const ready = await window.electronAPI.isBackendReady()
    if (!ready) {
      waitingBackend.value = true
      // 等待后端就绪事件
      await new Promise<void>((resolve) => {
        const onReady = () => {
          window.electronAPI?.removeBackendReadyListener()
          resolve()
        }
        window.electronAPI?.onBackendReady(onReady)
        // 超时保底：30秒后无论如何继续
        setTimeout(() => {
          window.electronAPI?.removeBackendReadyListener()
          resolve()
        }, 30000)
      })
      waitingBackend.value = false
    }
  }

  await appStore.initialize()
})

onUnmounted(() => {
  appStore.cleanup()
})
</script>

<style>
/* 加载落霞孤鹜字体 - 开源中文字体，温暖文艺 */
@import url('https://cdn.jsdelivr.net/npm/lxgw-wenkai-webfont@1.7.0/style.css');

html,
body,
#app {
  margin: 0;
  padding: 0;
  height: 100%;
  width: 100%;
  font-family: var(--cis-font-family);
  font-size: var(--cis-font-size-base);
  line-height: var(--cis-line-height-normal);
  background-color: var(--cis-bg);
  color: var(--cis-text-primary);
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

/* 后端等待页面 */
.backend-waiting {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100vh;
  width: 100vw;
}

.waiting-content {
  text-align: center;
  color: var(--cis-text-secondary);
}

.waiting-spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--cis-border-light);
  border-top-color: var(--cis-primary);
  border-radius: 50%;
  margin: 0 auto 16px;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* 全局滚动条美化 */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: transparent;
}

::-webkit-scrollbar-thumb {
  background: var(--cis-text-placeholder);
  border-radius: var(--cis-radius-full);
}

::-webkit-scrollbar-thumb:hover {
  background: var(--cis-text-tertiary);
}

/* Element Plus 主题覆盖 */
.el-button--primary {
  --el-button-bg-color: var(--cis-primary);
  --el-button-border-color: var(--cis-primary);
  --el-button-hover-bg-color: var(--cis-primary-light);
  --el-button-hover-border-color: var(--cis-primary-light);
  --el-button-active-bg-color: var(--cis-primary-dark);
  --el-button-active-border-color: var(--cis-primary-dark);
}

.el-menu-item.is-active {
  color: var(--cis-primary) !important;
}

/* 全局选中色 */
::selection {
  background-color: var(--cis-primary-light-7);
  color: var(--cis-primary-dark);
}

/* 页面过渡动画 */
.fade-slide-enter-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}

.fade-slide-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(8px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
