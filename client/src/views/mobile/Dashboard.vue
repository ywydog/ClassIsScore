<template>
  <div class="m-dashboard">
    <header class="m-dashboard__head">
      <span class="cis-eyebrow">Dashboard</span>
      <h1 class="cis-display m-dashboard__title">总览</h1>
    </header>
    <MobileEmptyState eyebrow="Loading" description="总览面板加载中…" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-dashboard { display: flex; flex-direction: column; gap: 16px; }
.m-dashboard__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
