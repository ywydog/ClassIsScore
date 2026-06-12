import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { ScoreRecord, ScoreUpdateEvent } from '@/types'
import { scoreApi } from '@/services/score'
import { connectWebSocket, disconnectWebSocket } from '@/services/websocket'

export const useScoreStore = defineStore('score', () => {
  const scoreRecords = ref<ScoreRecord[]>([])
  const currentStudentId = ref<string | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const currentStudentRecords = computed(() => {
    if (!currentStudentId.value) return []
    return scoreRecords.value.filter((r) => r.studentId === currentStudentId.value)
  })

  const recentRecords = computed(() => {
    return [...scoreRecords.value]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 50)
  })

  async function fetchRecords(studentId?: string) {
    loading.value = true
    error.value = null
    try {
      const response = await scoreApi.getRecords(studentId)
      scoreRecords.value = response.data.data
    } catch (err) {
      error.value = err instanceof Error ? err.message : '获取积分记录失败'
    } finally {
      loading.value = false
    }
  }

  async function addScore(studentId: string, scoreChange: number, reason: string) {
    loading.value = true
    error.value = null
    try {
      const response = await scoreApi.addScore({ studentId, scoreChange, reason })
      scoreRecords.value.unshift(response.data.data)
    } catch (err) {
      error.value = err instanceof Error ? err.message : '添加积分失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function batchAddScore(studentIds: string[], scoreChange: number, reason: string) {
    loading.value = true
    error.value = null
    try {
      const response = await scoreApi.batchAddScore({ studentIds, scoreChange, reason })
      await fetchRecords(currentStudentId.value ?? undefined)
      return response.data.data
    } catch (err) {
      error.value = err instanceof Error ? err.message : '批量添加积分失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function revertScore(recordId: string) {
    loading.value = true
    error.value = null
    try {
      await scoreApi.revertScore(recordId)
      const record = scoreRecords.value.find((r) => r.id === recordId)
      if (record) {
        record.isReverted = true
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : '撤销积分失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  function onScoreUpdate(event: ScoreUpdateEvent) {
    const newRecord: ScoreRecord = {
      id: '',
      studentId: event.studentId,
      studentName: event.studentName,
      scoreChange: event.scoreChange,
      reason: event.reason,
      createdAt: new Date().toISOString(),
      isReverted: false,
      canQuickRevert: true,
      needsAdminRevert: false,
    }
    scoreRecords.value.unshift(newRecord)
  }

  function setupWebSocket() {
    connectWebSocket({
      onScoreUpdate: onScoreUpdate,
    })
  }

  function teardownWebSocket() {
    disconnectWebSocket()
  }

  return {
    scoreRecords,
    currentStudentId,
    loading,
    error,
    currentStudentRecords,
    recentRecords,
    fetchRecords,
    addScore,
    batchAddScore,
    revertScore,
    onScoreUpdate,
    setupWebSocket,
    teardownWebSocket,
  }
})
