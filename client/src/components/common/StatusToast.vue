<template>
  <transition-group name="toast-slide" tag="div" class="status-toast">
    <div
      v-for="toast in toasts"
      :key="toast.id"
      :class="['status-toast__item', `status-toast__item--${toast.type}`]"
      @click="removeToast(toast.id)"
    >
      <el-icon v-if="toast.type === 'success'"><SuccessFilled /></el-icon>
      <el-icon v-else-if="toast.type === 'error'"><CircleCloseFilled /></el-icon>
      <el-icon v-else-if="toast.type === 'warning'"><WarningFilled /></el-icon>
      <el-icon v-else><InfoFilled /></el-icon>
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
  top: 16px;
  right: 16px;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.status-toast__item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  border-radius: 8px;
  font-size: 14px;
  cursor: pointer;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.15);
  transition: opacity 0.3s, transform 0.3s;
}

.status-toast__item--success {
  background-color: var(--el-color-success-light-9);
  color: var(--el-color-success);
}

.status-toast__item--error {
  background-color: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
}

.status-toast__item--warning {
  background-color: var(--el-color-warning-light-9);
  color: var(--el-color-warning);
}

.status-toast__item--info {
  background-color: var(--el-color-info-light-9);
  color: var(--el-color-info);
}

.toast-slide-enter-active,
.toast-slide-leave-active {
  transition: all 0.3s ease;
}

.toast-slide-enter-from {
  opacity: 0;
  transform: translateX(30px);
}

.toast-slide-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
