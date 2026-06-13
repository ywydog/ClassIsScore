<template>
  <div class="student-management">
    <div class="student-management__header">
      <h2>学生管理</h2>
      <div class="student-management__actions">
        <el-input
          v-model="searchText"
          placeholder="搜索学生..."
          clearable
          style="width: 200px"
          :prefix-icon="Search"
        />
        <el-button @click="showImportDialog = true">
          <el-icon><Upload /></el-icon>
          批量导入
        </el-button>
        <el-button type="primary" @click="openAddDialog()">
          <el-icon><Plus /></el-icon>
          添加学生
        </el-button>
      </div>
    </div>

    <el-card class="student-management__table-card">
      <el-table
        :data="filteredStudents"
        stripe
        style="width: 100%"
        v-loading="studentStore.loading"
        empty-text="暂无学生数据"
      >
        <el-table-column prop="name" label="姓名" width="120" />
        <el-table-column prop="studentNumber" label="学号" width="120">
          <template #default="{ row }">
            {{ row.studentNumber || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="gender" label="性别" width="80">
          <template #default="{ row }">
            {{ row.gender || '-' }}
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
            <span :class="row.score > 0 ? 'score-positive' : row.score < 0 ? 'score-negative' : ''">
              {{ row.score }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" size="small" text @click="openEditDialog(row)">编辑</el-button>
            <el-button type="danger" size="small" text @click="handleDelete(row.id)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
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

    <!-- 批量导入对话框 -->
    <el-dialog v-model="showImportDialog" title="批量导入学生" width="520px">
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
      />
      <template #footer>
        <el-button @click="showImportDialog = false">取消</el-button>
        <el-button type="primary" @click="handleBatchImport">导入</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Plus, Upload, Search } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import { useStudentStore } from '@/stores/student'
import { groupApi } from '@/services/group'
import type { Student, StudentGroup } from '@/types'
import StudentForm from '@/components/student/StudentForm.vue'

const studentStore = useStudentStore()

const searchText = ref('')
const showFormDialog = ref(false)
const showImportDialog = ref(false)
const editingStudent = ref<Partial<Student> | undefined>(undefined)
const groups = ref<StudentGroup[]>([])
const importText = ref('')

const filteredStudents = computed(() => {
  if (!searchText.value) return studentStore.students
  const keyword = searchText.value.toLowerCase()
  return studentStore.students.filter(s =>
    s.name.toLowerCase().includes(keyword) ||
    (s.studentNumber && s.studentNumber.includes(keyword))
  )
})

onMounted(async () => {
  await Promise.all([
    studentStore.fetchStudents(),
    fetchGroups(),
  ])
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

function openEditDialog(student: Student) {
  editingStudent.value = student
  showFormDialog.value = true
}

async function handleDelete(id: string) {
  await ElMessageBox.confirm('确定删除该学生吗？删除后积分记录将保留。', '确认删除', { type: 'warning' })
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

async function handleBatchImport() {
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
  } catch { /* error handled in store */ }
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
}

.student-management__header h2 {
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

.student-management__actions {
  display: flex;
  gap: 8px;
  align-items: center;
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
</style>
