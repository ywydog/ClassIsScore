<template>
  <div class="m-students">
    <header class="m-students__head">
      <span class="cis-eyebrow">Students</span>
      <h1 class="cis-display m-students__title">学生</h1>
    </header>

    <div class="m-students__search">
      <el-input
        v-model="search"
        placeholder="搜索学生…"
        clearable
        aria-label="搜索学生"
        autocomplete="off"
      >
        <template #prefix>
          <el-icon :size="16" aria-hidden="true"><Search /></el-icon>
        </template>
      </el-input>
      <el-select v-model="sortBy" aria-label="排序方式" class="m-students__sort">
        <el-option value="score-desc" label="分数↓" />
        <el-option value="score-asc" label="分数↑" />
        <el-option value="name-asc" label="姓名 A→Z" />
        <el-option value="name-desc" label="姓名 Z→A" />
      </el-select>
      <button
        type="button"
        class="m-students__export-btn"
        aria-label="导出学生"
        @click="showExportSheet = true"
      >
        <el-icon :size="16" aria-hidden="true"><Download /></el-icon>
      </button>
    </div>

    <ul v-if="filteredStudents.length > 0" class="m-students__list" role="list">
      <li v-for="s in filteredStudents" :key="s.id">
        <router-link
          :to="`/m/students/${s.id}`"
          class="m-students__row"
          :aria-label="`${s.name}，${s.score}分`"
        >
          <div class="m-students__avatar" aria-hidden="true">
            <span class="m-students__avatar-text">{{ (s.name || '?').slice(0, 1) }}</span>
          </div>
          <div class="m-students__body">
            <span class="m-students__name">{{ s.name }}</span>
            <span class="m-students__id">{{ s.studentNumber || '—' }}</span>
          </div>
          <span class="m-students__score cis-num">{{ s.score }}</span>
          <el-icon :size="14" class="m-students__chevron" aria-hidden="true"><ArrowRight /></el-icon>
        </router-link>
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" :description="search ? '没有匹配的学生' : '暂无学生'" />

    <button
      type="button"
      class="m-students__fab"
      aria-label="添加学生"
      @click="openCreateDialog"
    >
      <el-icon :size="22" aria-hidden="true"><Plus /></el-icon>
    </button>

    <el-dialog
      v-model="createOpen"
      title="添加学生"
      width="320px"
      :close-on-click-modal="false"
      :show-close="true"
      class="m-students__dialog"
      @close="resetForm"
    >
      <el-form label-position="top" @submit.prevent>
        <el-form-item label="姓名" required>
          <el-input
            v-model="form.name"
            placeholder="例：张三"
            maxlength="20"
            show-word-limit
            autocomplete="off"
            aria-label="学生姓名"
            @keyup.enter="submitCreate"
          />
        </el-form-item>
        <el-form-item label="学号">
          <el-input
            v-model="form.studentNumber"
            placeholder="例：20240001（选填）"
            maxlength="20"
            autocomplete="off"
            aria-label="学号"
            @keyup.enter="submitCreate"
          />
        </el-form-item>
        <el-form-item label="分组">
          <el-select v-model="form.groupId" placeholder="未分组" clearable aria-label="所属分组" class="m-students__group-select" @keyup.enter="submitCreate">
            <el-option
              v-for="g in groups"
              :key="g.id"
              :label="g.name"
              :value="g.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="m-students__dialog-actions">
          <el-button @click="createOpen = false">取消</el-button>
          <el-button
            type="primary"
            :loading="submitting"
            :disabled="!form.name.trim()"
            @click="submitCreate"
          >添加</el-button>
        </div>
      </template>
    </el-dialog>

    <BottomSheet v-model="showExportSheet" title="导出学生" height="auto">
      <p class="m-students__sheet-hint">选择导出格式与范围。</p>
      <div class="m-students__sheet-section">
        <span class="cis-eyebrow m-students__sheet-label">格式</span>
        <el-radio-group v-model="exportForm.format" aria-label="导出格式" class="m-students__sheet-radio">
          <el-radio-button value="xlsx">Excel</el-radio-button>
          <el-radio-button value="csv">CSV</el-radio-button>
        </el-radio-group>
      </div>
      <div class="m-students__sheet-section">
        <span class="cis-eyebrow m-students__sheet-label">范围</span>
        <el-radio-group v-model="exportForm.scope" aria-label="导出范围" class="m-students__sheet-radio">
          <el-radio-button value="all">全部学生</el-radio-button>
          <el-radio-button value="filtered">当前筛选</el-radio-button>
        </el-radio-group>
      </div>
      <div class="m-students__sheet-actions">
        <el-button @click="showExportSheet = false">取消</el-button>
        <el-button type="primary" @click="handleExport">导出</el-button>
      </div>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Search, ArrowRight, Plus, Download } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import { useStudentStore } from '@/stores/student'
import { groupApi } from '@/services/group'
import { exportToExcel, exportToCSV } from '@/utils/excelHelper'
import type { StudentGroup, Student } from '@/types'

const studentStore = useStudentStore()
const search = ref('')
const sortBy = ref<'score-desc' | 'score-asc' | 'name-asc' | 'name-desc'>('score-desc')

const createOpen = ref(false)
const submitting = ref(false)
const form = reactive({ name: '', studentNumber: '', groupId: '' })
const groups = ref<StudentGroup[]>([])

const showExportSheet = ref(false)
const exportForm = reactive({
  format: 'xlsx' as 'xlsx' | 'csv',
  scope: 'all' as 'all' | 'filtered',
})

onMounted(async () => {
  await studentStore.fetchStudents()
  try {
    const res = await groupApi.getAll()
    groups.value = res.data.data
  } catch {
    groups.value = []
  }
})

const filteredStudents = computed(() => {
  const q = search.value.trim().toLowerCase()
  let list = studentStore.students
  if (q) list = list.filter(s => s.name.toLowerCase().includes(q) || s.studentNumber?.toLowerCase().includes(q))
  list = [...list]
  switch (sortBy.value) {
    case 'score-desc': list.sort((a, b) => b.score - a.score); break
    case 'score-asc': list.sort((a, b) => a.score - b.score); break
    case 'name-asc': list.sort((a, b) => a.name.localeCompare(b.name, 'zh-CN')); break
    case 'name-desc': list.sort((a, b) => b.name.localeCompare(a.name, 'zh-CN')); break
  }
  return list
})

function getGroupName(groupId?: string): string {
  if (!groupId) return ''
  return groups.value.find(g => g.id === groupId)?.name || ''
}

function openCreateDialog() {
  form.name = ''
  form.studentNumber = ''
  form.groupId = ''
  createOpen.value = true
}

function resetForm() {
  form.name = ''
  form.studentNumber = ''
  form.groupId = ''
}

async function submitCreate() {
  // 入口清空残留 toast，避免堆叠遮住表单
  ElMessage.closeAll()

  const name = form.name.trim()
  if (!name) {
    ElMessage.warning('请填写学生姓名')
    return
  }

  // 二次防御：正在提交时直接吞掉
  if (submitting.value) return
  submitting.value = true

  try {
    await studentStore.createStudent({
      name,
      studentNumber: form.studentNumber.trim() || undefined,
      groupId: form.groupId || undefined,
    })
    ElMessage.success(`已添加 ${name}`)
    createOpen.value = false
    resetForm()
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err)
    console.error('[StudentManagement] createStudent failed', err)
    ElMessage.error(`添加失败：${msg || '未知错误'}`)
  } finally {
    submitting.value = false
  }
}

function handleExport() {
  const sourceStudents: Student[] = exportForm.scope === 'filtered'
    ? filteredStudents.value
    : studentStore.students

  if (sourceStudents.length === 0) {
    ElMessage.warning('没有可导出的学生')
    return
  }

  const columns = [
    { header: '姓名', key: 'name' },
    { header: '学号', key: 'studentNumber' },
    { header: '小组', key: 'groupName' },
    { header: '积分', key: 'score' },
  ]

  const data = sourceStudents.map(s => ({
    name: s.name,
    studentNumber: s.studentNumber || '',
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
  showExportSheet.value = false
}
</script>

<style scoped>
.m-students { display: flex; flex-direction: column; gap: 16px; padding-bottom: 96px; }
.m-students__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-students__search { display: flex; gap: 8px; }
.m-students__search .el-input { flex: 1; }
.m-students__sort { width: 120px; }
.m-students__export-btn { display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; flex-shrink: 0; }
.m-students__export-btn:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-students__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-students__row { display: flex; align-items: center; gap: 12px; min-height: 60px; padding: 10px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); color: var(--cis-text-primary); text-decoration: none; -webkit-tap-highlight-color: transparent; }
.m-students__list li:last-child .m-students__row { border-bottom: none; }
.m-students__row:active { background: var(--cis-primary-tint); }
.m-students__avatar { width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; flex-shrink: 0; }
.m-students__avatar-text { font-family: var(--cis-font-serif); font-size: 15px; font-weight: 600; line-height: 1; }
.m-students__body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.m-students__name { font-size: 15px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.m-students__id { font-size: 12px; color: var(--cis-text-tertiary); font-family: var(--cis-font-mono); }
.m-students__score { font-family: var(--cis-font-mono); font-size: 16px; font-weight: 700; font-variant-numeric: tabular-nums; color: var(--cis-text-primary); flex-shrink: 0; }
.m-students__chevron { color: var(--cis-text-tertiary); flex-shrink: 0; }

/* FAB：右下浮动按钮，避开底部 nav */
.m-students__fab {
  position: fixed;
  right: 16px;
  bottom: calc(80px + env(safe-area-inset-bottom, 0));
  z-index: 100;
  width: 56px;
  height: 56px;
  border: none;
  border-radius: 9999px;
  background: var(--cis-primary);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3);
  -webkit-tap-highlight-color: transparent;
}
.m-students__fab:active {
  transform: scale(var(--cis-press-scale-strong));
}

.m-students__group-select { width: 100%; }

.m-students__dialog-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  width: 100%;
}

.m-students__sheet-hint { margin: 0 0 16px; font-size: 13px; color: var(--cis-text-tertiary); }
.m-students__sheet-section { display: flex; flex-direction: column; gap: 8px; margin-bottom: 16px; }
.m-students__sheet-label { color: var(--cis-text-tertiary); }
.m-students__sheet-radio { display: flex; }
.m-students__sheet-actions { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
</style>
