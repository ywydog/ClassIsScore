<template>
  <el-dialog
    :model-value="modelValue"
    title="导出积分报表"
    width="700px"
    destroy-on-close
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <!-- 步骤条 -->
    <el-steps :active="step" finish-status="success" align-center style="margin-bottom: 24px">
      <el-step title="维度与日期" />
      <el-step title="列配置" />
      <el-step title="分组排序" />
      <el-step title="预览" />
      <el-step title="导出" />
    </el-steps>

    <!-- 步骤1：维度与日期 -->
    <div v-if="step === 0">
      <el-form label-width="100px">
        <el-form-item label="时间维度" required>
          <el-radio-group v-model="config.dimension">
            <el-radio-button value="day">按日</el-radio-button>
            <el-radio-button value="week">按周</el-radio-button>
            <el-radio-button value="month">按月</el-radio-button>
            <el-radio-button value="semester">整个学期</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="日期范围" required>
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            style="width: 100%"
            @change="onDateRangeChange"
          />
        </el-form-item>
      </el-form>
    </div>

    <!-- 步骤2：列配置 -->
    <div v-if="step === 1">
      <el-form label-width="120px">
        <el-form-item label="积分显示方式" required>
          <el-radio-group v-model="config.scoreDisplayMode">
            <el-radio value="net">净加分（加分 - 减分）</el-radio>
            <el-radio value="split">加减分两列显示</el-radio>
          </el-radio>
        </el-form-item>
        <el-form-item label="总计列">
          <el-checkbox-group v-model="config.totalColumns">
            <el-checkbox value="rangeNet">日期范围内净积分</el-checkbox>
            <el-checkbox value="currentTotal">当前总积分</el-checkbox>
          </el-checkbox-group>
        </el-form-item>
        <el-form-item
          v-if="config.dimension === 'week' || config.dimension === 'semester'"
          label="周列头格式"
          required
        >
          <el-radio-group v-model="config.weekHeaderFormat">
            <el-radio value="weekNumber">第1周、第2周...</el-radio>
            <el-radio value="dateRange">3/1-3/7、3/8-3/14...</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
    </div>

    <!-- 步骤3：分组排序 -->
    <div v-if="step === 2">
      <el-form label-width="120px">
        <el-form-item label="按小组排列">
          <el-switch v-model="config.groupByGroup" />
          <span style="margin-left: 12px; font-size: 12px; color: var(--cis-text-tertiary)">
            开启后姓名按小组分组显示
          </span>
        </el-form-item>
        <el-form-item label="排序方式">
          <el-radio-group v-model="config.sortOrder">
            <el-radio value="desc">积分从高到低</el-radio>
            <el-radio value="asc">积分从低到高</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
    </div>

    <!-- 步骤4：预览 -->
    <div v-if="step === 3">
      <div v-if="preview" style="overflow-x: auto">
        <el-alert type="info" :closable="false" style="margin-bottom: 12px">
          以下为报表预览（最多显示前10行）
        </el-alert>
        <table class="preview-table">
          <thead>
            <tr>
              <th v-for="(h, i) in preview.headers" :key="i">{{ h }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, ri) in preview.rows.slice(0, 10)" :key="ri"
                :class="{ 'preview-table__group-row': isGroupRow(row) }">
              <td v-for="(cell, ci) in row" :key="ci"
                  :style="getCellStyle(cell, ci)">
                {{ formatCell(cell, ci) }}
              </td>
            </tr>
          </tbody>
        </table>
        <div v-if="preview.rows.length > 10" style="margin-top: 8px; font-size: 12px; color: var(--cis-text-tertiary)">
          共 {{ preview.rows.length }} 行，仅显示前 10 行
        </div>
      </div>
      <el-empty v-else description="无法生成预览，请检查配置" />
    </div>

    <!-- 步骤5：导出 -->
    <div v-if="step === 4">
      <el-result icon="success" title="配置完成" sub-title="选择导出格式并下载">
        <template #extra>
          <el-radio-group v-model="exportFormat" style="margin-bottom: 16px">
            <el-radio-button value="xlsx">Excel (.xlsx)</el-radio-button>
            <el-radio-button value="csv">CSV</el-radio-button>
          </el-radio-group>
          <div>
            <el-button type="primary" size="large" @click="handleExport">
              导出报表
            </el-button>
          </div>
        </template>
      </el-result>
    </div>

    <template #footer>
      <el-button v-if="step > 0" @click="step--">上一步</el-button>
      <el-button v-if="step < 4" type="primary" :disabled="!canNext" @click="nextStep">
        {{ step === 3 ? '确认' : '下一步' }}
      </el-button>
      <el-button @click="$emit('update:modelValue', false)">取消</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { Student, StudentGroup, ScoreRecord } from '@/types'
import {
  generateReport,
  exportReportToExcel,
  exportReportToCSV,
  type ReportConfig,
  type ReportPreview,
  type ReportStudent,
  type ReportScoreRecord,
} from '@/utils/excelHelper'

const props = defineProps<{
  modelValue: boolean
  students: Student[]
  groups: StudentGroup[]
  records: ScoreRecord[]
}>()

defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const step = ref(0)
const dateRange = ref<[Date, Date] | null>(null)
const exportFormat = ref<'xlsx' | 'csv'>('xlsx')

const config = reactive<ReportConfig>({
  dimension: 'week',
  startDate: new Date(),
  endDate: new Date(),
  scoreDisplayMode: 'net',
  totalColumns: ['rangeNet'],
  weekHeaderFormat: 'weekNumber',
  groupByGroup: false,
  sortOrder: 'desc',
})

// 初始化日期范围为当月
function initDateRange() {
  const now = new Date()
  const start = new Date(now.getFullYear(), now.getMonth(), 1)
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  dateRange.value = [start, end]
  config.startDate = start
  config.endDate = end
}
initDateRange()

function onDateRangeChange(val: [Date, Date] | null) {
  if (val) {
    config.startDate = val[0]
    config.endDate = val[1]
  }
}

// 学期维度自动扩展日期范围
watch(() => config.dimension, (dim) => {
  if (dim === 'semester' && dateRange.value) {
    // 扩展到包含完整月份
    const start = new Date(dateRange.value[0].getFullYear(), dateRange.value[0].getMonth(), 1)
    const end = new Date(dateRange.value[1].getFullYear(), dateRange.value[1].getMonth() + 1, 0)
    config.startDate = start
    config.endDate = end
  }
})

const canNext = computed(() => {
  if (step.value === 0) {
    return !!dateRange.value
  }
  return true
})

// 构建报表数据
const reportStudents = computed<ReportStudent[]>(() => {
  return props.students.map(s => {
    const group = s.groupId ? props.groups.find(g => g.id === s.groupId) : undefined
    return {
      id: s.id,
      name: s.name,
      groupId: s.groupId,
      groupName: group?.name,
      currentScore: s.score,
    }
  })
})

const reportRecords = computed<ReportScoreRecord[]>(() => {
  return props.records.map(r => ({
    studentId: r.studentId,
    scoreChange: r.scoreChange,
    createdAt: r.createdAt,
    isReverted: r.isReverted,
  }))
})

const preview = ref<ReportPreview | null>(null)

function buildPreview() {
  try {
    preview.value = generateReport(config, reportStudents.value, reportRecords.value)
  } catch {
    preview.value = null
  }
}

function nextStep() {
  if (step.value === 0 && !dateRange.value) {
    ElMessage.warning('请选择日期范围')
    return
  }
  if (step.value === 2) {
    buildPreview()
  }
  step.value++
}

function isGroupRow(row: (string | number)[]): boolean {
  return row.slice(1).every(v => v === '')
}

function getCellStyle(cell: string | number, colIndex: number) {
  if (colIndex === 0) return {}
  if (typeof cell === 'number') {
    if (cell > 0) return { color: 'var(--cis-success, #22c55e)' }
    if (cell < 0) return { color: 'var(--cis-danger, #ef4444)' }
  }
  return {}
}

function formatCell(cell: string | number, colIndex: number): string {
  if (colIndex === 0) return String(cell)
  if (typeof cell === 'number') {
    if (cell > 0) return `+${cell}`
    if (cell === 0) return '-'
    return String(cell)
  }
  return String(cell)
}

function handleExport() {
  if (!preview.value) {
    buildPreview()
    if (!preview.value) {
      ElMessage.error('生成报表失败')
      return
    }
  }

  const now = new Date()
  const dateStr = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}`
  const filename = `积分报表_${dateStr}`

  if (exportFormat.value === 'xlsx') {
    exportReportToExcel(preview.value, filename, config.groupByGroup)
  } else {
    exportReportToCSV(preview.value, filename)
  }

  ElMessage.success('报表已导出')
}
</script>

<style scoped>
.preview-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.preview-table th,
.preview-table td {
  border: 1px solid var(--cis-border-color-light, #e4e7ed);
  padding: 6px 10px;
  text-align: center;
  white-space: nowrap;
}

.preview-table th {
  background: var(--cis-fill-color-light, #f5f7fa);
  font-weight: 600;
  color: var(--cis-text-primary);
}

.preview-table td:first-child {
  text-align: left;
  font-weight: 500;
}

.preview-table__group-row td {
  background: var(--cis-fill-color, #f0f2f5);
  font-weight: 600;
  color: var(--cis-text-secondary);
}
</style>
