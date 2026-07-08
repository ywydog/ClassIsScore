<template>
  <el-dialog
    v-model="visible"
    title="渡劫"
    width="420px"
    :close-on-click-modal="false"
    :close-on-press-escape="!isTribulating"
    :show-close="!isTribulating"
    class="tribulation-dialog"
    @close="handleClose"
  >
    <div v-if="!isTribulating && !tribulationResult" class="tribulation-confirm">
      <div class="tribulation-icon" aria-hidden="true">⚡</div>
      <h3 class="tribulation-title" id="tribulation-title">
        {{ isPetTribulation ? '仙宠渡劫' : '道友渡劫' }}
      </h3>
      <div class="tribulation-info" role="group" aria-labelledby="tribulation-title">
        <div class="tribulation-info-row">
          <span class="tribulation-info-label">目标</span>
          <span class="tribulation-info-value">{{ targetName }}</span>
        </div>
        <div class="tribulation-info-row">
          <span class="tribulation-info-label">成功率</span>
          <span class="tribulation-info-value tribulation-info-value--rate" :aria-label="`成功率 ${Math.round(successRate * 100)}%`">
            {{ Math.round(successRate * 100) }}%
          </span>
        </div>
      </div>

      <div class="tribulation-warning">
        <div class="tribulation-warning-title">失败惩罚</div>
        <div class="tribulation-warning-items">
          <span class="tribulation-warning-item">40%经验 — 天劫降世</span>
          <span class="tribulation-warning-item">20%经验 — 雷劫反噬</span>
          <span class="tribulation-warning-item">10%经验 — 心魔侵扰</span>
          <span class="tribulation-warning-item">5%经验 — 劫雷擦身</span>
          <span class="tribulation-warning-item">0%经验 — 全身而退</span>
        </div>
      </div>
    </div>

    <!-- 渡劫动画 -->
    <div v-else-if="isTribulating" class="tribulation-animation" aria-live="polite" aria-label="天劫降临">
      <div class="tribulation-lightning" v-for="i in lightningCount" :key="i"
        :style="{ animationDelay: `${i * 0.3}s` }" aria-hidden="true">
        ⚡
      </div>
      <div class="tribulation-animation-text">天劫降临…</div>
    </div>

    <!-- 渡劫结果 -->
    <div v-else class="tribulation-result" aria-live="polite" role="status">
      <div v-if="tribulationResult?.success" class="tribulation-result--success">
        <div class="tribulation-result-icon" aria-hidden="true">✨</div>
        <h3>渡劫成功！</h3>
        <p>{{ tribulationResult?.description }}</p>
      </div>
      <div v-else class="tribulation-result--fail">
        <div class="tribulation-result-icon" aria-hidden="true">💀</div>
        <h3>渡劫失败</h3>
        <p>{{ tribulationResult?.description }}</p>
        <p class="tribulation-result-penalty">
          损失 {{ Math.round((tribulationResult?.penaltyRate ?? 0) * 100) }}% 经验
        </p>
      </div>
    </div>

    <template #footer>
      <template v-if="!isTribulating && !tribulationResult">
        <el-button aria-label="取消" @click="handleClose">取消</el-button>
        <el-button type="primary" aria-label="开始渡劫" @click="startTribulation" class="tribulation-start-btn">
          开始渡劫
        </el-button>
      </template>
      <template v-else-if="tribulationResult">
        <el-button type="primary" aria-label="确定" @click="handleClose">确定</el-button>
      </template>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

interface TribulationResult {
  success: boolean
  penaltyRate: number
  description: string
}

const props = defineProps<{
  modelValue: boolean
  targetName: string
  successRate: number
  isPetTribulation?: boolean
  onExecute: () => TribulationResult
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'result': [result: TribulationResult]
}>()

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val),
})

const isTribulating = ref(false)
const tribulationResult = ref<TribulationResult | null>(null)
const lightningCount = computed(() => props.isPetTribulation ? (props.targetName.includes('仙尊') ? 9 : 3) : 9)

async function startTribulation() {
  isTribulating.value = true
  tribulationResult.value = null

  // 模拟渡劫过程
  await new Promise(resolve => setTimeout(resolve, 2000))

  const result = props.onExecute()
  tribulationResult.value = result
  isTribulating.value = false
  emit('result', result)
}

function handleClose() {
  if (isTribulating.value) return
  tribulationResult.value = null
  visible.value = false
}
</script>

<style scoped>
.tribulation-confirm {
  text-align: center;
}

.tribulation-icon {
  font-size: 48px;
  margin-bottom: 12px;
  animation: pulse-glow 1.5s infinite ease-in-out;
}

@keyframes pulse-glow {
  0%, 100% { transform: scale(1); opacity: 0.8; }
  50% { transform: scale(1.1); opacity: 1; }
}

.tribulation-title {
  font-size: 20px;
  margin: 0 0 16px;
  color: #C9A84C;
}

.tribulation-info {
  margin-bottom: 16px;
}

.tribulation-info-row {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.tribulation-info-label {
  color: rgba(255, 255, 255, 0.5);
}

.tribulation-info-value {
  font-weight: 600;
  color: #fff;
}

.tribulation-info-value--rate {
  color: #C9A84C;
  font-size: 18px;
  font-variant-numeric: tabular-nums;
}

.tribulation-warning {
  background: rgba(239, 68, 68, 0.08);
  border: 1px solid rgba(239, 68, 68, 0.2);
  border-radius: 8px;
  padding: 12px;
  text-align: left;
}

.tribulation-warning-title {
  font-size: 13px;
  font-weight: 600;
  color: #f87171;
  margin-bottom: 8px;
}

.tribulation-warning-items {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.tribulation-warning-item {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
}

.tribulation-start-btn {
  background: linear-gradient(135deg, #C9A84C, #8B6914) !important;
  border-color: #C9A84C !important;
}

.tribulation-animation {
  text-align: center;
  padding: 40px 0;
  position: relative;
  min-height: 200px;
}

.tribulation-lightning {
  font-size: 36px;
  position: absolute;
  animation: lightning-strike 0.6s ease-out forwards;
  opacity: 0;
}

.tribulation-lightning:nth-child(1) { left: 30%; top: 20%; }
.tribulation-lightning:nth-child(2) { left: 55%; top: 40%; }
.tribulation-lightning:nth-child(3) { left: 40%; top: 60%; }
.tribulation-lightning:nth-child(4) { left: 60%; top: 15%; }
.tribulation-lightning:nth-child(5) { left: 25%; top: 50%; }
.tribulation-lightning:nth-child(6) { left: 70%; top: 35%; }
.tribulation-lightning:nth-child(7) { left: 45%; top: 70%; }
.tribulation-lightning:nth-child(8) { left: 35%; top: 30%; }
.tribulation-lightning:nth-child(9) { left: 65%; top: 55%; }

@keyframes lightning-strike {
  0% { opacity: 0; transform: scale(0.5) translateY(-20px); }
  30% { opacity: 1; transform: scale(1.3); }
  60% { opacity: 0.8; transform: scale(0.9); }
  100% { opacity: 0; transform: scale(0.5) translateY(20px); }
}

.tribulation-animation-text {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  font-size: 18px;
  font-weight: 600;
  color: #C9A84C;
  animation: text-pulse 0.8s infinite ease-in-out;
}

@keyframes text-pulse {
  0%, 100% { opacity: 0.6; }
  50% { opacity: 1; }
}

.tribulation-result {
  text-align: center;
  padding: 20px 0;
}

.tribulation-result-icon {
  font-size: 48px;
  margin-bottom: 12px;
}

.tribulation-result h3 {
  font-size: 22px;
  margin: 0 0 8px;
}

.tribulation-result--success h3 {
  color: #4ade80;
}

.tribulation-result--fail h3 {
  color: #f87171;
}

.tribulation-result p {
  color: rgba(255, 255, 255, 0.7);
  margin: 4px 0;
}

.tribulation-result-penalty {
  color: #f87171 !important;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
</style>
