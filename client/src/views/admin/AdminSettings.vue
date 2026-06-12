<template>
  <div class="admin-settings">
    <div class="admin-settings__header">
      <h2>管理员设置</h2>
    </div>

    <div class="admin-settings__content">
      <el-card class="admin-settings__card">
        <template #header>
          <span>验证方式</span>
        </template>
        <el-form label-width="100px">
          <el-form-item label="启用验证">
            <el-switch v-model="adminSettings.isEnabled" />
          </el-form-item>
          <el-form-item label="验证方式">
            <el-radio-group v-model="adminSettings.verificationMethod">
              <el-radio-button value="Password">密码</el-radio-button>
              <el-radio-button value="Usb">U盘</el-radio-button>
              <el-radio-button value="Face">人脸</el-radio-button>
            </el-radio-group>
          </el-form-item>
          <el-form-item v-if="adminSettings.verificationMethod === 'Password'" label="设置密码">
            <el-input v-model="password" type="password" placeholder="请输入管理员密码" show-password />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSave">保存</el-button>
          </el-form-item>
        </el-form>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { settingsApi } from '@/services/settings'
import { VerificationMethod } from '@/types'
import type { AdminSettings } from '@/types'

const adminSettings = reactive<AdminSettings>({
  isEnabled: false,
  verificationMethod: VerificationMethod.Password,
  isPasswordEnabled: true,
  isUsbEnabled: false,
  isFaceEnabled: false,
})

const password = ref('')

onMounted(async () => {
  try {
    const response = await settingsApi.getAdminSettings()
    Object.assign(adminSettings, response.data.data)
  } catch {
    // 首次使用可能没有设置
  }
})

async function handleSave() {
  try {
    await settingsApi.updateAdminSettings(adminSettings)
    ElMessage.success('管理员设置已保存')
  } catch {
    ElMessage.error('保存失败')
  }
}
</script>

<style scoped>
.admin-settings__header {
  margin-bottom: 20px;
}

.admin-settings__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

.admin-settings__card {
  max-width: 600px;
}
</style>
