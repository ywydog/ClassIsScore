<template>
  <div class="m-settle">
    <header class="m-settle__head">
      <span class="cis-eyebrow">Settlement</span>
      <h1 class="cis-display m-settle__title">结算</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无结算数据" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-settle { display: flex; flex-direction: column; gap: 16px; }
.m-settle__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
