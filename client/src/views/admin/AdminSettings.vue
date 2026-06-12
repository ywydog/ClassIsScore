<template>
  <div class="admin-settings">
    <div class="admin-settings__header">
      <h2>管理员设置</h2>
    </div>

    <div class="admin-settings__content">
      <el-card class="admin-settings__card">
        <template #header>
          <div class="card-header">
            <span>验证方式</span>
            <el-switch v-model="adminSettings.isEnabled" active-text="启用保护" inactive-text="未启用" />
          </div>
        </template>
        <el-form label-width="100px">
          <el-form-item label="验证方式">
            <el-radio-group v-model="adminSettings.verificationMethod" :disabled="!adminSettings.isEnabled">
              <el-radio-button value="Password">密码</el-radio-button>
              <el-radio-button value="Usb">U盘</el-radio-button>
              <el-radio-button value="Face">人脸</el-radio-button>
            </el-radio-group>
          </el-form-item>

          <el-form-item v-if="adminSettings.verificationMethod === 'Password' && adminSettings.isEnabled" label="设置密码">
            <el-input v-model="password" type="password" placeholder="请输入管理员密码" show-password />
          </el-form-item>

          <el-form-item v-if="adminSettings.verificationMethod === 'Usb' && adminSettings.isEnabled" label="U盘设备">
            <el-button @click="detectUsb">检测U盘</el-button>
            <span v-if="adminSettings.usbDeviceId" style="margin-left: 12px; color: var(--cis-text-secondary); font-size: 13px">
              已绑定: {{ adminSettings.usbDeviceId }}
            </span>
          </el-form-item>

          <el-form-item>
            <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
            <el-button v-if="adminSettings.isEnabled" @click="showVerifyDialog = true">验证设置</el-button>
          </el-form-item>
        </el-form>
      </el-card>

      <el-card class="admin-settings__card" style="margin-top: 16px">
        <template #header>
          <span>危险操作</span>
        </template>
        <el-button type="danger" @click="handleResetAll">重置所有数据</el-button>
      </el-card>
    </div>

    <!-- 验证对话框 -->
    <el-dialog v-model="showVerifyDialog" title="验证管理员身份" width="400px">
      <el-input v-model="verifyPassword" type="password" placeholder="请输入管理员密码" show-password />
      <template #footer>
        <el-button @click="showVerifyDialog = false">取消</el-button>
        <el-button type="primary" @click="handleVerify">验证</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { settingsApi } from '@/services/settings'
import { VerificationMethod } from '@/types'
import type { AdminSettings } from '@/types'
import api from '@/services/api'

const adminSettings = reactive<AdminSettings>({
  isEnabled: false,
  verificationMethod: VerificationMethod.Password,
  isPasswordEnabled: true,
  isUsbEnabled: false,
  isFaceEnabled: false,
})

const password = ref('')
const saving = ref(false)
const showVerifyDialog = ref(false)
const verifyPassword = ref('')

onMounted(async () => {
  try {
    const response = await settingsApi.getAdminSettings()
    Object.assign(adminSettings, response.data.data)
  } catch {
    // first use
  }
})

async function handleSave() {
  saving.value = true
  try {
    await settingsApi.updateAdminSettings({
      ...adminSettings,
      passwordHash: password.value || undefined,
    })
    ElMessage.success('管理员设置已保存')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    saving.value = false
  }
}

async function handleVerify() {
  try {
    const response = await settingsApi.verifyAdmin(verifyPassword.value)
    if (response.data.data) {
      ElMessage.success('验证成功')
      showVerifyDialog.value = false
    } else {
      ElMessage.error('密码错误')
    }
  } catch {
    ElMessage.error('验证失败')
  }
}

function detectUsb() {
  ElMessage.info('U盘检测功能需要Electron环境支持')
}

async function handleResetAll() {
  await ElMessageBox.confirm(
    '此操作将清除所有学生、积分和设置数据，且不可恢复！',
    '危险操作',
    { type: 'error', confirmButtonText: '确认重置', cancelButtonText: '取消' }
  )
  try {
    await api.post('/api/admin/reset')
    ElMessage.success('数据已重置')
  } catch {
    ElMessage.error('重置失败')
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

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
