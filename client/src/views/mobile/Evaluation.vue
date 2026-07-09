<template>
  <div class="m-eval">
    <header class="m-eval__head">
      <div class="m-eval__head-text">
        <span class="cis-eyebrow">Evaluation</span>
        <h1 class="cis-display m-eval__title">评价项</h1>
      </div>
      <button
        type="button"
        class="m-eval__add-btn"
        aria-label="添加评价项"
        @click="openCreateDialog"
      >
        <el-icon :size="16" aria-hidden="true"><Plus /></el-icon>
        <span>添加</span>
      </button>
    </header>

    <ul v-if="items.length > 0" class="m-eval__list" role="list">
      <li v-for="item in items" :key="item.id" class="m-eval__row">
        <div class="m-eval__row-main">
          <span
            class="m-eval__dot"
            :class="item.isPositive ? 'is-plus' : 'is-minus'"
            aria-hidden="true"
          />
          <div class="m-eval__row-body">
            <span class="m-eval__row-name">{{ item.name }}</span>
            <span class="m-eval__row-meta cis-num">
              {{ item.isPositive ? '+' : '' }}{{ item.scoreChange }} 分
            </span>
          </div>
          <div class="m-eval__row-actions">
            <button
              type="button"
              class="m-eval__row-btn"
              aria-label="编辑评价项"
              @click.stop="openEditDialog(item)"
            >
              <el-icon :size="14" aria-hidden="true"><Edit /></el-icon>
            </button>
            <button
              type="button"
              class="m-eval__row-btn is-danger"
              aria-label="删除评价项"
              @click.stop="handleDelete(item)"
            >
              <el-icon :size="14" aria-hidden="true"><Delete /></el-icon>
            </button>
          </div>
        </div>
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无评价项" />

    <el-dialog
      v-model="formOpen"
      :title="editingItem ? '编辑评价项' : '新建评价项'"
      width="320px"
      :close-on-click-modal="false"
      destroy-on-close
    >
      <el-form label-position="top" @submit.prevent="handleSubmit">
        <el-form-item label="名称" required>
          <el-input
            v-model="form.name"
            placeholder="例：积极发言"
            maxlength="20"
            show-word-limit
            autocomplete="off"
            aria-label="名称"
          />
        </el-form-item>
        <el-form-item label="分值" required>
          <el-input-number
            v-model="form.scoreChange"
            :step="1"
            :min="0"
            :max="100"
            controls-position="right"
            class="m-eval__form-number"
            inputmode="numeric"
            aria-label="分值"
          />
        </el-form-item>
        <el-form-item label="类型">
          <el-radio-group v-model="form.isPositive" aria-label="评价项类型">
            <el-radio-button :value="true">加分</el-radio-button>
            <el-radio-button :value="false">减分</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="m-eval__dialog-actions">
          <el-button @click="formOpen = false">取消</el-button>
          <el-button
            type="primary"
            :disabled="!form.name.trim() || form.scoreChange <= 0"
            @click="handleSubmit"
          >确定</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus, Edit, Delete } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { invoke } from '@/services/tauri'
import type { EvaluationItem } from '@/types'

const items = ref<EvaluationItem[]>([])
const formOpen = ref(false)
const editingItem = ref<EvaluationItem | null>(null)
const form = reactive({ name: '', scoreChange: 1, isPositive: true })

onMounted(async () => {
  await fetchItems()
})

async function fetchItems() {
  try {
    items.value = await invoke<EvaluationItem[]>('evaluation_list', {})
  } catch { items.value = [] }
}

function openCreateDialog() {
  editingItem.value = null
  form.name = ''
  form.scoreChange = 1
  form.isPositive = true
  formOpen.value = true
}

function openEditDialog(item: EvaluationItem) {
  editingItem.value = item
  form.name = item.name
  form.scoreChange = Math.abs(item.scoreChange)
  form.isPositive = item.isPositive ?? true
  formOpen.value = true
}

async function handleSubmit() {
  const name = form.name.trim()
  if (!name) {
    ElMessage.warning('请输入评价项名称')
    return
  }
  if (form.scoreChange <= 0) {
    ElMessage.warning('分值必须大于 0')
    return
  }
  const payload = {
    name,
    scoreChange: form.isPositive ? form.scoreChange : -form.scoreChange,
    isPositive: form.isPositive,
  }
  try {
    if (editingItem.value) {
      await invoke('evaluation_update', { id: Number(editingItem.value.id), ...payload })
      ElMessage.success('已更新')
    } else {
      await invoke('evaluation_create', payload)
      ElMessage.success('已添加')
    }
    formOpen.value = false
    await fetchItems()
  } catch { /* ignore */ }
}

async function handleDelete(item: EvaluationItem) {
  try {
    await ElMessageBox.confirm(`确定删除评价项"${item.name}"吗？`, '确认删除', { type: 'warning' })
  } catch {
    return
  }
  try {
    await invoke('evaluation_delete', { id: Number(item.id) })
    ElMessage.success('已删除')
    await fetchItems()
  } catch { /* ignore */ }
}
</script>

<style scoped>
.m-eval { display: flex; flex-direction: column; gap: 16px; padding-bottom: 96px; }
.m-eval__head { display: flex; align-items: flex-end; justify-content: space-between; gap: 12px; }
.m-eval__head-text { display: flex; flex-direction: column; gap: 2px; }
.m-eval__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-eval__add-btn { display: inline-flex; align-items: center; gap: 4px; height: 36px; padding: 0 12px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); font-size: 13px; font-weight: 500; font-family: inherit; cursor: pointer; -webkit-tap-highlight-color: transparent; flex-shrink: 0; }
.m-eval__add-btn:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-eval__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-eval__row { background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-eval__list li:last-child .m-eval__row { border-bottom: none; }
.m-eval__row-main { display: flex; align-items: center; gap: 12px; min-height: 56px; padding: 8px 12px 8px 16px; }
.m-eval__dot { width: 6px; height: 24px; border-radius: 2px; flex-shrink: 0; }
.m-eval__dot.is-plus { background: var(--cis-success); }
.m-eval__dot.is-minus { background: var(--cis-accent); }
.m-eval__row-body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.m-eval__row-name { font-size: 14px; font-weight: 500; }
.m-eval__row-meta { font-size: 12px; color: var(--cis-text-tertiary); font-variant-numeric: tabular-nums; }
.m-eval__row-actions { display: flex; align-items: center; gap: 4px; flex-shrink: 0; }
.m-eval__row-btn { display: inline-flex; align-items: center; justify-content: center; width: 32px; height: 32px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; }
.m-eval__row-btn:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-eval__row-btn.is-danger { color: var(--cis-accent); border-color: rgba(185, 28, 28, 0.3); }
.m-eval__row-btn.is-danger:active { background: var(--cis-accent-tint); }

.m-eval__dialog-actions { display: flex; gap: 8px; justify-content: flex-end; width: 100%; }
.m-eval__form-number { width: 100%; }
</style>
