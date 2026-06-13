<template>
  <div class="settings-page">
    <div class="settings-page__header">
      <h2>设置</h2>
    </div>

    <el-tabs v-model="activeTab" class="settings-page__tabs">
      <el-tab-pane label="通用" name="general">
        <el-card class="settings-page__card">
          <el-form label-width="100px">
            <el-form-item label="主题">
              <el-select v-model="settings.theme" @change="handleThemeChange">
                <el-option label="浅色" value="light" />
                <el-option label="深色" value="dark" />
                <el-option label="跟随系统" value="system" />
              </el-select>
            </el-form-item>
            <el-form-item label="字体大小">
              <el-slider v-model="settings.fontSize" :min="12" :max="20" :step="1" show-input @change="handleFontSizeChange" />
            </el-form-item>
            <el-form-item label="字体">
              <el-select v-model="settings.fontFamily" @change="handleFontFamilyChange" placeholder="选择字体">
                <el-option label="系统默认" value="system" />
                <el-option label="LXGW WenKai" value="LXGW WenKai" />
                <el-option label="HarmonyOS Sans SC" value="HarmonyOS Sans SC" />
                <el-option label="Noto Sans CJK SC" value="Noto Sans CJK SC" />
                <el-option label="Microsoft YaHei" value="Microsoft YaHei" />
                <el-option label="SimHei" value="SimHei" />
              </el-select>
            </el-form-item>
            <el-form-item label="显示模式">
              <el-radio-group v-model="settings.displayMode" @change="handleSave">
                <el-radio-button value="Card">卡片</el-radio-button>
                <el-radio-button value="Circle">圆形</el-radio-button>
                <el-radio-button value="Pet">宠物</el-radio-button>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="主题色">
              <el-color-picker v-model="settings.customAccentColor" @change="handleSave" />
            </el-form-item>
            <el-form-item label="开学日期">
              <el-date-picker
                v-model="settings.semesterStartDate"
                type="date"
                placeholder="选择开学日期"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="max-width: 200px"
                @change="handleSemesterStartDateChange"
              />
              <span v-if="currentWeek" class="semester-week-hint">当前第 {{ currentWeek }} 周</span>
            </el-form-item>
          </el-form>
        </el-card>

        <!-- 自定义快捷分值 -->
        <el-card class="settings-page__card" style="margin-top: 16px">
          <template #header>
            <span style="font-weight: 600">自定义快捷分值</span>
          </template>
          <div class="custom-scores">
            <div class="custom-scores__list">
              <el-tag
                v-for="(val, idx) in customQuickScores"
                :key="idx"
                :type="val >= 0 ? 'success' : 'danger'"
                closable
                size="large"
                class="custom-scores__tag"
                @close="removeCustomScore(idx)"
              >
                {{ val >= 0 ? '+' : '' }}{{ val }}
              </el-tag>
              <span v-if="customQuickScores.length === 0" class="custom-scores__empty">
                未设置，将使用默认值 +1/+2/+5/-1/-2/-5
              </span>
            </div>
            <div v-if="showAddScoreInput" class="custom-scores__add-row">
              <el-input-number
                v-model="newScoreValue"
                :min="-999"
                :max="999"
                size="small"
                style="width: 140px"
                @keyup.enter="addCustomScore"
              />
              <el-button type="primary" size="small" @click="addCustomScore">确认</el-button>
              <el-button size="small" @click="showAddScoreInput = false">取消</el-button>
            </div>
            <el-button v-else type="primary" size="small" @click="showAddScoreInput = true">
              <el-icon><Plus /></el-icon>
              添加
            </el-button>
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="悬浮窗" name="floating">
        <el-card class="settings-page__card">
          <el-form label-width="100px">
            <el-form-item label="启用悬浮窗">
              <el-switch v-model="floatingSettings.enabled" @change="handleFloatingSave" />
            </el-form-item>
            <el-form-item label="窗口样式">
              <el-radio-group v-model="floatingSettings.style" @change="handleFloatingSave">
                <el-radio-button value="classic">经典</el-radio-button>
                <el-radio-button value="modern">现代</el-radio-button>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="透明度">
              <el-slider v-model="floatingSettings.opacity" :min="0.3" :max="1" :step="0.05" show-input @change="handleFloatingSave" />
            </el-form-item>
            <el-form-item label="大小">
              <el-slider v-model="floatingSettings.size" :min="40" :max="80" :step="2" show-input @change="handleFloatingSave" />
            </el-form-item>
            <el-form-item label="显示文字">
              <el-input v-model="floatingSettings.displayText" placeholder="悬浮窗显示文字" @change="handleFloatingSave" style="max-width: 200px" />
            </el-form-item>
            <el-form-item label="显示标签">
              <el-switch v-model="floatingSettings.showLabel" @change="handleFloatingSave" />
            </el-form-item>
            <el-form-item label="强调色">
              <el-color-picker v-model="floatingSettings.accentColor" @change="handleFloatingSave" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleFloatingSave">保存悬浮窗设置</el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="宠物" name="pet">
        <el-card class="settings-page__card">
          <el-form label-width="120px">
            <el-form-item label="启用宠物系统">
              <el-switch v-model="petSettings.enabled" @change="handlePetSave" />
            </el-form-item>
            <el-form-item label="默认宠物类型">
              <el-select v-model="petSettings.defaultPetType" @change="handlePetSave" placeholder="选择默认宠物" style="max-width: 200px">
                <el-option
                  v-for="pet in ALL_PET_TYPES"
                  :key="pet.id"
                  :label="pet.emoji + ' ' + pet.name"
                  :value="pet.id"
                />
              </el-select>
            </el-form-item>
            <el-divider content-position="left">等级经验阈值</el-divider>
            <el-form-item v-for="i in 7" :key="i" :label="`等级 ${i} → ${i + 1}`">
              <el-input-number
                v-model="petSettings.levelThresholds[i - 1]"
                :min="1"
                :max="9999"
                size="small"
                style="width: 140px"
                @change="handlePetSave"
              />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handlePetSave">保存宠物设置</el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="数据管理" name="data">
        <el-card class="settings-page__card">
          <el-form label-width="140px">
            <el-form-item label="导出所有数据">
              <el-button type="primary" @click="handleExportAll" :loading="dataLoading">
                <el-icon><Download /></el-icon>
                导出所有数据
              </el-button>
              <span class="data-hint">将学生、积分、分组、评价项全部导出到一个 Excel 文件</span>
            </el-form-item>
            <el-form-item label="导入数据">
              <el-button @click="handleImportData" :loading="dataLoading">
                <el-icon><Upload /></el-icon>
                导入数据
              </el-button>
              <input ref="importFileInput" type="file" accept=".xlsx,.xls" style="display: none" @change="onImportFileChange" />
              <span class="data-hint">从 Excel 文件导入数据</span>
            </el-form-item>
            <el-divider />
            <el-form-item label="仅导出学生数据">
              <el-button @click="handleExportStudents" :loading="dataLoading">
                <el-icon><Download /></el-icon>
                仅导出学生数据
              </el-button>
            </el-form-item>
            <el-form-item label="仅导入学生数据">
              <el-button @click="handleImportStudents" :loading="dataLoading">
                <el-icon><Upload /></el-icon>
                仅导入学生数据
              </el-button>
              <input ref="importStudentFileInput" type="file" accept=".xlsx,.xls" style="display: none" @change="onImportStudentFileChange" />
            </el-form-item>
            <el-divider />
            <el-form-item label="数据目录">
              <div class="data-folder-row">
                <el-input :model-value="dataFolderPath" readonly style="flex: 1" />
                <el-button @click="handleOpenDataFolder" style="margin-left: 8px">
                  打开数据目录
                </el-button>
              </div>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Upload, Download, Plus } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'
import { DisplayMode } from '@/types'
import type { Student, StudentGroup, EvaluationItem } from '@/types'
import { ALL_PET_TYPES } from '@/utils/petSystem'
import api from '@/services/api'
import { studentApi } from '@/services/student'
import { groupApi } from '@/services/group'
import { exportToExcel, readExcelFile } from '@/utils/excelHelper'
import * as XLSX from 'xlsx'

const settingsStore = useSettingsStore()
const activeTab = ref('general')

async function promptRelaunch(reason: string) {
  try {
    await ElMessageBox.confirm(
      `${reason}需要重启应用才能完全生效。是否立即重启？`,
      '需要重启',
      {
        confirmButtonText: '立即重启',
        cancelButtonText: '稍后手动重启',
        type: 'info',
      }
    )
    if (window.electronAPI?.relaunchApp) {
      await window.electronAPI.relaunchApp()
    } else {
      ElMessage.info('当前环境不支持自动重启，请手动关闭并重新打开应用')
    }
  } catch {
    // 用户选择稍后重启，不做任何操作
  }
}

const settings = reactive({
  theme: 'light' as 'light' | 'dark' | 'system',
  fontSize: 14,
  displayMode: DisplayMode.Card,
  customAccentColor: '',
  fontFamily: 'system',
  semesterStartDate: '' as string,
})

const currentWeek = computed(() => {
  if (!settings.semesterStartDate) return null
  const start = new Date(settings.semesterStartDate)
  const now = new Date()
  const diffMs = now.getTime() - start.getTime()
  if (diffMs < 0) return null
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))
  return Math.floor(diffDays / 7) + 1
})

const floatingSettings = reactive({
  enabled: false,
  style: 'classic' as 'classic' | 'modern',
  opacity: 1.0,
  size: 60,
  displayText: 'CS',
  showLabel: true,
  accentColor: '#409EFF',
})

const dataLoading = ref(false)
const dataFolderPath = ref('')
const importFileInput = ref<HTMLInputElement | null>(null)
const importStudentFileInput = ref<HTMLInputElement | null>(null)

// 自定义快捷分值
const customQuickScores = ref<number[]>([])
const showAddScoreInput = ref(false)
const newScoreValue = ref(1)

// 宠物设置
const petSettings = reactive({
  enabled: true,
  defaultPetType: 'cat',
  levelThresholds: [40, 60, 80, 100, 120, 140, 160] as number[],
})

onMounted(async () => {
  await settingsStore.fetchSettings()
  Object.assign(settings, settingsStore.settings)
  await Promise.all([fetchFloatingSettings(), fetchDataFolderPath()])
  loadCustomQuickScores()
  loadPetSettings()
})

async function fetchFloatingSettings() {
  try {
    const response = await api.get<{ data: Record<string, unknown> }>('/api/settings/floating')
    const data = response.data.data
    if (data) {
      if (data['floating.enabled'] !== undefined) floatingSettings.enabled = !!data['floating.enabled']
      if (data['floating.style']) floatingSettings.style = data['floating.style'] as 'classic' | 'modern'
      if (data['floating.opacity'] !== undefined) floatingSettings.opacity = Number(data['floating.opacity'])
      if (data['floating.size'] !== undefined) floatingSettings.size = Number(data['floating.size'])
      if (data['floating.displayText']) floatingSettings.displayText = String(data['floating.displayText'])
      if (data['floating.showLabel'] !== undefined) floatingSettings.showLabel = !!data['floating.showLabel']
      if (data['floating.accentColor']) floatingSettings.accentColor = String(data['floating.accentColor'])
    }
  } catch { /* ignore */ }
}

async function fetchDataFolderPath() {
  try {
    const response = await api.get<{ data: { path: string } }>('/api/settings/data-path')
    dataFolderPath.value = response.data.data?.path || ''
  } catch { /* ignore */ }
}

async function handleThemeChange(theme: 'light' | 'dark' | 'system') {
  await settingsStore.updateSettings({ theme })
}

async function handleFontSizeChange(fontSize: number) {
  await settingsStore.updateSettings({ fontSize })
}

async function handleFontFamilyChange(fontFamily: string) {
  const fontStack = fontFamily === 'system'
    ? ''
    : `"${fontFamily}", system-ui, -apple-system, sans-serif`
  document.documentElement.style.fontFamily = fontStack || ''
  await settingsStore.updateSettings({ fontFamily } as Record<string, unknown>)
}

async function handleSave() {
  await settingsStore.updateSettings(settings)
}

async function handleSemesterStartDateChange(date: string) {
  await settingsStore.updateSettings({ semesterStartDate: date } as Record<string, unknown>)
}

async function handleFloatingSave() {
  try {
    await api.put('/api/settings/floating', {
      'floating.enabled': floatingSettings.enabled,
      'floating.style': floatingSettings.style,
      'floating.opacity': floatingSettings.opacity,
      'floating.size': floatingSettings.size,
      'floating.displayText': floatingSettings.displayText,
      'floating.showLabel': floatingSettings.showLabel,
      'floating.accentColor': floatingSettings.accentColor,
    })
    ElMessage.success('悬浮窗设置已保存')
    promptRelaunch('悬浮窗设置')
  } catch {
    ElMessage.error('保存悬浮窗设置失败')
  }
}

// ===== 数据管理 =====

async function handleExportAll() {
  dataLoading.value = true
  try {
    const [studentsRes, groupsRes, evaluationRes] = await Promise.all([
      studentApi.getAll(),
      groupApi.getAll(),
      api.get<{ data: EvaluationItem[] }>('/api/evaluation-items').catch(() => ({ data: { data: [] } })),
    ])

    const students: Student[] = studentsRes.data.data || []
    const groups: StudentGroup[] = groupsRes.data.data || []
    const evaluationItems: EvaluationItem[] = evaluationRes.data.data || []

    const wb = XLSX.utils.book_new()

    // 学生表
    const studentCols = ['id', 'name', 'studentNumber', 'gender', 'score', 'groupId', 'petType', 'petExp', 'createdAt']
    const studentHeaders = ['ID', '姓名', '学号', '性别', '积分', '分组ID', '宠物类型', '宠物经验', '创建时间']
    const studentRows = students.map(s => studentCols.map(c => (s as unknown as Record<string, unknown>)[c] ?? ''))
    const ws1 = XLSX.utils.aoa_to_sheet([studentHeaders, ...studentRows])
    ws1['!cols'] = studentHeaders.map(h => ({ wch: Math.max(h.length * 2, 12) }))
    XLSX.utils.book_append_sheet(wb, ws1, '学生')

    // 分组表
    const groupCols = ['id', 'name', 'studentIds', 'createdAt']
    const groupHeaders = ['ID', '名称', '学生ID列表', '创建时间']
    const groupRows = groups.map(g => groupCols.map(c => {
      const val = (g as unknown as Record<string, unknown>)[c]
      return Array.isArray(val) ? val.join(',') : (val ?? '')
    }))
    const ws2 = XLSX.utils.aoa_to_sheet([groupHeaders, ...groupRows])
    ws2['!cols'] = groupHeaders.map(h => ({ wch: Math.max(h.length * 2, 12) }))
    XLSX.utils.book_append_sheet(wb, ws2, '分组')

    // 评价项表
    const evalCols = ['id', 'name', 'scoreChange', 'isPositive', 'createdAt']
    const evalHeaders = ['ID', '名称', '积分变化', '正向', '创建时间']
    const evalRows = evaluationItems.map(e => evalCols.map(c => (e as unknown as Record<string, unknown>)[c] ?? ''))
    const ws3 = XLSX.utils.aoa_to_sheet([evalHeaders, ...evalRows])
    ws3['!cols'] = evalHeaders.map(h => ({ wch: Math.max(h.length * 2, 12) }))
    XLSX.utils.book_append_sheet(wb, ws3, '评价项')

    XLSX.writeFile(wb, 'ClassIsScore_全部数据.xlsx')
    ElMessage.success('数据导出成功')
  } catch {
    ElMessage.error('导出数据失败')
  } finally {
    dataLoading.value = false
  }
}

function handleImportData() {
  importFileInput.value?.click()
}

async function onImportFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  dataLoading.value = true
  try {
    // Re-read the file for workbook access
    const arrayBuffer = await file.arrayBuffer()
    const wb = XLSX.read(new Uint8Array(arrayBuffer), { type: 'array' })

    let imported = 0

    // 导入学生
    if (wb.SheetNames.includes('学生')) {
      const ws = wb.Sheets['学生']
      const rows = XLSX.utils.sheet_to_json<Record<string, string>>(ws)
      const students = rows.map(r => ({
        name: r['姓名'] || r['name'] || '',
        studentNumber: r['学号'] || r['studentNumber'] || '',
        gender: r['性别'] || r['gender'] || '',
        score: Number(r['积分'] || r['score'] || 0),
        groupId: r['分组ID'] || r['groupId'] || '',
      })).filter(s => s.name)
      if (students.length > 0) {
        await studentApi.batchCreate(students)
        imported += students.length
      }
    }

    // 导入分组
    if (wb.SheetNames.includes('分组')) {
      const ws = wb.Sheets['分组']
      const rows = XLSX.utils.sheet_to_json<Record<string, string>>(ws)
      for (const r of rows) {
        const name = r['名称'] || r['name']
        if (name) {
          await groupApi.create({ name, studentIds: (r['学生ID列表'] || '').split(',').filter(Boolean) })
          imported++
        }
      }
    }

    ElMessage.success(`数据导入成功，共导入 ${imported} 条记录`)
  } catch {
    ElMessage.error('导入数据失败')
  } finally {
    dataLoading.value = false
    input.value = ''
  }
}

async function handleExportStudents() {
  dataLoading.value = true
  try {
    const response = await studentApi.getAll()
    const students: Student[] = response.data.data || []
    exportToExcel(
      students.map(s => ({
        id: s.id,
        name: s.name,
        studentNumber: s.studentNumber || '',
        gender: s.gender || '',
        score: s.score,
        groupId: s.groupId || '',
        petType: s.petType || '',
        petExp: s.petExp,
        createdAt: s.createdAt,
      })),
      [
        { header: 'ID', key: 'id' },
        { header: '姓名', key: 'name' },
        { header: '学号', key: 'studentNumber' },
        { header: '性别', key: 'gender' },
        { header: '积分', key: 'score' },
        { header: '分组ID', key: 'groupId' },
        { header: '宠物类型', key: 'petType' },
        { header: '宠物经验', key: 'petExp' },
        { header: '创建时间', key: 'createdAt' },
      ],
      'ClassIsScore_学生数据'
    )
    ElMessage.success('学生数据导出成功')
  } catch {
    ElMessage.error('导出学生数据失败')
  } finally {
    dataLoading.value = false
  }
}

function handleImportStudents() {
  importStudentFileInput.value?.click()
}

async function onImportStudentFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  dataLoading.value = true
  try {
    const { headers, rows } = await readExcelFile(file)
    const students = rows.map(row => {
      const getValue = (keys: string[]) => {
        for (const key of keys) {
          const idx = headers.indexOf(key)
          if (idx >= 0) return row[idx]
        }
        return ''
      }
      return {
        name: getValue(['姓名', 'name']) || '',
        studentNumber: getValue(['学号', 'studentNumber']) || '',
        gender: getValue(['性别', 'gender']) || '',
        score: Number(getValue(['积分', 'score']) || 0),
        groupId: getValue(['分组ID', 'groupId']) || '',
      }
    }).filter(s => s.name)

    if (students.length === 0) {
      ElMessage.warning('未找到有效的学生数据')
      return
    }

    await studentApi.batchCreate(students)
    ElMessage.success(`成功导入 ${students.length} 名学生`)
  } catch {
    ElMessage.error('导入学生数据失败')
  } finally {
    dataLoading.value = false
    input.value = ''
  }
}

// ===== 自定义快捷分值 =====

function loadCustomQuickScores() {
  try {
    const stored = localStorage.getItem('customQuickScores')
    if (stored) {
      customQuickScores.value = JSON.parse(stored)
    }
  } catch { /* ignore */ }
}

function saveCustomQuickScores() {
  localStorage.setItem('customQuickScores', JSON.stringify(customQuickScores.value))
}

function addCustomScore() {
  if (customQuickScores.value.includes(newScoreValue.value)) {
    ElMessage.warning('该分值已存在')
    return
  }
  customQuickScores.value.push(newScoreValue.value)
  customQuickScores.value.sort((a, b) => b - a)
  saveCustomQuickScores()
  newScoreValue.value = 1
  showAddScoreInput.value = false
  ElMessage.success('已添加')
}

function removeCustomScore(idx: number) {
  customQuickScores.value.splice(idx, 1)
  saveCustomQuickScores()
}

// ===== 宠物设置 =====

function loadPetSettings() {
  try {
    const stored = localStorage.getItem('petSettings')
    if (stored) {
      const data = JSON.parse(stored)
      if (data.enabled !== undefined) petSettings.enabled = data.enabled
      if (data.defaultPetType) petSettings.defaultPetType = data.defaultPetType
      if (Array.isArray(data.levelThresholds) && data.levelThresholds.length === 7) {
        petSettings.levelThresholds = data.levelThresholds
      }
    }
  } catch { /* ignore */ }
}

function handlePetSave() {
  localStorage.setItem('petSettings', JSON.stringify({
    enabled: petSettings.enabled,
    defaultPetType: petSettings.defaultPetType,
    levelThresholds: petSettings.levelThresholds,
  }))
  ElMessage.success('宠物设置已保存')
}

function handleOpenDataFolder() {
  if (window.electronAPI?.openPath) {
    window.electronAPI.openPath(dataFolderPath.value)
  } else if (dataFolderPath.value) {
    ElMessage.info(`数据目录: ${dataFolderPath.value}`)
  } else {
    ElMessage.info('数据目录路径不可用')
  }
}
</script>

<style scoped>
.settings-page__header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.settings-page__header h2 {
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

.settings-page__card {
  max-width: 700px;
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.settings-page__card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

/* 数据管理 */
.data-hint {
  margin-left: 12px;
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.data-folder-row {
  display: flex;
  align-items: center;
  width: 100%;
  max-width: 500px;
}

/* 自定义快捷分值 */
.custom-scores__list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}

.custom-scores__tag {
  font-size: 14px;
  font-weight: 600;
}

.custom-scores__empty {
  font-size: 13px;
  color: var(--cis-text-tertiary);
}

.custom-scores__add-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.semester-week-hint {
  margin-left: 12px;
  font-size: 13px;
  color: var(--cis-text-secondary);
  font-weight: 500;
}
</style>
