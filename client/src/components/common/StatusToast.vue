<template>
  <div class="status-toast" :class="`status-toast--${type}`" role="status">
    <span class="status-toast__bar" aria-hidden="true"></span>
    <span class="status-toast__dot" aria-hidden="true"></span>
    <span class="status-toast__msg">
      <slot>{{ message }}</slot>
    </span>
  </div>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'success' | 'warning' | 'error' | 'info'
    message?: string
  }>(),
  { type: 'info', message: '' }
)
</script>

<style scoped>
.status-toast {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  min-width: 240px;
  max-width: 480px;
  padding: 10px 14px 10px 0;
  background: var(--cis-surface-1);
  border: 1px solid var(--cis-border-strong);
  border-radius: var(--cis-radius-btn);
  box-shadow: var(--cis-shadow-dropdown);
  font-size: 13px;
  font-weight: 500;
  color: var(--cis-text-primary);
  position: relative;
  overflow: hidden;
}
.status-toast__bar {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: var(--cis-primary);
}
.status-toast--success .status-toast__bar { background: var(--cis-success); }
.status-toast--warning .status-toast__bar { background: var(--cis-warning); }
.status-toast--error .status-toast__bar { background: var(--cis-accent); }
.status-toast--info .status-toast__bar { background: var(--cis-primary); }
.status-toast__dot {
  margin-left: 16px;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--cis-primary);
  flex-shrink: 0;
}
.status-toast--success .status-toast__dot { background: var(--cis-success); }
.status-toast--warning .status-toast__dot { background: var(--cis-warning); }
.status-toast--error .status-toast__dot { background: var(--cis-accent); }
.status-toast__msg {
  flex: 1;
  letter-spacing: 0;
}
</style>
