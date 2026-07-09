<template>
  <div class="m-eval">
    <header class="m-eval__head">
      <span class="cis-eyebrow">Evaluation</span>
      <h1 class="cis-display m-eval__title">评估项</h1>
    </header>
    <p class="m-eval__hint">评估项管理请前往桌面端</p>
    <ul v-if="items.length > 0" class="m-eval__list" role="list">
      <li v-for="item in items" :key="item.id" class="m-eval__row" :class="item.isPositive ? 'is-plus' : 'is-minus'">
        <span class="m-eval__name">{{ item.name }}</span>
        <span class="m-eval__value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
      </li>
    </ul>
    <p v-if="items.length === 0" class="m-eval__empty">暂无评估项</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { invoke } from '@/services/tauri'
import type { EvaluationItem } from '@/types'

const items = ref<EvaluationItem[]>([])

onMounted(async () => {
  try { items.value = await invoke<EvaluationItem[]>('evaluation_list', {}) }
  catch { items.value = [] }
})
</script>

<style scoped>
.m-eval { display: flex; flex-direction: column; gap: 12px; }
.m-eval__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-eval__hint { margin: 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-eval__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-eval__row { display: flex; align-items: center; gap: 12px; min-height: 52px; padding: 8px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-eval__list li:last-child .m-eval__row { border-bottom: none; }
.m-eval__name { flex: 1; font-size: 15px; font-weight: 500; }
.m-eval__value { font-family: var(--cis-font-mono); font-size: 15px; font-weight: 700; font-variant-numeric: tabular-nums; }
.m-eval__row.is-plus .m-eval__value { color: var(--cis-success); }
.m-eval__row.is-minus .m-eval__value { color: var(--cis-accent); }
.m-eval__empty { text-align: center; color: var(--cis-text-tertiary); padding: 24px 0; }
</style>
