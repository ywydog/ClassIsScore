<template>
  <div class="group-management">
    <div class="group-management__header">
      <h2>分组管理</h2>
      <el-button type="primary" @click="showAddDialog = true">
        <el-icon><Plus /></el-icon>
        创建小组
      </el-button>
    </div>

    <div class="group-management__list">
      <el-card v-for="group in groups" :key="group.id" class="group-management__card">
        <template #header>
          <div class="group-management__card-header">
            <span>{{ group.name }}</span>
            <div>
              <el-button type="primary" size="small" text @click="handleEdit(group)">编辑</el-button>
              <el-button type="danger" size="small" text @click="handleDelete(group.id)">删除</el-button>
            </div>
          </div>
        </template>
        <div class="group-management__card-content">
          <span class="group-management__member-count">成员: {{ group.studentIds.length }} 人</span>
        </div>
      </el-card>
      <el-empty v-if="groups.length === 0" description="暂无小组" />
    </div>

    <el-dialog v-model="showAddDialog" :title="editingGroup ? '编辑小组' : '创建小组'" width="480px">
      <el-form :model="groupForm" label-width="80px">
        <el-form-item label="小组名称">
          <el-input v-model="groupForm.name" placeholder="请输入小组名称" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus'
import { groupApi } from '@/services/group'
import type { StudentGroup } from '@/types'

const groups = ref<StudentGroup[]>([])
const showAddDialog = ref(false)
const editingGroup = ref<StudentGroup | null>(null)

const groupForm = reactive({
  name: '',
})

onMounted(async () => {
  await fetchGroups()
})

async function fetchGroups() {
  const response = await groupApi.getAll()
  groups.value = response.data.data
}

function handleEdit(group: StudentGroup) {
  editingGroup.value = group
  groupForm.name = group.name
  showAddDialog.value = true
}

async function handleDelete(id: string) {
  await ElMessageBox.confirm('确定删除该小组吗？', '确认', { type: 'warning' })
  await groupApi.delete(id)
  await fetchGroups()
}

async function handleSubmit() {
  if (editingGroup.value) {
    await groupApi.update(editingGroup.value.id, { name: groupForm.name })
  } else {
    await groupApi.create({ name: groupForm.name })
  }
  showAddDialog.value = false
  editingGroup.value = null
  groupForm.name = ''
  await fetchGroups()
}
</script>

<style scoped>
.group-management__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.group-management__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

.group-management__list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
}

.group-management__card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.group-management__card-content {
  font-size: 14px;
  color: var(--cis-text-secondary);
}
</style>
