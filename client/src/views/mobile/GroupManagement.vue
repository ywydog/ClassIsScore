<template>
  <div class="m-groups">
    <header class="m-groups__head">
      <span class="cis-eyebrow">Groups</span>
      <h1 class="cis-display m-groups__title">分组</h1>
    </header>
    <ul v-if="groups.length > 0" class="m-groups__list" role="list">
      <li v-for="g in groups" :key="g.id" class="m-groups__row">
        <button
          type="button"
          class="m-groups__row-main"
          :aria-label="`${g.name}，${g.studentIds.length} 人`"
          @click="openGroup(g)"
        >
          <div class="m-groups__icon" aria-hidden="true">
            <span class="m-groups__icon-text">{{ g.name.slice(0, 1) }}</span>
          </div>
          <div class="m-groups__body">
            <span class="m-groups__name">{{ g.name }}</span>
            <span class="m-groups__count">{{ g.studentIds.length }} 人</span>
          </div>
          <el-icon :size="14" class="m-groups__chevron" aria-hidden="true"><ArrowRight /></el-icon>
        </button>
        <div class="m-groups__row-actions">
          <button
            type="button"
            class="m-groups__row-btn"
            aria-label="编辑小组"
            @click.stop="openEditDialog(g)"
          >
            <el-icon :size="14" aria-hidden="true"><Edit /></el-icon>
          </button>
          <button
            type="button"
            class="m-groups__row-btn is-danger"
            aria-label="删除小组"
            @click.stop="handleDelete(g.id)"
          >
            <el-icon :size="14" aria-hidden="true"><Delete /></el-icon>
          </button>
        </div>
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无分组" />

    <button
      type="button"
      class="m-groups__fab"
      aria-label="新建分组"
      @click="openCreateDialog"
    >
      <el-icon :size="22" aria-hidden="true"><Plus /></el-icon>
    </button>

    <BottomSheet v-model="sheetOpen" :title="currentGroup?.name || '成员'" height="half">
      <div v-if="currentGroup" class="m-groups__sheet-head">
        <span class="m-groups__sheet-hint">长按成员可移除，点击"添加成员"加入新成员。</span>
        <el-button size="small" type="primary" plain @click="openAddMembersDialog">
          <el-icon :size="14" aria-hidden="true"><Plus /></el-icon>
          <span>添加成员</span>
        </el-button>
      </div>
      <ul v-if="currentGroup" class="m-groups__members" role="list">
        <li v-for="sid in currentGroup.studentIds" :key="sid" class="m-groups__member">
          <div class="m-groups__member-avatar" aria-hidden="true">
            <span class="m-groups__member-avatar-text">{{ (memberName(sid) || '?').slice(0, 1) }}</span>
          </div>
          <span class="m-groups__member-name">{{ memberName(sid) }}</span>
          <button
            type="button"
            class="m-groups__member-remove"
            :aria-label="`移除成员 ${memberName(sid)}`"
            @click="removeStudentFromGroup(sid)"
          >
            <el-icon :size="14" aria-hidden="true"><Close /></el-icon>
          </button>
        </li>
        <li v-if="currentGroup.studentIds.length === 0" class="m-groups__member-empty">本组暂无成员</li>
      </ul>
    </BottomSheet>

    <el-dialog
      v-model="formOpen"
      :title="editingGroup ? '编辑分组' : '新建分组'"
      width="320px"
      :close-on-click-modal="false"
      destroy-on-close
    >
      <el-form label-position="top" @submit.prevent="handleSubmit">
        <el-form-item label="小组名称" required>
          <el-input
            v-model="groupForm.name"
            placeholder="请输入小组名称…"
            maxlength="20"
            show-word-limit
            autocomplete="off"
            aria-label="小组名称"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="m-groups__dialog-actions">
          <el-button @click="formOpen = false">取消</el-button>
          <el-button
            type="primary"
            :disabled="!groupForm.name.trim()"
            @click="handleSubmit"
          >确定</el-button>
        </div>
      </template>
    </el-dialog>

    <el-dialog
      v-model="memberFormOpen"
      title="添加成员"
      width="320px"
      :close-on-click-modal="false"
      destroy-on-close
    >
      <el-form label-position="top">
        <el-form-item label="选择学生（可多选）">
          <el-select
            v-model="selectedStudentIds"
            multiple
            filterable
            collapse-tags
            collapse-tags-tooltip
            placeholder="选择学生…"
            class="m-groups__member-select"
            aria-label="选择学生"
          >
            <el-option
              v-for="s in availableStudents"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="m-groups__dialog-actions">
          <el-button @click="memberFormOpen = false">取消</el-button>
          <el-button
            type="primary"
            :disabled="selectedStudentIds.length === 0"
            @click="handleAddMembers"
          >添加</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ArrowRight, Plus, Edit, Delete, Close } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { groupApi } from '@/services/group'
import { useStudentStore } from '@/stores/student'
import type { StudentGroup } from '@/types'

const studentStore = useStudentStore()
const groups = ref<StudentGroup[]>([])
const sheetOpen = ref(false)
const currentGroup = ref<StudentGroup | null>(null)
const formOpen = ref(false)
const editingGroup = ref<StudentGroup | null>(null)
const groupForm = reactive({ name: '' })
const memberFormOpen = ref(false)
const selectedStudentIds = ref<string[]>([])

const availableStudents = computed(() => {
  if (!currentGroup.value) return studentStore.students
  const memberIds = new Set(currentGroup.value.studentIds)
  return studentStore.students.filter(s => !memberIds.has(s.id))
})

onMounted(async () => {
  await studentStore.fetchStudents()
  await fetchGroups()
})

async function fetchGroups() {
  try {
    const res = await groupApi.getAll()
    groups.value = res.data.data
    if (currentGroup.value) {
      const refreshed = groups.value.find(g => g.id === currentGroup.value?.id)
      if (refreshed) currentGroup.value = refreshed
    }
  } catch { groups.value = [] }
}

function memberName(id: string) { return studentStore.getStudentById(id)?.name || '—' }

function openGroup(g: StudentGroup) {
  currentGroup.value = g
  sheetOpen.value = true
}

function openCreateDialog() {
  editingGroup.value = null
  groupForm.name = ''
  formOpen.value = true
}

function openEditDialog(g: StudentGroup) {
  editingGroup.value = g
  groupForm.name = g.name
  formOpen.value = true
}

async function handleSubmit() {
  const name = groupForm.name.trim()
  if (!name) {
    ElMessage.warning('请输入小组名称')
    return
  }
  try {
    if (editingGroup.value) {
      await groupApi.update(editingGroup.value.id, { name })
      ElMessage.success('已更新')
    } else {
      await groupApi.create({ name })
      ElMessage.success('已创建')
    }
    formOpen.value = false
    await fetchGroups()
  } catch { /* interceptor handled */ }
}

async function handleDelete(id: string) {
  try {
    await ElMessageBox.confirm('确定删除该小组吗？', '确认删除', { type: 'warning' })
  } catch {
    return
  }
  try {
    await groupApi.delete(id)
    ElMessage.success('已删除')
    if (currentGroup.value?.id === id) {
      currentGroup.value = null
      sheetOpen.value = false
    }
    await fetchGroups()
  } catch { /* ignore */ }
}

function openAddMembersDialog() {
  if (!currentGroup.value) return
  selectedStudentIds.value = []
  memberFormOpen.value = true
}

async function handleAddMembers() {
  if (!currentGroup.value || selectedStudentIds.value.length === 0) return
  try {
    for (const sid of selectedStudentIds.value) {
      await groupApi.addStudent(currentGroup.value.id, sid)
    }
    ElMessage.success('已添加成员')
    memberFormOpen.value = false
    selectedStudentIds.value = []
    await fetchGroups()
    if (currentGroup.value) {
      const refreshed = groups.value.find(g => g.id === currentGroup.value?.id)
      if (refreshed) currentGroup.value = refreshed
    }
  } catch { /* ignore */ }
}

async function removeStudentFromGroup(studentId: string) {
  if (!currentGroup.value) return
  try {
    await groupApi.removeStudent(currentGroup.value.id, studentId)
    await fetchGroups()
    if (currentGroup.value) {
      const refreshed = groups.value.find(g => g.id === currentGroup.value?.id)
      if (refreshed) currentGroup.value = refreshed
    }
  } catch { /* ignore */ }
}
</script>

<style scoped>
.m-groups { display: flex; flex-direction: column; gap: 16px; padding-bottom: 96px; }
.m-groups__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-groups__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-groups__row { display: flex; align-items: stretch; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-groups__list li:last-child .m-groups__row { border-bottom: none; }
.m-groups__row-main { display: flex; align-items: center; gap: 12px; flex: 1; min-width: 0; min-height: 60px; padding: 10px 12px 10px 16px; background: transparent; border: none; color: var(--cis-text-primary); font-family: inherit; text-align: left; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-groups__row-main:active { background: var(--cis-primary-tint); }
.m-groups__icon { width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: var(--cis-radius-btn); flex-shrink: 0; }
.m-groups__icon-text { font-family: var(--cis-font-serif); font-size: 16px; font-weight: 600; line-height: 1; }
.m-groups__body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.m-groups__name { font-size: 15px; font-weight: 500; }
.m-groups__count { font-size: 12px; color: var(--cis-text-tertiary); }
.m-groups__chevron { color: var(--cis-text-tertiary); flex-shrink: 0; }
.m-groups__row-actions { display: flex; align-items: center; gap: 4px; padding: 0 8px 0 0; flex-shrink: 0; }
.m-groups__row-btn { display: inline-flex; align-items: center; justify-content: center; width: 36px; height: 36px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-btn); background: var(--cis-surface-1); color: var(--cis-text-secondary); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; }
.m-groups__row-btn:active { transform: scale(var(--cis-press-scale)); background: var(--cis-primary-tint); color: var(--cis-primary); }
.m-groups__row-btn.is-danger { color: var(--cis-accent); border-color: rgba(185, 28, 28, 0.3); }
.m-groups__row-btn.is-danger:active { background: var(--cis-accent-tint); }

.m-groups__sheet-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 12px; }
.m-groups__sheet-hint { font-size: 12px; color: var(--cis-text-tertiary); flex: 1; }
.m-groups__members { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-groups__member { position: relative; display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 12px 8px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); }
.m-groups__member-avatar { width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; }
.m-groups__member-avatar-text { font-family: var(--cis-font-serif); font-size: 16px; font-weight: 600; }
.m-groups__member-name { font-size: 13px; font-weight: 500; text-align: center; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 100%; }
.m-groups__member-remove { position: absolute; top: 4px; right: 4px; display: inline-flex; align-items: center; justify-content: center; width: 24px; height: 24px; border: 1px solid var(--cis-border); border-radius: 9999px; background: var(--cis-surface-1); color: var(--cis-accent); cursor: pointer; font-family: inherit; -webkit-tap-highlight-color: transparent; }
.m-groups__member-remove:active { transform: scale(var(--cis-press-scale)); background: var(--cis-accent-tint); }
.m-groups__member-empty { grid-column: 1 / -1; text-align: center; color: var(--cis-text-tertiary); font-size: 13px; padding: 16px 0; }

.m-groups__fab { position: fixed; right: 16px; bottom: calc(80px + env(safe-area-inset-bottom, 0)); z-index: 100; width: 56px; height: 56px; border: none; border-radius: 9999px; background: var(--cis-primary); color: #fff; display: flex; align-items: center; justify-content: center; cursor: pointer; box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3); -webkit-tap-highlight-color: transparent; }
.m-groups__fab:active { transform: scale(var(--cis-press-scale-strong)); }

.m-groups__dialog-actions { display: flex; gap: 8px; justify-content: flex-end; width: 100%; }
.m-groups__member-select { width: 100%; }
</style>
