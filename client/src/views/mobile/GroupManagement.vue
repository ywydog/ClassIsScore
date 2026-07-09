<template>
  <div class="m-groups">
    <header class="m-groups__head">
      <span class="cis-eyebrow">Groups</span>
      <h1 class="cis-display m-groups__title">分组</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无分组" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-groups { display: flex; flex-direction: column; gap: 16px; }
.m-groups__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
