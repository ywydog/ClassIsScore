<template>
  <el-popover
    :width="320"
    placement="bottom-end"
    :show-arrow="false"
    popper-class="display-settings-popover"
    trigger="click"
  >
    <template #reference>
      <button class="display-settings-trigger" :title="t('settings')">
        <el-icon><Setting /></el-icon>
      </button>
    </template>

    <div class="display-settings">
      <div class="display-settings__header">
        <h3 class="display-settings__title">{{ t('displaySettings') }}</h3>
        <p class="display-settings__subtitle">{{ t('displaySettingsHint') }}</p>
      </div>

      <!-- 排序方式 -->
      <section class="display-settings__section">
        <div class="display-settings__label">{{ t('sortBy') }}</div>
        <el-radio-group v-model="localSortBy" size="default" class="display-settings__radio-group">
          <el-radio-button value="rank">{{ t('sortByRank') }}</el-radio-button>
          <el-radio-button value="studentNumber">{{ t('sortByNumber') }}</el-radio-button>
          <el-radio-button value="pinyinFirstLetter">{{ t('sortByFirstLetter') }}</el-radio-button>
        </el-radio-group>
      </section>

      <!-- 隐私模式 -->
      <section class="display-settings__section">
        <div class="display-settings__label">{{ t('privacyMode') }}</div>
        <el-radio-group v-model="localPrivacy" size="default" class="display-settings__radio-group">
          <el-radio-button value="name">{{ t('showRealName') }}</el-radio-button>
          <el-radio-button value="alias">{{ t('showAlias') }}</el-radio-button>
          <el-radio-button value="number">{{ t('showNumber') }}</el-radio-button>
        </el-radio-group>
      </section>

      <!-- 显示项开关 -->
      <section class="display-settings__section">
        <div class="display-settings__label">{{ t('displayItems') }}</div>
        <div class="display-settings__toggles">
          <label class="display-settings__toggle">
            <el-switch v-model="localShowPet" size="default" />
            <span>{{ t('showPet') }}</span>
          </label>
          <label class="display-settings__toggle">
            <el-switch v-model="localShowGroup" size="default" />
            <span>{{ t('showGroup') }}</span>
          </label>
          <label class="display-settings__toggle">
            <el-switch v-model="localShowTrend" size="default" />
            <span>{{ t('showTrend') }}</span>
          </label>
        </div>
      </section>

      <!-- 刷新间隔 -->
      <section class="display-settings__section">
        <div class="display-settings__label">
          {{ t('refreshInterval') }}
          <span class="display-settings__value">{{ localRefreshInterval }}s</span>
        </div>
        <el-slider
          v-model="localRefreshInterval"
          :min="3"
          :max="60"
          :step="1"
          :show-tooltip="false"
          class="display-settings__slider"
        />
      </section>

      <!-- 高级设置入口 -->
      <section class="display-settings__section display-settings__section--footer">
        <button class="display-settings__link" @click="$emit('open-advanced')">
          <el-icon><Tools /></el-icon>
          <span>{{ t('advancedSettings') }}</span>
          <el-icon class="display-settings__link-arrow"><ArrowRight /></el-icon>
        </button>
      </section>
    </div>
  </el-popover>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { Setting, Tools, ArrowRight } from '@element-plus/icons-vue'
import { useTerminology } from '@/themes/xianxia/useTerminology'

const { t } = useTerminology()

const props = defineProps<{
  sortBy: 'rank' | 'studentNumber' | 'pinyinFirstLetter'
  privacy: 'name' | 'alias' | 'number'
  showPet: boolean
  showGroup: boolean
  showTrend: boolean
  refreshInterval: number
}>()

const emit = defineEmits<{
  'update:sortBy': [value: 'rank' | 'studentNumber' | 'pinyinFirstLetter']
  'update:privacy': [value: 'name' | 'alias' | 'number']
  'update:showPet': [value: boolean]
  'update:showGroup': [value: boolean]
  'update:showTrend': [value: boolean]
  'update:refreshInterval': [value: number]
  'open-advanced': []
  'save': []
}>()

// 本地副本 - 延迟提交避免每次切换都触发后端
const localSortBy = ref(props.sortBy)
const localPrivacy = ref(props.privacy)
const localShowPet = ref(props.showPet)
const localShowGroup = ref(props.showGroup)
const localShowTrend = ref(props.showTrend)
const localRefreshInterval = ref(props.refreshInterval)

watch(localSortBy, v => emit('update:sortBy', v))
watch(localPrivacy, v => emit('update:privacy', v))
watch(localShowPet, v => emit('update:showPet', v))
watch(localShowGroup, v => emit('update:showGroup', v))
watch(localShowTrend, v => emit('update:showTrend', v))
watch(localRefreshInterval, v => emit('update:refreshInterval', v))

watch(
  () => props.sortBy,
  v => (localSortBy.value = v)
)
watch(
  () => props.privacy,
  v => (localPrivacy.value = v)
)
watch(
  () => props.showPet,
  v => (localShowPet.value = v)
)
watch(
  () => props.showGroup,
  v => (localShowGroup.value = v)
)
watch(
  () => props.showTrend,
  v => (localShowTrend.value = v)
)
watch(
  () => props.refreshInterval,
  v => (localRefreshInterval.value = v)
)
</script>

<style scoped>
.display-settings-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: none;
  border-radius: var(--cis-radius-md);
  background: transparent;
  color: var(--cis-text-secondary);
  cursor: pointer;
  transition: all var(--cis-transition-fast);
}

.display-settings-trigger:hover {
  background: var(--cis-bg-secondary);
  color: var(--cis-text-primary);
}

.display-settings-trigger .el-icon {
  font-size: 18px;
}

.display-settings {
  font-family: var(--cis-font-family);
  color: var(--cis-text-primary);
}

.display-settings__header {
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--cis-border-color-light);
}

.display-settings__title {
  margin: 0 0 4px;
  font-size: 15px;
  font-weight: 600;
  color: var(--cis-text-primary);
}

.display-settings__subtitle {
  margin: 0;
  font-size: 12px;
  color: var(--cis-text-tertiary);
}

.display-settings__section {
  margin-bottom: 16px;
}

.display-settings__section:last-child {
  margin-bottom: 0;
}

.display-settings__section--footer {
  margin-top: 8px;
  padding-top: 12px;
  border-top: 1px solid var(--cis-border-color-light);
}

.display-settings__label {
  font-size: 12px;
  font-weight: 500;
  color: var(--cis-text-tertiary);
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.4px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.display-settings__value {
  color: var(--cis-primary);
  font-weight: 600;
  font-size: 13px;
  text-transform: none;
  letter-spacing: 0;
}

.display-settings__radio-group {
  width: 100%;
  display: flex;
}

.display-settings__radio-group :deep(.el-radio-button) {
  flex: 1;
}

.display-settings__radio-group :deep(.el-radio-button__inner) {
  width: 100%;
  padding: 6px 10px;
  font-size: 12px;
}

.display-settings__toggles {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.display-settings__toggle {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  font-size: 13px;
  color: var(--cis-text-primary);
}

.display-settings__slider {
  margin: 8px 0 0;
}

.display-settings__link {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px 10px;
  border: none;
  border-radius: var(--cis-radius-sm);
  background: transparent;
  color: var(--cis-text-primary);
  font-size: 13px;
  cursor: pointer;
  transition: background var(--cis-transition-fast);
}

.display-settings__link:hover {
  background: var(--cis-bg-secondary);
}

.display-settings__link-arrow {
  margin-left: auto;
  color: var(--cis-text-tertiary);
  font-size: 14px;
}
</style>
