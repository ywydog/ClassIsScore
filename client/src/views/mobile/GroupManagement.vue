<template>
  <div class="m-groups">
    <header class="m-groups__head">
      <span class="cis-eyebrow">Groups</span>
      <h1 class="cis-display m-groups__title">分组</h1>
    </header>
    <ul v-if="groups.length > 0" class="m-groups__list" role="list">
      <li v-for="g in groups" :key="g.id">
        <button
          type="button"
          class="m-groups__row"
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
      </li>
    </ul>
    <MobileEmptyState v-else eyebrow="Empty" description="暂无分组" />
    <BottomSheet v-model="sheetOpen" :title="currentGroup?.name || '成员'" height="half">
      <ul v-if="currentGroup" class="m-groups__members" role="list">
        <li v-for="sid in currentGroup.studentIds" :key="sid" class="m-groups__member">
          <div class="m-groups__member-avatar" aria-hidden="true">
            <span class="m-groups__member-avatar-text">{{ (memberName(sid) || '?').slice(0, 1) }}</span>
          </div>
          <span class="m-groups__member-name">{{ memberName(sid) }}</span>
        </li>
        <li v-if="currentGroup.studentIds.length === 0" class="m-groups__member-empty">本组暂无成员</li>
      </ul>
    </BottomSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ArrowRight } from '@element-plus/icons-vue'
import BottomSheet from '@/components/mobile/BottomSheet.vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { groupApi } from '@/services/group'
import { useStudentStore } from '@/stores/student'
import type { StudentGroup } from '@/types'

const studentStore = useStudentStore()
const groups = ref<StudentGroup[]>([])
const sheetOpen = ref(false)
const currentGroup = ref<StudentGroup | null>(null)

onMounted(async () => {
  await studentStore.fetchStudents()
  try {
    const res = await groupApi.getAll()
    groups.value = res.data.data
  } catch { groups.value = [] }
})

function openGroup(g: StudentGroup) { currentGroup.value = g; sheetOpen.value = true }
function memberName(id: string) { return studentStore.getStudentById(id)?.name || '—' }
</script>

<style scoped>
.m-groups { display: flex; flex-direction: column; gap: 16px; }
.m-groups__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-groups__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-groups__row { display: flex; align-items: center; gap: 12px; width: 100%; min-height: 60px; padding: 10px 16px; background: var(--cis-surface-1); border: none; border-bottom: 1px solid var(--cis-border-light); color: var(--cis-text-primary); font-family: inherit; text-align: left; cursor: pointer; -webkit-tap-highlight-color: transparent; }
.m-groups__list li:last-child .m-groups__row { border-bottom: none; }
.m-groups__row:active { background: var(--cis-primary-tint); }
.m-groups__icon { width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: var(--cis-radius-btn); flex-shrink: 0; }
.m-groups__icon-text { font-family: var(--cis-font-serif); font-size: 16px; font-weight: 600; line-height: 1; }
.m-groups__body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.m-groups__name { font-size: 15px; font-weight: 500; }
.m-groups__count { font-size: 12px; color: var(--cis-text-tertiary); }
.m-groups__chevron { color: var(--cis-text-tertiary); flex-shrink: 0; }
.m-groups__members { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.m-groups__member { display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 12px 8px; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); }
.m-groups__member-avatar { width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; border: 1px solid var(--cis-border); background: var(--cis-surface-2); border-radius: 9999px; }
.m-groups__member-avatar-text { font-family: var(--cis-font-serif); font-size: 16px; font-weight: 600; }
.m-groups__member-name { font-size: 13px; font-weight: 500; text-align: center; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 100%; }
.m-groups__member-empty { grid-column: 1 / -1; text-align: center; color: var(--cis-text-tertiary); font-size: 13px; padding: 16px 0; }
</style>
