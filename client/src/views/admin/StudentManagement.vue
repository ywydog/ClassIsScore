<template>
  <div class="student-management">
    <div class="student-management__header">
      <h2 id="student-management-title" class="student-management__title">学生管理</h2>
      <div class="student-management__actions">
        <el-input
          v-model="searchText"
          placeholder="搜索学生…"
          clearable
          style="width: 200px"
          :prefix-icon="Search"
          role="searchbox"
          aria-label="搜索学生"
        />
        <el-button @click="showExportDialog = true" aria-label="导出">
          <el-icon aria-hidden="true"><Download /></el-icon>
          导出
        </el-button>
        <el-button @click="showImportDialog = true" aria-label="导入">
          <el-icon aria-hidden="true"><Upload /></el-icon>
          导入
        </el-button>
        <el-button type="primary" @click="openAddDialog()" aria-label="添加学生">
          <el-icon aria-hidden="true"><Plus /></el-icon>
          添加学生
        </el-button>
      </div>
    </div>

    <el-card class="student-management__table-card">
      <el-skeleton v-if="loading" :loading="loading" :rows="5" animated />
      <el-table
        v-else
        :data="paginatedStudents"
        stripe
        style="width: 100%"
        v-loading="studentStore.loading"
        empty-text="暂无学生数据"
        aria-label="学生列表"
      >
        <el-table-column prop="name" label="姓名" width="120" />
        <el-table-column prop="studentNumber" label="学号" width="120">
          <template #default="{ row }">
            {{ row.studentNumber || '—' }}
          </template>
        </el-table-column>
        <el-table-column prop="gender" label="性别" width="80">
          <template #default="{ row }">
            {{ row.gender || '—' }}
          </template>
        </el-table-column>
        <el-table-column label="小组" width="120">
          <template #default="{ row }">
            <el-tag v-if="getGroupName(row.groupId)" size="small">{{ getGroupName(row.groupId) }}</el-tag>
            <span v-else style="color: var(--cis-text-tertiary)">未分组</span>
          </template>
        </el-table-column>
        <el-table-column prop="score" label="积分" width="100" sortable>
          <template #default="{ row }">
            <span :class="row.score > 0 ? 'score-positive' : row.score < 0 ? 'score-negative' : ''" style="font-variant-numeric: tabular-nums">
              {{ row.score }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button type="success" size="small" text @click="viewProfile(row.id)">查看</el-button>
            <el-button type="primary" size="small" text @click="openEditDialog(row)">编辑</el-button>
            <el-button type="danger" size="small" text @click="handleDelete(row.id)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="student-management__pagination" v-if="filteredStudents.length > pageSize">
        <el-pagination
          v-model:current-page="currentPage"
          :page-size="pageSize"
          :total="filteredStudents.length"
          layout="total, prev, pager, next"
          small
          aria-label="学生列表分页"
        />
      </div>
    </el-card>

    <!-- 添加/编辑对话框 -->
    <el-dialog v-model="showFormDialog" :title="editingStudent ? '编辑学生' : '添加学生'" width="480px" destroy-on-close>
      <StudentForm
        :student="editingStudent"
        :groups="groups"
        @submit="handleFormSubmit"
        @cancel="showFormDialog = false"
      />
    </el-dialog>

    <!-- 导入对话框 -->
    <el-dialog v-model="showImportDialog" title="导入学生" width="600px" destroy-on-close>
      <!-- 步骤1: 选择导入方式 -->
      <div v-if="importStep === 0">
        <div class="import-methods">
          <div
            class="import-method"
            role="button"
            tabindex="0"
            aria-label="从文件导入"
            @click="importMode = 'file'; importStep = 1"
            @keydown.enter="importMode = 'file'; importStep = 1"
            @keydown.space.prevent="importMode = 'file'; importStep = 1"
          >
            <el-icon :size="32" color="var(--cis-primary)" aria-hidden="true"><Document /></el-icon>
            <div class="import-method__title">从文件导入</div>
            <div class="import-method__desc">支持 Excel (.xlsx/.xls) 和 CSV 文件</div>
          </div>
          <div
            class="import-method"
            role="button"
            tabindex="0"
            aria-label="手动输入"
            @click="importMode = 'text'; importStep = 1"
            @keydown.enter="importMode = 'text'; importStep = 1"
            @keydown.space.prevent="importMode = 'text'; importStep = 1"
          >
            <el-icon :size="32" color="var(--cis-warning)" aria-hidden="true"><EditPen /></el-icon>
            <div class="import-method__title">手动输入</div>
            <div class="import-method__desc">逐行输入学生信息</div>
          </div>
        </div>
      </div>

      <!-- 步骤2a: 文件导入 -->
      <div v-else-if="importStep === 1 && importMode === 'file'">
        <el-upload
          drag
          :auto-upload="false"
          :limit="1"
          accept=".xlsx,.xls,.csv"
          :on-change="handleFileChange"
          :on-remove="() => importFile = null"
        >
          <el-icon :size="40" style="color: var(--cis-text-tertiary)" aria-hidden="true"><Upload /></el-icon>
          <div style="margin-top: 8px">拖拽文件到此处，或 <em>点击选择文件</em></div>
          <template #tip>
            <div style="font-size: 12px; color: var(--cis-text-tertiary); margin-top: 8px">
              表格需包含姓名列，可选学号、性别、小组名列
            </div>
          </template>
        </el-upload>
      </div>

      <!-- 步骤2b: 手动输入 -->
      <div v-else-if="importStep === 1 && importMode === 'text'">
        <div class="import-hint">
          <p>请按以下格式输入，每行一个学生：</p>
          <code>姓名,学号,性别,小组名</code>
          <p style="margin-top: 4px; font-size: 12px; color: var(--cis-text-tertiary)">学号、性别、小组名为可选项</p>
        </div>
        <el-input
          v-model="importText"
          type="textarea"
          :rows="8"
          placeholder="张三,2024001,男,第一组&#10;李四,2024002,女,第一组"
          aria-label="学生数据"
        />
      </div>

      <!-- 步骤3: 列映射（仅文件导入） -->
      <div v-else-if="importStep === 2">
        <el-alert type="info" :closable="false" style="margin-bottom: 16px">
          请为每个字段选择对应的表格列
        </el-alert>
        <el-form label-width="80px">
          <el-form-item label="姓名列" required>
            <el-select v-model="importMapping.nameColumnIndex" placeholder="选择姓名列…" style="width: 100%" aria-label="姓名列">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
          <el-form-item label="学号列">
            <el-select v-model="importMapping.numberColumnIndex" placeholder="可选…" clearable style="width: 100%" aria-label="学号列">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
          <el-form-item label="性别列">
            <el-select v-model="importMapping.genderColumnIndex" placeholder="可选…" clearable style="width: 100%" aria-label="性别列">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
          <el-form-item label="小组列">
            <el-select v-model="importMapping.groupColumnIndex" placeholder="可选…" clearable style="width: 100%" aria-label="小组列">
              <el-option v-for="(h, i) in importHeaders" :key="i" :label="h" :value="i" />
            </el-select>
          </el-form-item>
        </el-form>

        <div v-if="importPreviewStudents.length > 0" style="margin-top: 16px">
          <div style="font-size: 13px; font-weight: 600; margin-bottom: 8px; color: var(--cis-text-primary)">
            预览 (前 5 条)
          </div>
          <el-table :data="importPreviewStudents.slice(0, 5)" size="small" border aria-label="导入预览">
            <el-table-column prop="name" label="姓名" width="100" />
            <el-table-column prop="studentNumber" label="学号" width="100" />
            <el-table-column prop="gender" label="性别" width="60" />
            <el-table-column prop="groupName" label="小组" />
          </el-table>
          <div style="margin-top: 8px; font-size: 12px; color: var(--cis-text-tertiary)">
            共 {{ importPreviewStudents.length }} 条记录
          </div>
        </div>
      </div>

      <!-- 步骤4: 导入结果 -->
      <div v-else-if="importStep === 3">
        <el-result :icon="importResult.failCount === 0 ? 'success' : 'warning'" :title="importResultTitle">
          <template #sub-title>
            <p>成功: <span style="font-variant-numeric: tabular-nums">{{ importResult.successCount }}</span> 条</p>
            <p v-if="importResult.skipCount > 0">跳过 (姓名为空): <span style="font-variant-numeric: tabular-nums">{{ importResult.skipCount }}</span> 条</p>
            <p v-if="importResult.failCount > 0">失败: <span style="font-variant-numeric: tabular-nums">{{ importResult.failCount }}</span> 条</p>
          </template>
        </el-result>
      </div>

      <template #footer>
        <el-button v-if="importStep > 0 && importStep < 3" @click="importStep--">上一步</el-button>
        <el-button v-if="importStep === 1 && importMode === 'text'" type="primary" :disabled="!importText.trim()" :aria-disabled="!importText.trim()" @click="handleTextImport">
          导入
        </el-button>
        <el-button v-if="importStep === 1 && importMode === 'file'" :disabled="!importFile" :aria-disabled="!importFile" type="primary" @click="handleFileParse">
          下一步
        </el-button>
        <el-button v-if="importStep === 2" type="primary" :disabled="importPreviewStudents.length === 0" :aria-disabled="importPreviewStudents.length === 0" @click="handleFileImport">
          确认导入 ({{ importPreviewStudents.length }}条)
        </el-button>
        <el-button v-if="importStep === 3" type="primary" @click="closeImportDialog">完成</el-button>
      </template>
    </el-dialog>

    <!-- 导出对话框 -->
    <el-dialog v-model="showExportDialog" title="导出学生列表" width="420px">
      <el-form :model="exportForm" label-width="80px">
        <el-form-item label="导出格式">
          <el-radio-group v-model="exportForm.format" aria-label="导出格式">
            <el-radio-button value="xlsx">Excel</el-radio-button>
            <el-radio-button value="csv">CSV</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="导出范围">
          <el-radio-group v-model="exportForm.scope" aria-label="导出范围">
            <el-radio-button value="all">全部学生</el-radio-button>
            <el-radio-button value="filtered">当前筛选</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showExportDialog = false">取消</el-button>
        <el-button type="primary" @click="handleExport">导出</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, Upload, Download, Search, Document, EditPen } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import type { UploadFile } from 'element-plus'
import { useStudentStore } from '@/stores/student'
import { groupApi } from '@/services/group'
import type { Student, StudentGroup } from '@/types'
import { readExcelFile, exportToExcel, exportToCSV } from '@/utils/excelHelper'
import StudentForm from '@/components/student/StudentForm.vue'

const router = useRouter()
const studentStore = useStudentStore()

const searchText = ref('')
const showFormDialog = ref(false)
const showImportDialog = ref(false)
const showExportDialog = ref(false)
const editingStudent = ref<Partial<Student> | undefined>(undefined)
const groups = ref<StudentGroup[]>([])
const importText = ref('')
const currentPage = ref(1)
const pageSize = 20
const loading = ref(true)

// 导入相关状态
const importStep = ref(0)
const importMode = ref<'file' | 'text'>('file')
const importFile = ref<File | null>(null)
const importHeaders = ref<string[]>([])
const importRows = ref<string[][]>([])
const importMapping = reactive({
  nameColumnIndex: -1,
  numberColumnIndex: -1,
  genderColumnIndex: -1,
  groupColumnIndex: -1,
})
const importResult = reactive({ successCount: 0, skipCount: 0, failCount: 0 })

interface ImportPreviewStudent {
  name: string
  studentNumber: string
  gender: string
  groupName: string
}

const importPreviewStudents = ref<ImportPreviewStudent[]>([])

const importResultTitle = computed(() => {
  if (importResult.failCount === 0) return '导入完成'
  return '导入完成（部分失败）'
})

// 导出相关状态
const exportForm = reactive({
  format: 'xlsx' as 'xlsx' | 'csv',
  scope: 'all' as 'all' | 'filtered',
})

const filteredStudents = computed(() => {
  if (!searchText.value) return studentStore.students
  const keyword = searchText.value.toLowerCase()
  return studentStore.students.filter(s =>
    s.name.toLowerCase().includes(keyword) ||
    (s.studentNumber && s.studentNumber.includes(keyword))
  )
})

const paginatedStudents = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filteredStudents.value.slice(start, start + pageSize)
})

onMounted(async () => {
  loading.value = true
  await Promise.all([
    studentStore.fetchStudents(),
    fetchGroups(),
  ])
  loading.value = false
})

async function fetchGroups() {
  try {
    const response = await groupApi.getAll()
    groups.value = response.data.data
  } catch { /* ignore */ }
}

function getGroupName(groupId?: string): string {
  if (!groupId) return ''
  return groups.value.find(g => g.id === groupId)?.name || ''
}

function openAddDialog() {
  editingStudent.value = undefined
  showFormDialog.value = true
}

function viewProfile(studentId: string) {
  router.push(`/admin/students/${studentId}`)
}

function openEditDialog(student: Student) {
  editingStudent.value = student
  showFormDialog.value = true
}

async function handleDelete(id: string) {
  try {
    await ElMessageBox.confirm('确定删除该学生吗？删除后积分记录将保留。', '确认删除', { type: 'warning' })
  } catch {
    return
  }
  try {
    await studentStore.deleteStudent(id)
    ElMessage.success('已删除')
  } catch { /* error handled in store */ }
}

async function handleFormSubmit(data: Partial<Student>) {
  try {
    if (data.id) {
      await studentStore.updateStudent(data.id, data)
      ElMessage.success('已更新')
    } else {
      await studentStore.createStudent(data)
      ElMessage.success('已添加')
    }
    showFormDialog.value = false
    editingStudent.value = undefined
  } catch { /* error handled in store */ }
}

// ========== 导入功能 ==========

async function handleFileChange(file: UploadFile) {
  if (file.raw) {
    importFile.value = file.raw
  }
}

async function handleFileParse() {
  if (!importFile.value) return
  try {
    const { headers, rows } = await readExcelFile(importFile.value)
    importHeaders.value = headers
    importRows.value = rows

    // 自动匹配列
    importMapping.nameColumnIndex = headers.findIndex(h => /姓名|名字|学生/.test(h))
    importMapping.numberColumnIndex = headers.findIndex(h => /学号|编号/.test(h))
    importMapping.genderColumnIndex = headers.findIndex(h => /性别/.test(h))
    importMapping.groupColumnIndex = headers.findIndex(h => /小组|组别|分组|班级/.test(h))

    importStep.value = 2
    updateImportPreview()
  } catch (err) {
    ElMessage.error('文件读取失败: ' + (err as Error).message)
  }
}

function updateImportPreview() {
  if (importMapping.nameColumnIndex < 0) {
    importPreviewStudents.value = []
    return
  }

  const entries: ImportPreviewStudent[] = importRows.value.map(row => ({
    name: row[importMapping.nameColumnIndex] || '',
    studentNumber: importMapping.numberColumnIndex >= 0 ? row[importMapping.numberColumnIndex] : '',
    gender: importMapping.genderColumnIndex >= 0 ? row[importMapping.genderColumnIndex] : '',
    groupName: importMapping.groupColumnIndex >= 0 ? row[importMapping.groupColumnIndex] : '',
  })).filter(e => e.name)

  importPreviewStudents.value = entries
}

watch(() => [
  importMapping.nameColumnIndex,
  importMapping.numberColumnIndex,
  importMapping.genderColumnIndex,
  importMapping.groupColumnIndex,
], () => {
  if (importStep.value === 2) {
    updateImportPreview()
  }
})

async function handleFileImport() {
  const entries = importPreviewStudents.value
  let successCount = 0
  let skipCount = 0
  let failCount = 0

  const students: Partial<Student>[] = entries.map(e => ({
    name: e.name,
    studentNumber: e.studentNumber || undefined,
    gender: e.gender || undefined,
  })).filter(s => {
    if (!s.name) { skipCount++; return false }
    return true
  })

  try {
    await studentStore.batchCreateStudent(students)
    successCount = students.length
  } catch {
    failCount = students.length
  }

  importResult.successCount = successCount
  importResult.skipCount = skipCount
  importResult.failCount = failCount
  importStep.value = 3

  await studentStore.fetchStudents()
}

async function handleTextImport() {
  if (!importText.value.trim()) {
    ElMessage.warning('请输入学生数据')
    return
  }
  const lines = importText.value.trim().split('\n').filter(l => l.trim())
  const students = lines.map(line => {
    const parts = line.split(',').map(s => s.trim())
    return {
      name: parts[0] || '',
      studentNumber: parts[1] || undefined,
      gender: parts[2] || undefined,
    } as Partial<Student>
  }).filter(s => s.name)

  if (students.length === 0) {
    ElMessage.warning('未解析到有效数据')
    return
  }

  try {
    await studentStore.batchCreateStudent(students)
    ElMessage.success(`成功导入 ${students.length} 名学生`)
    showImportDialog.value = false
    importText.value = ''
    importStep.value = 0
    await studentStore.fetchStudents()
  } catch { /* error handled in store */ }
}

function closeImportDialog() {
  showImportDialog.value = false
  importStep.value = 0
  importFile.value = null
  importHeaders.value = []
  importRows.value = []
  importPreviewStudents.value = []
  importText.value = ''
  importMapping.nameColumnIndex = -1
  importMapping.numberColumnIndex = -1
  importMapping.genderColumnIndex = -1
  importMapping.groupColumnIndex = -1
}

// ========== 导出功能 ==========

function handleExport() {
  const sourceStudents = exportForm.scope === 'filtered' ? filteredStudents.value : studentStore.students

  if (sourceStudents.length === 0) {
    ElMessage.warning('没有可导出的学生')
    return
  }

  const columns = [
    { header: '姓名', key: 'name' },
    { header: '学号', key: 'studentNumber' },
    { header: '性别', key: 'gender' },
    { header: '小组', key: 'groupName' },
    { header: '积分', key: 'score' },
  ]

  const data = sourceStudents.map(s => ({
    name: s.name,
    studentNumber: s.studentNumber || '',
    gender: s.gender || '',
    groupName: getGroupName(s.groupId),
    score: s.score,
  }))

  const filename = `学生列表_${new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())}`

  if (exportForm.format === 'xlsx') {
    exportToExcel(data, columns, filename)
  } else {
    exportToCSV(data, columns, filename)
  }

  ElMessage.success(`已导出 ${sourceStudents.length} 名学生`)
  showExportDialog.value = false
}
</script>

<style scoped>
.student-management__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
  flex-wrap: wrap;
  gap: 12px;
}

.student-management__title {
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
  scroll-margin-top: 80px;
}

.student-management__actions {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.student-management__table-card {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.student-management__table-card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.student-management__pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
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

/* 导入方式选择 */
.import-methods {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.import-method {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 24px 16px;
  background: var(--cis-bg-secondary);
  border-radius: var(--cis-radius-lg);
  border: 2px solid var(--cis-border-color-light);
  cursor: pointer;
  transition: border-color var(--cis-transition-fast), background-color var(--cis-transition-fast), transform var(--cis-transition-fast), box-shadow var(--cis-transition-fast);
}

.import-method:focus-visible {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
}

.import-method:hover {
  border-color: var(--cis-primary);
  background: var(--cis-primary-light-9);
  transform: translateY(-2px);
  box-shadow: var(--cis-shadow-glow);
}

.import-method__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.import-method__desc {
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.import-hint {
  margin-bottom: 16px;
  padding: 12px;
  background-color: var(--cis-bg-secondary);
  border-radius: var(--cis-radius-md);
  font-size: 13px;
  color: var(--cis-text-secondary);
}

.import-hint code {
  display: block;
  margin-top: 4px;
  padding: 4px 8px;
  background-color: var(--cis-card-bg);
  border-radius: var(--cis-radius-sm);
  font-size: 12px;
}

.import-hint p {
  margin: 0;
}

@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}
</style>
