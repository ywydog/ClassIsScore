<template>
  <div class="m-eval">
    <header class="m-eval__head">
      <span class="cis-eyebrow">Evaluation</span>
      <h1 class="cis-display m-eval__title">评估项</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无评估项" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-eval { display: flex; flex-direction: column; gap: 16px; }
.m-eval__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
