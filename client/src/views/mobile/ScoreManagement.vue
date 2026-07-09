<template>
  <div class="m-score">
    <header class="m-score__head">
      <span class="cis-eyebrow">Score</span>
      <h1 class="cis-display m-score__title">积分</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无积分表单" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-score { display: flex; flex-direction: column; gap: 16px; }
.m-score__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
