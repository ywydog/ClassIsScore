<template>
  <div class="m-admin">
    <header class="m-admin__head">
      <span class="cis-eyebrow">Admin</span>
      <h1 class="cis-display m-admin__title">管理员</h1>
    </header>
    <ul class="m-admin__list" role="list">
      <li class="m-admin__row">
        <div class="m-admin__row-body">
          <span class="m-admin__row-label">撤销时限</span>
          <span class="m-admin__row-desc">超过此时限的撤销需管理员密码</span>
        </div>
        <el-input-number
          v-model="revertWindow"
          :min="1"
          :max="60"
          :step="1"
          controls-position="right"
          aria-label="撤销时限（分钟）"
        />
      </li>
      <li class="m-admin__row">
        <div class="m-admin__row-body">
          <span class="m-admin__row-label">修改管理员密码</span>
          <span class="m-admin__row-desc">用于高风险操作验证</span>
        </div>
        <el-button @click="showPasswordDialog = true">修改</el-button>
      </li>
      <li class="m-admin__row">
        <div class="m-admin__row-body">
          <span class="m-admin__row-label">网络伺服 PIN</span>
          <span class="m-admin__row-desc">
            {{ hasNetworkPin ? '已启用 HTTP Bearer 鉴权' : '未启用（拒绝任何网络访问）' }}
          </span>
        </div>
        <el-button @click="showNetworkPinDialog = true">
          {{ hasNetworkPin ? '修改' : '设置' }}
        </el-button>
      </li>
      <li v-if="hasNetworkPin" class="m-admin__row">
        <div class="m-admin__row-body">
          <span class="m-admin__row-label">清除网络伺服 PIN</span>
          <span class="m-admin__row-desc">立即关闭网络伺服并拒绝所有请求</span>
        </div>
        <el-button type="danger" plain @click="clearNetworkPin">清除</el-button>
      </li>
    </ul>
    <el-dialog v-model="showPasswordDialog" title="修改管理员密码" width="320px" destroy-on-close>
      <el-form label-width="100px">
        <el-form-item label="新密码" required>
          <el-input v-model="newPassword" type="password" show-password aria-label="新密码" />
        </el-form-item>
        <el-form-item label="确认" required>
          <el-input v-model="confirmPassword" type="password" show-password aria-label="确认密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showPasswordDialog = false">取消</el-button>
        <el-button type="primary" @click="changePassword">确定</el-button>
      </template>
    </el-dialog>
    <el-dialog
      v-model="showNetworkPinDialog"
      :title="hasNetworkPin ? '修改网络伺服 PIN' : '设置网络伺服 PIN'"
      width="320px"
      destroy-on-close
    >
      <el-form label-width="100px">
        <el-form-item label="新 PIN" required>
          <el-input
            v-model="newNetworkPin"
            type="password"
            show-password
            aria-label="网络伺服 PIN"
            autocomplete="new-password"
          />
        </el-form-item>
        <el-form-item label="确认" required>
          <el-input
            v-model="confirmNetworkPin"
            type="password"
            show-password
            aria-label="确认网络伺服 PIN"
            autocomplete="new-password"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showNetworkPinDialog = false">取消</el-button>
        <el-button type="primary" @click="saveNetworkPin">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { invoke } from '@/services/tauri'
import { settingsApi } from '@/services/settings'

const revertWindow = ref(3)
const showPasswordDialog = ref(false)
const newPassword = ref('')
const confirmPassword = ref('')
const showNetworkPinDialog = ref(false)
const newNetworkPin = ref('')
const confirmNetworkPin = ref('')
const hasNetworkPin = ref(false)

onMounted(async () => {
  try {
    hasNetworkPin.value = await settingsApi.hasNetworkPin()
  } catch {
    hasNetworkPin.value = false
  }
})

async function changePassword() {
  if (newPassword.value !== confirmPassword.value) { ElMessage.warning('两次密码不一致'); return }
  if (newPassword.value.length < 6) { ElMessage.warning('密码至少 6 位'); return }
  try {
    await invoke('admin_change_password', { password: newPassword.value })
    ElMessage.success('已修改')
    showPasswordDialog.value = false
    newPassword.value = ''
    confirmPassword.value = ''
  } catch {
    ElMessage.error('修改失败')
  }
}

async function saveNetworkPin() {
  if (!newNetworkPin.value || !confirmNetworkPin.value) {
    ElMessage.warning('请输入并确认 PIN')
    return
  }
  if (newNetworkPin.value !== confirmNetworkPin.value) {
    ElMessage.warning('两次 PIN 不一致')
    return
  }
  if (newNetworkPin.value.length < 4) {
    ElMessage.warning('PIN 至少 4 位')
    return
  }
  if (newNetworkPin.value.length > 64) {
    ElMessage.warning('PIN 不能超过 64 位')
    return
  }
  try {
    const response = await settingsApi.setNetworkPin(newNetworkPin.value)
    const result = response.data.data
    if (result?.success) {
      hasNetworkPin.value = true
      ElMessage.success(result.message || '已保存')
      showNetworkPinDialog.value = false
      newNetworkPin.value = ''
      confirmNetworkPin.value = ''
    } else {
      ElMessage.error(result?.message || '保存失败')
    }
  } catch {
    ElMessage.error('保存失败')
  }
}

async function clearNetworkPin() {
  await ElMessageBox.confirm(
    '清除网络伺服 PIN 将立即停止当前网络伺服，并拒绝所有后续 HTTP 请求。是否继续？',
    '清除网络 PIN',
    { type: 'warning', confirmButtonText: '确认清除', cancelButtonText: '取消' }
  )
  try {
    const response = await settingsApi.setNetworkPin('')
    const result = response.data.data
    if (result?.success) {
      hasNetworkPin.value = false
      newNetworkPin.value = ''
      confirmNetworkPin.value = ''
      ElMessage.success(result.message || '已清除')
    } else {
      ElMessage.error(result?.message || '清除失败')
    }
  } catch {
    ElMessage.error('清除失败')
  }
}
</script>

<style scoped>
.m-admin { display: flex; flex-direction: column; gap: 16px; }
.m-admin__title { font-size: 28px; margin: 4px 0 0; font-weight: 600; }
.m-admin__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-admin__row { display: flex; align-items: center; gap: 12px; min-height: 64px; padding: 12px 16px; background: var(--cis-surface-1); border-bottom: 1px solid var(--cis-border-light); }
.m-admin__list li:last-child .m-admin__row { border-bottom: none; }
.m-admin__row-body { flex: 1; display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.m-admin__row-label { font-size: 15px; font-weight: 500; }
.m-admin__row-desc { font-size: 12px; color: var(--cis-text-tertiary); }
</style>
