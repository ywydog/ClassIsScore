<template>
  <div class="settings-page">
    <div class="settings-page__header">
      <h2>设置</h2>
    </div>

    <div class="settings-page__content">
      <el-card class="settings-page__card">
        <template #header>
          <span>通用设置</span>
        </template>
        <el-form label-width="100px">
          <el-form-item label="主题">
            <el-select v-model="settings.theme" @change="handleThemeChange">
              <el-option label="浅色" value="light" />
              <el-option label="深色" value="dark" />
              <el-option label="跟随系统" value="system" />
            </el-select>
          </el-form-item>
          <el-form-item label="字体大小">
            <el-slider v-model="settings.fontSize" :min="12" :max="20" :step="1" show-input />
          </el-form-item>
          <el-form-item label="显示模式">
            <el-radio-group v-model="settings.displayMode">
              <el-radio-button value="Card">卡片</el-radio-button>
              <el-radio-button value="Circle">圆形</el-radio-button>
              <el-radio-button value="Pet">宠物</el-radio-button>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="主题色">
            <el-color-picker v-model="settings.customAccentColor" />
          </el-form-item>
        </el-form>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useSettingsStore } from '@/stores/settings'
import { DisplayMode } from '@/types'

const settingsStore = useSettingsStore()

const settings = reactive({
  theme: 'light' as 'light' | 'dark' | 'system',
  fontSize: 14,
  displayMode: DisplayMode.Card,
  customAccentColor: '',
})

onMounted(async () => {
  await settingsStore.fetchSettings()
  Object.assign(settings, settingsStore.settings)
})

async function handleThemeChange(theme: 'light' | 'dark' | 'system') {
  await settingsStore.updateSettings({ theme })
}
</script>

<style scoped>
.settings-page__header {
  margin-bottom: 20px;
}

.settings-page__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

.settings-page__card {
  max-width: 600px;
}
</style>
