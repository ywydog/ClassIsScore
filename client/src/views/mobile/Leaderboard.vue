<template>
  <div class="m-leaderboard">
    <header class="m-leaderboard__head">
      <span class="cis-eyebrow">Leaderboard</span>
      <h1 class="cis-display m-leaderboard__title">排行榜</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无排行数据" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-leaderboard { display: flex; flex-direction: column; gap: 16px; }
.m-leaderboard__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
