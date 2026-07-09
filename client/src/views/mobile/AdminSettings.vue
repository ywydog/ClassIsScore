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
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { invoke } from '@/services/tauri'

const revertWindow = ref(3)
const showPasswordDialog = ref(false)
const newPassword = ref('')
const confirmPassword = ref('')

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
