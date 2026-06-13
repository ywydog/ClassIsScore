<template>
  <div class="score-management">
    <div class="score-management__header">
      <h2>积分管理</h2>
      <div class="score-management__actions">
        <el-button @click="showExportDialog = true">
          <el-icon><Download /></el-icon>
          导出记录
        </el-button>
        <el-button @click="showImportDialog = true">
          <el-icon><Upload /></el-icon>
          从表格导入
        </el-button>
        <el-button @click="showBatchDialog = true">
          <el-icon><Operation /></el-icon>
          批量操作
        </el-button>
        <el-button type="primary" @click="showAddDialog = true">
          <el-icon><Plus /></el-icon>
          自定义积分
        </el-button>
      </div>
    </div>

    <div class="score-management__content">
      <div class="score-management__panel">
        <QuickScorePanel
          :evaluation-items="evaluationItems"
          :students="studentStore.students"
          @score="handleQuickScore"
        />
      </div>

      <div class="score-management__history">
        <ScoreHistory
          :records="scoreStore.recentRecords"
          @revert="handleRevert"
          @admin-revert="handleAdminRevert"
        />
      </div>
    </div>

    <!-- 自定义积分对话框 -->
    <el-dialog v-model="showAddDialog" title="自定义积分" width="480px" destroy-on-close>
      <el-form :model="addForm" label-width="80px">
        <el-form-item label="学生" required>
          <el-select v-model="addForm.studentId" placeholder="选择学生" filterable style="width: 100%">
            <el-option
              v-for="s in studentStore.students"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            >
              <span>{{ s.name }}</span>
              <span style="float: right; color: var(--cis-text-tertiary); font-size: 12px">{{ s.score }}分</span>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="积分变动" required>
          <el-input-number v-model="addForm.scoreChange" :step="1" :min="-100" :max="100" style="width: 100%" />
        </el-form-item>
        <el-form-item label="原因" required>
          <el-input v-model="addForm.reason" placeholder="请输入原因" maxlength="50" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" :loading="scoreStore.loading" @click="handleAddScore">确定</el-button>
      </template>
    </el-dialog>

    <!-- 批量操作对话框 -->
    <el-dialog v-model="showBatchDialog" title="批量积分" width="520px" destroy-on-close>
      <el-form :model="batchForm" label-width="80px">
        <el-form-item label="目标学生">
          <el-select v-model="batchForm.studentIds" multiple placeholder="选择学生" filterable style="width: 100%">
            <el-option
              v-for="s in studentStore.students"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="或按小组">
          <el-select v-model="batchForm.groupId" placeholder="选择小组" clearable style="width: 100%" @change="handleGroupSelect">
            <el-option
              v-for="g in groups"
              :key="g.id"
              :label="g.name"
              :value="g.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="积分变动" required>
          <el-input-number v-model="batchForm.scoreChange" :step="1" style="width: 100%" />
        </el-form-item>
        <el-form-item label="原因" required>
          <el-input v-model="batchForm.reason" placeholder="请输入原因" maxlength="50" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showBatchDialog = false">取消</el-button>
        <el-button type="primary" :loading="scoreStore.loading" @click="handleBatchScore">
          确定 ({{ batchForm.studentIds.length }}人)
        </el-button>
      </template>
    </el-dialog>

    <!-- 从表格导入对话框 -->
    <el-dialog v-model="showImportDialog" title="从表格导入积分" width="640px" destroy-on-close>
      <!-- 步骤1: 选择文件 -->
      <div v-if="importStep === 0">
        <el-upload
          ref="uploadRef"
          drag
          :auto-upload="false"
          :limit="1"
          accept=".xlsx,.xls,.csv"
          :on-change="handleFileChange"
          :on-remove="() => importFile = null"
        >
          <el-icon :size="40" style="color: var(--cis-text-tertiary)"><Upload /></el-icon>
          <div style="margin-top: 8px">拖拽文件到此处，或 <em>点击选择文件</em></div>
          <template #tip>
            <div style="font-size: 12px; color: var(--cis-text-tertiary); margin-top: 8px">
              支持 .xlsx、.xls、.csv 格式，表格需包含姓名列和积分列
            </div>
          </template>
        </el-upload>
      </div>

      <!-- 步骤2: 列映射 + 预览 -->
      <div v-else-if="importStep === 1">
        <el-alert type="info" :closable="false" style="margin-bottom: 16px">
          请为每个字段选择对应的表格列
        </el-alert>
        <el-form label-width="80px">
          <el-form-item label="姓名列" required>
            <el-select v-model="importMapping.nameColumnIndex" placeholder="选择姓名列" style="width: 100%">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
          <el-form-item label="学号列">
            <el-select v-model="importMapping.numberColumnIndex" placeholder="可选" clearable style="width: 100%">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
          <el-form-item label="积分列" required>
            <el-select v-model="importMapping.scoreColumnIndex" placeholder="选择积分列" style="width: 100%">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
          <el-form-item label="原因列">
            <el-select v-model="importMapping.reasonColumnIndex" placeholder="可选" clearable style="width: 100%">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
        </el-form>

        <div v-if="importPreviewEntries.length > 0" style="margin-top: 16px">
          <div style="font-size: 13px; font-weight: 600; margin-bottom: 8px; color: var(--cis-text-primary)">
            预览 (前5条)
          </div>
          <el-table :data="importPreviewEntries.slice(0, 5)" size="small" border>
            <el-table-column prop="studentName" label="姓名" width="100" />
            <el-table-column prop="scoreChange" label="积分" width="80">
              <template #default="{ row }">
                <span :style="{ color: row.scoreChange > 0 ? 'var(--cis-success)' : 'var(--cis-danger)' }">
                  {{ row.scoreChange > 0 ? '+' : '' }}{{ row.scoreChange }}
                </span>
              </template>
            </el-table-column>
            <el-table-column prop="reason" label="原因" />
            <el-table-column label="状态" width="80">
              <template #default="{ row }">
                <el-tag :type="row.isMatched ? 'success' : 'danger'" size="small">
                  {{ row.isMatched ? '已匹配' : '未找到' }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
          <div style="margin-top: 8px; font-size: 12px; color: var(--cis-text-tertiary)">
            共 {{ importPreviewEntries.length }} 条，已匹配 {{ importPreviewEntries.filter(e => e.isMatched).length }} 条
          </div>
        </div>
      </div>

      <!-- 步骤3: 导入结果 -->
      <div v-else-if="importStep === 2">
        <el-result :icon="importResult.failCount === 0 ? 'success' : 'warning'" :title="importResultTitle">
          <template #sub-title>
            <p>成功: {{ importResult.successCount }} 条</p>
            <p v-if="importResult.skipCount > 0">跳过: {{ importResult.skipCount }} 条</p>
            <p v-if="importResult.failCount > 0">失败: {{ importResult.failCount }} 条</p>
          </template>
        </el-result>
      </div>

      <template #footer>
        <el-button v-if="importStep > 0 && importStep < 2" @click="importStep--">上一步</el-button>
        <el-button v-if="importStep === 0" :disabled="!importFile" type="primary" @click="handleImportNext">
          下一步
        </el-button>
        <el-button v-if="importStep === 1" type="primary" :disabled="importPreviewEntries.length === 0" @click="handleExecuteImport">
          确认导入 ({{ importPreviewEntries.filter(e => e.isMatched).length }}条)
        </el-button>
        <el-button v-if="importStep === 2" type="primary" @click="closeImportDialog">完成</el-button>
      </template>
    </el-dialog>

    <!-- 导出对话框 -->
    <el-dialog v-model="showExportDialog" title="导出积分记录" width="420px">
      <el-form :model="exportForm" label-width="80px">
        <el-form-item label="日期范围">
          <el-date-picker
            v-model="exportForm.dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="导出格式">
          <el-radio-group v-model="exportForm.format">
            <el-radio-button value="xlsx">Excel</el-radio-button>
            <el-radio-button value="csv">CSV</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showExportDialog = false">取消</el-button>
        <el-button type="primary" @click="handleExport">导出</el-button>
      </template>
    </el-dialog>

    <!-- 管理员密码验证对话框 -->
    <el-dialog v-model="showAdminRevertDialog" title="管理员验证" width="400px" destroy-on-close>
      <el-alert type="warning" :closable="false" style="margin-bottom: 16px">
        该积分记录已超过3分钟快速撤销窗口，需要管理员密码验证才能撤销
      </el-alert>
      <el-form label-width="100px">
        <el-form-item label="管理员密码" required>
          <el-input
            v-model="adminRevertPassword"
            type="password"
            placeholder="请输入管理员密码"
            show-password
            @keyup.enter="confirmAdminRevert"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAdminRevertDialog = false">取消</el-button>
        <el-button type="danger" :loading="scoreStore.loading" @click="confirmAdminRevert">确认撤销</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { Plus, Operation, Upload, Download } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import { groupApi } from '@/services/group'
import type { EvaluationItem, StudentGroup } from '@/types'
import api from '@/services/api'
import { readExcelFile, exportToExcel, exportToCSV } from '@/utils/excelHelper'
import type { UploadFile } from 'element-plus'
import QuickScorePanel from '@/components/score/QuickScorePanel.vue'
import ScoreHistory from '@/components/score/ScoreHistory.vue'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()

const showAddDialog = ref(false)
const showBatchDialog = ref(false)
const showImportDialog = ref(false)
const showExportDialog = ref(false)
const showAdminRevertDialog = ref(false)
const adminRevertPassword = ref('')
const pendingRevertRecordId = ref<string | null>(null)
const evaluationItems = ref<EvaluationItem[]>([])
const groups = ref<StudentGroup[]>([])

const addForm = reactive({ studentId: '', scoreChange: 1, reason: '' })
const batchForm = reactive({ studentIds: [] as string[], groupId: '', scoreChange: 1, reason: '' })

// 导入相关状态
const importStep = ref(0)
const importFile = ref<File | null>(null)
const importHeaders = ref<string[]>([])
const importRows = ref<string[][]>([])
const importMapping = reactive({
  nameColumnIndex: -1,
  numberColumnIndex: -1,
  scoreColumnIndex: -1,
  reasonColumnIndex: -1,
})
const importResult = reactive({ successCount: 0, skipCount: 0, failCount: 0 })

interface ImportPreviewEntry {
  studentName: string
  studentNumber: string
  scoreChange: number
  reason: string
  isMatched: boolean
  studentId?: string
}

const importPreviewEntries = ref<ImportPreviewEntry[]>([])

const importResultTitle = computed(() => {
  if (importResult.failCount === 0) return '导入完成'
  return '导入完成（部分失败）'
})

// 导出相关状态
const exportForm = reactive({
  dateRange: null as [Date, Date] | null,
  format: 'xlsx' as 'xlsx' | 'csv',
})

onMounted(async () => {
  await Promise.all([
    scoreStore.fetchRecords(),
    studentStore.fetchStudents(),
    fetchEvaluationItems(),
    fetchGroups(),
  ])
})

async function fetchEvaluationItems() {
  try {
    const response = await api.get<{ data: EvaluationItem[] }>('/api/evaluations')
    evaluationItems.value = response.data.data
  } catch {
    evaluationItems.value = [
      { id: '1', name: '回答问题', scoreChange: 2, isPositive: true, createdAt: '' },
      { id: '2', name: '课堂表现', scoreChange: 1, isPositive: true, createdAt: '' },
      { id: '3', name: '作业优秀', scoreChange: 3, isPositive: true, createdAt: '' },
      { id: '4', name: '帮助同学', scoreChange: 1, isPositive: true, createdAt: '' },
      { id: '5', name: '迟到', scoreChange: -1, isPositive: false, createdAt: '' },
      { id: '6', name: '未交作业', scoreChange: -2, isPositive: false, createdAt: '' },
      { id: '7', name: '课堂违纪', scoreChange: -3, isPositive: false, createdAt: '' },
    ]
  }
}

async function fetchGroups() {
  try {
    const response = await groupApi.getAll()
    groups.value = response.data.data
  } catch { /* ignore */ }
}

async function handleQuickScore(studentId: string, item: EvaluationItem) {
  try {
    await scoreStore.addScore(studentId, item.scoreChange, item.name)
    ElMessage.success(`已为 ${studentStore.getStudentById(studentId)?.name || '学生'} ${item.isPositive ? '加' : '扣'}${Math.abs(item.scoreChange)}分`)
  } catch { /* error handled in store */ }
}

async function handleAddScore() {
  if (!addForm.studentId || !addForm.reason) {
    ElMessage.warning('请填写完整信息')
    return
  }
  try {
    await scoreStore.addScore(addForm.studentId, addForm.scoreChange, addForm.reason)
    ElMessage.success('积分已添加')
    showAddDialog.value = false
    addForm.studentId = ''
    addForm.scoreChange = 1
    addForm.reason = ''
  } catch { /* error handled in store */ }
}

async function handleBatchScore() {
  if (batchForm.studentIds.length === 0 || !batchForm.reason) {
    ElMessage.warning('请选择学生并填写原因')
    return
  }
  try {
    await scoreStore.batchAddScore(batchForm.studentIds, batchForm.scoreChange, batchForm.reason)
    ElMessage.success(`已为 ${batchForm.studentIds.length} 名学生添加积分`)
    showBatchDialog.value = false
    batchForm.studentIds = []
    batchForm.groupId = ''
    batchForm.scoreChange = 1
    batchForm.reason = ''
  } catch { /* error handled in store */ }
}

function handleGroupSelect(groupId: string) {
  if (!groupId) return
  const group = groups.value.find(g => g.id === groupId)
  if (group) {
    batchForm.studentIds = [...group.studentIds]
  }
}

async function handleRevert(recordId: string) {
  try {
    await scoreStore.revertScore(recordId)
    ElMessage.success('已撤销')
  } catch { /* error handled in store */ }
}

function handleAdminRevert(recordId: string) {
  pendingRevertRecordId.value = recordId
  adminRevertPassword.value = ''
  showAdminRevertDialog.value = true
}

async function confirmAdminRevert() {
  if (!adminRevertPassword.value) {
    ElMessage.warning('请输入管理员密码')
    return
  }
  if (!pendingRevertRecordId.value) return
  try {
    await scoreStore.revertScore(pendingRevertRecordId.value, adminRevertPassword.value)
    ElMessage.success('已撤销')
    showAdminRevertDialog.value = false
    pendingRevertRecordId.value = null
    adminRevertPassword.value = ''
  } catch {
    ElMessage.error('管理员密码错误或撤销失败')
  }
}

// ========== 导入功能 ==========

async function handleFileChange(file: UploadFile) {
  if (file.raw) {
    importFile.value = file.raw
  }
}

async function handleImportNext() {
  if (!importFile.value) return
  try {
    const { headers, rows } = await readExcelFile(importFile.value)
    importHeaders.value = headers
    importRows.value = rows

    // 自动匹配列
    importMapping.nameColumnIndex = headers.findIndex(h => /姓名|名字|学生/.test(h))
    importMapping.numberColumnIndex = headers.findIndex(h => /学号|编号/.test(h))
    importMapping.scoreColumnIndex = headers.findIndex(h => /积分|分数|得分|成绩/.test(h))
    importMapping.reasonColumnIndex = headers.findIndex(h => /原因|理由|备注|说明/.test(h))

    importStep.value = 1
    updateImportPreview()
  } catch (err) {
    ElMessage.error('文件读取失败: ' + (err as Error).message)
  }
}

function updateImportPreview() {
  if (importMapping.nameColumnIndex < 0 || importMapping.scoreColumnIndex < 0) {
    importPreviewEntries.value = []
    return
  }

  const entries: ImportPreviewEntry[] = importRows.value.map(row => {
    const studentName = row[importMapping.nameColumnIndex] || ''
    const studentNumber = importMapping.numberColumnIndex >= 0 ? row[importMapping.numberColumnIndex] : ''
    const scoreChange = parseFloat(row[importMapping.scoreColumnIndex]) || 0
    const reason = importMapping.reasonColumnIndex >= 0 ? row[importMapping.reasonColumnIndex] : '表格导入'

    // 匹配学生
    let matchedStudent = studentStore.students.find(s => s.name === studentName)
    if (!matchedStudent && studentNumber) {
      matchedStudent = studentStore.students.find(s => s.studentNumber === studentNumber)
    }

    return {
      studentName,
      studentNumber,
      scoreChange,
      reason: reason || '表格导入',
      isMatched: !!matchedStudent,
      studentId: matchedStudent?.id,
    }
  })

  importPreviewEntries.value = entries
}

// 监听列映射变化，更新预览
watch(() => [importMapping.nameColumnIndex, importMapping.scoreColumnIndex, importMapping.numberColumnIndex, importMapping.reasonColumnIndex], () => {
  if (importStep.value === 1) {
    updateImportPreview()
  }
})

async function handleExecuteImport() {
  const matchedEntries = importPreviewEntries.value.filter(e => e.isMatched && e.studentId)
  let successCount = 0
  let failCount = 0

  for (const entry of matchedEntries) {
    try {
      await scoreStore.addScore(entry.studentId!, entry.scoreChange, entry.reason)
      successCount++
    } catch {
      failCount++
    }
  }

  importResult.successCount = successCount
  importResult.skipCount = importPreviewEntries.value.length - matchedEntries.length
  importResult.failCount = failCount
  importStep.value = 2

  // 刷新数据
  await scoreStore.fetchRecords()
  await studentStore.fetchStudents()
}

function closeImportDialog() {
  showImportDialog.value = false
  importStep.value = 0
  importFile.value = null
  importHeaders.value = []
  importRows.value = []
  importPreviewEntries.value = []
  importMapping.nameColumnIndex = -1
  importMapping.numberColumnIndex = -1
  importMapping.scoreColumnIndex = -1
  importMapping.reasonColumnIndex = -1
}

// ========== 导出功能 ==========

function handleExport() {
  let records = scoreStore.recentRecords

  // 日期过滤
  if (exportForm.dateRange) {
    const [start, end] = exportForm.dateRange
    records = records.filter(r => {
      const date = new Date(r.createdAt)
      return date >= start && date <= end
    })
  }

  if (records.length === 0) {
    ElMessage.warning('没有可导出的记录')
    return
  }

  const columns = [
    { header: '学生姓名', key: 'studentName' },
    { header: '积分变动', key: 'scoreChange' },
    { header: '原因', key: 'reason' },
    { header: '时间', key: 'createdAt' },
    { header: '是否撤销', key: 'isReverted' },
  ]

  const data = records.map(r => ({
    studentName: r.studentName,
    scoreChange: r.scoreChange,
    reason: r.reason,
    createdAt: new Date(r.createdAt).toLocaleString('zh-CN'),
    isReverted: r.isReverted ? '是' : '否',
  }))

  const filename = `积分记录_${new Date().toISOString().slice(0, 10)}`

  if (exportForm.format === 'xlsx') {
    exportToExcel(data, columns, filename)
  } else {
    exportToCSV(data, columns, filename)
  }

  ElMessage.success(`已导出 ${records.length} 条记录`)
  showExportDialog.value = false
}
</script>

<style scoped>
.score-management__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.score-management__header h2 {
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

.score-management__actions {
  display: flex;
  gap: 8px;
}

.score-management__content {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  align-items: start;
}

.score-management__panel,
.score-management__history {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.score-management__panel:hover,
.score-management__history:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

@media (max-width: 900px) {
  .score-management__content {
    grid-template-columns: 1fr;
  }
}
</style>
