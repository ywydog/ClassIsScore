<template>
  <transition-group name="toast-slide" tag="div" class="status-toast">
    <div
      v-for="toast in toasts"
      :key="toast.id"
      :class="['status-toast__item', `status-toast__item--${toast.type}`]"
      @click="removeToast(toast.id)"
    >
      <div class="status-toast__icon">
        <el-icon v-if="toast.type === 'success'"><SuccessFilled /></el-icon>
        <el-icon v-else-if="toast.type === 'error'"><CircleCloseFilled /></el-icon>
        <el-icon v-else-if="toast.type === 'warning'"><WarningFilled /></el-icon>
        <el-icon v-else><InfoFilled /></el-icon>
      </div>
      <span class="status-toast__message">{{ toast.message }}</span>
    </div>
  </transition-group>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useAppStore } from '@/stores/app'
import { SuccessFilled, CircleCloseFilled, WarningFilled, InfoFilled } from '@element-plus/icons-vue'

const appStore = useAppStore()
const { toasts } = storeToRefs(appStore)
const { removeToast } = appStore
</script>

<style scoped>
.status-toast {
  position: fixed;
  top: 60px;
  right: 20px;
  z-index: var(--cis-z-toast, 500);
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.status-toast__item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 18px;
  border-radius: var(--cis-radius-lg);
  font-size: 13px;
  cursor: pointer;
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border: 1px solid transparent;
  box-shadow: var(--cis-shadow-lg);
  transition: all var(--cis-transition-fast);
  min-width: 200px;
}

.status-toast__item:hover {
  transform: translateX(-4px);
}

.status-toast__icon {
  display: flex;
  align-items: center;
  font-size: 16px;
  flex-shrink: 0;
}

.status-toast__message {
  font-weight: 500;
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
  transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.toast-slide-leave-active {
  transition: all 0.2s ease;
}

.toast-slide-enter-from {
  opacity: 0;
  transform: translateX(40px) scale(0.95);
}

.toast-slide-leave-to {
  opacity: 0;
  transform: translateX(40px) scale(0.95);
}
</style>
