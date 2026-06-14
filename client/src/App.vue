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
