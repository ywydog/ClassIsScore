<template>
  <div class="m-plugins">
    <header class="m-plugins__head">
      <span class="cis-eyebrow">Plugins</span>
      <h1 class="cis-display m-plugins__title">插件</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无插件" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-plugins { display: flex; flex-direction: column; gap: 16px; }
.m-plugins__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
