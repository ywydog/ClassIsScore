export interface Student {
  id: string
  name: string
  studentNumber?: string
  gender?: string
  avatar?: string
  score: number
  groupId?: string
  petType?: string
  petName?: string
  petExp: number
  createdAt: string
  updatedAt: string
}

export interface ScoreRecord {
  id: string
  studentId: string
  studentName: string
  scoreChange: number
  reason: string
  operator?: string
  createdAt: string
  isReverted: boolean
  canQuickRevert: boolean
  needsAdminRevert: boolean
}

export interface StudentGroup {
  id: string
  name: string
  description?: string
  studentIds: string[]
  createdAt: string
}

export interface EvaluationItem {
  id: string
  name: string
  scoreChange: number
  isPositive: boolean
  color?: string
  createdAt: string
}

export interface AutoEvaluationConfig {
  id: string
  name: string
  triggerType: 'Daily' | 'Weekly' | 'Monthly' | 'BeforeSettlement'
  triggerTime: string
  dayOfWeek: number | null
  dayOfMonth: number | null
  evaluationItemId: number | null
  scoreChange: number | null
  reason: string
  targetType: 'AllStudents' | 'SpecificGroup' | 'SpecificStudent'
  targetGroupId: number | null
  targetStudentId: number | null
  isEnabled: boolean
  createdAt: string
}

export interface SettlementRecord {
  id: string
  name?: string
  period?: string
  snapshotData?: string
  status?: number
  settledAt: string
  createdAt?: string
  studentCount: number
  totalScore: number
  backupFilePath: string
  isReverted: boolean
}

export interface LeaderboardEntry {
  rank: number
  name: string
  score: number
  isGroup: boolean
}

export interface PluginManifest {
  id: string
  name: string
  version: string
  author: string
  description: string
  entranceAssembly: string
}

export interface ThemeManifest {
  id: string
  name: string
  version: string
  author: string
  description: string
  targetApiVersion: string
}

export enum VerificationMethod {
  Password = 'Password',
  Usb = 'Usb',
  Face = 'Face',
}

export interface AdminSettings {
  isEnabled: boolean
  passwordHash?: string
  usbDeviceId?: string
  faceDataPath?: string
  verificationMethod: VerificationMethod
  isPasswordEnabled: boolean
  isUsbEnabled: boolean
  isFaceEnabled: boolean
}

export enum DisplayMode {
  Card = 'Card',
  Circle = 'Circle',
  Pet = 'Pet',
}

export enum PetModeBehavior {
  Coexist = 'Coexist',
  Replace = 'Replace',
}

export interface AppSettings {
  theme: 'light' | 'dark' | 'system'
  fontSize: number
  displayMode: DisplayMode
  customAccentColor?: string
  petStyle?: string
  petModeBehavior?: PetModeBehavior
  fontFamily?: string
  semesterStartDate?: string
  themeMode?: 'default' | 'xianxia'
}

export enum PetCategory {
  Normal = 'Normal',
  Mythical = 'Mythical',
}

export interface PetTypeInfo {
  id: string
  name: string
  category: PetCategory
  emoji: string
  description: string
}

export interface LevelProgress {
  current: number
  required: number
  percentage: number
  isMaxLevel: boolean
}

export interface PetState {
  petType: string
  level: number
  exp: number
  levelProgress: LevelProgress
}

export interface AppState {
  isOnboardingCompleted: boolean
  firstLaunchDate?: string
  appVersion?: string
}

export interface ApiResponse<T = unknown> {
  code: number
  message: string
  data: T
}

export interface PaginatedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ScoreUpdateEvent {
  studentId: string
  studentName: string
  scoreChange: number
  reason: string
  newScore: number
}

export interface StudentScoreStats {
  studentId: number
  studentName: string
  totalScore: number
  dayPlus: number
  dayMinus: number
  dayNet: number
  weekPlus: number
  weekMinus: number
  weekNet: number
  monthPlus: number
  monthMinus: number
  monthNet: number
  semesterPlus?: number
  semesterMinus?: number
  semesterNet?: number
}
