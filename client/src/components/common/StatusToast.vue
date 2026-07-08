<template>
  <transition-group name="toast-slide" tag="div" class="status-toast" aria-live="polite" aria-atomic="true">
    <div
      v-for="toast in toasts"
      :key="toast.id"
      :class="['status-toast__item', `status-toast__item--${toast.type}`]"
      role="status"
    >
      <div class="status-toast__icon" aria-hidden="true">
        <el-icon v-if="toast.type === 'success'"><SuccessFilled /></el-icon>
        <el-icon v-else-if="toast.type === 'error'"><CircleCloseFilled /></el-icon>
        <el-icon v-else-if="toast.type === 'warning'"><WarningFilled /></el-icon>
        <el-icon v-else><InfoFilled /></el-icon>
      </div>
      <span class="status-toast__message">{{ toast.message }}</span>
      <button
        type="button"
        class="status-toast__close"
        :aria-label="`关闭通知：${toast.message}`"
        @click="removeToast(toast.id)"
      >
        <el-icon><Close /></el-icon>
      </button>
    </div>
  </transition-group>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useAppStore } from '@/stores/app'
import { SuccessFilled, CircleCloseFilled, WarningFilled, InfoFilled, Close } from '@element-plus/icons-vue'

const appStore = useAppStore()
const { toasts } = storeToRefs(appStore)
const { removeToast } = appStore
</script>

<style scoped>
.status-toast {
  position: fixed;
  top: calc(60px + var(--cis-safe-top, 0));
  right: calc(20px + var(--cis-safe-right, 0));
  z-index: var(--cis-z-toast, 500);
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-width: min(420px, calc(100vw - 32px));
}

.status-toast__item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 18px;
  border-radius: var(--cis-radius-lg);
  font-size: 13px;
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border: 1px solid transparent;
  box-shadow: var(--cis-shadow-lg);
  transition: transform var(--cis-transition-fast), box-shadow var(--cis-transition-fast);
  min-width: 200px;
}

.status-toast__item:hover {
  transform: translateX(-4px);
  box-shadow: var(--cis-shadow-dropdown);
}

.status-toast__icon {
  display: flex;
  align-items: center;
  font-size: 16px;
  flex-shrink: 0;
}

.status-toast__message {
  font-weight: 500;
  flex: 1;
  min-width: 0;
  word-break: break-word;
}

.status-toast__close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  padding: 0;
  border: none;
  border-radius: var(--cis-radius-sm);
  background: transparent;
  color: inherit;
  cursor: pointer;
  flex-shrink: 0;
  transition: background-color var(--cis-transition-fast);
}

.status-toast__close:hover {
  background: rgba(0, 0, 0, 0.06);
}

.status-toast__close .el-icon {
  font-size: 14px;
}

.status-toast__item--success {
  background: rgba(34, 197, 94, 0.12);
  color: var(--cis-success);
  border-color: rgba(34, 197, 94, 0.2);
}

.status-toast__item--error {
  background: rgba(239, 68, 68, 0.12);
  color: var(--cis-danger);
  border-color: rgba(239, 68, 68, 0.2);
}

.status-toast__item--warning {
  background: rgba(245, 158, 11, 0.12);
  color: var(--cis-warning);
  border-color: rgba(245, 158, 11, 0.2);
}

.status-toast__item--info {
  background: rgba(13, 148, 136, 0.12);
  color: var(--cis-primary);
  border-color: rgba(13, 148, 136, 0.2);
}

.toast-slide-enter-active {
  transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.3s ease;
}

.toast-slide-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.toast-slide-enter-from,
.toast-slide-leave-to {
  opacity: 0;
  transform: translateX(40px) scale(0.95);
}

@media (prefers-reduced-motion: reduce) {
  .toast-slide-enter-active,
  .toast-slide-leave-active {
    transition: none;
  }
  .toast-slide-enter-from,
  .toast-slide-leave-to {
    transform: none;
  }
  .status-toast__item:hover {
    transform: none;
  }
}
</style>
