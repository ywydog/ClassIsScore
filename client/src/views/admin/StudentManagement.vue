<template>
  <div class="student-management">
    <div class="student-management__header">
      <h2>学生管理</h2>
      <div class="student-management__actions">
        <el-button type="primary" @click="showAddDialog = true">
          <el-icon><Plus /></el-icon>
          添加学生
        </el-button>
        <el-button @click="handleBatchImport">批量导入</el-button>
      </div>
    </div>

    <div class="student-management__list">
      <StudentCard
        v-for="student in studentStore.students"
        :key="student.id"
        :student="student"
        @edit="handleEdit"
        @delete="handleDelete"
      />
      <el-empty v-if="studentStore.students.length === 0" description="暂无学生" />
    </div>

    <el-dialog v-model="showAddDialog" :title="editingStudent ? '编辑学生' : '添加学生'" width="480px">
      <StudentForm
        :student="editingStudent"
        :groups="groups"
        @submit="handleFormSubmit"
        @cancel="showAddDialog = false"
      />
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus'
import { useStudentStore } from '@/stores/student'
import type { Student, StudentGroup } from '@/types'
import StudentCard from '@/components/student/StudentCard.vue'
import StudentForm from '@/components/student/StudentForm.vue'

const studentStore = useStudentStore()

const showAddDialog = ref(false)
const editingStudent = ref<Partial<Student> | undefined>(undefined)
const groups = ref<StudentGroup[]>([])

onMounted(async () => {
  await studentStore.fetchStudents()
})

function handleEdit(student: Student) {
  editingStudent.value = student
  showAddDialog.value = true
}

async function handleDelete(id: string) {
  await ElMessageBox.confirm('确定删除该学生吗？', '确认', { type: 'warning' })
  await studentStore.deleteStudent(id)
}

async function handleFormSubmit(data: Partial<Student>) {
  if (data.id) {
    await studentStore.updateStudent(data.id, data)
  } else {
    await studentStore.createStudent(data)
  }
  showAddDialog.value = false
  editingStudent.value = undefined
}

function handleBatchImport() {
  // 批量导入逻辑
}
</script>

<style scoped>
.student-management__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.student-management__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

.student-management__actions {
  display: flex;
  gap: 8px;
}

.student-management__list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 12px;
}
</style>
