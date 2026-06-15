import { invoke } from './tauri'
import type { LeaderboardEntry, Student } from '@/types'

interface RustLeaderboardEntry {
  student: {
    id: number
    name: string
    student_number: string | null
    group_id: number | null
    total_score: number
    avatar: string | null
    pet_type: string | null
    pet_name: string | null
    pet_exp: number
    created_at: string
    updated_at: string
  }
  rank: number
}

interface RustIndividualStats {
  student: {
    id: number
    name: string
    student_number: string | null
    group_id: number | null
    total_score: number
    avatar: string | null
    pet_type: string | null
    pet_name: string | null
    pet_exp: number
    created_at: string
    updated_at: string
  }
  total_positive: number
  total_negative: number
  record_count: number
}

function toLeaderboardEntry(r: RustLeaderboardEntry): LeaderboardEntry {
  return {
    rank: r.rank,
    name: r.student.name,
    score: r.student.total_score,
    isGroup: false,
  }
}

export const leaderboardApi = {
  async query() {
    const entries = await invoke<RustLeaderboardEntry[]>('leaderboard_query', {})
    return { data: { data: entries.map(toLeaderboardEntry) } }
  },

  async byGroup(groupId: string) {
    const entries = await invoke<RustLeaderboardEntry[]>('leaderboard_by_group', {
      group_id: Number(groupId),
    })
    return { data: { data: entries.map(toLeaderboardEntry) } }
  },

  async individual(studentId: string) {
    const stats = await invoke<RustIndividualStats>('leaderboard_individual', {
      student_id: Number(studentId),
    })
    return { data: { data: stats } }
  },
}