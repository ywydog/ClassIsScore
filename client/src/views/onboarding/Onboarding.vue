<template>
  <div class="onboarding">
    <div class="onboarding__panel">
      <div class="onboarding__brand">
        <div class="onboarding__brand-mark" aria-hidden="true">C</div>
        <span class="onboarding__brand-name">ClassIsScore</span>
      </div>

      <div
        class="onboarding__steps"
        role="progressbar"
        :aria-valuenow="currentStep + 1"
        aria-valuemin="1"
        aria-valuemax="4"
        :aria-label="`引导步骤 ${currentStep + 1} / 4`"
      >
        <button
          v-for="step in steps"
          :key="step.idx"
          type="button"
          class="onboarding__step"
          :class="{ 'is-active': currentStep === step.idx, 'is-done': currentStep > step.idx }"
          :aria-label="`跳转到步骤 ${step.idx + 1}`"
          :aria-current="currentStep === step.idx ? 'step' : undefined"
          @click="goToStep(step.idx)"
        >
          <span class="cis-eyebrow onboarding__step-num">{{ String(step.idx + 1).padStart(2, '0') }}</span>
          <span class="onboarding__step-label">{{ step.label }}</span>
        </button>
      </div>
    </div>

    <main class="onboarding__content">
      <!-- 步骤1: 欢迎 -->
      <section v-if="currentStep === 0" class="onboarding__step-pane" role="region" aria-live="polite">
        <span class="cis-eyebrow onboarding__eyebrow">Welcome</span>
        <h1 class="cis-display onboarding__title">
          欢迎使用 <span translate="no">ClassIsScore</span>
        </h1>
        <p class="onboarding__description">
          面向课堂场景的积分管理工具。<br />
          让课堂互动更有趣、更高效。
        </p>
        <ol class="onboarding__features" role="list">
          <li class="onboarding__feature">
            <span class="cis-eyebrow onboarding__feature-num">01</span>
            <div class="onboarding__feature-body">
              <span class="onboarding__feature-name">学生管理</span>
              <span class="onboarding__feature-desc">导入名单，分组标签，学号排序</span>
            </div>
          </li>
          <li class="onboarding__feature">
            <span class="cis-eyebrow onboarding__feature-num">02</span>
            <div class="onboarding__feature-body">
              <span class="onboarding__feature-name">积分系统</span>
              <span class="onboarding__feature-desc">加减分明，理由可追溯</span>
            </div>
          </li>
          <li class="onboarding__feature">
            <span class="cis-eyebrow onboarding__feature-num">03</span>
            <div class="onboarding__feature-body">
              <span class="onboarding__feature-name">实时排行</span>
              <span class="onboarding__feature-desc">日 / 周 / 月，多档时间窗</span>
            </div>
          </li>
          <li class="onboarding__feature">
            <span class="cis-eyebrow onboarding__feature-num">04</span>
            <div class="onboarding__feature-body">
              <span class="onboarding__feature-name">大屏展示</span>
              <span class="onboarding__feature-desc">投影 / 浮动条，独立窗口</span>
            </div>
          </li>
        </ol>
        <div class="onboarding__actions">
          <button type="button" class="onboarding__btn onboarding__btn--primary" @click="currentStep = 1">
            开始设置
            <span class="onboarding__btn-arrow" aria-hidden="true">→</span>
          </button>
        </div>
      </section>

      <!-- 步骤2: 基本设置 -->
      <section v-else-if="currentStep === 1" class="onboarding__step-pane" role="region" aria-live="polite">
        <span class="cis-eyebrow onboarding__eyebrow">Step 02</span>
        <h2 class="cis-display onboarding__title">基本设置</h2>
        <p class="onboarding__description">这些信息会显示在排行榜与大屏顶部。</p>
        <div class="onboarding__form">
          <label class="onboarding__field">
            <span class="cis-eyebrow onboarding__field-label">班级名称</span>
            <el-input
              v-model="setupForm.className"
              placeholder="如：高一(3)班…"
              aria-label="班级名称"
              autocomplete="off"
              size="large"
            />
          </label>
          <div class="onboarding__field">
            <span class="cis-eyebrow onboarding__field-label">主题</span>
            <div class="onboarding__theme-tabs" role="radiogroup" aria-label="主题">
              <button
                v-for="opt in themeOptions"
                :key="opt.value"
                type="button"
                class="onboarding__theme-tab"
                :class="{ 'is-active': setupForm.theme === opt.value }"
                :aria-checked="setupForm.theme === opt.value"
                role="radio"
                @click="setupForm.theme = opt.value"
              >
                <span class="onboarding__theme-eyebrow cis-mono">{{ opt.eyebrow }}</span>
                <span class="onboarding__theme-label">{{ opt.label }}</span>
              </button>
            </div>
          </div>
        </div>
        <div class="onboarding__actions">
          <button type="button" class="onboarding__btn" @click="currentStep = 0">上一步</button>
          <button type="button" class="onboarding__btn onboarding__btn--primary" @click="currentStep = 2">
            下一步
            <span class="onboarding__btn-arrow" aria-hidden="true">→</span>
          </button>
        </div>
      </section>

      <!-- 步骤3: 添加学生 -->
      <section v-else-if="currentStep === 2" class="onboarding__step-pane" role="region" aria-live="polite">
        <span class="cis-eyebrow onboarding__eyebrow">Step 03</span>
        <h2 class="cis-display onboarding__title">添加学生</h2>
        <p class="onboarding__description">每行输入一个学生姓名，也可以稍后在管理页面添加。</p>
        <div class="onboarding__form">
          <label class="onboarding__field">
            <span class="cis-eyebrow onboarding__field-label">学生名单</span>
            <el-input
              v-model="studentInput"
              type="textarea"
              :rows="8"
              placeholder="张三&#10;李四&#10;王五…"
              aria-label="学生名单"
              autocomplete="off"
            />
          </label>
        </div>
        <div class="onboarding__actions">
          <button type="button" class="onboarding__btn" @click="currentStep = 1">上一步</button>
          <button type="button" class="onboarding__btn onboarding__btn--primary" @click="currentStep = 3">
            下一步
            <span class="onboarding__btn-arrow" aria-hidden="true">→</span>
          </button>
        </div>
      </section>

      <!-- 步骤4: 完成 -->
      <section v-else-if="currentStep === 3" class="onboarding__step-pane" role="region" aria-live="polite">
        <span class="cis-eyebrow onboarding__eyebrow">Step 04</span>
        <h2 class="cis-display onboarding__title">设置完成</h2>
        <p class="onboarding__description">
          你已准备好使用 <span translate="no">ClassIsScore</span> 管理课堂积分。
        </p>
        <div class="onboarding__actions">
          <button type="button" class="onboarding__btn onboarding__btn--accent" @click="handleComplete">
            进入应用
            <span class="onboarding__btn-arrow" aria-hidden="true">→</span>
          </button>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useSettingsStore } from '@/stores/settings'
import { useStudentStore } from '@/stores/student'
import { useAppStore } from '@/stores/app'
import { isMobile } from '@/utils/platform'
import { ElMessage } from 'element-plus'

const router = useRouter()
const settingsStore = useSettingsStore()
const studentStore = useStudentStore()
const appStore = useAppStore()

const currentStep = ref(0)
const studentInput = ref('')

const steps = [
  { idx: 0, label: '欢迎' },
  { idx: 1, label: '基本设置' },
  { idx: 2, label: '添加学生' },
  { idx: 3, label: '完成' },
]

const themeOptions: { value: 'light' | 'dark' | 'system'; label: string; eyebrow: string }[] = [
  { value: 'light', label: '浅色', eyebrow: 'L' },
  { value: 'dark', label: '深色', eyebrow: 'D' },
  { value: 'system', label: '跟随系统', eyebrow: 'A' },
]

const setupForm = reactive({
  className: '',
  theme: 'light' as 'light' | 'dark' | 'system',
})

function goToStep(idx: number) {
  currentStep.value = Math.max(0, Math.min(3, idx))
}

async function handleComplete() {
  try {
    // 1. 保存设置：主题 + 班级名称（如果用户填了）
    const newSettings: Record<string, string> = { theme: setupForm.theme }
    const className = setupForm.className.trim()
    if (className) {
      newSettings.className = className
    }
    await settingsStore.updateSettings(newSettings as unknown as Parameters<typeof settingsStore.updateSettings>[0])

    // 2. 批量添加学生
    if (studentInput.value.trim()) {
      const names = studentInput.value.trim().split('\n').filter(n => n.trim())
      if (names.length > 0) {
        const students = names.map(name => ({ name: name.trim() }))
        await studentStore.batchCreateStudent(students)
        ElMessage.success(`已添加 ${names.length} 名学生`)
      }
    }

    // 3. 标记引导完成
    await appStore.completeOnboarding()

    // 4. 根据平台选择落地路径
    const homePath = isMobile() ? '/m/scores' : '/admin/scores'
    router.replace(homePath)
  } catch (err) {
    console.error('[onboarding] handleComplete failed:', err)
    ElMessage.error('设置保存失败，请重试')
  }
}
</script>

<style scoped>
.onboarding {
  width: 100vw;
  min-height: 100vh;
  display: grid;
  grid-template-columns: 320px 1fr;
  background: var(--cis-canvas);
}

@media (max-width: 900px) {
  .onboarding {
    grid-template-columns: 1fr;
  }
}

/* ===== 左侧：深 navy 横幅 ===== */
.onboarding__panel {
  background: var(--cis-surface-inverse);
  color: #fff;
  padding: 48px 40px;
  display: flex;
  flex-direction: column;
  gap: 32px;
  position: relative;
  min-height: 100vh;
}

.onboarding__brand {
  display: flex;
  align-items: center;
  gap: 12px;
}

.onboarding__brand-mark {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--cis-primary);
  color: #fff;
  border-radius: var(--cis-radius-btn);
  font-family: var(--cis-font-serif);
  font-weight: 700;
  font-size: 18px;
  line-height: 1;
}

.onboarding__brand-name {
  font-family: var(--cis-font-serif);
  font-size: 18px;
  font-weight: 600;
  color: #fff;
  letter-spacing: -0.3px;
}

/* 步骤列表：Linear 风 underline 列表 */
.onboarding__steps {
  display: flex;
  flex-direction: column;
  gap: 0;
  margin-top: 8px;
}

.onboarding__step {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 0;
  background: transparent;
  border: none;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  text-align: left;
  color: rgba(255, 255, 255, 0.45);
  font-family: inherit;
  cursor: pointer;
  transition: color var(--cis-transition-fast);
}

.onboarding__step:last-child {
  border-bottom: none;
}

.onboarding__step:hover:not(.is-active) {
  color: rgba(255, 255, 255, 0.75);
}

.onboarding__step.is-active {
  color: #fff;
}

.onboarding__step.is-done {
  color: rgba(255, 255, 255, 0.65);
}

.onboarding__step-num {
  color: currentColor;
  opacity: 0.6;
  font-size: 10px;
  letter-spacing: 0.6px;
  min-width: 20px;
}

.onboarding__step.is-active .onboarding__step-num {
  color: var(--cis-primary-hover);
  opacity: 1;
}

.onboarding__step-label {
  font-size: 14px;
  font-weight: 600;
  letter-spacing: 0.1px;
}

/* 左侧底部 1px 文字 */
.onboarding__panel::after {
  content: 'ClassIsScore · v1.0.0';
  position: absolute;
  bottom: 24px;
  left: 40px;
  font-family: var(--cis-font-mono);
  font-size: 11px;
  color: rgba(255, 255, 255, 0.25);
  letter-spacing: 0.3px;
}

@media (max-width: 900px) {
  .onboarding__panel {
    min-height: auto;
    padding: 24px 20px;
  }
  .onboarding__steps {
    flex-direction: row;
    overflow-x: auto;
    gap: 16px;
  }
  .onboarding__step {
    border-bottom: none;
    border-right: 1px solid rgba(255, 255, 255, 0.08);
    padding: 0 16px 0 0;
    flex-shrink: 0;
  }
  .onboarding__step:last-child {
    border-right: none;
  }
  .onboarding__panel::after {
    display: none;
  }
}

/* ===== 右侧：内容区 ===== */
.onboarding__content {
  padding: 64px 56px;
  max-width: 720px;
  width: 100%;
  align-self: center;
  justify-self: center;
}

@media (max-width: 900px) {
  .onboarding__content {
    padding: 32px 20px;
  }
}

.onboarding__step-pane {
  display: flex;
  flex-direction: column;
  gap: 12px;
  animation: step-in 0.25s ease;
}

@keyframes step-in {
  from { opacity: 0; transform: translateY(4px); }
  to { opacity: 1; transform: translateY(0); }
}

.onboarding__eyebrow {
  color: var(--cis-primary);
}

.onboarding__title {
  font-size: 36px;
  margin: 4px 0 0;
  font-weight: 600;
  letter-spacing: -0.5px;
  color: var(--cis-text-display);
}

.onboarding__description {
  margin: 8px 0 0;
  font-size: 15px;
  line-height: 1.7;
  color: var(--cis-text-secondary);
}

/* ===== Features 列表 ===== */
.onboarding__features {
  list-style: none;
  margin: 24px 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0;
  border-top: 1px solid var(--cis-border);
}

.onboarding__feature {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding: 18px 0;
  border-bottom: 1px solid var(--cis-border);
}

.onboarding__feature-num {
  flex-shrink: 0;
  padding-top: 2px;
  min-width: 24px;
  color: var(--cis-text-tertiary);
}

.onboarding__feature-body {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.onboarding__feature-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.onboarding__feature-desc {
  font-size: 12px;
  color: var(--cis-text-tertiary);
  line-height: 1.5;
}

/* ===== Form 字段 ===== */
.onboarding__form {
  display: flex;
  flex-direction: column;
  gap: 24px;
  margin-top: 24px;
}

.onboarding__field {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.onboarding__field-label {
  color: var(--cis-text-tertiary);
}

.onboarding__theme-tabs {
  display: flex;
  align-items: stretch;
  gap: 0;
  border: 1px solid var(--cis-border);
  border-radius: var(--cis-radius-btn);
  overflow: hidden;
  background: var(--cis-surface-1);
  width: fit-content;
}

.onboarding__theme-tab {
  display: flex;
  flex-direction: column;
  gap: 1px;
  padding: 8px 18px 10px;
  background: transparent;
  border: none;
  color: var(--cis-text-tertiary);
  font-family: inherit;
  cursor: pointer;
  border-right: 1px solid var(--cis-border);
  transition: background-color var(--cis-transition-fast), color var(--cis-transition-fast);
  text-align: left;
}

.onboarding__theme-tab:last-child {
  border-right: none;
}

.onboarding__theme-tab:hover:not(.is-active) {
  background: var(--cis-surface-2);
  color: var(--cis-text-primary);
}

.onboarding__theme-tab.is-active {
  background: var(--cis-primary-tint);
  color: var(--cis-primary);
}

.onboarding__theme-eyebrow {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.6px;
  text-transform: uppercase;
  opacity: 0.7;
}

.onboarding__theme-label {
  font-size: 13px;
  font-weight: 600;
}

/* ===== 行动按钮 ===== */
.onboarding__actions {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 32px;
}

.onboarding__btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  height: 40px;
  padding: 0 18px;
  border: 1px solid var(--cis-border-strong);
  border-radius: var(--cis-radius-btn);
  background: var(--cis-surface-1);
  color: var(--cis-text-primary);
  font-size: 14px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: border-color var(--cis-transition-fast), color var(--cis-transition-fast), background-color var(--cis-transition-fast);
}

.onboarding__btn:hover:not(:disabled) {
  border-color: var(--cis-primary);
  color: var(--cis-primary);
}

.onboarding__btn--primary {
  background: var(--cis-primary);
  border-color: var(--cis-primary);
  color: #fff;
}

.onboarding__btn--primary:hover:not(:disabled) {
  background: var(--cis-primary-hover);
  border-color: var(--cis-primary-hover);
  color: #fff;
}

.onboarding__btn--accent {
  background: var(--cis-accent);
  border-color: var(--cis-accent);
  color: #fff;
}

.onboarding__btn--accent:hover:not(:disabled) {
  background: var(--cis-accent-hover);
  border-color: var(--cis-accent-hover);
  color: #fff;
}

.onboarding__btn-arrow {
  font-size: 14px;
  transition: transform var(--cis-transition-fast);
  display: inline-block;
}

.onboarding__btn:hover:not(:disabled) .onboarding__btn-arrow {
  transform: translateX(2px);
}

@media (prefers-reduced-motion: reduce) {
  .onboarding__step-pane,
  .onboarding__btn,
  .onboarding__btn-arrow {
    animation: none;
    transition: none;
  }
}
</style>
