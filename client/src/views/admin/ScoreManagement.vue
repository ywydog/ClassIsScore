<template>
  <div class="score-management">
    <div class="score-management__header">
      <h2>积分管理</h2>
      <div class="score-management__actions">
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
        />      </div>

      <div class="score-management__history">
        <ScoreHistory
          :records="scoreStore.recentRecords"
          @revert="handleRevert"
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus, Operation } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import { groupApi } from '@/services/group'
import type { EvaluationItem, StudentGroup } from '@/types'
import api from '@/services/api'
import QuickScorePanel from '@/components/score/QuickScorePanel.vue'
import ScoreHistory from '@/components/score/ScoreHistory.vue'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()

const showAddDialog = ref(false)
const showBatchDialog = ref(false)
const evaluationItems = ref<EvaluationItem[]>([])
const groups = ref<StudentGroup[]>([])

const addForm = reactive({
  studentId: '',
  scoreChange: 1,
  reason: '',
})

const batchForm = reactive({
  studentIds: [] as string[],
  groupId: '',
  scoreChange: 1,
  reason: '',
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
    // 使用默认评估项
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
  } catch {
    // ignore
  }
}

async function handleQuickScore(studentId: string, item: EvaluationItem) {
  try {
    await scoreStore.addScore(studentId, item.scoreChange, item.name)
    ElMessage.success(`已为 ${studentStore.getStudentById(studentId)?.name || '学生'} ${item.isPositive ? '加' : '扣'}${Math.abs(item.scoreChange)}分`)
  } catch {
    // error handled in store
  }
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
  } catch {
    // error handled in store
  }
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
  } catch {
    // error handled in store
  }
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
  } catch {
    // error handled in store
  }
}
</script>

<style scoped>
.score-management__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.score-management__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
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

@media (max-width: 900px) {
  .score-management__content {
    grid-template-columns: 1fr;
  }
}
</style>
