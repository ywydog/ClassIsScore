<template>
  <div class="onboarding">
    <div class="onboarding__content">
      <div class="onboarding__step" v-if="currentStep === 0">
        <h1 class="onboarding__title">欢迎使用 ClassIsScore</h1>
        <p class="onboarding__description">
          一款面向课堂场景的积分管理工具，让课堂互动更有趣。
        </p>
        <el-button type="primary" size="large" @click="currentStep = 1">
          开始使用
        </el-button>
      </div>

      <div class="onboarding__step" v-else-if="currentStep === 1">
        <h2 class="onboarding__subtitle">基本设置</h2>
        <el-form :model="setupForm" label-width="100px" class="onboarding__form">
          <el-form-item label="班级名称">
            <el-input v-model="setupForm.className" placeholder="请输入班级名称" />
          </el-form-item>
          <el-form-item label="主题">
            <el-select v-model="setupForm.theme">
              <el-option label="浅色" value="light" />
              <el-option label="深色" value="dark" />
            </el-select>
          </el-form-item>
        </el-form>
        <div class="onboarding__actions">
          <el-button @click="currentStep = 0">上一步</el-button>
          <el-button type="primary" @click="handleComplete">完成</el-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useSettingsStore } from '@/stores/settings'

const router = useRouter()
const settingsStore = useSettingsStore()

const currentStep = ref(0)

const setupForm = reactive({
  className: '',
  theme: 'light' as 'light' | 'dark',
})

async function handleComplete() {
  await settingsStore.updateSettings({ theme: setupForm.theme })
  router.replace('/')
}
</script>

<style scoped>
.onboarding {
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--cis-primary-light-5), var(--cis-primary));
}

.onboarding__content {
  text-align: center;
  max-width: 480px;
  padding: 40px;
}

.onboarding__title {
  font-size: 32px;
  font-weight: 700;
  color: #fff;
  margin: 0 0 16px;
}

.onboarding__subtitle {
  font-size: 24px;
  font-weight: 600;
  color: #fff;
  margin: 0 0 24px;
}

.onboarding__description {
  font-size: 16px;
  color: rgba(255, 255, 255, 0.85);
  margin: 0 0 32px;
  line-height: 1.6;
}

.onboarding__form {
  text-align: left;
  background: rgba(255, 255, 255, 0.95);
  padding: 24px;
  border-radius: 12px;
  margin-bottom: 24px;
}

.onboarding__actions {
  display: flex;
  justify-content: center;
  gap: 12px;
}
</style>
