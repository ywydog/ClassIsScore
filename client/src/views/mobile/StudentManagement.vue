<template>
  <div class="m-students">
    <header class="m-students__head">
      <span class="cis-eyebrow">Students</span>
      <h1 class="cis-display m-students__title">学生</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无学生" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-students { display: flex; flex-direction: column; gap: 16px; }
.m-students__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
