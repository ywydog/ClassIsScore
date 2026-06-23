<template>
  <div class="score-management">
    <div class="score-management__header">
      <h2>{{ t('scoreManagement') }}</h2>
      <div class="score-management__actions">
        <el-button @click="showExportDialog = true">
          <el-icon><Download /></el-icon>
          {{ t('exportReport') }}
        </el-button>
        <el-button @click="showImportDialog = true">
          <el-icon><Upload /></el-icon>
          从表格导入
        </el-button>
        <el-button @click="showBatchDialog = true">
          <el-icon><Operation /></el-icon>
          {{ t('batchOperation') }}
        </el-button>
        <el-button v-if="isXianxiaMode" type="warning" @click="openBattleDialog">
          ⚔️ 道友切磋
        </el-button>
      </div>
    </div>

    <!-- 内联积分操作区 -->
    <div class="score-management__operator">
      <div class="score-operator">
        <div class="score-operator__row">
          <el-select
            v-model="addForm.studentId"
            :placeholder="t('selectStudent')"
            filterable
            class="score-operator__student-select"
          >
            <el-option
              v-for="s in studentStore.students"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            >
              <span>{{ s.name }}</span>
              <span style="float: right; color: var(--cis-text-tertiary); font-size: 12px">{{ s.score }}{{ t('scoreUnit') }}</span>
            </el-option>
          </el-select>
          <el-input-number
            v-model="addForm.scoreChange"
            :step="1"
            :min="-100"
            :max="100"
            controls-position="right"
            class="score-operator__score-input"
          />
          <el-input
            v-model="addForm.reason"
            placeholder="请输入原因"
            maxlength="50"
            class="score-operator__reason-input"
            @keyup.enter="handleAddScore"
          />
          <el-button type="success" :loading="scoreStore.loading" @click="handleAddScore">
            {{ t('addScore') }}
          </el-button>
          <el-button type="danger" :loading="scoreStore.loading" @click="handleSubtractScore">
            {{ t('subtractScore') }}
          </el-button>
        </div>
        <!-- 选中学生的周期积分 -->
        <div v-if="selectedStudentStats" class="score-operator__stats">
          <span class="score-operator__stats-item score-operator__stats-item--day">
            今日 <em>+{{ selectedStudentStats.dayPlus }}</em> / <em>{{ selectedStudentStats.dayMinus }}</em> = <strong>{{ formatStatNet(selectedStudentStats.dayNet) }}</strong>
          </span>
          <span class="score-operator__stats-item score-operator__stats-item--week">
            本周 <em>+{{ selectedStudentStats.weekPlus }}</em> / <em>{{ selectedStudentStats.weekMinus }}</em> = <strong>{{ formatStatNet(selectedStudentStats.weekNet) }}</strong>
          </span>
          <span class="score-operator__stats-item score-operator__stats-item--month">
            本月 <em>+{{ selectedStudentStats.monthPlus }}</em> / <em>{{ selectedStudentStats.monthMinus }}</em> = <strong>{{ formatStatNet(selectedStudentStats.monthNet) }}</strong>
          </span>
          <span v-if="selectedStudentStats.semesterNet !== undefined" class="score-operator__stats-item score-operator__stats-item--semester">
            学期 <em>+{{ selectedStudentStats.semesterPlus }}</em> / <em>{{ selectedStudentStats.semesterMinus }}</em> = <strong>{{ formatStatNet(selectedStudentStats.semesterNet) }}</strong>
          </span>
        </div>
        <!-- 快捷评价项 -->
        <div class="score-operator__quick">
          <span class="score-operator__quick-label">{{ t('quickLabel') + '：' }}</span>
          <div class="score-operator__quick-items">
            <div
              v-for="item in evaluationItems"
              :key="item.id"
              :class="['score-operator__quick-item', item.isPositive ? 'score-operator__quick-item--positive' : 'score-operator__quick-item--negative']"
              @click="applyEvaluationItem(item)"
            >
              <span class="score-operator__quick-item-name">{{ item.name }}</span>
              <span class="score-operator__quick-item-value">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="score-management__content">
      <div class="score-management__history">
        <ScoreHistory
          :records="scoreStore.recentRecords"
          :evaluation-items="evaluationItems"
          :loading="scoreStore.loading"
          @revert="handleRevert"
          @admin-revert="handleAdminRevert"
        />
      </div>
      <div class="score-management__stats-panel">
        <div class="stats-panel">
          <div class="stats-panel__header">
            <span class="stats-panel__title">{{ t('scoreStats') }}</span>
            <div class="stats-panel__toggles">
              <button
                v-for="p in periodOptions"
                :key="p.key"
                :class="['stats-panel__toggle', { 'stats-panel__toggle--active': activePeriod === p.key }]"
                @click="activePeriod = p.key"
              >{{ p.label }}</button>
            </div>
          </div>
          <div class="stats-panel__body">
            <div
              v-for="stat in periodStatsList"
              :key="stat.studentId"
              class="stats-panel__row"
              :class="{ 'stats-panel__row--highlight': addForm.studentId && String(stat.studentId) === String(addForm.studentId) }"
            >
              <span class="stats-panel__name">{{ stat.studentName }}</span>
              <span class="stats-panel__detail">
                <span class="stats-panel__plus">+{{ stat.plus }}</span>
                <span class="stats-panel__slash">/</span>
                <span class="stats-panel__minus">{{ stat.minus }}</span>
              </span>
              <span class="stats-panel__net" :class="stat.net > 0 ? 'stats-panel__net--pos' : stat.net < 0 ? 'stats-panel__net--neg' : ''">
                {{ formatStatNet(stat.net) }}
              </span>
            </div>
            <div v-if="periodStatsList.length === 0" class="stats-panel__empty">
              暂无数据
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 批量操作对话框 -->
    <el-dialog v-model="showBatchDialog" :title="t('batchScore')" width="520px" destroy-on-close>
      <el-form :model="batchForm" label-width="80px">
        <el-form-item :label="t('targetStudent')">
          <el-select v-model="batchForm.studentIds" multiple :placeholder="t('selectStudent')" filterable style="width: 100%">
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
        <el-form-item :label="t('scoreChange')" required>
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
    <el-dialog v-model="showImportDialog" :title="t('importScore')" width="640px" destroy-on-close>
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
              支持 .xlsx、.xls、.csv 格式，表格需包含姓名列和{{ t('score') }}列
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
          <el-form-item :label="t('scoreColumn')" required>
            <el-select v-model="importMapping.scoreColumnIndex" :placeholder="'选择' + t('scoreColumn')" style="width: 100%">
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
            <el-table-column prop="scoreChange" :label="t('score')" width="80">
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

    <!-- 导出报表向导 -->
    <ExportReportDialog
      v-model="showExportDialog"
      :students="studentStore.students"
      :groups="groups"
      :records="scoreStore.scoreRecords"
      :semester-start-date="settingsStore.settings.semesterStartDate"
    />

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

    <!-- 道友切磋对话框 -->
    <BattleDialog
      v-if="isXianxiaMode"
      v-model="showBattleDialog"
      :challenger="battleChallenger"
      :opponents="battleOpponents"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { Operation, Upload, Download } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import { useSettingsStore } from '@/stores/settings'
import { useTerminology } from '@/themes/xianxia/useTerminology'
import { groupApi } from '@/services/group'
import type { EvaluationItem, StudentGroup, StudentScoreStats } from '@/types'
import api from '@/services/api'
import { readExcelFile } from '@/utils/excelHelper'
import type { UploadFile } from 'element-plus'
import ScoreHistory from '@/components/score/ScoreHistory.vue'
import ExportReportDialog from '@/components/score/ExportReportDialog.vue'
import BattleDialog from '@/components/xianxia/BattleDialog.vue'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()
const settingsStore = useSettingsStore()

const showBatchDialog = ref(false)
const showImportDialog = ref(false)
const showExportDialog = ref(false)
const showAdminRevertDialog = ref(false)
const showBattleDialog = ref(false)
const adminRevertPassword = ref('')
const pendingRevertRecordId = ref<string | null>(null)
const evaluationItems = ref<EvaluationItem[]>([])
const groups = ref<StudentGroup[]>([])
const scoreStats = ref<StudentScoreStats[]>([])
const selectedStudentStats = computed(() => {
  if (!addForm.studentId) return null
  return scoreStats.value.find(s => String(s.studentId) === String(addForm.studentId))
})

// 修仙模式 & 道友切磋
const { isXianxia: isXianxiaMode, t } = useTerminology()
const battleChallenger = computed(() => {
  if (!addForm.studentId) return null
  return studentStore.students.find(s => String(s.id) === String(addForm.studentId)) || null
})
const battleOpponents = computed(() => {
  if (!addForm.studentId) return studentStore.students
  return studentStore.students.filter(s => String(s.id) !== String(addForm.studentId))
})

function openBattleDialog() {
  if (studentStore.students.length < 2) {
    ElMessage.warning('至少需要两名道友才能切磋')
    return
  }
  showBattleDialog.value = true
}

// 周期积分面板
type PeriodKey = 'day' | 'week' | 'month' | 'semester'
const activePeriod = ref<PeriodKey>('week')
const periodOptions = computed(() => {
  const opts: { key: PeriodKey; label: string }[] = [
    { key: 'day', label: '日' },
    { key: 'week', label: '周' },
    { key: 'month', label: '月' },
  ]
  if (scoreStats.value.some(s => s.semesterNet !== undefined)) {
    opts.push({ key: 'semester', label: '学期' })
  }
  return opts
})

interface PeriodStatRow {
  studentId: number
  studentName: string
  plus: number
  minus: number
  net: number
}

const periodStatsList = computed<PeriodStatRow[]>(() => {
  return scoreStats.value
    .map(s => {
      let plus = 0, minus = 0, net = 0
      switch (activePeriod.value) {
        case 'day': plus = s.dayPlus; minus = s.dayMinus; net = s.dayNet; break
        case 'week': plus = s.weekPlus; minus = s.weekMinus; net = s.weekNet; break
        case 'month': plus = s.monthPlus; minus = s.monthMinus; net = s.monthNet; break
        case 'semester': plus = s.semesterPlus || 0; minus = s.semesterMinus || 0; net = s.semesterNet || 0; break
      }
      return { studentId: s.studentId, studentName: s.studentName, plus, minus, net }
    })
    .sort((a, b) => b.net - a.net)
})

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

onMounted(async () => {
  await Promise.all([
    scoreStore.fetchRecords(),
    studentStore.fetchStudents(),
    fetchEvaluationItems(),
    fetchGroups(),
    fetchScoreStats(),
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

async function fetchScoreStats() {
  try {
    scoreStats.value = []
  } catch { /* ignore */ }
}

function formatStatNet(val: number | undefined): string {
  if (val === undefined || val === 0) return '0'
  return val > 0 ? `+${val}` : `${val}`
}

function applyEvaluationItem(item: EvaluationItem) {
  addForm.scoreChange = Math.abs(item.scoreChange)
  addForm.reason = item.name
}

async function handleAddScore() {
  if (!addForm.studentId) {
    ElMessage.warning(t('selectStudent'))
    return
  }
  if (!addForm.reason) {
    ElMessage.warning('请输入原因')
    return
  }
  if (addForm.scoreChange <= 0) {
    ElMessage.warning(t('addScoreRequired'))
    return
  }
  try {
    const student = studentStore.getStudentById(addForm.studentId)
    await scoreStore.addScore(addForm.studentId, addForm.scoreChange, addForm.reason)
    ElMessage.success(`已为 ${student?.name || t('student')} ${t('addScore')} ${addForm.scoreChange} ${t('scoreUnit')}`)
    addForm.reason = ''
  } catch { /* error handled in store */ }
}

async function handleSubtractScore() {
  if (!addForm.studentId) {
    ElMessage.warning(t('selectStudent'))
    return
  }
  if (!addForm.reason) {
    ElMessage.warning('请输入原因')
    return
  }
  if (addForm.scoreChange <= 0) {
    ElMessage.warning(t('subtractScoreRequired'))
    return
  }
  try {
    const student = studentStore.getStudentById(addForm.studentId)
    await scoreStore.addScore(addForm.studentId, -addForm.scoreChange, addForm.reason)
    ElMessage.success(`已为 ${student?.name || t('student')} ${t('subtractScore')} ${addForm.scoreChange} ${t('scoreUnit')}`)
    addForm.reason = ''
  } catch { /* error handled in store */ }
}

async function handleBatchScore() {
  if (batchForm.studentIds.length === 0 || !batchForm.reason) {
    ElMessage.warning(t('selectStudentAndReason'))
    return
  }
  try {
    await scoreStore.batchAddScore(batchForm.studentIds, batchForm.scoreChange, batchForm.reason)
    ElMessage.success(`已为 ${batchForm.studentIds.length} 名${t('student')}${t('addScore')}`)
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

/* 内联积分操作区 */
.score-management__operator {
  margin-bottom: 20px;
}

.score-operator {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  padding: 16px 20px;
  border: 1px solid var(--cis-border-color-light);
  transition: box-shadow var(--cis-transition-fast);
}

.score-operator:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.score-operator__row {
  display: flex;
  gap: 10px;
  align-items: center;
}

.score-operator__student-select {
  width: 180px;
  flex-shrink: 0;
}

.score-operator__score-input {
  width: 130px;
  flex-shrink: 0;
}

.score-operator__reason-input {
  flex: 1;
  min-width: 120px;
}

.score-operator__quick {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid var(--cis-border-color-lighter, rgba(0, 0, 0, 0.06));
}

.score-operator__quick-label {
  font-size: 13px;
  color: var(--cis-text-secondary);
  line-height: 32px;
  flex-shrink: 0;
}

.score-operator__quick-items {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.score-operator__quick-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 12px;
  border-radius: var(--cis-radius-md, 6px);
  cursor: pointer;
  font-size: 13px;
  transition: all var(--cis-transition-fast, 0.15s);
  user-select: none;
  border: 1px solid transparent;
}

.score-operator__quick-item:hover {
  transform: translateY(-1px);
  box-shadow: var(--cis-shadow-card);
}

.score-operator__quick-item:active {
  transform: translateY(0) scale(0.97);
}

.score-operator__quick-item--positive {
  background: linear-gradient(135deg, rgba(34, 197, 94, 0.08), rgba(34, 197, 94, 0.15));
  color: var(--cis-success, #22c55e);
  border-color: rgba(34, 197, 94, 0.2);
}

.score-operator__quick-item--positive:hover {
  background: linear-gradient(135deg, rgba(34, 197, 94, 0.12), rgba(34, 197, 94, 0.2));
  border-color: rgba(34, 197, 94, 0.35);
  box-shadow: 0 2px 8px rgba(34, 197, 94, 0.15);
}

.score-operator__quick-item--negative {
  background: linear-gradient(135deg, rgba(239, 68, 68, 0.08), rgba(239, 68, 68, 0.15));
  color: var(--cis-danger, #ef4444);
  border-color: rgba(239, 68, 68, 0.2);
}

.score-operator__quick-item--negative:hover {
  background: linear-gradient(135deg, rgba(239, 68, 68, 0.12), rgba(239, 68, 68, 0.2));
  border-color: rgba(239, 68, 68, 0.35);
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.15);
}

.score-operator__quick-item-name {
  font-weight: 500;
}

.score-operator__quick-item-value {
  font-weight: 700;
  font-size: 12px;
}

.score-operator__stats {
  display: flex;
  gap: 16px;
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid var(--cis-border-color-lighter, rgba(0, 0, 0, 0.06));
  flex-wrap: wrap;
}

.score-operator__stats-item {
  font-size: 12px;
  color: var(--cis-text-secondary);
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 3px 8px;
  border-radius: 4px;
  background: var(--cis-fill-color-light, #f5f7fa);
}

.score-operator__stats-item em {
  font-style: normal;
  font-weight: 600;
}

.score-operator__stats-item strong {
  font-weight: 700;
  margin-left: 2px;
}

.score-operator__stats-item--day strong { color: #3b82f6; }
.score-operator__stats-item--week strong { color: #a855f7; }
.score-operator__stats-item--month strong { color: #f59e0b; }
.score-operator__stats-item--semester strong { color: #0d9488; }

.score-management__content {
  display: grid;
  grid-template-columns: 1fr 280px;
  gap: 16px;
  align-items: start;
}

.score-management__history,
.score-management__stats-panel {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.score-management__history:hover,
.score-management__stats-panel:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

/* 周期积分统计面板 */
.stats-panel {
  padding: 16px;
}

.stats-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.stats-panel__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.stats-panel__toggles {
  display: flex;
  gap: 3px;
}

.stats-panel__toggle {
  padding: 3px 10px;
  border-radius: 6px;
  border: 1px solid var(--cis-border-color-light, #e4e7ed);
  background: var(--cis-fill-color-light, #f5f7fa);
  color: var(--cis-text-secondary);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}

.stats-panel__toggle:hover {
  border-color: var(--cis-primary, #0d9488);
  color: var(--cis-primary, #0d9488);
}

.stats-panel__toggle--active {
  background: var(--cis-primary, #0d9488);
  border-color: var(--cis-primary, #0d9488);
  color: #fff;
  box-shadow: 0 2px 6px rgba(13, 148, 136, 0.25);
}

.stats-panel__body {
  display: flex;
  flex-direction: column;
  gap: 2px;
  max-height: 500px;
  overflow-y: auto;
}

.stats-panel__row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 8px;
  border-radius: 6px;
  transition: background 0.12s;
}

.stats-panel__row:hover {
  background: var(--cis-fill-color-light, #f5f7fa);
}

.stats-panel__row--highlight {
  background: rgba(13, 148, 136, 0.08);
  border: 1px solid rgba(13, 148, 136, 0.2);
}

.stats-panel__name {
  flex: 1;
  font-size: 13px;
  font-weight: 500;
  color: var(--cis-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.stats-panel__detail {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: 11px;
}

.stats-panel__plus {
  color: var(--cis-success, #22c55e);
  font-weight: 600;
}

.stats-panel__slash {
  color: var(--cis-text-tertiary, #999);
}

.stats-panel__minus {
  color: var(--cis-danger, #ef4444);
  font-weight: 600;
}

.stats-panel__net {
  font-size: 14px;
  font-weight: 700;
  min-width: 36px;
  text-align: right;
  color: var(--cis-text-secondary);
}

.stats-panel__net--pos {
  color: var(--cis-success, #22c55e);
}

.stats-panel__net--neg {
  color: var(--cis-danger, #ef4444);
}

.stats-panel__empty {
  padding: 24px;
  text-align: center;
  font-size: 13px;
  color: var(--cis-text-tertiary);
}

@media (max-width: 900px) {
  .score-management__content {
    grid-template-columns: 1fr;
  }
  .score-operator__row {
    flex-wrap: wrap;
  }
  .score-operator__student-select {
    width: 100%;
  }
  .score-operator__score-input {
    width: 120px;
  }
}
</style>
