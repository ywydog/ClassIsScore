<template>
  <Teleport to="body">
    <Transition name="sheet-fade">
      <div
        v-if="modelValue"
        class="bottom-sheet__backdrop"
        :aria-hidden="!modelValue"
        @click="onBackdropClick"
      ></div>
    </Transition>
    <Transition :name="`sheet-slide-${placement}`">
      <div
        v-if="modelValue"
        class="bottom-sheet"
        :class="[`bottom-sheet--${placement}`, `bottom-sheet--${height}`]"
        role="dialog"
        aria-modal="true"
        :aria-label="title"
        @touchstart.passive="onTouchStart"
        @touchmove.passive="onTouchMove"
        @touchend.passive="onTouchEnd"
      >
        <div v-if="dismissible" class="bottom-sheet__handle" aria-hidden="true"></div>
        <header v-if="title || $slots.header" class="bottom-sheet__header">
          <slot name="header">
            <h2 class="bottom-sheet__title">{{ title }}</h2>
            <button
              v-if="dismissible"
              type="button"
              class="bottom-sheet__close"
              :aria-label="`关闭 ${title || '面板'}`"
              @click="close"
            >
              <el-icon :size="18" aria-hidden="true"><Close /></el-icon>
            </button>
          </slot>
        </header>
        <div class="bottom-sheet__body">
          <slot></slot>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { Close } from '@element-plus/icons-vue'

const props = withDefaults(defineProps<{
  modelValue: boolean
  title?: string
  placement?: 'bottom' | 'top'
  height?: 'auto' | 'half' | 'full'
  closeOnBackdrop?: boolean
  dismissible?: boolean
}>(), {
  title: '',
  placement: 'bottom',
  height: 'auto',
  closeOnBackdrop: true,
  dismissible: true,
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'close': []
}>()

function close() {
  emit('update:modelValue', false)
  emit('close')
}

function onBackdropClick() {
  if (props.closeOnBackdrop) close()
}

// 触屏下滑关闭
const touchStartY = ref(0)
const touchDeltaY = ref(0)

function onTouchStart(e: TouchEvent) {
  touchStartY.value = e.touches[0].clientY
  touchDeltaY.value = 0
}

function onTouchMove(e: TouchEvent) {
  touchDeltaY.value = e.touches[0].clientY - touchStartY.value
}

function onTouchEnd() {
  if (props.placement === 'bottom' && touchDeltaY.value > 80) {
    close()
  }
  touchDeltaY.value = 0
}

watch(() => props.modelValue, (open) => {
  if (typeof document === 'undefined') return
  document.body.style.overflow = open ? 'hidden' : ''
})
</script>

<style scoped>
.bottom-sheet__backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
  z-index: 1999;
}

.bottom-sheet {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 2000;
  background: var(--cis-surface-1);
  border-top: 1px solid var(--cis-border);
  border-radius: 12px 12px 0 0;
  max-height: 92vh;
  display: flex;
  flex-direction: column;
  padding-bottom: env(safe-area-inset-bottom);
  box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.08);
}

.bottom-sheet--half {
  height: 50vh;
}

.bottom-sheet--full {
  height: 92vh;
}

.bottom-sheet--top {
  top: 0;
  bottom: auto;
  border-top: none;
  border-bottom: 1px solid var(--cis-border);
  border-radius: 0 0 12px 12px;
}

.bottom-sheet__handle {
  width: 32px;
  height: 4px;
  background: var(--cis-border-strong);
  border-radius: 2px;
  margin: 8px auto 0;
  flex-shrink: 0;
}

.bottom-sheet__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 12px 20px 8px;
  border-bottom: 1px solid var(--cis-border-light);
  flex-shrink: 0;
}

.bottom-sheet__title {
  font-family: var(--cis-font-serif);
  font-size: 17px;
  font-weight: 600;
  color: var(--cis-text-display);
  margin: 0;
  letter-spacing: var(--cis-letter-spacing-display);
}

.bottom-sheet__close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: none;
  border-radius: var(--cis-radius-btn);
  background: transparent;
  color: var(--cis-text-tertiary);
  cursor: pointer;
  font-family: inherit;
  transition: background-color var(--cis-transition-fast), color var(--cis-transition-fast);
}

.bottom-sheet__close:active {
  background: var(--cis-primary-tint);
  color: var(--cis-primary);
}

.bottom-sheet__body {
  flex: 1;
  overflow-y: auto;
  padding: 16px 20px 24px;
  -webkit-overflow-scrolling: touch;
}

.sheet-fade-enter-active,
.sheet-fade-leave-active {
  transition: opacity 0.2s ease;
}
.sheet-fade-enter-from,
.sheet-fade-leave-to {
  opacity: 0;
}

.sheet-slide-bottom-enter-active,
.sheet-slide-bottom-leave-active {
  transition: transform 0.25s cubic-bezier(0.32, 0.72, 0, 1);
}
.sheet-slide-bottom-enter-from,
.sheet-slide-bottom-leave-to {
  transform: translateY(100%);
}

.sheet-slide-top-enter-active,
.sheet-slide-top-leave-active {
  transition: transform 0.25s cubic-bezier(0.32, 0.72, 0, 1);
}
.sheet-slide-top-enter-from,
.sheet-slide-top-leave-to {
  transform: translateY(-100%);
}

@media (prefers-reduced-motion: reduce) {
  .sheet-fade-enter-active,
  .sheet-fade-leave-active,
  .sheet-slide-bottom-enter-active,
  .sheet-slide-bottom-leave-active,
  .sheet-slide-top-enter-active,
  .sheet-slide-top-leave-active {
    transition: none;
  }
}
</style>
