<template>
  <div class="onboarding">
    <div class="onboarding__bg">
      <div class="onboarding__orb" v-for="i in 6" :key="i" :style="orbStyle(i)"></div>
    </div>
    <div class="onboarding__content">
      <div class="onboarding__steps">
        <div
          v-for="i in 4"
          :key="i"
          class="onboarding__step-dot"
          :class="{ active: currentStep === i - 1, done: currentStep > i - 1 }"
        ></div>
      </div>

      <!-- 步骤1: 欢迎 -->
      <div v-if="currentStep === 0" class="onboarding__step">
        <div class="onboarding__icon-wrapper">
          <div class="onboarding__icon">
            <el-icon :size="48"><Trophy /></el-icon>
          </div>
        </div>
        <h1 class="onboarding__title">欢迎使用 ClassIsScore</h1>
        <p class="onboarding__description">
          面向课堂场景的积分管理工具<br />
          让课堂互动更有趣、更高效
        </p>
        <div class="onboarding__features">
          <div class="onboarding__feature">
            <div class="onboarding__feature-icon" style="background: linear-gradient(135deg, #0d9488, #14b8a6)">
              <el-icon :size="18"><User /></el-icon>
            </div>
            <span>学生管理</span>
          </div>
          <div class="onboarding__feature">
            <div class="onboarding__feature-icon" style="background: linear-gradient(135deg, #6366f1, #818cf8)">
              <el-icon :size="18"><Trophy /></el-icon>
            </div>
            <span>积分系统</span>
          </div>
          <div class="onboarding__feature">
            <div class="onboarding__feature-icon" style="background: linear-gradient(135deg, #f59e0b, #f97316)">
              <el-icon :size="18"><Rank /></el-icon>
            </div>
            <span>实时排行</span>
          </div>
          <div class="onboarding__feature">
            <div class="onboarding__feature-icon" style="background: linear-gradient(135deg, #22c55e, #10b981)">
              <el-icon :size="18"><Monitor /></el-icon>
            </div>
            <span>大屏展示</span>
          </div>
        </div>
        <el-button type="primary" size="large" @click="currentStep = 1" round class="onboarding__start-btn">
          开始设置
        </el-button>
      </div>

      <!-- 步骤2: 基本设置 -->
      <div v-else-if="currentStep === 1" class="onboarding__step">
        <h2 class="onboarding__subtitle">基本设置</h2>
        <div class="onboarding__form-card">
          <el-form :model="setupForm" label-width="80px">
            <el-form-item label="班级名称">
              <el-input v-model="setupForm.className" placeholder="如：高一(3)班" />
            </el-form-item>
            <el-form-item label="主题">
              <el-radio-group v-model="setupForm.theme">
                <el-radio-button value="light">浅色</el-radio-button>
                <el-radio-button value="dark">深色</el-radio-button>
                <el-radio-button value="system">跟随系统</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-form>
        </div>
        <div class="onboarding__actions">
          <el-button @click="currentStep = 0" round>上一步</el-button>
          <el-button type="primary" @click="currentStep = 2" round>下一步</el-button>
        </div>
      </div>

      <!-- 步骤3: 添加学生 -->
      <div v-else-if="currentStep === 2" class="onboarding__step">
        <h2 class="onboarding__subtitle">添加学生</h2>
        <p class="onboarding__hint">每行输入一个学生姓名，也可以稍后在管理页面添加</p>
        <div class="onboarding__form-card">
          <el-input
            v-model="studentInput"
            type="textarea"
            :rows="6"
            placeholder="张三&#10;李四&#10;王五"
          />
        </div>
        <div class="onboarding__actions">
          <el-button @click="currentStep = 1" round>上一步</el-button>
          <el-button type="primary" @click="currentStep = 3" round>下一步</el-button>
        </div>
      </div>

      <!-- 步骤4: 完成 -->
      <div v-else-if="currentStep === 3" class="onboarding__step">
        <div class="onboarding__icon-wrapper">
          <div class="onboarding__icon onboarding__icon--success">
            <el-icon :size="48"><SuccessFilled /></el-icon>
          </div>
        </div>
        <h2 class="onboarding__subtitle">设置完成!</h2>
        <p class="onboarding__description">
          你已准备好使用 ClassIsScore 管理课堂积分
        </p>
        <el-button type="primary" size="large" @click="handleComplete" round class="onboarding__start-btn">
          进入应用
        </el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { Trophy, User, Rank, Monitor, SuccessFilled } from '@element-plus/icons-vue'
import { useSettingsStore } from '@/stores/settings'
import { useStudentStore } from '@/stores/student'
import { ElMessage } from 'element-plus'

const router = useRouter()
const settingsStore = useSettingsStore()
const studentStore = useStudentStore()

const currentStep = ref(0)
const studentInput = ref('')

const setupForm = reactive({
  className: '',
  theme: 'light' as 'light' | 'dark' | 'system',
})

function orbStyle(i: number) {
  const size = 100 + i * 60
  const x = (i * 17 + 5) % 85
  const y = (i * 23 + 10) % 80
  return {
    width: `${size}px`,
    height: `${size}px`,
    left: `${x}%`,
    top: `${y}%`,
    animationDelay: `${i * 2}s`,
    animationDuration: `${16 + i * 3}s`,
  }
}

async function handleComplete() {
  try {
    await settingsStore.updateSettings({ theme: setupForm.theme })
    if (studentInput.value.trim()) {
      const names = studentInput.value.trim().split('\n').filter(n => n.trim())
      if (names.length > 0) {
        const students = names.map(name => ({ name: name.trim() }))
        await studentStore.batchCreateStudent(students)
        ElMessage.success(`已添加 ${names.length} 名学生`)
      }
    }
    router.replace('/admin/scores')
  } catch {
    ElMessage.error('设置保存失败，请重试')
  }
}
</script>

<style scoped>
.onboarding {
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(160deg, #0a1628 0%, #0d2137 40%, #0a2a3c 70%, #061a2e 100%);
  position: relative;
  overflow: hidden;
}

.onboarding__bg {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.onboarding__orb {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(13, 148, 136, 0.1) 0%, transparent 70%);
  animation: orb-drift 18s infinite ease-in-out;
}

@keyframes orb-drift {
  0%, 100% { transform: translate(0, 0) scale(1); opacity: 0.3; }
  50% { transform: translate(15px, -20px) scale(1.05); opacity: 0.5; }
}

.onboarding__content {
  position: relative;
  z-index: 1;
  text-align: center;
  max-width: 480px;
  width: 100%;
  padding: 40px;
}

.onboarding__steps {
  display: flex;
  justify-content: center;
  gap: 8px;
  margin-bottom: 40px;
}

.onboarding__step-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.15);
  transition: all var(--cis-transition-normal, 0.25s ease);
}

.onboarding__step-dot.active {
  background: #2dd4bf;
  width: 28px;
  border-radius: 4px;
  box-shadow: 0 0 12px rgba(45, 212, 191, 0.3);
}

.onboarding__step-dot.done {
  background: #22c55e;
}

.onboarding__step {
  display: flex;
  flex-direction: column;
  align-items: center;
  animation: step-in 0.4s ease;
}

@keyframes step-in {
  from { opacity: 0; transform: translateY(12px); }
  to { opacity: 1; transform: translateY(0); }
}

.onboarding__icon-wrapper {
  margin-bottom: 24px;
}

.onboarding__icon {
  width: 80px;
  height: 80px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  border-radius: 20px;
  color: #fff;
  box-shadow: 0 8px 32px rgba(13, 148, 136, 0.3);
}

.onboarding__icon--success {
  background: linear-gradient(135deg, #22c55e, #10b981);
  box-shadow: 0 8px 32px rgba(34, 197, 94, 0.3);
}

.onboarding__title {
  font-family: var(--cis-font-family-display, serif);
  font-size: 30px;
  font-weight: 700;
  color: #fff;
  margin: 0 0 12px;
  background: linear-gradient(135deg, #2dd4bf, #5eead4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.onboarding__subtitle {
  font-size: 22px;
  font-weight: 600;
  color: #fff;
  margin: 0 0 16px;
}

.onboarding__description {
  font-size: 15px;
  color: rgba(255, 255, 255, 0.55);
  margin: 0 0 28px;
  line-height: 1.7;
}

.onboarding__hint {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.4);
  margin: 0 0 16px;
}

.onboarding__features {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin-bottom: 28px;
}

.onboarding__feature {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 10px;
  border: 1px solid rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.75);
  font-size: 13px;
  font-weight: 500;
}

.onboarding__feature-icon {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  color: #fff;
  flex-shrink: 0;
}

.onboarding__form-card {
  background: rgba(255, 255, 255, 0.95);
  padding: 24px;
  border-radius: 14px;
  margin-bottom: 20px;
  width: 100%;
  text-align: left;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
}

.onboarding__actions {
  display: flex;
  justify-content: center;
  gap: 12px;
}

.onboarding__start-btn {
  padding: 12px 40px;
  font-size: 15px;
  font-weight: 600;
  background: linear-gradient(135deg, #0d9488, #14b8a6) !important;
  border-color: #0d9488 !important;
}

.onboarding__start-btn:hover {
  background: linear-gradient(135deg, #14b8a6, #2dd4bf) !important;
  border-color: #14b8a6 !important;
}
</style>
