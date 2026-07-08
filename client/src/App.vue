<template>
  <router-view v-slot="{ Component }">
    <transition name="fade-slide" mode="out-in">
      <component :is="Component" />
    </transition>
  </router-view>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useAppStore } from '@/stores/app'

const appStore = useAppStore()

onMounted(async () => {
  // Tauri 模式下后端已在 setup() 中同步初始化，无需等待
  await appStore.initialize()
})

onUnmounted(() => {
  appStore.cleanup()
})
</script>

<style>
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
  /* 适配 iOS / 桌面 Tauri 窗口安全区 */
  padding-top: env(safe-area-inset-top, 0);
  padding-right: env(safe-area-inset-right, 0);
  padding-bottom: env(safe-area-inset-bottom, 0);
  padding-left: env(safe-area-inset-left, 0);
}

/* 关闭 mobile 默认高亮，使用主题色 */
* {
  -webkit-tap-highlight-color: transparent;
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

/* 全局键盘焦点环（键盘用户可见，鼠标点击不出现） */
:where(a, button, [role='button'], input, select, textarea, [tabindex]):focus-visible {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
  border-radius: var(--cis-radius-sm);
}

button:focus,
[role='button']:focus {
  outline: none;
}

/* 标题锚点跳转的视口偏移 */
:where(h1, h2, h3, h4, h5, h6)[id] {
  scroll-margin-top: 80px;
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

/* 尊重用户的减少动画偏好 */
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }

  .fade-slide-enter-active,
  .fade-slide-leave-active {
    transition: none;
  }

  .fade-slide-enter-from,
  .fade-slide-leave-to {
    transform: none;
  }
}
</style>
