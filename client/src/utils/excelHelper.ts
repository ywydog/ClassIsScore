import * as XLSX from 'xlsx'

export interface ExcelColumn {
  header: string
  key: string
}

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
