<template>
  <transition name="settings-slide">
    <div v-if="visible" class="display-settings-panel">
      <div class="display-settings-panel__header">
        <h3>显示设置</h3>
        <button class="display-settings-panel__close" @click="emit('update:visible', false)">
          <el-icon :size="16"><Close /></el-icon>
        </button>
      </div>

      <div class="display-settings-panel__body">
        <!-- 背景主题 -->
        <div class="display-settings-panel__section">
          <div class="display-settings-panel__label">背景主题</div>
          <el-radio-group v-model="settings.background" size="small" @change="emit('save-settings')">
            <el-radio-button value="deepblue">深蓝</el-radio-button>
            <el-radio-button value="pureblack">纯黑</el-radio-button>
            <el-radio-button value="warmgray">暖灰</el-radio-button>
            <el-radio-button value="custom">自定义</el-radio-button>
          </el-radio-group>
          <div v-if="settings.background === 'custom'" class="display-settings-panel__color-picker">
            <input type="color" v-model="settings.customColor" @input="emit('save-settings')" />
          </div>
        </div>

        <!-- 展示模式 -->
        <div class="display-settings-panel__section">
          <div class="display-settings-panel__label">展示模式</div>
          <el-radio-group :model-value="displayMode" size="small" class="display-settings-panel__mode" @update:model-value="emit('update:displayMode', $event)">
            <el-radio-button value="Card">卡片</el-radio-button>
            <el-radio-button value="Circle">圆形</el-radio-button>
            <el-radio-button value="Pet">宠物</el-radio-button>
          </el-radio-group>
        </div>

        <!-- 字体大小 -->
        <div class="display-settings-panel__section">
          <div class="display-settings-panel__label">字体大小</div>
          <el-radio-group v-model="settings.fontSize" size="small" @change="emit('save-settings')">
            <el-radio-button value="small">小</el-radio-button>
            <el-radio-button value="medium">中</el-radio-button>
            <el-radio-button value="large">大</el-radio-button>
            <el-radio-button value="xlarge">特大</el-radio-button>
          </el-radio-group>
        </div>

        <!-- 排行榜条目数 -->
        <div class="display-settings-panel__section">
          <div class="display-settings-panel__label">排行榜条目数</div>
          <el-radio-group v-model="settings.maxItems" size="small" @change="emit('save-settings')">
            <el-radio-button :value="5">5</el-radio-button>
            <el-radio-button :value="10">10</el-radio-button>
            <el-radio-button :value="15">15</el-radio-button>
            <el-radio-button :value="20">20</el-radio-button>
          </el-radio-group>
        </div>

        <!-- 自动刷新间隔 -->
        <div class="display-settings-panel__section">
          <div class="display-settings-panel__label">自动刷新间隔</div>
          <el-radio-group v-model="settings.refreshInterval" size="small" @change="emit('update:refreshInterval', settings.refreshInterval)">
            <el-radio-button :value="10">10s</el-radio-button>
            <el-radio-button :value="30">30s</el-radio-button>
            <el-radio-button :value="60">60s</el-radio-button>
            <el-radio-button :value="0">关闭</el-radio-button>
          </el-radio-group>
        </div>

        <!-- 显示/隐藏开关 -->
        <div class="display-settings-panel__section">
          <div class="display-settings-panel__label">显示元素</div>
          <div class="display-settings-panel__switches">
            <label class="display-settings-panel__switch">
              <span>时钟</span>
              <input type="checkbox" v-model="settings.showClock" @change="emit('save-settings')" />
            </label>
            <label class="display-settings-panel__switch">
              <span>排名数字</span>
              <input type="checkbox" v-model="settings.showRank" @change="emit('save-settings')" />
            </label>
            <label class="display-settings-panel__switch">
              <span>分数</span>
              <input type="checkbox" v-model="settings.showScore" @change="emit('save-settings')" />
            </label>
          </div>
        </div>

        <!-- 全屏按钮 -->
        <div class="display-settings-panel__section">
          <button class="display-settings-panel__fullscreen-btn" @click="emit('toggle-fullscreen')">
            {{ isFullscreen ? '退出全屏' : '进入全屏' }}
          </button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { Close } from '@element-plus/icons-vue'

export interface DisplaySettings {
  background: 'deepblue' | 'pureblack' | 'warmgray' | 'custom'
  customColor: string
  fontSize: 'small' | 'medium' | 'large' | 'xlarge'
  maxItems: number
  refreshInterval: number
  showClock: boolean
  showRank: boolean
  showScore: boolean
}

defineProps<{
  visible: boolean
  settings: DisplaySettings
  displayMode: string
  isFullscreen: boolean
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  'save-settings': []
  'update:displayMode': [value: string]
  'toggle-fullscreen': []
  'update:refreshInterval': [value: number]
}>()
</script>

<style scoped>
.display-settings-panel {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  width: 320px;
  z-index: 150;
  background: rgba(10, 22, 40, 0.88);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border-left: 1px solid rgba(13, 148, 136, 0.2);
  box-shadow: -8px 0 32px rgba(0, 0, 0, 0.4);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.display-settings-panel * {
  cursor: auto;
}

.display-settings-panel button,
.display-settings-panel input,
.display-settings-panel label {
  cursor: pointer;
}

.display-settings-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.display-settings-panel__header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #fff;
}

.display-settings-panel__close {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
}

.display-settings-panel__close:hover {
  background: rgba(255, 255, 255, 0.12);
  color: #fff;
}

.display-settings-panel__body {
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.display-settings-panel__section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.display-settings-panel__label {
  font-size: 13px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.5);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.display-settings-panel__section :deep(.el-radio-button__inner) {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.12);
  color: rgba(255, 255, 255, 0.5);
  font-size: 12px;
  padding: 6px 12px;
}

.display-settings-panel__section :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
  background: linear-gradient(135deg, #0d9488, #14b8a6);
  border-color: #0d9488;
  color: #fff;
}

.display-settings-panel__color-picker {
  margin-top: 4px;
}

.display-settings-panel__color-picker input[type="color"] {
  width: 48px;
  height: 32px;
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  padding: 2px;
}

.display-settings-panel__switches {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.display-settings-panel__switch {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.8);
}

.display-settings-panel__switch input[type="checkbox"] {
  width: 40px;
  height: 22px;
  appearance: none;
  -webkit-appearance: none;
  background: rgba(255, 255, 255, 0.12);
  border-radius: 11px;
  position: relative;
  transition: background 0.2s ease;
  cursor: pointer;
}

.display-settings-panel__switch input[type="checkbox"]::after {
  content: '';
  position: absolute;
  top: 2px;
  left: 2px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.6);
  transition: transform 0.2s ease, background 0.2s ease;
}

.display-settings-panel__switch input[type="checkbox"]:checked {
  background: rgba(13, 148, 136, 0.6);
}

.display-settings-panel__switch input[type="checkbox"]:checked::after {
  transform: translateX(18px);
  background: #2dd4bf;
}

.display-settings-panel__fullscreen-btn {
  width: 100%;
  padding: 10px 0;
  border-radius: 10px;
  border: 1px solid rgba(13, 148, 136, 0.3);
  background: rgba(13, 148, 136, 0.1);
  color: #2dd4bf;
  font-size: 14px;
  font-weight: 600;
  transition: all 0.2s ease;
}

.display-settings-panel__fullscreen-btn:hover {
  background: rgba(13, 148, 136, 0.2);
  border-color: rgba(13, 148, 136, 0.5);
  box-shadow: 0 2px 12px rgba(13, 148, 136, 0.2);
}

/* 设置面板滑入动画 */
.settings-slide-enter-active,
.settings-slide-leave-active {
  transition: transform 0.35s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.35s ease;
}

.settings-slide-enter-from,
.settings-slide-leave-to {
  transform: translateX(100%);
  opacity: 0;
}
</style>
