<template>
  <el-tooltip :content="isDark ? '切换浅色模式' : '切换深色模式'" placement="bottom">
    <button
      type="button"
      class="theme-toggle"
      :aria-label="isDark ? '切换到浅色模式' : '切换到深色模式'"
      :aria-pressed="isDark"
      @click="toggleTheme"
    >
      <transition name="theme-icon" mode="out-in">
        <svg
          v-if="isDark"
          key="sun"
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
        >
          <circle cx="12" cy="12" r="5"></circle>
          <line x1="12" y1="1" x2="12" y2="3"></line>
          <line x1="12" y1="21" x2="12" y2="23"></line>
          <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line>
          <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line>
          <line x1="1" y1="12" x2="3" y2="12"></line>
          <line x1="21" y1="12" x2="23" y2="12"></line>
          <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line>
          <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line>
        </svg>
        <svg
          v-else
          key="moon"
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
        >
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>
        </svg>
      </transition>
    </button>
  </el-tooltip>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()

const isDark = computed(() => {
  if (settingsStore.settings.theme === 'system') {
    if (typeof window === 'undefined' || !window.matchMedia) return false
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  }
  return settingsStore.settings.theme === 'dark'
})

async function toggleTheme() {
  const theme = isDark.value ? 'light' : 'dark'
  await settingsStore.updateSettings({ theme })
}
</script>

<style scoped>
.theme-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  border-radius: var(--cis-radius-md);
  background: transparent;
  color: var(--cis-text-tertiary);
  cursor: pointer;
  transition: background-color var(--cis-transition-fast), color var(--cis-transition-fast);
}

.theme-toggle:hover {
  background: var(--cis-primary-light-9);
  color: var(--cis-primary);
}

.theme-icon-enter-active,
.theme-icon-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.theme-icon-enter-from {
  opacity: 0;
  transform: rotate(-90deg) scale(0.8);
}

.theme-icon-leave-to {
  opacity: 0;
  transform: rotate(90deg) scale(0.8);
}

@media (prefers-reduced-motion: reduce) {
  .theme-icon-enter-active,
  .theme-icon-leave-active {
    transition: none;
  }
  .theme-icon-enter-from,
  .theme-icon-leave-to {
    transform: none;
  }
}
</style>
