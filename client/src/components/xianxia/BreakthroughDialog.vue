<template>
  <el-dialog
    v-model="visible"
    :title="needsTribulation ? '渡劫突破' : '境界突破'"
    width="440px"
    :close-on-click-modal="false"
    :close-on-press-escape="!isBreaking && !isTribulating"
    :show-close="!isBreaking && !isTribulating"
    class="breakthrough-dialog"
    @close="handleClose"
  >
    <!-- 突破确认/展示 -->
    <div v-if="!isBreaking && !isTribulating && !breakthroughResult" class="breakthrough-confirm">
      <div class="breakthrough-icon" aria-hidden="true">{{ needsTribulation ? '⚡' : '🌟' }}</div>
      <div class="breakthrough-level-change" id="breakthrough-level-title">
        <span class="breakthrough-level-old">{{ oldLevelName }}</span>
        <span class="breakthrough-arrow" aria-hidden="true">→</span>
        <span class="breakthrough-level-new">{{ newLevelName }}</span>
      </div>

      <div v-if="needsTribulation" class="breakthrough-tribulation-info">
        <div class="breakthrough-info-row">
          <span class="breakthrough-info-label">突破成功率</span>
          <span class="breakthrough-info-value breakthrough-info-value--rate" :aria-label="`突破成功率 ${Math.round(successRate * 100)}%`">
            {{ Math.round(successRate * 100) }}%
          </span>
        </div>
        <div class="breakthrough-warning">
          <div class="breakthrough-warning-title">渡劫失败将损失修为</div>
          <div class="breakthrough-warning-items">
            <span class="breakthrough-warning-item">40%修为 — 天劫降世</span>
            <span class="breakthrough-warning-item">20%修为 — 雷劫反噬</span>
            <span class="breakthrough-warning-item">10%修为 — 心魔侵扰</span>
            <span class="breakthrough-warning-item">5%修为 — 劫雷擦身</span>
            <span class="breakthrough-warning-item">0%修为 — 全身而退</span>
          </div>
        </div>
      </div>

      <div v-else class="breakthrough-normal-info">
        <p class="breakthrough-guarantee">此境界突破为确定性成功</p>
      </div>
    </div>

    <!-- 突破动画 -->
    <div v-else-if="isBreaking" class="breakthrough-animation" aria-live="polite" aria-label="突破中">
      <div class="breakthrough-particles" v-for="i in 12" :key="i"
        :style="{ animationDelay: `${i * 0.1}s` }" aria-hidden="true">
        ✦
      </div>
      <div class="breakthrough-animation-text">突破中…</div>
    </div>

    <!-- 渡劫动画（复用渡劫逻辑） -->
    <div v-else-if="isTribulating" class="breakthrough-animation" aria-live="polite" aria-label="天劫降临">
      <div class="breakthrough-lightning" v-for="i in 9" :key="i"
        :style="{ animationDelay: `${i * 0.3}s` }" aria-hidden="true">
        ⚡
      </div>
      <div class="breakthrough-animation-text">天劫降临…</div>
    </div>

    <!-- 突破结果 -->
    <div v-else-if="breakthroughResult" class="breakthrough-result" aria-live="polite" role="status">
      <div v-if="breakthroughResult.success" class="breakthrough-result--success">
        <div class="breakthrough-result-icon" aria-hidden="true">🎉</div>
        <h3>突破成功！</h3>
        <div class="breakthrough-level-change">
          <span class="breakthrough-level-old">{{ oldLevelName }}</span>
          <span class="breakthrough-arrow" aria-hidden="true">→</span>
          <span class="breakthrough-level-new">{{ newLevelName }}</span>
        </div>
      </div>
      <div v-else class="breakthrough-result--fail">
        <div class="breakthrough-result-icon" aria-hidden="true">💥</div>
        <h3>突破失败</h3>
        <p>{{ breakthroughResult.description }}</p>
        <p class="breakthrough-result-penalty">
          损失 {{ Math.round(breakthroughResult.penaltyRate * 100) }}% 修为
        </p>
      </div>
    </div>

    <template #footer>
      <template v-if="!isBreaking && !isTribulating && !breakthroughResult">
        <el-button aria-label="取消" @click="handleClose">取消</el-button>
        <el-button
          :type="needsTribulation ? 'warning' : 'primary'"
          :aria-label="needsTribulation ? '开始渡劫' : '确认突破'"
          @click="startBreakthrough"
          class="breakthrough-start-btn"
        >
          {{ needsTribulation ? '开始渡劫' : '确认突破' }}
        </el-button>
      </template>
      <template v-else-if="breakthroughResult">
        <el-button type="primary" aria-label="确定" @click="handleClose">确定</el-button>
      </template>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { isTribulationRequired, calculateBreakthroughSuccessRate, executeBreakthroughTribulation } from '@/utils/cultivationSystem'

interface BreakthroughResult {
  success: boolean
  penaltyRate: number
  description: string
}

const props = defineProps<{
  modelValue: boolean
  oldLevelName: string
  newLevelName: string
  newLevelId: number
  cultivation: number
  targetExp: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'success': []
  'fail': [penaltyRate: number]
}>()

const visible = computed({
  get: () => props.modelValue,
  set: (val: boolean) => emit('update:modelValue', val),
})

const needsTribulation = computed(() => isTribulationRequired(props.newLevelId))
const successRate = computed(() =>
  calculateBreakthroughSuccessRate(props.newLevelId, props.cultivation, props.targetExp)
)

const isBreaking = ref(false)
const isTribulating = ref(false)
const breakthroughResult = ref<BreakthroughResult | null>(null)

async function startBreakthrough() {
  if (!needsTribulation.value) {
    // 普通突破：确定性成功
    isBreaking.value = true
    await new Promise(resolve => setTimeout(resolve, 1500))
    breakthroughResult.value = { success: true, penaltyRate: 0, description: '突破成功！' }
    isBreaking.value = false
    emit('success')
  } else {
    // 渡劫突破：概率判定
    isTribulating.value = true
    await new Promise(resolve => setTimeout(resolve, 2500))
    const result = executeBreakthroughTribulation(props.newLevelId, props.cultivation, props.targetExp)
    breakthroughResult.value = result
    isTribulating.value = false
    if (result.success) {
      emit('success')
    } else {
      emit('fail', result.penaltyRate)
    }
  }
}

function handleClose() {
  if (isBreaking.value || isTribulating.value) return
  breakthroughResult.value = null
  visible.value = false
}
</script>

<style scoped>
.breakthrough-confirm {
  text-align: center;
}

.breakthrough-icon {
  font-size: 48px;
  margin-bottom: 12px;
  animation: pulse-glow 1.5s infinite ease-in-out;
}

@keyframes pulse-glow {
  0%, 100% { transform: scale(1); opacity: 0.8; }
  50% { transform: scale(1.1); opacity: 1; }
}

.breakthrough-level-change {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  margin: 16px 0;
  font-size: 18px;
}

.breakthrough-level-old {
  color: rgba(255, 255, 255, 0.5);
}

.breakthrough-arrow {
  color: #C9A84C;
  font-size: 24px;
}

.breakthrough-level-new {
  color: #C9A84C;
  font-weight: 700;
  font-size: 20px;
}

.breakthrough-tribulation-info {
  margin-top: 16px;
}

.breakthrough-info-row {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.breakthrough-info-label {
  color: rgba(255, 255, 255, 0.5);
}

.breakthrough-info-value--rate {
  color: #C9A84C;
  font-size: 18px;
  font-weight: 700;
}

.breakthrough-warning {
  background: rgba(239, 68, 68, 0.08);
  border: 1px solid rgba(239, 68, 68, 0.2);
  border-radius: 8px;
  padding: 12px;
  text-align: left;
  margin-top: 12px;
}

.breakthrough-warning-title {
  font-size: 13px;
  font-weight: 600;
  color: #f87171;
  margin-bottom: 8px;
}

.breakthrough-warning-items {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.breakthrough-warning-item {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
}

.breakthrough-normal-info {
  margin-top: 12px;
}

.breakthrough-guarantee {
  color: #4ade80;
  font-size: 14px;
}

.breakthrough-start-btn {
  background: linear-gradient(135deg, #C9A84C, #8B6914) !important;
  border-color: #C9A84C !important;
}

.breakthrough-animation {
  text-align: center;
  padding: 40px 0;
  position: relative;
  min-height: 200px;
}

.breakthrough-particles {
  font-size: 20px;
  position: absolute;
  color: #C9A84C;
  animation: particle-rise 1.2s ease-out forwards;
  opacity: 0;
}

.breakthrough-particles:nth-child(odd) { left: 30%; }
.breakthrough-particles:nth-child(even) { left: 60%; }

@keyframes particle-rise {
  0% { opacity: 0; transform: translateY(40px) scale(0.5); }
  40% { opacity: 1; transform: translateY(-10px) scale(1.2); }
  100% { opacity: 0; transform: translateY(-60px) scale(0.3); }
}

.breakthrough-lightning {
  font-size: 36px;
  position: absolute;
  animation: lightning-strike 0.6s ease-out forwards;
  opacity: 0;
}

.breakthrough-lightning:nth-child(1) { left: 30%; top: 20%; }
.breakthrough-lightning:nth-child(2) { left: 55%; top: 40%; }
.breakthrough-lightning:nth-child(3) { left: 40%; top: 60%; }
.breakthrough-lightning:nth-child(4) { left: 60%; top: 15%; }
.breakthrough-lightning:nth-child(5) { left: 25%; top: 50%; }
.breakthrough-lightning:nth-child(6) { left: 70%; top: 35%; }
.breakthrough-lightning:nth-child(7) { left: 45%; top: 70%; }
.breakthrough-lightning:nth-child(8) { left: 35%; top: 30%; }
.breakthrough-lightning:nth-child(9) { left: 65%; top: 55%; }

@keyframes lightning-strike {
  0% { opacity: 0; transform: scale(0.5) translateY(-20px); }
  30% { opacity: 1; transform: scale(1.3); }
  60% { opacity: 0.8; transform: scale(0.9); }
  100% { opacity: 0; transform: scale(0.5) translateY(20px); }
}

.breakthrough-animation-text {
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

.breakthrough-result {
  text-align: center;
  padding: 20px 0;
}

.breakthrough-result-icon {
  font-size: 48px;
  margin-bottom: 12px;
}

.breakthrough-result h3 {
  font-size: 22px;
  margin: 0 0 8px;
}

.breakthrough-result--success h3 {
  color: #4ade80;
}

.breakthrough-result--fail h3 {
  color: #f87171;
}

.breakthrough-result p {
  color: rgba(255, 255, 255, 0.7);
  margin: 4px 0;
}

.breakthrough-result-penalty {
  color: #f87171 !important;
  font-weight: 600;
}

@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
</style>
