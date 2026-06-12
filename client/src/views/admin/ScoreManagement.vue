<template>
  <div class="score-management">
    <div class="score-management__header">
      <h2>积分管理</h2>
      <el-button type="primary" @click="showAddDialog = true">
        <el-icon><Plus /></el-icon>
        添加积分
      </el-button>
    </div>

    <div class="score-management__content">
      <div class="score-management__panel">
        <QuickScorePanel
          :evaluation-items="evaluationItems"
          @score="handleQuickScore"
        />
      </div>

      <div class="score-management__history">
        <ScoreHistory
          :records="scoreStore.recentRecords"
          @revert="handleRevert"
        />
      </div>
    </div>

    <el-dialog v-model="showAddDialog" title="添加积分" width="480px">
      <el-form :model="addForm" label-width="80px">
        <el-form-item label="学生">
          <el-select v-model="addForm.studentId" placeholder="选择学生" filterable>
            <el-option
              v-for="s in studentStore.students"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="积分变动">
          <el-input-number v-model="addForm.scoreChange" :step="1" />
        </el-form-item>
        <el-form-item label="原因">
          <el-input v-model="addForm.reason" placeholder="请输入原因" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" @click="handleAddScore">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { useScoreStore } from '@/stores/score'
import { useStudentStore } from '@/stores/student'
import type { EvaluationItem } from '@/types'
import QuickScorePanel from '@/components/score/QuickScorePanel.vue'
import ScoreHistory from '@/components/score/ScoreHistory.vue'

const scoreStore = useScoreStore()
const studentStore = useStudentStore()

const showAddDialog = ref(false)
const evaluationItems = ref<EvaluationItem[]>([])

const addForm = reactive({
  studentId: '',
  scoreChange: 1,
  reason: '',
})

onMounted(async () => {
  await Promise.all([
    scoreStore.fetchRecords(),
    studentStore.fetchStudents(),
  ])
})

async function handleQuickScore(item: EvaluationItem) {
  if (!addForm.studentId) return
  await scoreStore.addScore(addForm.studentId, item.scoreChange, item.name)
}

async function handleAddScore() {
  if (!addForm.studentId || !addForm.reason) return
  await scoreStore.addScore(addForm.studentId, addForm.scoreChange, addForm.reason)
  showAddDialog.value = false
  addForm.studentId = ''
  addForm.scoreChange = 1
  addForm.reason = ''
}

async function handleRevert(recordId: string) {
  await scoreStore.revertScore(recordId)
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

.score-management__content {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}
</style>
