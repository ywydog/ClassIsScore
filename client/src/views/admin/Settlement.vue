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

    <!-- 导出区域 -->
    <div class="settlement__export">
      <div class="settlement__export-controls">
        <el-radio-group v-model="exportFormat" size="small">
          <el-radio-button value="daily">日度</el-radio-button>
          <el-radio-button value="weekly">周度</el-radio-button>
          <el-radio-button value="monthly">月度</el-radio-button>
        </el-radio-group>
        <el-date-picker
          v-model="exportDate"
          :type="exportDatePickerType"
          :placeholder="exportDatePlaceholder"
          size="small"
          :value-format="exportDateFormat"
          style="width: 180px"
        />
        <el-button size="small" type="primary" :icon="Download" @click="handleExport">
          导出结算数据
        </el-button>
      </div>
    </div>

    <div class="settlement__list">
      <el-card v-for="record in settlements" :key="record.id" class="settlement__card">
        <div class="settlement__card-content">
          <div class="settlement__card-left">
            <div class="settlement__card-time">
              <el-icon><Clock /></el-icon>
              {{ formatTime(record.settledAt || record.createdAt || '') }}
            </div>
            <div class="settlement__card-stats">
              <span>参与学生: <strong>{{ record.studentCount }}</strong> 人</span>
              <span>总积分: <strong>{{ record.totalScore }}</strong></span>
            </div>
          </div>
          <div class="settlement__card-right">
            <el-tag v-if="record.isReverted || record.status === 2" type="info" size="small">已撤销</el-tag>
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
import { ref, computed, onMounted } from 'vue'
import { Finished, Clock, Download } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import { useStudentStore } from '@/stores/student'
import type { SettlementRecord } from '@/types'
import { settlementApi } from '@/services/settlement'
import { exportToExcel, type ExcelColumn } from '@/utils/excelHelper'

const studentStore = useStudentStore()
const settlements = ref<SettlementRecord[]>([])

const exportFormat = ref<'daily' | 'weekly' | 'monthly'>('daily')
const exportDate = ref<string>('')

const exportDatePickerType = computed(() => {
  switch (exportFormat.value) {
    case 'daily': return 'date'
    case 'weekly': return 'week'
    case 'monthly': return 'month'
  }
})

const exportDatePlaceholder = computed(() => {
  switch (exportFormat.value) {
    case 'daily': return '选择日期'
    case 'weekly': return '选择周'
    case 'monthly': return '选择月份'
  }
})

const exportDateFormat = computed(() => {
  switch (exportFormat.value) {
    case 'daily': return 'YYYY-MM-DD'
    case 'weekly': return 'YYYY-MM-DD'
    case 'monthly': return 'YYYY-MM'
  }
})

onMounted(async () => {
  await fetchSettlements()
})

async function fetchSettlements() {
  try {
    const response = await settlementApi.getAll()
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
    await settlementApi.create({
      name: '积分结算',
      period: new Date().toISOString().slice(0, 10),
    })
    ElMessage.success('结算完成')
    await fetchSettlements()
    await studentStore.fetchStudents()
  } catch { /* ignore */ }
}

async function handleRevert(id: string) {
  await ElMessageBox.confirm('撤销结算将恢复结算前的积分数据。确定？', '确认撤销', { type: 'warning' })
  try {
    await settlementApi.revert(String(id))
    ElMessage.success('已撤销结算')
    await fetchSettlements()
    await studentStore.fetchStudents()
  } catch { /* ignore */ }
}

function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString('zh-CN')
}

function getDateRange(): { start: Date; end: Date } | null {
  if (!exportDate.value) return null

  const dateStr = exportDate.value

  switch (exportFormat.value) {
    case 'daily': {
      const start = new Date(dateStr + 'T00:00:00')
      const end = new Date(dateStr + 'T23:59:59')
      return { start, end }
    }
    case 'weekly': {
      // el-date-picker week 模式返回的是该周的某一天
      const baseDate = new Date(dateStr + 'T00:00:00')
      const day = baseDate.getDay() || 7
      const start = new Date(baseDate)
      start.setDate(baseDate.getDate() - day + 1)
      const end = new Date(start)
      end.setDate(start.getDate() + 6)
      end.setHours(23, 59, 59)
      return { start, end }
    }
    case 'monthly': {
      const [year, month] = dateStr.split('-').map(Number)
      const start = new Date(year, month - 1, 1, 0, 0, 0)
      const end = new Date(year, month, 0, 23, 59, 59)
      return { start, end }
    }
  }
}

function handleExport() {
  if (!exportDate.value) {
    ElMessage.warning('请先选择日期')
    return
  }

  const range = getDateRange()
  if (!range) return

  // 从结算记录中筛选匹配日期的记录
  const matchedRecords = settlements.value.filter(record => {
    const recordDate = new Date(record.settledAt || record.createdAt || '')
    return recordDate >= range.start && recordDate <= range.end
  })

  if (matchedRecords.length === 0) {
    ElMessage.warning('所选日期范围内无结算记录')
    return
  }

  // 解析所有匹配记录的 snapshotData 并汇总导出
  const allRows: Record<string, string | number | null>[] = []

  for (const record of matchedRecords) {
    let snapshot: any[] = []
    if (record.snapshotData) {
      try {
        snapshot = JSON.parse(record.snapshotData)
      } catch {
        // 解析失败则跳过
      }
    }

    if (snapshot.length > 0) {
      for (const item of snapshot) {
        allRows.push({
          settlementTime: formatTime(record.settledAt || record.createdAt || ''),
          studentNumber: item.studentNumber ?? '',
          name: item.name ?? '',
          totalScore: item.totalScore ?? 0,
          groupId: item.groupId ?? '',
        })
      }
    } else {
      // 无快照数据时，导出结算摘要
      allRows.push({
        settlementTime: formatTime(record.settledAt || record.createdAt || ''),
        studentNumber: '',
        name: record.name ?? '结算记录',
        totalScore: record.totalScore ?? 0,
        groupId: '',
      })
    }
  }

  const columns: ExcelColumn[] = [
    { header: '结算时间', key: 'settlementTime' },
    { header: '学号', key: 'studentNumber' },
    { header: '姓名', key: 'name' },
    { header: '积分', key: 'totalScore' },
    { header: '小组ID', key: 'groupId' },
  ]

  const formatLabel = { daily: '日度', weekly: '周度', monthly: '月度' }[exportFormat.value]
  const filename = `结算数据_${formatLabel}_${exportDate.value}`

  exportToExcel(allRows, columns, filename)
  ElMessage.success('导出成功')
}
</script>

<style scoped>
.settlement__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.settlement__header h2 {
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

.settlement__export {
  margin-bottom: 20px;
  padding: 16px;
  background-color: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  border: 1px solid var(--cis-border-color-light);
  box-shadow: var(--cis-shadow-card);
}

.settlement__export-controls {
  display: flex;
  align-items: center;
  gap: 12px;
}

.settlement__list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.settlement__card {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.settlement__card:hover {
  box-shadow: var(--cis-shadow-card-hover);
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

.settlement__card-stats strong {
  color: var(--cis-primary);
  font-weight: 700;
}
</style>
