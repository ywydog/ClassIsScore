<template>
  <div class="network-serve-toggle">
    <el-tooltip
      :content="isRunning ? `网络伺服运行中 · ${statusUrl}` : '启动网络伺服，让局域网设备通过浏览器访问'"
      placement="bottom"
    >
      <el-button
        :icon="Connection"
        :type="isRunning ? 'primary' : 'default'"
        text
        size="small"
        class="network-serve-toggle__btn"
        :class="{ 'is-running': isRunning }"
        :aria-label="isRunning ? '停止网络伺服' : '启动网络伺服'"
        :loading="loading"
        @click="toggle"
      />
    </el-tooltip>
    <transition name="network-info-fade">
      <div v-if="isRunning && statusUrl" class="network-serve-toggle__url">
        <code class="network-serve-toggle__url-text">{{ statusUrl }}</code>
        <el-button
          text
          size="small"
          :icon="CopyDocument"
          class="network-serve-toggle__copy"
          aria-label="复制地址"
          @click="copyUrl"
        />
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Connection, CopyDocument } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { invoke } from '@/services/tauri'

interface ServerStatus {
  running: boolean
  port: number
  url: string | null
}

const isRunning = ref(false)
const statusUrl = ref('')
const loading = ref(false)

onMounted(async () => {
  try {
    const status = await invoke<ServerStatus>('server_status')
    isRunning.value = status.running
    statusUrl.value = status.url || ''
  } catch {
    // 非 Tauri 环境忽略
  }
})

async function toggle() {
  loading.value = true
  try {
    if (isRunning.value) {
      await invoke('server_stop')
      isRunning.value = false
      statusUrl.value = ''
      ElMessage.success('网络伺服已停止')
    } else {
      const status = await invoke<ServerStatus>('server_start')
      isRunning.value = status.running
      statusUrl.value = status.url || ''
      ElMessage.success(`网络伺服已启动 · ${statusUrl.value}`)
    }
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err)
    ElMessage.error(`操作失败：${msg}`)
  } finally {
    loading.value = false
  }
}

async function copyUrl() {
  if (!statusUrl.value) return
  try {
    await navigator.clipboard.writeText(statusUrl.value)
    ElMessage.success('地址已复制')
  } catch {
    // 安全最佳实践：Clipboard API 不可用时回退到 execCommand。
    // 该 API 已被主流浏览器标记 deprecated，仅在 HTTPS / 受信上下文可用，
    // 故仅作为最后 fallback 使用。statusUrl 是后端拼接的 IP+端口，不含用户输入。
    const input = document.createElement('input')
    input.value = statusUrl.value
    document.body.appendChild(input)
    input.select()
    document.execCommand('copy')
    document.body.removeChild(input)
    ElMessage.success('地址已复制')
  }
}
</script>

<style scoped>
.network-serve-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
}

.network-serve-toggle__btn {
  transition: color var(--cis-transition-fast);
}

.network-serve-toggle__btn.is-running {
  color: var(--cis-primary);
}

.network-serve-toggle__url {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  background: var(--cis-primary-tint);
  border-radius: var(--cis-radius-btn);
  border: 1px solid var(--cis-primary-border);
}

.network-serve-toggle__url-text {
  font-size: 11px;
  color: var(--cis-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 180px;
  font-family: var(--cis-mono);
}

.network-serve-toggle__copy {
  color: var(--cis-primary);
  flex-shrink: 0;
}

.network-info-fade-enter-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}
.network-info-fade-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.network-info-fade-enter-from {
  opacity: 0;
  transform: translateX(-8px);
}
.network-info-fade-leave-to {
  opacity: 0;
  transform: translateX(-8px);
}
</style>