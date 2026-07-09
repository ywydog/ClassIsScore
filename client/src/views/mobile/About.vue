<template>
  <div class="m-about">
    <header class="m-about__head">
      <span class="cis-eyebrow">About</span>
      <h1 class="cis-display m-about__title">关于</h1>
    </header>
    <MobileEmptyState eyebrow="Info" description="ClassIsScore 移动版" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-about { display: flex; flex-direction: column; gap: 16px; }
.m-about__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
