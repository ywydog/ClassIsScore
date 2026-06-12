<template>
  <router-view />
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useAppStore } from '@/stores/app'

const appStore = useAppStore()

onMounted(async () => {
  await appStore.initialize()
})

onUnmounted(() => {
  appStore.cleanup()
})
</script>

<style>
html,
body,
#app {
  margin: 0;
  padding: 0;
  height: 100%;
  width: 100%;
  font-family: var(--cis-font-family);
  background-color: var(--cis-bg);
  color: var(--cis-text-primary);
}
</style>
