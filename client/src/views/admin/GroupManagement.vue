<template>
  <div class="group-management">
    <h2 id="group-management-title" class="group-management__header">分组管理</h2>
    <div class="group-management__toolbar">
      <el-button type="primary" @click="openCreateDialog" aria-label="创建小组">
        <el-icon aria-hidden="true"><Plus /></el-icon>
        创建小组
      </el-button>
    </div>

    <div class="group-management__list" role="list" aria-label="小组列表">
      <el-card v-for="group in groups" :key="group.id" class="group-card" role="listitem">
        <template #header>
          <div class="group-card__header">
            <div class="group-card__title">
              <el-icon color="var(--cis-primary)" aria-hidden="true"><Grid /></el-icon>
              <span>{{ group.name }}</span>
            </div>
            <div class="group-card__actions">
              <el-button type="primary" size="small" text @click="openEditDialog(group)">编辑</el-button>
              <el-button type="danger" size="small" text @click="handleDelete(group.id)">删除</el-button>
            </div>
          </div>
        </template>
        <div class="group-card__content">
          <div class="group-card__stats">
            <div class="group-card__stat">
              <span class="group-card__stat-label">成员数</span>
              <span class="group-card__stat-value" style="font-variant-numeric: tabular-nums">{{ group.studentIds.length }}</span>
            </div>
            <div class="group-card__stat">
              <span class="group-card__stat-label">小组总分</span>
              <span class="group-card__stat-value" style="font-variant-numeric: tabular-nums">{{ getGroupScore(group) }}</span>
            </div>
          </div>
          <div class="group-card__members" v-if="group.studentIds.length > 0">
            <el-tag
              v-for="sid in group.studentIds"
              :key="sid"
              size="small"
              closable
              :aria-label="`移除成员 ${getStudentName(sid)}`"
              @close="removeStudentFromGroup(group.id, sid)"
              class="group-card__member-tag"
            >
              {{ getStudentName(sid) }}
            </el-tag>
          </div>
          <div v-else class="group-card__empty">暂无成员</div>
          <el-button size="small" text type="primary" @click="openMemberDialog(group)" aria-label="添加成员">
            <el-icon aria-hidden="true"><Plus /></el-icon> 添加成员
          </el-button>
        </div>
      </el-card>
      <el-empty v-if="groups.length === 0" description="暂无小组" />
    </div>

    <!-- 创建/编辑对话框 -->
    <el-dialog v-model="showFormDialog" :title="editingGroup ? '编辑小组' : '创建小组'" width="480px">
      <el-form :model="groupForm" label-width="80px">
        <el-form-item label="小组名称" required>
          <el-input v-model="groupForm.name" placeholder="请输入小组名称…" aria-label="小组名称" autocomplete="off" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showFormDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 添加成员对话框 -->
    <el-dialog v-model="showMemberDialog" title="添加成员" width="480px">
      <el-select v-model="selectedStudentIds" multiple placeholder="选择学生…" filterable style="width: 100%" aria-label="选择学生">
        <el-option
          v-for="s in availableStudents"
          :key="s.id"
          :label="s.name"
          :value="s.id"
        />
      </el-select>
      <template #footer>
        <el-button @click="showMemberDialog = false">取消</el-button>
        <el-button type="primary" @click="handleAddMembers">添加</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Plus, Grid } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import { groupApi } from '@/services/group'
import { useStudentStore } from '@/stores/student'
import type { StudentGroup } from '@/types'

const studentStore = useStudentStore()

const groups = ref<StudentGroup[]>([])
const showFormDialog = ref(false)
const showMemberDialog = ref(false)
const editingGroup = ref<StudentGroup | null>(null)
const currentGroup = ref<StudentGroup | null>(null)
const selectedStudentIds = ref<string[]>([])

const groupForm = reactive({ name: '' })

const availableStudents = computed(() => {
  if (!currentGroup.value) return studentStore.students
  const memberIds = new Set(currentGroup.value.studentIds)
  return studentStore.students.filter(s => !memberIds.has(s.id))
})

onMounted(async () => {
  await Promise.all([
    fetchGroups(),
    studentStore.fetchStudents(),
  ])
})

async function fetchGroups() {
  try {
    const response = await groupApi.getAll()
    groups.value = response.data.data
  } catch { /* ignore */ }
}

function getStudentName(studentId: string): string {
  return studentStore.getStudentById(studentId)?.name || '未知'
}

function getGroupScore(group: StudentGroup): number {
  return group.studentIds.reduce((sum, sid) => {
    const student = studentStore.getStudentById(sid)
    return sum + (student?.score || 0)
  }, 0)
}

function openCreateDialog() {
  editingGroup.value = null
  groupForm.name = ''
  showFormDialog.value = true
}

function openEditDialog(group: StudentGroup) {
  editingGroup.value = group
  groupForm.name = group.name
  showFormDialog.value = true
}

function openMemberDialog(group: StudentGroup) {
  currentGroup.value = group
  selectedStudentIds.value = []
  showMemberDialog.value = true
}

async function handleSubmit() {
  if (!groupForm.name.trim()) {
    ElMessage.warning('请输入小组名称')
    return
  }
  try {
    if (editingGroup.value) {
      await groupApi.update(editingGroup.value.id, { name: groupForm.name })
      ElMessage.success('已更新')
    } else {
      await groupApi.create({ name: groupForm.name })
      ElMessage.success('已创建')
    }
    showFormDialog.value = false
    await fetchGroups()
  } catch { /* error handled by interceptor */ }
}

async function handleDelete(id: string) {
  await ElMessageBox.confirm('确定删除该小组吗？', '确认删除', { type: 'warning' })
  try {
    await groupApi.delete(id)
    ElMessage.success('已删除')
    await fetchGroups()
  } catch { /* ignore */ }
}

async function handleAddMembers() {
  if (!currentGroup.value || selectedStudentIds.value.length === 0) return
  try {
    for (const sid of selectedStudentIds.value) {
      await groupApi.addStudent(currentGroup.value.id, sid)
    }
    ElMessage.success('已添加成员')
    showMemberDialog.value = false
    await fetchGroups()
  } catch { /* ignore */ }
}

async function removeStudentFromGroup(groupId: string, studentId: string) {
  try {
    await groupApi.removeStudent(groupId, studentId)
    await fetchGroups()
  } catch { /* ignore */ }
}
</script>

<style scoped>
.group-management__header {
  margin: 0 0 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
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

.group-management__toolbar {
  margin-bottom: 16px;
}

.group-management__list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
}

.group-card {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.group-card:focus-within {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
}

.group-card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.group-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.group-card__title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
}

.group-card__actions {
  display: flex;
  gap: 4px;
}

.group-card__stats {
  display: flex;
  gap: 24px;
  margin-bottom: 12px;
}

.group-card__stat {
  display: flex;
  flex-direction: column;
}

.group-card__stat-label {
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.group-card__stat-value {
  font-size: 18px;
  font-weight: 700;
  background: var(--cis-gradient-primary);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.group-card__members {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 8px;
}

.group-card__member-tag {
  cursor: default;
}

.group-card__empty {
  color: var(--cis-text-tertiary);
  font-size: 13px;
  margin-bottom: 8px;
}

@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}
</style>
