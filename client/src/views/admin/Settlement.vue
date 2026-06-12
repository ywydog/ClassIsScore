<template>
  <div class="settlement">
    <div class="settlement__header">
      <h2>结算</h2>
      <el-button type="primary" @click="handleSettle">
        <el-icon><Finished /></el-icon>
        执行结算
      </el-button>
    </div>

    <div class="settlement__list">
      <el-card v-for="record in settlements" :key="record.id" class="settlement__card">
        <div class="settlement__card-content">
          <div class="settlement__card-info">
            <span class="settlement__card-time">{{ formatTime(record.settledAt) }}</span>
            <span class="settlement__card-count">参与学生: {{ record.studentCount }} 人</span>
            <span class="settlement__card-total">总积分: {{ record.totalScore }}</span>
          </div>
          <div class="settlement__card-actions">
            <el-tag v-if="record.isReverted" type="info">已撤销</el-tag>
            <el-button v-else type="danger" size="small" text @click="handleRevert(record.id)">
              撤销
            </el-button>
          </div>
        </div>
      </el-card>
      <el-empty v-if="settlements.length === 0" description="暂无结算记录" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Finished } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import type { SettlementRecord } from '@/types'
import api from '@/services/api'

const settlements = ref<SettlementRecord[]>([])

onMounted(async () => {
  await fetchSettlements()
})

async function fetchSettlements() {
  const response = await api.get<{ data: SettlementRecord[] }>('/api/settlements')
  settlements.value = response.data.data
}

async function handleSettle() {
  await ElMessageBox.confirm('确定执行结算？结算后积分将被重置。', '确认结算', { type: 'warning' })
  await api.post('/api/settlements/settle')
  ElMessage.success('结算完成')
  await fetchSettlements()
}

async function handleRevert(id: string) {
  await ElMessageBox.confirm('确定撤销该结算？', '确认撤销', { type: 'warning' })
  await api.post(`/api/settlements/${id}/revert`)
  ElMessage.success('已撤销')
  await fetchSettlements()
}

function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>

<style scoped>
.settlement__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.settlement__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

.settlement__list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.settlement__card-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.settlement__card-info {
  display: flex;
  gap: 24px;
  font-size: 14px;
  color: var(--cis-text-secondary);
}
</style>
