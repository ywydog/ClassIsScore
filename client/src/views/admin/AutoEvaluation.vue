<template>
  <div class="auto-evaluation">
    <h2 id="auto-evaluation-title" class="auto-evaluation__header">{{ t('autoEvaluation') }}</h2>

    <div class="auto-evaluation__content">
      <!-- 评估项目 Section -->
      <el-card class="auto-evaluation__card">
        <template #header>
          <div class="card-header">
            <span>{{ t('evaluationItem') }}</span>
            <el-button type="primary" size="small" @click="openItemDialog()" aria-label="添加评估项目">
              <el-icon aria-hidden="true"><Plus /></el-icon>
              添加项目
            </el-button>
          </div>
        </template>
        <el-table :data="evaluationItems" stripe empty-text="暂无评估项目">
          <el-table-column prop="name" label="名称">
            <template #default="{ row }">
              <span v-if="row.color" class="eval-color-dot" :style="{ backgroundColor: row.color }" aria-hidden="true"></span>
              {{ row.name }}
            </template>
          </el-table-column>
          <el-table-column prop="scoreChange" :label="t('scoreChange')" width="120">
            <template #default="{ row }">
              <span :class="row.scoreChange >= 0 ? 'score-positive' : 'score-negative'" style="font-variant-numeric: tabular-nums">
                {{ row.scoreChange >= 0 ? '+' : '' }}{{ row.scoreChange }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="类型" width="100">
            <template #default="{ row }">
              <el-tag :type="row.scoreChange >= 0 ? 'success' : 'danger'" size="small">
                {{ row.scoreChange >= 0 ? t('addScore') : t('subtractScore') }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="140">
            <template #default="{ row }">
              <el-button type="primary" size="small" text @click="openItemDialog(row)">编辑</el-button>
              <el-button type="danger" size="small" text @click="handleDeleteItem(row.id)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>

      <!-- 自动评估配置 Section -->
      <el-card class="auto-evaluation__card">
        <template #header>
          <div class="card-header">
            <span>自动评估配置</span>
            <el-button type="primary" size="small" @click="openConfigDialog()" aria-label="添加自动评估配置">
              <el-icon aria-hidden="true"><Plus /></el-icon>
              添加配置
            </el-button>
          </div>
        </template>
        <el-table :data="configList" stripe empty-text="暂无自动评估配置">
          <el-table-column prop="name" label="名称" min-width="120" />
          <el-table-column label="触发方式" width="120">
            <template #default="{ row }">
              <el-tag size="small" :type="triggerTagType(row.triggerType)">
                {{ triggerLabel(row.triggerType) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="触发时间" width="100">
            <template #default="{ row }">
              {{ formatTriggerTime(row) }}
            </template>
          </el-table-column>
          <el-table-column label="目标" width="120">
            <template #default="{ row }">
              {{ targetLabel(row) }}
            </template>
          </el-table-column>
          <el-table-column :label="t('scoreChange')" width="100">
            <template #default="{ row }">
              <span :class="(row.scoreChange ?? 0) >= 0 ? 'score-positive' : 'score-negative'" style="font-variant-numeric: tabular-nums">
                {{ (row.scoreChange ?? 0) >= 0 ? '+' : '' }}{{ row.scoreChange }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="reason" label="原因" min-width="120" show-overflow-tooltip />
          <el-table-column label="状态" width="80" align="center">
            <template #default="{ row }">
              <el-switch
                :model-value="row.isEnabled"
                size="small"
                :aria-label="`启用自动评估配置 ${row.name}`"
                @change="handleToggleConfig(row)"
              />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="140">
            <template #default="{ row }">
              <el-button type="primary" size="small" text @click="openConfigDialog(row)">编辑</el-button>
              <el-button type="danger" size="small" text @click="handleDeleteConfig(row.id)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </div>

    <!-- 评估项目对话框 -->
    <el-dialog v-model="showItemDialog" :title="editingItem ? t('editEvaluationItem') : t('addEvaluationItem')" width="420px">
      <el-form :model="itemForm" label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="itemForm.name" placeholder="如：回答问题…" aria-label="评估项名称" autocomplete="off" />
        </el-form-item>
        <el-form-item :label="t('scoreChange')" required>
          <el-input-number v-model="itemForm.scoreChange" :min="-100" :max="100" inputmode="numeric" aria-label="分值变化" />
        </el-form-item>
        <el-form-item label="类型">
          <el-radio-group v-model="itemForm.isPositive">
            <el-radio-button :value="true">{{ t('addScore') }}</el-radio-button>
            <el-radio-button :value="false">{{ t('subtractScore') }}</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="颜色标签">
          <div class="color-picker-row" role="radiogroup" aria-label="颜色标签">
            <button
              v-for="c in presetColors"
              :key="c.value"
              type="button"
              class="color-preset-dot"
              :class="{ 'color-preset-dot--active': itemForm.color === c.value }"
              :style="{ backgroundColor: c.value }"
              :aria-label="c.label"
              :aria-pressed="itemForm.color === c.value"
              @click="itemForm.color = itemForm.color === c.value ? '' : c.value"
              :title="c.label"
            ></button>
            <el-color-picker v-model="itemForm.color" size="small" @change="onColorPickerChange" />
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showItemDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSaveItem">保存</el-button>
      </template>
    </el-dialog>

    <!-- 自动评估配置对话框 -->
    <el-dialog v-model="showConfigDialog" :title="editingConfig ? '编辑自动评估配置' : '添加自动评估配置'" width="520px">
      <el-form :model="configForm" label-width="100px">
        <el-form-item label="配置名称" required>
          <el-input v-model="configForm.name" placeholder="如：每日签到加分…" aria-label="配置名称" autocomplete="off" />
        </el-form-item>
        <el-form-item label="触发方式" required>
          <el-select v-model="configForm.triggerType" placeholder="选择触发方式…" style="width: 100%" aria-label="触发方式">
            <el-option label="每天" value="Daily" />
            <el-option label="每周" value="Weekly" />
            <el-option label="每月" value="Monthly" />
            <el-option label="结算前" value="BeforeSettlement" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="configForm.triggerType !== 'BeforeSettlement'" label="触发时间">
          <el-time-picker
            v-model="configForm.triggerTimeObj"
            format="HH:mm"
            placeholder="选择时间…"
            style="width: 100%"
            aria-label="触发时间"
          />
        </el-form-item>
        <el-form-item v-if="configForm.triggerType === 'Weekly'" label="星期">
          <el-select v-model="configForm.dayOfWeek" placeholder="选择星期…" style="width: 100%" aria-label="星期">
            <el-option label="周一" :value="1" />
            <el-option label="周二" :value="2" />
            <el-option label="周三" :value="3" />
            <el-option label="周四" :value="4" />
            <el-option label="周五" :value="5" />
            <el-option label="周六" :value="6" />
            <el-option label="周日" :value="7" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="configForm.triggerType === 'Monthly'" label="日期">
          <el-input-number v-model="configForm.dayOfMonth" :min="1" :max="31" placeholder="几号…" style="width: 100%" inputmode="numeric" aria-label="日期" />
        </el-form-item>
        <el-form-item label="评估项目">
          <el-select
            v-model="configForm.evaluationItemId"
            placeholder="选择评估项目（可选）…"
            clearable
            style="width: 100%"
            aria-label="评估项目"
            @change="onEvaluationItemChange"
          >
            <el-option
              v-for="item in evaluationItems"
              :key="item.id"
              :label="item.name"
              :value="Number(item.id)"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('scoreChange')">
          <el-input-number v-model="configForm.scoreChange" :min="-1000" :max="1000" :precision="1" style="width: 100%" inputmode="numeric" aria-label="分值变化" />
        </el-form-item>
        <el-form-item label="原因">
          <el-input v-model="configForm.reason" placeholder="评估原因…" aria-label="原因" autocomplete="off" />
        </el-form-item>
        <el-form-item label="目标类型" required>
          <el-select v-model="configForm.targetType" placeholder="选择目标类型…" style="width: 100%" aria-label="目标类型">
            <el-option :label="t('allStudents')" value="AllStudents" />
            <el-option label="指定小组" value="SpecificGroup" />
            <el-option label="指定学生" value="SpecificStudent" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="configForm.targetType === 'SpecificGroup'" label="目标小组">
          <el-select v-model="configForm.targetGroupId" placeholder="选择小组…" style="width: 100%" aria-label="目标小组">
            <el-option
              v-for="g in groups"
              :key="g.id"
              :label="g.name"
              :value="Number(g.id)"
            />
          </el-select>
        </el-form-item>
        <el-form-item v-if="configForm.targetType === 'SpecificStudent'" label="目标学生">
          <el-select v-model="configForm.targetStudentId" placeholder="选择学生…" filterable style="width: 100%" aria-label="目标学生">
            <el-option
              v-for="s in students"
              :key="s.id"
              :label="s.name"
              :value="Number(s.id)"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="configForm.isEnabled" aria-label="启用配置" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showConfigDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSaveConfig">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { EvaluationItem, AutoEvaluationConfig, StudentGroup, Student } from '@/types'
import { invoke } from '@/services/tauri'
import { groupApi } from '@/services/group'
import { useStudentStore } from '@/stores/student'
import { useTerminology } from '@/themes/xianxia/useTerminology'

const { t } = useTerminology()
const studentStore = useStudentStore()

// ==================== 评估项目 ====================
const evaluationItems = ref<EvaluationItem[]>([])
const showItemDialog = ref(false)
const editingItem = ref<EvaluationItem | null>(null)

const itemForm = reactive({
  name: '',
  scoreChange: 1,
  isPositive: true,
  color: '',
})

const presetColors = [
  { label: '绿色', value: '#22c55e' },
  { label: '红色', value: '#ef4444' },
  { label: '蓝色', value: '#3b82f6' },
  { label: '紫色', value: '#8b5cf6' },
  { label: '橙色', value: '#f97316' },
  { label: '黄色', value: '#eab308' },
]

function onColorPickerChange(val: string | null) {
  if (!val) {
    itemForm.color = ''
  }
}

async function fetchItems() {
  try {
    // IPC 改造：evaluation_list
    const items = await invoke<EvaluationItem[]>('evaluation_list', {})
    evaluationItems.value = items || []
  } catch { /* ignore */ }
}

function openItemDialog(item?: EvaluationItem) {
  editingItem.value = item || null
  if (item) {
    itemForm.name = item.name
    itemForm.scoreChange = item.scoreChange
    itemForm.isPositive = item.scoreChange >= 0
    itemForm.color = item.color || ''
  } else {
    itemForm.name = ''
    itemForm.scoreChange = 1
    itemForm.isPositive = true
    itemForm.color = ''
  }
  showItemDialog.value = true
}

async function handleSaveItem() {
  if (!itemForm.name) {
    ElMessage.warning('请输入名称')
    return
  }
  const payload = {
    name: itemForm.name,
    scoreChange: itemForm.isPositive ? Math.abs(itemForm.scoreChange) : -Math.abs(itemForm.scoreChange),
    isPositive: itemForm.isPositive,
    color: itemForm.color || undefined,
  }
  try {
    if (editingItem.value) {
      await invoke('evaluation_update', { id: editingItem.value.id, ...payload })
    } else {
      await invoke('evaluation_create', payload)
    }
    ElMessage.success('已保存')
    showItemDialog.value = false
    await fetchItems()
  } catch { /* ignore */ }
}

async function handleDeleteItem(id: string) {
  await ElMessageBox.confirm('确定删除该评估项？', '确认', { type: 'warning' })
  try {
    await invoke('evaluation_delete', { id })
    ElMessage.success('已删除')
    await fetchItems()
  } catch { /* ignore */ }
}

// ==================== 自动评估配置 ====================
const configList = ref<AutoEvaluationConfig[]>([])
const showConfigDialog = ref(false)
const editingConfig = ref<AutoEvaluationConfig | null>(null)
const groups = ref<StudentGroup[]>([])
const students = ref<Student[]>([])

const configForm = reactive({
  name: '',
  triggerType: 'Daily' as AutoEvaluationConfig['triggerType'],
  triggerTimeObj: null as Date | null,
  dayOfWeek: null as number | null,
  dayOfMonth: null as number | null,
  evaluationItemId: null as number | null,
  scoreChange: null as number | null,
  reason: '',
  targetType: 'AllStudents' as AutoEvaluationConfig['targetType'],
  targetGroupId: null as number | null,
  targetStudentId: null as number | null,
  isEnabled: false,
})

async function fetchConfigs() {
  try {
    // IPC 改造：auto-evaluation-configs → auto_score_get_rules
    const configs = await invoke<AutoEvaluationConfig[]>('auto_score_get_rules', {})
    configList.value = configs || []
  } catch { /* ignore */ }
}

async function fetchGroups() {
  try {
    // 改用 groupApi（已走 invoke）
    const response = await groupApi.getAll()
    groups.value = response.data.data || []
  } catch { /* ignore */ }
}

async function fetchStudents() {
  try {
    // 改用 studentStore（store 内已走 invoke）
    await studentStore.fetchStudents()
    students.value = studentStore.students
  } catch { /* ignore */ }
}

function triggerLabel(type: string): string {
  const map: Record<string, string> = {
    Daily: '每天',
    Weekly: '每周',
    Monthly: '每月',
    BeforeSettlement: '结算前',
  }
  return map[type] || type
}

function triggerTagType(type: string): string {
  const map: Record<string, string> = {
    Daily: '',
    Weekly: 'success',
    Monthly: 'warning',
    BeforeSettlement: 'danger',
  }
  return map[type] || ''
}

function formatTriggerTime(row: AutoEvaluationConfig): string {
  if (row.triggerType === 'BeforeSettlement') return '—'
  if (row.triggerType === 'Weekly' && row.dayOfWeek) {
    const days = ['', '周一', '周二', '周三', '周四', '周五', '周六', '周日']
    return `${days[row.dayOfWeek] || ''} ${row.triggerTime || ''}`
  }
  if (row.triggerType === 'Monthly' && row.dayOfMonth) {
    return `${row.dayOfMonth}日 ${row.triggerTime || ''}`
  }
  return row.triggerTime || '—'
}

function targetLabel(row: AutoEvaluationConfig): string {
  if (row.targetType === 'AllStudents') return '所有学生'
  if (row.targetType === 'SpecificGroup') {
    const g = groups.value.find(g => String(g.id) === String(row.targetGroupId))
    return g ? g.name : '未知小组'
  }
  if (row.targetType === 'SpecificStudent') {
    const s = students.value.find(s => String(s.id) === String(row.targetStudentId))
    return s ? s.name : '未知学生'
  }
  return '—'
}

function onEvaluationItemChange(itemId: number | null) {
  if (!itemId) return
  const item = evaluationItems.value.find(i => Number(i.id) === itemId)
  if (item) {
    configForm.scoreChange = item.scoreChange
    configForm.reason = item.name
  }
}

function resetConfigForm() {
  configForm.name = ''
  configForm.triggerType = 'Daily'
  configForm.triggerTimeObj = null
  configForm.dayOfWeek = null
  configForm.dayOfMonth = null
  configForm.evaluationItemId = null
  configForm.scoreChange = null
  configForm.reason = ''
  configForm.targetType = 'AllStudents'
  configForm.targetGroupId = null
  configForm.targetStudentId = null
  configForm.isEnabled = false
}

function openConfigDialog(config?: AutoEvaluationConfig) {
  editingConfig.value = config || null
  if (config) {
    configForm.name = config.name
    configForm.triggerType = config.triggerType
    configForm.triggerTimeObj = config.triggerTime ? parseTime(config.triggerTime) : null
    configForm.dayOfWeek = config.dayOfWeek
    configForm.dayOfMonth = config.dayOfMonth
    configForm.evaluationItemId = config.evaluationItemId ? Number(config.evaluationItemId) : null
    configForm.scoreChange = config.scoreChange
    configForm.reason = config.reason || ''
    configForm.targetType = config.targetType
    configForm.targetGroupId = config.targetGroupId ? Number(config.targetGroupId) : null
    configForm.targetStudentId = config.targetStudentId ? Number(config.targetStudentId) : null
    configForm.isEnabled = config.isEnabled
  } else {
    resetConfigForm()
  }
  showConfigDialog.value = true
}

function parseTime(timeStr: string): Date | null {
  const parts = timeStr.split(':')
  if (parts.length < 2) return null
  const d = new Date()
  d.setHours(parseInt(parts[0], 10), parseInt(parts[1], 10), 0, 0)
  return d
}

function formatTimeObj(date: Date | null): string {
  if (!date) return ''
  const h = String(date.getHours()).padStart(2, '0')
  const m = String(date.getMinutes()).padStart(2, '0')
  return `${h}:${m}`
}

async function handleSaveConfig() {
  if (!configForm.name) {
    ElMessage.warning('请输入配置名称')
    return
  }
  const payload = {
    name: configForm.name,
    triggerType: configForm.triggerType,
    triggerTime: configForm.triggerType !== 'BeforeSettlement' ? formatTimeObj(configForm.triggerTimeObj) : null,
    dayOfWeek: configForm.triggerType === 'Weekly' ? configForm.dayOfWeek : null,
    dayOfMonth: configForm.triggerType === 'Monthly' ? configForm.dayOfMonth : null,
    evaluationItemId: configForm.evaluationItemId,
    scoreChange: configForm.scoreChange,
    reason: configForm.reason,
    targetType: configForm.targetType,
    targetGroupId: configForm.targetType === 'SpecificGroup' ? configForm.targetGroupId : null,
    targetStudentId: configForm.targetType === 'SpecificStudent' ? configForm.targetStudentId : null,
    isEnabled: configForm.isEnabled,
  }
  try {
    if (editingConfig.value) {
      await invoke('auto_score_update_rule', { id: editingConfig.value.id, ...payload })
    } else {
      await invoke('auto_score_add_rule', payload)
    }
    ElMessage.success('已保存')
    showConfigDialog.value = false
    await fetchConfigs()
  } catch { /* ignore */ }
}

async function handleToggleConfig(row: AutoEvaluationConfig) {
  try {
    // IPC 改造：auto-evaluation-configs/{id}/toggle → auto_score_toggle_rule
    await invoke('auto_score_toggle_rule', { id: row.id, enabled: !row.isEnabled })
    await fetchConfigs()
  } catch { /* ignore */ }
}

async function handleDeleteConfig(id: string) {
  await ElMessageBox.confirm('确定删除该自动评估配置？', '确认', { type: 'warning' })
  try {
    // IPC 改造：auto-evaluation-configs/{id} → auto_score_delete_rule
    await invoke('auto_score_delete_rule', { id })
    ElMessage.success('已删除')
    await fetchConfigs()
  } catch { /* ignore */ }
}

// ==================== 初始化 ====================
onMounted(async () => {
  await Promise.all([fetchItems(), fetchConfigs(), fetchGroups(), fetchStudents()])
})
</script>

<style scoped>
.auto-evaluation__header {
  margin: 0 0 24px;
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

.auto-evaluation__content {
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-width: 900px;
}

.auto-evaluation__card {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.auto-evaluation__card:focus-within {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
}

.auto-evaluation__card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
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

.eval-color-dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  margin-right: 6px;
  vertical-align: middle;
  flex-shrink: 0;
}

.color-picker-row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.color-preset-dot {
  display: inline-block;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  cursor: pointer;
  border: 2px solid transparent;
  transition: border-color var(--cis-transition-fast), transform var(--cis-transition-fast);
  padding: 0;
}

.color-preset-dot:focus-visible {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
}

.color-preset-dot:hover {
  transform: scale(1.15);
}

.color-preset-dot--active {
  border-color: var(--cis-text-primary);
  box-shadow: 0 0 0 2px var(--cis-bg);
}

@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}
</style>
