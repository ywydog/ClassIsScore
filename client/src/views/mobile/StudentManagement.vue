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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Search, ArrowRight } from '@element-plus/icons-vue'
import MobileEmptyState from '@/components/mobile/MobileEmptyState.vue'
import { useStudentStore } from '@/stores/student'

const studentStore = useStudentStore()
const search = ref('')
const sortBy = ref<'score-desc' | 'score-asc' | 'name-asc' | 'name-desc'>('score-desc')

onMounted(async () => { await studentStore.fetchStudents() })

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
</script>

<style scoped>
.m-students { display: flex; flex-direction: column; gap: 16px; }
.m-students__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-students__search { display: flex; gap: 8px; }
.m-students__search .el-input { flex: 1; }
.m-students__sort { width: 120px; }
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
</style>
