<template>
  <div class="m-themes">
    <header class="m-themes__head">
      <span class="cis-eyebrow">Themes</span>
      <h1 class="cis-display m-themes__title">主题</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无主题" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-themes { display: flex; flex-direction: column; gap: 16px; }
.m-themes__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
