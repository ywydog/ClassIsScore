<template>
  <component
    :is="to ? 'router-link' : 'div'"
    :to="to"
    class="list-item"
    :class="{ 'list-item--clickable': clickable }"
    :aria-label="ariaLabel || title"
  >
    <div v-if="avatar || $slots.avatar" class="list-item__avatar">
      <slot name="avatar">
        <span class="list-item__avatar-text" aria-hidden="true">{{ avatar }}</span>
      </slot>
    </div>
    <div class="list-item__body">
      <span class="list-item__title">{{ title }}</span>
      <span v-if="subtitle" class="list-item__subtitle">{{ subtitle }}</span>
    </div>
    <div v-if="$slots.trailing || trailing || chevron" class="list-item__trailing">
      <slot name="trailing">
        <span v-if="trailing" class="list-item__trailing-text cis-num">{{ trailing }}</span>
        <el-icon v-if="chevron" :size="16" aria-hidden="true"><ArrowRight /></el-icon>
      </slot>
    </div>
  </component>
</template>

<script setup lang="ts">
import { ArrowRight } from '@element-plus/icons-vue'

withDefaults(defineProps<{
  to?: string | object
  title: string
  subtitle?: string
  avatar?: string
  trailing?: string
  chevron?: boolean
  clickable?: boolean
  ariaLabel?: string
}>(), {
  chevron: false,
  clickable: true,
})
</script>

<style scoped>
.list-item {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 56px;
  padding: 10px 16px;
  border-bottom: 1px solid var(--cis-border-light);
  background: var(--cis-surface-1);
  color: var(--cis-text-primary);
  text-decoration: none;
  font-family: inherit;
  transition: background-color var(--cis-transition-fast);
  -webkit-tap-highlight-color: transparent;
}

.list-item--clickable:active {
  background: var(--cis-primary-tint);
}

.list-item__avatar {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--cis-border);
  background: var(--cis-surface-2);
  border-radius: 9999px;
  flex-shrink: 0;
}

.list-item__avatar-text {
  font-family: var(--cis-font-serif);
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
  line-height: 1;
}

.list-item__body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.list-item__title {
  font-size: 15px;
  font-weight: 500;
  color: var(--cis-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.list-item__subtitle {
  font-size: 12px;
  color: var(--cis-text-tertiary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.list-item__trailing {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.list-item__trailing-text {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-secondary);
  font-variant-numeric: tabular-nums;
}
</style>
