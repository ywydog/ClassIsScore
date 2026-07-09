<template>
  <div class="empty-state" role="status" aria-live="polite">
    <span v-if="eyebrow" class="cis-eyebrow empty-state__eyebrow">{{ eyebrow }}</span>
    <p class="empty-state__text">{{ description }}</p>
    <slot name="action"></slot>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  eyebrow?: string
  description: string
}>()
</script>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 48px 16px;
  border: 1px dashed var(--cis-border-strong);
  border-radius: var(--cis-radius-card);
  background: var(--cis-surface-1);
  text-align: center;
}

.empty-state__eyebrow {
  color: var(--cis-text-tertiary);
}

.empty-state__text {
  margin: 0;
  color: var(--cis-text-tertiary);
  font-size: 14px;
  line-height: 1.5;
}
</style>
