<template>
  <div class="score-management">
    <!-- 顶部标题 -->
    <header class="score-management__head">
      <div>
        <span class="cis-eyebrow">Score</span>
        <h1 class="cis-display score-management__title">{{ t('scoreManagement') }}</h1>
      </div>
      <div class="score-management__head-actions">
        <el-button @click="showExportDialog = true" aria-label="导出报表">
          <el-icon aria-hidden="true"><Download /></el-icon>
          {{ t('exportReport') }}
        </el-button>
        <el-button @click="showImportDialog = true" aria-label="从表格导入">
          <el-icon aria-hidden="true"><Upload /></el-icon>
          从表格导入
        </el-button>
        <el-button @click="showBatchDialog = true" aria-label="批量操作">
          <el-icon aria-hidden="true"><Operation /></el-icon>
          {{ t('batchOperation') }}
        </el-button>
        <el-button v-if="isXianxiaMode" type="warning" plain @click="openBattleDialog" aria-label="道友切磋">
          切磋
        </el-button>
      </div>
    </header>

    <!-- 内联积分操作区 -->
    <section class="score-operator cis-hairline">
      <div class="score-operator__row">
        <el-select
          v-model="addForm.studentId"
          :placeholder="t('selectStudent')"
          filterable
          class="score-operator__student-select"
          aria-label="选择学生"
        >
          <el-option
            v-for="s in studentStore.students"
            :key="s.id"
            :label="s.name"
            :value="s.id"
          >
            <span>{{ s.name }}</span>
            <span class="score-operator__option-score cis-num">{{ s.score }}{{ t('scoreUnit') }}</span>
          </el-option>
        </el-select>
        <el-input-number
          v-model="addForm.scoreChange"
          :step="1"
          :min="-100"
          :max="100"
          controls-position="right"
          class="score-operator__score-input"
          inputmode="numeric"
          aria-label="分值变化"
        />
        <el-input
          v-model="addForm.reason"
          placeholder="请输入原因…"
          maxlength="50"
          class="score-operator__reason-input"
          aria-label="原因"
          autocomplete="off"
          @keyup.enter="handleAddScore"
        />
        <el-button type="primary" :loading="scoreStore.loading" @click="handleAddScore">
          {{ t('addScore') }}
        </el-button>
        <el-button type="danger" plain :loading="scoreStore.loading" @click="handleSubtractScore">
          {{ t('subtractScore') }}
        </el-button>
      </div>
      <!-- 选中学生的周期积分 -->
      <div v-if="selectedStudentStats" class="score-operator__stats">
        <span class="score-operator__stats-item score-operator__stats-item--day">
          <span class="cis-eyebrow">Day</span>
          <em class="cis-num">+{{ selectedStudentStats.dayPlus }}</em>
          <span class="score-operator__stats-sep">/</span>
          <em class="cis-num">{{ selectedStudentStats.dayMinus }}</em>
          <span class="score-operator__stats-eq">=</span>
          <strong class="cis-num">{{ formatStatNet(selectedStudentStats.dayNet) }}</strong>
        </span>
        <span class="score-operator__stats-item score-operator__stats-item--week">
          <span class="cis-eyebrow">Week</span>
          <em class="cis-num">+{{ selectedStudentStats.weekPlus }}</em>
          <span class="score-operator__stats-sep">/</span>
          <em class="cis-num">{{ selectedStudentStats.weekMinus }}</em>
          <span class="score-operator__stats-eq">=</span>
          <strong class="cis-num">{{ formatStatNet(selectedStudentStats.weekNet) }}</strong>
        </span>
        <span class="score-operator__stats-item score-operator__stats-item--month">
          <span class="cis-eyebrow">Month</span>
          <em class="cis-num">+{{ selectedStudentStats.monthPlus }}</em>
          <span class="score-operator__stats-sep">/</span>
          <em class="cis-num">{{ selectedStudentStats.monthMinus }}</em>
          <span class="score-operator__stats-eq">=</span>
          <strong class="cis-num">{{ formatStatNet(selectedStudentStats.monthNet) }}</strong>
        </span>
        <span v-if="selectedStudentStats.semesterNet !== undefined" class="score-operator__stats-item score-operator__stats-item--semester">
          <span class="cis-eyebrow">Term</span>
          <em class="cis-num">+{{ selectedStudentStats.semesterPlus }}</em>
          <span class="score-operator__stats-sep">/</span>
          <em class="cis-num">{{ selectedStudentStats.semesterMinus }}</em>
          <span class="score-operator__stats-eq">=</span>
          <strong class="cis-num">{{ formatStatNet(selectedStudentStats.semesterNet) }}</strong>
        </span>
      </div>
      <!-- 快捷评价项 -->
      <div class="score-operator__quick">
        <span class="cis-eyebrow score-operator__quick-label">{{ t('quickLabel') }}</span>
        <div class="score-operator__quick-items">
          <button
            v-for="item in evaluationItems"
            :key="item.id"
            type="button"
            class="score-operator__quick-item"
            :class="item.isPositive ? 'is-plus' : 'is-minus'"
            :aria-label="`应用评估项 ${item.name}，分值变化 ${item.isPositive ? '+' : ''}${item.scoreChange}`"
            @click="applyEvaluationItem(item)"
          >
            <span class="score-operator__quick-item-name">{{ item.name }}</span>
            <span class="score-operator__quick-item-value cis-num">{{ item.isPositive ? '+' : '' }}{{ item.scoreChange }}</span>
          </button>
        </div>
      </div>
    </section>

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
      <aside class="score-management__stats-panel cis-hairline">
        <div class="stats-panel">
          <header class="stats-panel__header">
            <span class="stats-panel__title">周期积分</span>
            <div class="stats-panel__tabs" role="tablist">
              <button
                v-for="p in periodOptions"
                :key="p.key"
                type="button"
                class="stats-panel__tab"
                :class="{ 'is-active': activePeriod === p.key }"
                :aria-selected="activePeriod === p.key"
                @click="activePeriod = p.key"
              >{{ p.label }}</button>
            </div>
          </header>
          <div class="stats-panel__body">
            <div
              v-for="stat in periodStatsList"
              :key="stat.studentId"
              class="stats-panel__row"
              :class="{ 'stats-panel__row--highlight': addForm.studentId && String(stat.studentId) === String(addForm.studentId) }"
            >
              <span class="stats-panel__name">{{ stat.studentName }}</span>
              <span class="stats-panel__detail cis-num">
                <span class="stats-panel__plus">+{{ stat.plus }}</span>
                <span class="stats-panel__slash">/</span>
                <span class="stats-panel__minus">{{ stat.minus }}</span>
              </span>
              <span
                class="stats-panel__net cis-num"
                :class="stat.net > 0 ? 'is-plus' : stat.net < 0 ? 'is-minus' : ''"
              >
                {{ formatStatNet(stat.net) }}
              </span>
            </div>
            <div v-if="periodStatsList.length === 0" class="stats-panel__empty">
              暂无数据
            </div>
          </div>
        </div>
      </aside>
    </div>

    <!-- 批量操作对话框 -->
    <el-dialog v-model="showBatchDialog" :title="t('batchScore')" width="520px" destroy-on-close>
      <el-form :model="batchForm" label-width="80px">
        <el-form-item :label="t('targetStudent')">
          <el-select v-model="batchForm.studentIds" multiple :placeholder="t('selectStudent')" filterable style="width: 100%" aria-label="选择学生">
            <el-option
              v-for="s in studentStore.students"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="或按小组">
          <el-select v-model="batchForm.groupId" placeholder="选择小组…" clearable style="width: 100%" aria-label="选择小组" @change="handleGroupSelect">
            <el-option
              v-for="g in groups"
              :key="g.id"
              :label="g.name"
              :value="g.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('scoreChange')" required>
          <el-input-number v-model="batchForm.scoreChange" :step="1" style="width: 100%" inputmode="numeric" aria-label="分值变化" />
        </el-form-item>
        <el-form-item label="原因" required>
          <el-input v-model="batchForm.reason" placeholder="请输入原因…" maxlength="50" show-word-limit aria-label="原因" autocomplete="off" />
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
import { ElMessage, ElMessageBox } from 'element-plus'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import { useSettingsStore } from '@/stores/settings'
import { useTerminology } from '@/themes/xianxia/useTerminology'
import { groupApi } from '@/services/group'
import { invoke } from '@/services/tauri'
import type { EvaluationItem, StudentGroup, StudentScoreStats } from '@/types'
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
    // IPC 改造：/api/evaluations → evaluation_list
    const items = await invoke<EvaluationItem[]>('evaluation_list', {})
    evaluationItems.value = items || []
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
    await ElMessageBox.confirm('确定要撤销这条积分记录吗？此操作将恢复该学生的对应分值。', '撤销确认', {
      type: 'warning',
      confirmButtonText: '确定撤销',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }
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
/* ===== 顶部标题 ===== */
.score-management__head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}

.score-management__title {
  font-size: 28px;
  margin: 4px 0 0;
  font-weight: 600;
}

.score-management__head-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

/* ===== 内联积分操作区 ===== */
.score-operator {
  background: var(--cis-surface-1);
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-card);
  padding: 16px 20px;
  margin-bottom: 20px;
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

.score-operator__option-score {
  float: right;
  color: var(--cis-text-tertiary);
  font-size: 12px;
  margin-left: 16px;
}

/* ===== 周期积分 chips ===== */
.score-operator__stats {
  display: flex;
  gap: 8px;
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid var(--cis-border-light);
  flex-wrap: wrap;
}

.score-operator__stats-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-btn);
  background: var(--cis-surface-1);
  font-size: 12px;
  color: var(--cis-text-secondary);
}

.score-operator__stats-item em {
  font-style: normal;
  font-weight: 500;
}

.score-operator__stats-item .cis-eyebrow {
  color: var(--cis-text-tertiary);
  margin-right: 2px;
}

.score-operator__stats-sep,
.score-operator__stats-eq {
  color: var(--cis-text-tertiary);
  font-size: 11px;
}

.score-operator__stats-item strong {
  font-weight: 700;
  color: var(--cis-text-primary);
}

/* ===== 快捷评价项 ===== */
.score-operator__quick {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid var(--cis-border-light);
}

.score-operator__quick-label {
  flex-shrink: 0;
  padding-top: 4px;
}

.score-operator__quick-items {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.score-operator__quick-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-btn);
  background: var(--cis-surface-1);
  color: var(--cis-text-secondary);
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  user-select: none;
  font-family: inherit;
  transition: border-color var(--cis-transition-fast), color var(--cis-transition-fast);
}

.score-operator__quick-item.is-plus {
  color: var(--cis-success);
  border-color: rgba(21, 128, 61, 0.3);
  background: var(--cis-success-tint);
}
.score-operator__quick-item.is-plus:hover {
  border-color: var(--cis-success);
}

.score-operator__quick-item.is-minus {
  color: var(--cis-accent);
  border-color: rgba(185, 28, 28, 0.3);
  background: var(--cis-accent-tint);
}
.score-operator__quick-item.is-minus:hover {
  border-color: var(--cis-accent);
}

.score-operator__quick-item-value {
  font-weight: 700;
  font-size: 12px;
}

/* ===== 主体网格 ===== */
.score-management__content {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 16px;
  align-items: start;
}

.score-management__history,
.score-management__stats-panel {
  background: var(--cis-surface-1);
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-card);
}

/* ===== 周期积分面板 ===== */
.stats-panel {
  padding: 16px;
}

.stats-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  gap: 12px;
}

.stats-panel__title {
  font-size: 14px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

/* Tabs：underline 模式 */
.stats-panel__tabs {
  display: flex;
  align-items: stretch;
  gap: 0;
  border-bottom: 1px solid var(--cis-border-light);
}

.stats-panel__tab {
  position: relative;
  padding: 4px 10px 6px;
  background: transparent;
  border: none;
  font-size: 12px;
  font-weight: 500;
  color: var(--cis-text-tertiary);
  font-family: inherit;
  cursor: pointer;
  transition: color var(--cis-transition-fast);
}

.stats-panel__tab:hover:not(.is-active) {
  color: var(--cis-text-primary);
}

.stats-panel__tab.is-active {
  color: var(--cis-primary);
}

.stats-panel__tab::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  bottom: -1px;
  height: 2px;
  background: transparent;
  transition: background-color var(--cis-transition-fast);
}

.stats-panel__tab.is-active::after {
  background: var(--cis-primary);
}

.stats-panel__body {
  display: flex;
  flex-direction: column;
  gap: 0;
  max-height: 520px;
  overflow-y: auto;
}

.stats-panel__row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 0 8px;
  min-height: 36px;
  border-bottom: 1px solid var(--cis-border-light);
  transition: background-color var(--cis-transition-fast);
}

.stats-panel__row:hover {
  background: var(--cis-primary-tint);
}

.stats-panel__row--highlight {
  background: var(--cis-primary-tint);
  box-shadow: inset 2px 0 0 var(--cis-primary);
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
  color: var(--cis-text-tertiary);
}

.stats-panel__plus {
  color: var(--cis-success);
  font-weight: 600;
}

.stats-panel__slash {
  color: var(--cis-text-tertiary);
}

.stats-panel__minus {
  color: var(--cis-accent);
  font-weight: 600;
}

.stats-panel__net {
  font-size: 14px;
  font-weight: 700;
  min-width: 40px;
  text-align: right;
  color: var(--cis-text-secondary);
}

.stats-panel__net.is-plus { color: var(--cis-success); }
.stats-panel__net.is-minus { color: var(--cis-accent); }

.stats-panel__empty {
  padding: 24px;
  text-align: center;
  font-size: 13px;
  color: var(--cis-text-tertiary);
}

@media (max-width: 960px) {
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

@media (prefers-reduced-motion: reduce) {
  .stats-panel__row,
  .stats-panel__tab::after,
  .score-operator__quick-item {
    transition: none;
  }
}
</style>
