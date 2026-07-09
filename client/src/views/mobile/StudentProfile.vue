<template>
  <div class="m-profile">
    <header class="m-profile__head">
      <span class="cis-eyebrow">Student</span>
      <h1 class="cis-display m-profile__title">学生详情</h1>
    </header>
    <MobileEmptyState eyebrow="Empty" description="暂无学生数据" />
  </div>
</template>

<script setup lang="ts">
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
</script>

<style scoped>
.m-profile { display: flex; flex-direction: column; gap: 16px; }
.m-profile__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
</style>
