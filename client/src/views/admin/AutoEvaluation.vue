<template>
  <div class="auto-evaluation">
    <div class="auto-evaluation__header">
      <h2>自动评估</h2>
    </div>

    <div class="auto-evaluation__content">
      <el-card class="auto-evaluation__config-card">
        <template #header>
          <span>评估配置</span>
        </template>
        <el-form label-width="100px">
          <el-form-item label="启用自动评估">
            <el-switch v-model="config.enabled" />
          </el-form-item>
          <el-form-item label="评估周期">
            <el-select v-model="config.period">
              <el-option label="每日" value="daily" />
              <el-option label="每周" value="weekly" />
              <el-option label="每月" value="monthly" />
            </el-select>
          </el-form-item>
          <el-form-item label="评估规则">
            <el-input
              v-model="config.rules"
              type="textarea"
              :rows="4"
              placeholder="配置评估规则"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSave">保存配置</el-button>
          </el-form-item>
        </el-form>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue'
import { ElMessage } from 'element-plus'

const config = reactive({
  enabled: false,
  period: 'daily',
  rules: '',
})

function handleSave() {
  ElMessage.success('配置已保存')
}
</script>

<style scoped>
.auto-evaluation__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.auto-evaluation__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

.auto-evaluation__config-card {
  max-width: 600px;
}
</style>
