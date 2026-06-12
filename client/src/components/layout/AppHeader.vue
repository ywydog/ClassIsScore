<template>
  <div class="app-header">
    <el-button
      :icon="Fold"
      text
      class="app-header__toggle"
      @click="$emit('toggle-sidebar')"
    />
    <span class="app-header__title">ClassIsScore</span>
    <div class="app-header__actions">
      <ThemeToggle />
    </div>
  </div>
</template>

<script setup lang="ts">
import { Fold } from '@element-plus/icons-vue'
import ThemeToggle from '@/components/common/ThemeToggle.vue'

defineEmits<{
  'toggle-sidebar': []
}>()
</script>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  width: 100%;
  height: 100%;
}

.app-header__toggle {
  -webkit-app-region: no-drag;
  cursor: pointer;
}

.app-header__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
  margin-left: 8px;
}

.app-header__actions {
  margin-left: auto;
  -webkit-app-region: no-drag;
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
