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

          <!-- 密码验证设置 -->
          <template v-if="adminSettings.verificationMethod === 'Password' && adminSettings.isEnabled">
            <el-form-item label="设置密码">
              <div class="password-section">
                <el-input
                  v-model="newPassword"
                  type="password"
                  placeholder="请输入新密码"
                  show-password
                  style="flex: 1"
                />
                <el-input
                  v-model="confirmPassword"
                  type="password"
                  placeholder="确认密码"
                  show-password
                  style="flex: 1; margin-left: 8px"
                />
                <el-button type="primary" @click="handleSetPassword" :disabled="!newPassword || !confirmPassword">
                  设置密码
                </el-button>
              </div>
            </el-form-item>
            <el-form-item v-if="hasPassword" label="密码状态">
              <el-tag type="success">已设置密码</el-tag>
            </el-form-item>
          </template>

          <!-- U盘验证设置 -->
          <template v-if="adminSettings.verificationMethod === 'Usb' && adminSettings.isEnabled">
            <el-form-item label="U盘设备">
              <div class="usb-section">
                <el-button @click="detectUsb" :loading="detectingUsb">
                  <el-icon style="margin-right: 4px"><Monitor /></el-icon>
                  检测U盘
                </el-button>
                <span v-if="adminSettings.usbDeviceId" class="usb-bound">
                  已绑定: {{ adminSettings.usbDeviceId }}
                </span>
                <span v-else class="usb-unbound">未绑定U盘设备</span>
              </div>
            </el-form-item>
            <el-form-item label="设备ID">
              <el-input
                v-model="manualUsbDeviceId"
                placeholder="手动输入U盘设备ID"
                clearable
                style="max-width: 300px"
              />
              <el-button style="margin-left: 8px" @click="handleBindUsb" :disabled="!manualUsbDeviceId">
                绑定
              </el-button>
            </el-form-item>
            <el-alert
              type="info"
              :closable="false"
              show-icon
              style="margin-bottom: 16px"
            >
              <template #title>
                U盘验证的自动检测功能暂不可用，请手动输入设备ID进行绑定。
              </template>
            </el-alert>
          </template>

          <!-- 人脸验证设置 -->
          <template v-if="adminSettings.verificationMethod === 'Face' && adminSettings.isEnabled">
            <el-form-item label="人脸验证">
              <div class="face-section">
                <el-button disabled>
                  <el-icon style="margin-right: 4px"><Camera /></el-icon>
                  录入人脸
                </el-button>
              </div>
            </el-form-item>
            <el-alert
              type="warning"
              :closable="false"
              show-icon
              style="margin-bottom: 16px"
            >
              <template #title>
                人脸验证功能需要原生模块支持，当前版本暂不可用。该功能已预留接口，后续版本将支持。
              </template>
            </el-alert>
          </template>

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
      <template v-if="adminSettings.verificationMethod === 'Password'">
        <el-input v-model="verifyCredential" type="password" placeholder="请输入管理员密码" show-password />
      </template>
      <template v-else-if="adminSettings.verificationMethod === 'Usb'">
        <el-input v-model="verifyCredential" placeholder="请输入U盘设备ID" />
        <p style="color: var(--cis-text-secondary); font-size: 13px; margin-top: 8px">
          请插入已绑定的U盘设备，或手动输入设备ID
        </p>
      </template>
      <template v-else-if="adminSettings.verificationMethod === 'Face'">
        <p style="color: var(--cis-text-secondary); text-align: center; padding: 20px 0">
          人脸验证功能暂未实现，点击验证将直接通过
        </p>
      </template>
      <template #footer>
        <el-button @click="showVerifyDialog = false">取消</el-button>
        <el-button type="primary" @click="handleVerify" :loading="verifying">验证</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Monitor, Camera } from '@element-plus/icons-vue'
import { settingsApi } from '@/services/settings'
import { VerificationMethod } from '@/types'
import type { AdminSettings } from '@/types'

interface AdminSettingsExtended extends AdminSettings {
  hasPassword?: boolean
  usbDeviceId?: string
}

const adminSettings = reactive<AdminSettingsExtended>({
  isEnabled: false,
  verificationMethod: VerificationMethod.Password,
  isPasswordEnabled: true,
  isUsbEnabled: false,
  isFaceEnabled: false,
  hasPassword: false,
  usbDeviceId: '',
})

const newPassword = ref('')
const confirmPassword = ref('')
const hasPassword = ref(false)
const manualUsbDeviceId = ref('')
const saving = ref(false)
const detectingUsb = ref(false)
const verifying = ref(false)
const showVerifyDialog = ref(false)
const verifyCredential = ref('')

onMounted(async () => {
  try {
    const response = await settingsApi.getAdminSettings()
    const data = response.data.data
    if (data) {
      adminSettings.isEnabled = data.isEnabled ?? false
      adminSettings.verificationMethod = data.verificationMethod ?? VerificationMethod.Password
      adminSettings.isPasswordEnabled = data.isPasswordEnabled ?? true
      adminSettings.isUsbEnabled = data.isUsbEnabled ?? false
      adminSettings.isFaceEnabled = data.isFaceEnabled ?? false
      adminSettings.usbDeviceId = data.usbDeviceId ?? ''
      hasPassword.value = data.hasPassword ?? false
    }
  } catch {
    // first use
  }
})

async function handleSave() {
  saving.value = true
  try {
    await settingsApi.updateAdminSettings({
      isEnabled: adminSettings.isEnabled,
      verificationMethod: adminSettings.verificationMethod,
      isPasswordEnabled: adminSettings.verificationMethod === 'Password',
      isUsbEnabled: adminSettings.verificationMethod === 'Usb',
      isFaceEnabled: adminSettings.verificationMethod === 'Face',
      usbDeviceId: adminSettings.usbDeviceId,
    })
    ElMessage.success('管理员设置已保存')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    saving.value = false
  }
}

async function handleSetPassword() {
  if (!newPassword.value || !confirmPassword.value) {
    ElMessage.warning('请输入并确认密码')
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    ElMessage.error('两次输入的密码不一致')
    return
  }
  if (newPassword.value.length < 4) {
    ElMessage.warning('密码长度不能少于4位')
    return
  }
  try {
    await settingsApi.setPassword(newPassword.value)
    hasPassword.value = true
    newPassword.value = ''
    confirmPassword.value = ''
    ElMessage.success('密码设置成功')
  } catch {
    ElMessage.error('密码设置失败')
  }
}

async function detectUsb() {
  detectingUsb.value = true
  try {
    ElMessage.info('该功能暂不可用，请手动输入设备ID')
  } finally {
    detectingUsb.value = false
  }
}

function handleBindUsb() {
  if (!manualUsbDeviceId.value) {
    ElMessage.warning('请输入U盘设备ID')
    return
  }
  adminSettings.usbDeviceId = manualUsbDeviceId.value
  manualUsbDeviceId.value = ''
  ElMessage.success('U盘设备已绑定')
}

async function handleVerify() {
  verifying.value = true
  try {
    const method = adminSettings.verificationMethod
    const credential = verifyCredential.value || 'face-placeholder'
    const response = await settingsApi.verifyAdmin(method, credential)
    if (response.data.data) {
      ElMessage.success('验证成功')
      showVerifyDialog.value = false
      verifyCredential.value = ''
    } else {
      ElMessage.error('验证失败')
    }
  } catch {
    ElMessage.error('验证失败')
  } finally {
    verifying.value = false
  }
}

async function handleResetAll() {
  await ElMessageBox.confirm(
    '此操作将清除所有学生、积分和设置数据，且不可恢复！',
    '危险操作',
    { type: 'error', confirmButtonText: '确认重置', cancelButtonText: '取消' }
  )
  try {
    await settingsApi.resetAll()
    ElMessage.success('数据已重置')
  } catch {
    ElMessage.error('重置失败')
  }
}
</script>

<style scoped>
.admin-settings__header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.admin-settings__header h2 {
  margin: 0;
  font-family: var(--cis-font-family-display);
  font-size: 22px;
  color: var(--cis-text-primary);
  padding-left: 12px;
  border-left: 3px solid var(--cis-primary);
  background: linear-gradient(135deg, var(--cis-primary), var(--cis-primary-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.admin-settings__card {
  max-width: 600px;
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.admin-settings__card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.password-section {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 0;
}

.usb-section {
  display: flex;
  align-items: center;
  gap: 12px;
}

.usb-bound {
  color: var(--cis-text-secondary);
  font-size: 13px;
}

.usb-unbound {
  color: var(--cis-text-placeholder);
  font-size: 13px;
}

.face-section {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
