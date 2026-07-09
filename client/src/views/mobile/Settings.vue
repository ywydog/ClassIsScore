<template>
  <div class="m-settings">
    <header class="m-settings__head">
      <span class="cis-eyebrow">Settings</span>
      <h1 class="cis-display m-settings__title">设置</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无设置项" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-settings { display: flex; flex-direction: column; gap: 16px; }
.m-settings__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
