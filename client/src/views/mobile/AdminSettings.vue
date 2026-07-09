<template>
  <div class="m-admin">
    <header class="m-admin__head">
      <span class="cis-eyebrow">Admin</span>
      <h1 class="cis-display m-admin__title">管理员</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无管理员设置" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-admin { display: flex; flex-direction: column; gap: 16px; }
.m-admin__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
