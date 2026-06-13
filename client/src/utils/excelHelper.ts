import * as XLSX from 'xlsx'

export interface ExcelColumn {
  header: string
  key: string
}

// ========== 报表导出相关类型 ==========

export type ReportDimension = 'day' | 'week' | 'month' | 'semester'
export type ScoreDisplayMode = 'net' | 'split'
export type TotalColumnType = 'rangeNet' | 'currentTotal'
export type WeekHeaderFormat = 'weekNumber' | 'dateRange'
export type SortOrder = 'desc' | 'asc'

export interface ReportConfig {
  dimension: ReportDimension
  startDate: Date
  endDate: Date
  scoreDisplayMode: ScoreDisplayMode
  totalColumns: TotalColumnType[]
  weekHeaderFormat: WeekHeaderFormat
  groupByGroup: boolean
  sortOrder: SortOrder
}

export interface ReportStudent {
  id: string
  name: string
  groupId?: string
  groupName?: string
  currentScore: number
}

export interface ReportScoreRecord {
  studentId: string
  scoreChange: number
  createdAt: string
  isReverted: boolean
}

export interface ReportPreview {
  headers: string[]
  rows: (string | number)[][]
  groupMergeRanges: { startRow: number; endRow: number }[]
}

// ========== 报表生成逻辑 ==========

/** 获取日期所属的时间段键 */
function getPeriodKey(date: Date, dimension: ReportDimension): string {
  const y = date.getFullYear()
  const m = date.getMonth() + 1
  const d = date.getDate()
  switch (dimension) {
    case 'day':
      return `${y}-${String(m).padStart(2, '0')}-${String(d).padStart(2, '0')}`
    case 'week': {
      // 获取该日期所在周的周一日期作为键
      const dayOfWeek = date.getDay() || 7
      const monday = new Date(date)
      monday.setDate(d - dayOfWeek + 1)
      const my = monday.getFullYear()
      const mm = monday.getMonth() + 1
      const md = monday.getDate()
      return `${my}-${String(mm).padStart(2, '0')}-${String(md).padStart(2, '0')}`
    }
    case 'month':
      return `${y}-${String(m).padStart(2, '0')}`
    case 'semester':
      // 学期维度按周切分
      return getPeriodKey(date, 'week')
  }
}

/** 获取日期范围内的所有时间段键（有序） */
function getPeriodKeys(startDate: Date, endDate: Date, dimension: ReportDimension): string[] {
  const keys = new Set<string>()
  const current = new Date(startDate)
  while (current <= endDate) {
    keys.add(getPeriodKey(current, dimension))
    current.setDate(current.getDate() + 1)
  }
  return Array.from(keys).sort()
}

/** 格式化时间段键为显示文本 */
function formatPeriodHeader(
  key: string,
  dimension: ReportDimension,
  weekHeaderFormat: WeekHeaderFormat
): string {
  if (dimension === 'day') {
    const [, m, d] = key.split('-')
    return `${parseInt(m)}月${parseInt(d)}日`
  }
  if (dimension === 'month') {
    const [, m] = key.split('-')
    return `${parseInt(m)}月`
  }
  // week / semester 都按周处理
  if (weekHeaderFormat === 'dateRange') {
    const monday = new Date(key)
    const sunday = new Date(monday)
    sunday.setDate(monday.getDate() + 6)
    const fmt = (d: Date) => `${d.getMonth() + 1}/${d.getDate()}`
    return `${fmt(monday)}-${fmt(sunday)}`
  }
  // weekNumber 格式需要知道是第几周，用 key 的序号
  return key // 占位，后续替换
}

/** 为周维度键生成"第N周"格式 */
function formatWeekNumberHeaders(keys: string[]): Map<string, string> {
  const map = new Map<string, string>()
  keys.forEach((key, i) => {
    map.set(key, `第${i + 1}周`)
  })
  return map
}

/** 生成报表数据 */
export function generateReport(
  config: ReportConfig,
  students: ReportStudent[],
  records: ReportScoreRecord[]
): ReportPreview {
  const periodKeys = getPeriodKeys(config.startDate, config.endDate, config.dimension)

  // 为每个学生、每个时间段聚合积分
  const studentPeriodData = new Map<string, Map<string, { plus: number; minus: number }>>()

  for (const student of students) {
    const periodMap = new Map<string, { plus: number; minus: number }>()
    for (const key of periodKeys) {
      periodMap.set(key, { plus: 0, minus: 0 })
    }
    studentPeriodData.set(student.id, periodMap)
  }

  for (const record of records) {
    if (record.isReverted) continue
    const periodMap = studentPeriodData.get(record.studentId)
    if (!periodMap) continue

    const date = new Date(record.createdAt)
    if (date < config.startDate || date > config.endDate) continue

    const key = getPeriodKey(date, config.dimension)
    const data = periodMap.get(key)
    if (!data) continue

    if (record.scoreChange > 0) {
      data.plus += record.scoreChange
    } else {
      data.minus += record.scoreChange
    }
  }

  // 生成列头
  const headers: string[] = []
  if (config.groupByGroup) {
    headers.push('组别')
  }
  headers.push('姓名')

  // 周序号映射
  const weekNumberMap = (config.dimension === 'week' || config.dimension === 'semester') &&
    config.weekHeaderFormat === 'weekNumber'
    ? formatWeekNumberHeaders(periodKeys)
    : null

  for (const key of periodKeys) {
    if (config.dimension === 'week' || config.dimension === 'semester') {
      if (weekNumberMap) {
        const label = weekNumberMap.get(key)!
        if (config.scoreDisplayMode === 'split') {
          headers.push(`${label}(加分)`, `${label}(减分)`)
        } else {
          headers.push(label)
        }
      } else {
        const label = formatPeriodHeader(key, config.dimension, config.weekHeaderFormat)
        if (config.scoreDisplayMode === 'split') {
          headers.push(`${label}(加分)`, `${label}(减分)`)
        } else {
          headers.push(label)
        }
      }
    } else {
      const label = formatPeriodHeader(key, config.dimension, config.weekHeaderFormat)
      if (config.scoreDisplayMode === 'split') {
        headers.push(`${label}(加分)`, `${label}(减分)`)
      } else {
        headers.push(label)
      }
    }
  }

  // 总计列
  if (config.totalColumns.includes('rangeNet')) {
    headers.push('范围内净积分')
  }
  if (config.totalColumns.includes('currentTotal')) {
    headers.push('当前总积分')
  }

  // 排序学生
  let sortedStudents = [...students]
  if (config.groupByGroup) {
    // 按小组分组，组内按积分排序
    const groups = new Map<string, ReportStudent[]>()
    const noGroup: ReportStudent[] = []
    for (const s of sortedStudents) {
      if (s.groupId && s.groupName) {
        if (!groups.has(s.groupId)) groups.set(s.groupId, [])
        groups.get(s.groupId)!.push(s)
      } else {
        noGroup.push(s)
      }
    }
    const sortFn = (a: ReportStudent, b: ReportStudent) =>
      config.sortOrder === 'desc' ? b.currentScore - a.currentScore : a.currentScore - b.currentScore
    sortedStudents = []
    for (const [, members] of groups) {
      members.sort(sortFn)
      sortedStudents.push(...members)
    }
    noGroup.sort(sortFn)
    sortedStudents.push(...noGroup)
  } else {
    sortedStudents.sort((a, b) =>
      config.sortOrder === 'desc' ? b.currentScore - a.currentScore : a.currentScore - b.currentScore
    )
  }

  // 生成行数据
  const rows: (string | number)[][] = []
  const groupMergeRanges: { startRow: number; endRow: number }[] = []
  let lastGroupName = ''
  let groupStartRow = -1

  for (const student of sortedStudents) {
    const row: (string | number)[] = []

    // 组别列：同组只在首行显示组名，其余为空
    if (config.groupByGroup) {
      if (student.groupName && student.groupName !== lastGroupName) {
        // 上一个组的合并范围结束
        if (groupStartRow >= 0 && rows.length - 1 > groupStartRow) {
          groupMergeRanges.push({ startRow: groupStartRow, endRow: rows.length - 1 })
        }
        lastGroupName = student.groupName
        groupStartRow = rows.length
        row.push(student.groupName)
      } else {
        row.push('')
      }
    }

    row.push(student.name)

    const periodMap = studentPeriodData.get(student.id)!
    let rangeNet = 0
    for (const key of periodKeys) {
      const data = periodMap.get(key)!
      rangeNet += data.plus + data.minus

      if (config.scoreDisplayMode === 'net') {
        row.push(data.plus + data.minus)
      } else {
        row.push(data.plus, data.minus)
      }
    }

    if (config.totalColumns.includes('rangeNet')) {
      row.push(rangeNet)
    }
    if (config.totalColumns.includes('currentTotal')) {
      row.push(student.currentScore)
    }

    rows.push(row)
  }

  // 最后一个组的合并范围
  if (config.groupByGroup && groupStartRow >= 0 && rows.length - 1 > groupStartRow) {
    groupMergeRanges.push({ startRow: groupStartRow, endRow: rows.length - 1 })
  }

  return { headers, rows, groupMergeRanges }
}

/** 导出报表为 Excel */
export function exportReportToExcel(
  report: ReportPreview,
  filename: string,
  groupByGroup: boolean
): void {
  const wsData: (string | number)[][] = [report.headers, ...report.rows]
  const ws = XLSX.utils.aoa_to_sheet(wsData)

  // 设置列宽
  ws['!cols'] = report.headers.map((h, i) => ({
    wch: i === 0 ? 10 : Math.max(h.length * 2.5, 8),
  }))

  // 合并组别列的单元格
  if (groupByGroup && report.groupMergeRanges.length > 0) {
    const merges: XLSX.Range[] = report.groupMergeRanges.map(range => ({
      s: { r: range.startRow + 1, c: 0 },  // +1 因为第一行是表头
      e: { r: range.endRow + 1, c: 0 },
    }))
    ws['!merges'] = merges
  }

  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '积分报表')
  XLSX.writeFile(wb, `${filename}.xlsx`)
}

/** 导出报表为 CSV */
export function exportReportToCSV(report: ReportPreview, filename: string): void {
  const csvRows = [report.headers.map(escapeCSV).join(',')]
  for (const row of report.rows) {
    csvRows.push(row.map(v => escapeCSV(String(v))).join(','))
  }
  const csvContent = csvRows.join('\n')
  const BOM = '\uFEFF'
  const blob = new Blob([BOM + csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${filename}.csv`
  link.click()
  URL.revokeObjectURL(url)
}

function escapeCSV(val: string): string {
  return val.includes(',') || val.includes('"')
    ? `"${val.replace(/"/g, '""')}"`
    : val
}

// ========== 通用导入导出 ==========

/** 读取 Excel/CSV 文件，返回表头和行数据 */
export function readExcelFile(file: File): Promise<{
  headers: string[]
  rows: string[][]
  sheetNames: string[]
}> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target?.result as ArrayBuffer)
        const workbook = XLSX.read(data, { type: 'array' })
        const sheetNames = workbook.SheetNames
        const firstSheet = workbook.Sheets[sheetNames[0]]
        const jsonData = XLSX.utils.sheet_to_json<string[]>(firstSheet, { header: 1 })

        if (jsonData.length === 0) {
          reject(new Error('文件为空'))
          return
        }

        const headers = jsonData[0].map(h => String(h || ''))
        const rows = jsonData.slice(1).map(row =>
          row.map(cell => String(cell ?? ''))
        )

        resolve({ headers, rows, sheetNames })
      } catch (err) {
        reject(err)
      }
    }
    reader.onerror = () => reject(new Error('文件读取失败'))
    reader.readAsArrayBuffer(file)
  })
}

/** 导出数据为 Excel 文件并下载 */
export function exportToExcel(
  data: Record<string, string | number | boolean | null>[],
  columns: ExcelColumn[],
  filename: string
): void {
  const headers = columns.map(c => c.header)
  const rows = data.map(row =>
    columns.map(c => row[c.key] ?? '')
  )

  const wsData = [headers, ...rows]
  const ws = XLSX.utils.aoa_to_sheet(wsData)

  // 设置列宽
  ws['!cols'] = columns.map(c => ({
    wch: Math.max(c.header.length * 2, 12),
  }))

  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Sheet1')
  XLSX.writeFile(wb, `${filename}.xlsx`)
}

/** 导出数据为 CSV 文件并下载 */
export function exportToCSV(
  data: Record<string, string | number | boolean | null>[],
  columns: ExcelColumn[],
  filename: string
): void {
  const headers = columns.map(c => c.header)
  const rows = data.map(row =>
    columns.map(c => {
      const val = String(row[c.key] ?? '')
      return val.includes(',') || val.includes('"') ? `"${val.replace(/"/g, '""')}"` : val
    })
  )

  const csvContent = [headers.join(','), ...rows.map(r => r.join(','))].join('\n')
  const BOM = '\uFEFF'
  const blob = new Blob([BOM + csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${filename}.csv`
  link.click()
  URL.revokeObjectURL(url)
}
