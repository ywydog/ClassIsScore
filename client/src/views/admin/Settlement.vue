<template>
  <div class="settlement">
    <div class="settlement__header">
      <h2>结算</h2>
      <el-button type="primary" @click="handleSettle" :disabled="studentStore.studentCount === 0">
        <el-icon><Finished /></el-icon>
        执行结算
      </el-button>
    </div>

    <el-alert
      title="结算将重置所有学生积分并生成备份记录"
      type="warning"
      :closable="false"
      show-icon
      style="margin-bottom: 20px"
    />

    <div class="settlement__list">
      <el-card v-for="record in settlements" :key="record.id" class="settlement__card">
        <div class="settlement__card-content">
          <div class="settlement__card-left">
            <div class="settlement__card-time">
              <el-icon><Clock /></el-icon>
              {{ formatTime(record.settledAt) }}
            </div>
            <div class="settlement__card-stats">
              <span>参与学生: <strong>{{ record.studentCount }}</strong> 人</span>
              <span>总积分: <strong>{{ record.totalScore }}</strong></span>
            </div>
          </div>
          <div class="settlement__card-right">
            <el-tag v-if="record.isReverted" type="info" size="small">已撤销</el-tag>
            <template v-else>
              <el-button type="danger" size="small" @click="handleRevert(record.id)">
                撤销结算
              </el-button>
            </template>
          </div>
        </div>
      </el-card>
      <el-empty v-if="settlements.length === 0" description="暂无结算记录" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Finished, Clock } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import { useStudentStore } from '@/stores/student'
import type { SettlementRecord } from '@/types'
import api from '@/services/api'

const studentStore = useStudentStore()
const settlements = ref<SettlementRecord[]>([])

onMounted(async () => {
  await fetchSettlements()
})

async function fetchSettlements() {
  try {
    const response = await api.get<{ data: SettlementRecord[] }>('/api/settlements')
    settlements.value = response.data.data
  } catch { /* ignore */ }
}

async function handleSettle() {
  await ElMessageBox.confirm(
    '结算后所有学生积分将被重置为0，此操作不可自动恢复。确定继续？',
    '确认结算',
    { type: 'warning', confirmButtonText: '确认结算', cancelButtonText: '取消' }
  )
  try {
    await api.post('/api/settlements/settle')
    ElMessage.success('结算完成')
    await fetchSettlements()
    await studentStore.fetchStudents()
  } catch { /* ignore */ }
}

async function handleRevert(id: string) {
  await ElMessageBox.confirm('撤销结算将恢复结算前的积分数据。确定？', '确认撤销', { type: 'warning' })
  try {
    await api.post(`/api/settlements/${id}/revert`)
    ElMessage.success('已撤销结算')
    await fetchSettlements()
    await studentStore.fetchStudents()
  } catch { /* ignore */ }
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

.settlement__card-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.settlement__card-time {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 14px;
  color: var(--cis-text-primary);
  font-weight: 500;
}

.settlement__card-stats {
  display: flex;
  gap: 16px;
  font-size: 13px;
  color: var(--cis-text-secondary);
}
</style>
