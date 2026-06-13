<template>
  <div class="auto-evaluation">
    <div class="auto-evaluation__header">
      <h2>自动评估</h2>
    </div>

    <div class="auto-evaluation__content">
      <el-card class="auto-evaluation__config-card">
        <template #header>
          <div class="card-header">
            <span>评估配置</span>
            <el-switch v-model="config.enabled" active-text="启用" inactive-text="停用" />
          </div>
        </template>
        <el-form label-width="100px">
          <el-form-item label="评估周期">
            <el-select v-model="config.period">
              <el-option label="每日" value="daily" />
              <el-option label="每周" value="weekly" />
              <el-option label="每月" value="monthly" />
            </el-select>
          </el-form-item>
        </el-form>
      </el-card>

      <el-card class="auto-evaluation__items-card">
        <template #header>
          <div class="card-header">
            <span>评估项目</span>
            <el-button type="primary" size="small" @click="openItemDialog()">
              <el-icon><Plus /></el-icon>
              添加项目
            </el-button>
          </div>
        </template>
        <el-table :data="evaluationItems" stripe empty-text="暂无评估项目">
          <el-table-column prop="name" label="名称" />
          <el-table-column prop="scoreChange" label="积分变动" width="120">
            <template #default="{ row }">
              <span :class="row.isPositive ? 'score-positive' : 'score-negative'">
                {{ row.isPositive ? '+' : '' }}{{ row.scoreChange }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="类型" width="100">
            <template #default="{ row }">
              <el-tag :type="row.isPositive ? 'success' : 'danger'" size="small">
                {{ row.isPositive ? '加分' : '扣分' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="140">
            <template #default="{ row }">
              <el-button type="primary" size="small" text @click="openItemDialog(row)">编辑</el-button>
              <el-button type="danger" size="small" text @click="handleDeleteItem(row.id)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </div>

    <el-dialog v-model="showItemDialog" :title="editingItem ? '编辑评估项' : '添加评估项'" width="420px">
      <el-form :model="itemForm" label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="itemForm.name" placeholder="如：回答问题" />
        </el-form-item>
        <el-form-item label="积分变动" required>
          <el-input-number v-model="itemForm.scoreChange" :min="-100" :max="100" />
        </el-form-item>
        <el-form-item label="类型">
          <el-radio-group v-model="itemForm.isPositive">
            <el-radio-button :value="true">加分</el-radio-button>
            <el-radio-button :value="false">扣分</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showItemDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSaveItem">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { EvaluationItem } from '@/types'
import api from '@/services/api'

const config = reactive({
  enabled: false,
  period: 'daily',
})

const evaluationItems = ref<EvaluationItem[]>([])
const showItemDialog = ref(false)
const editingItem = ref<EvaluationItem | null>(null)

const itemForm = reactive({
  name: '',
  scoreChange: 1,
  isPositive: true,
})

onMounted(async () => {
  await fetchItems()
})

async function fetchItems() {
  try {
    const response = await api.get<{ data: EvaluationItem[] }>('/api/evaluations')
    evaluationItems.value = response.data.data
  } catch {
    // use defaults
  }
}

function openItemDialog(item?: EvaluationItem) {
  editingItem.value = item || null
  if (item) {
    itemForm.name = item.name
    itemForm.scoreChange = item.scoreChange
    itemForm.isPositive = item.isPositive
  } else {
    itemForm.name = ''
    itemForm.scoreChange = 1
    itemForm.isPositive = true
  }
  showItemDialog.value = true
}

async function handleSaveItem() {
  if (!itemForm.name) {
    ElMessage.warning('请输入名称')
    return
  }
  try {
    if (editingItem.value) {
      await api.put(`/api/evaluations/${editingItem.value.id}`, itemForm)
    } else {
      await api.post('/api/evaluations', itemForm)
    }
    ElMessage.success('已保存')
    showItemDialog.value = false
    await fetchItems()
  } catch { /* ignore */ }
}

async function handleDeleteItem(id: string) {
  await ElMessageBox.confirm('确定删除该评估项？', '确认', { type: 'warning' })
  try {
    await api.delete(`/api/evaluations/${id}`)
    ElMessage.success('已删除')
    await fetchItems()
  } catch { /* ignore */ }
}
</script>

<style scoped>
.auto-evaluation__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.auto-evaluation__header h2 {
  margin: 0;
  font-family: var(--cis-font-family-display);
  font-size: 22px;
  color: var(--cis-text-primary);
  padding-left: 12px;
  border-left: 3px solid var(--cis-primary);
  background: linear-gradient(135deg, var(--cis-primary), var(--cis-primary-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.auto-evaluation__content {
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-width: 800px;
}

.auto-evaluation__config-card,
.auto-evaluation__items-card {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.auto-evaluation__config-card:hover,
.auto-evaluation__items-card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.score-positive {
  color: var(--cis-success);
  font-weight: 700;
  font-size: 15px;
}

.score-negative {
  color: var(--cis-danger);
  font-weight: 700;
  font-size: 15px;
}
</style>
