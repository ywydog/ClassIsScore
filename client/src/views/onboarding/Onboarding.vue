<template>
  <div class="onboarding">
    <div class="onboarding__bg">
      <div class="onboarding__particle" v-for="i in 8" :key="i" :style="particleStyle(i)"></div>
    </div>
    <div class="onboarding__content">
      <!-- 步骤指示器 -->
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
        <div class="onboarding__icon">
          <el-icon :size="64" color="#409eff"><Trophy /></el-icon>
        </div>
        <h1 class="onboarding__title">欢迎使用 ClassIsScore</h1>
        <p class="onboarding__description">
          一款面向课堂场景的积分管理工具，<br />
          让课堂互动更有趣、更高效。
        </p>
        <div class="onboarding__features">
          <div class="onboarding__feature">
            <el-icon :size="20"><User /></el-icon>
            <span>学生管理</span>
          </div>
          <div class="onboarding__feature">
            <el-icon :size="20"><Trophy /></el-icon>
            <span>积分系统</span>
          </div>
          <div class="onboarding__feature">
            <el-icon :size="20"><Rank /></el-icon>
            <span>实时排行</span>
          </div>
          <div class="onboarding__feature">
            <el-icon :size="20"><Monitor /></el-icon>
            <span>大屏展示</span>
          </div>
        </div>
        <el-button type="primary" size="large" @click="currentStep = 1" round>
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
          <el-button @click="currentStep = 0">上一步</el-button>
          <el-button type="primary" @click="currentStep = 2">下一步</el-button>
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
          <el-button @click="currentStep = 1">上一步</el-button>
          <el-button type="primary" @click="currentStep = 3">下一步</el-button>
        </div>
      </div>

      <!-- 步骤4: 完成 -->
      <div v-else-if="currentStep === 3" class="onboarding__step">
        <div class="onboarding__icon">
          <el-icon :size="64" color="#67c23a"><SuccessFilled /></el-icon>
        </div>
        <h2 class="onboarding__subtitle">设置完成!</h2>
        <p class="onboarding__description">
          你已准备好使用 ClassIsScore 管理课堂积分。
        </p>
        <el-button type="primary" size="large" @click="handleComplete" round>
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

function particleStyle(i: number) {
  const size = 80 + i * 40
  const x = (i * 13) % 100
  const y = (i * 19) % 100
  return {
    width: `${size}px`,
    height: `${size}px`,
    left: `${x}%`,
    top: `${y}%`,
    animationDelay: `${i * 1.5}s`,
  }
}

async function handleComplete() {
  try {
    // 保存主题设置
    await settingsStore.updateSettings({ theme: setupForm.theme })

    // 批量创建学生
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
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
  position: relative;
  overflow: hidden;
}

.onboarding__bg {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.onboarding__particle {
  position: absolute;
  border-radius: 50%;
  background: rgba(64, 158, 255, 0.05);
  animation: float 18s infinite ease-in-out;
}

@keyframes float {
  0%, 100% { transform: translateY(0) scale(1); opacity: 0.2; }
  50% { transform: translateY(-30px) scale(1.05); opacity: 0.4; }
}

.onboarding__content {
  position: relative;
  z-index: 1;
  text-align: center;
  max-width: 520px;
  width: 100%;
  padding: 40px;
}

.onboarding__steps {
  display: flex;
  justify-content: center;
  gap: 12px;
  margin-bottom: 40px;
}

.onboarding__step-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.2);
  transition: all 0.3s;
}

.onboarding__step-dot.active {
  background: #409eff;
  width: 24px;
  border-radius: 4px;
}

.onboarding__step-dot.done {
  background: #67c23a;
}

.onboarding__step {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.onboarding__icon {
  margin-bottom: 20px;
}

.onboarding__title {
  font-size: 32px;
  font-weight: 700;
  color: #fff;
  margin: 0 0 16px;
  background: linear-gradient(135deg, #409eff, #79bbff);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.onboarding__subtitle {
  font-size: 24px;
  font-weight: 600;
  color: #fff;
  margin: 0 0 16px;
}

.onboarding__description {
  font-size: 16px;
  color: rgba(255, 255, 255, 0.7);
  margin: 0 0 32px;
  line-height: 1.6;
}

.onboarding__hint {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.5);
  margin: 0 0 16px;
}

.onboarding__features {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  margin-bottom: 32px;
}

.onboarding__feature {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  background: rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  color: rgba(255, 255, 255, 0.8);
  font-size: 14px;
}

.onboarding__form-card {
  background: rgba(255, 255, 255, 0.95);
  padding: 24px;
  border-radius: 12px;
  margin-bottom: 24px;
  width: 100%;
  text-align: left;
}

.onboarding__actions {
  display: flex;
  justify-content: center;
  gap: 12px;
}
</style>
