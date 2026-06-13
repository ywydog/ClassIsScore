using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using ClosedXML.Excel;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 数据传输服务实现，提供数据导出和导入功能
/// </summary>
public class DataTransferService : IDataTransferService
{
    private readonly ILogger<DataTransferService> _logger;
    private readonly IStudentService _studentService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 需要导出的数据文件列表
    /// </summary>
    private static readonly string[] DataFiles =
    [
        "students.json",
        "score_records.json",
        "groups.json",
        "evaluation_items.json",
        "auto_evaluation.json",
        "admin.json",
        "floating_window.json",
        "app_state.json",
        "settlement_records.json"
    ];

    /// <summary>
    /// 导出清单版本号
    /// </summary>
    private const string ManifestVersion = "1.0";

    public DataTransferService(
        ILogger<DataTransferService> logger,
        IStudentService studentService)
    {
        _logger = logger;
        _studentService = studentService;
    }

    /// <inheritdoc/>
    public event EventHandler<DataTransferProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// 触发进度变更事件
    /// </summary>
    private void OnProgressChanged(double progress, string statusMessage, bool isCompleted = false)
    {
        ProgressChanged?.Invoke(this, new DataTransferProgressEventArgs
        {
            Progress = progress,
            StatusMessage = statusMessage,
            IsCompleted = isCompleted
        });
    }

    /// <inheritdoc/>
    public async Task<string> ExportAllDataAsync(string outputPath)
    {
        try
        {
            OnProgressChanged(0, "正在准备导出数据...");

            // 确保输出目录存在
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 如果未指定输出路径，使用默认文件名
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"ClassIsScore_Data_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            }

            // 确保文件名以 .zip 结尾
            if (!outputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                outputPath += ".zip";
            }

            var existingFiles = new List<string>();
            var totalFiles = DataFiles.Length;
            var processedFiles = 0;

            using var fs = File.Create(outputPath);
            using var zipStream = new ZipOutputStream(fs);

            // 逐个添加数据文件
            foreach (var fileName in DataFiles)
            {
                var filePath = Path.Combine(AppPaths.DataFolderPath, fileName);
                if (File.Exists(filePath))
                {
                    var entry = new ZipEntry(fileName);
                    zipStream.PutNextEntry(entry);

                    var bytes = await File.ReadAllBytesAsync(filePath);
                    zipStream.Write(bytes, 0, bytes.Length);
                    zipStream.CloseEntry();

                    existingFiles.Add(fileName);
                    _logger.LogDebug("已添加文件到导出包: {FileName}", fileName);
                }
                else
                {
                    _logger.LogDebug("数据文件不存在，跳过: {FileName}", fileName);
                }

                processedFiles++;
                var progress = (double)processedFiles / (totalFiles + 1) * 80; // 预留20%给清单
                OnProgressChanged(progress, $"正在导出: {fileName}");
            }

            // 添加清单文件
            OnProgressChanged(85, "正在生成导出清单...");
            var manifest = new ExportManifest
            {
                Version = ManifestVersion,
                ExportedAt = DateTime.Now,
                Files = existingFiles
            };
            var manifestJson = JsonSerializer.Serialize(manifest, _jsonOptions);
            AddEntryToZip(zipStream, "manifest.json", manifestJson);

            zipStream.Finish();

            OnProgressChanged(100, "数据导出完成", isCompleted: true);
            _logger.LogInformation("数据已导出到: {OutputPath}，包含 {Count} 个文件", outputPath, existingFiles.Count);

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出数据失败");
            OnProgressChanged(0, $"导出失败: {ex.Message}", isCompleted: true);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ImportAllDataAsync(string zipFilePath)
    {
        string? tempBackupDir = null;

        try
        {
            OnProgressChanged(0, "正在验证导入文件...");

            if (!File.Exists(zipFilePath))
            {
                _logger.LogError("导入文件不存在: {Path}", zipFilePath);
                OnProgressChanged(0, "导入文件不存在", isCompleted: true);
                return false;
            }

            // 验证zip文件包含manifest.json
            using (var verifyFs = File.OpenRead(zipFilePath))
            using (var verifyZip = new ZipFile(verifyFs))
            {
                var manifestEntry = verifyZip.GetEntry("manifest.json");
                if (manifestEntry == null)
                {
                    _logger.LogError("导入文件缺少 manifest.json，不是有效的数据备份文件");
                    OnProgressChanged(0, "无效的备份文件：缺少清单", isCompleted: true);
                    return false;
                }
            }

            OnProgressChanged(10, "正在备份当前数据...");

            // 备份当前数据到临时目录
            tempBackupDir = Path.Combine(AppPaths.BackupFolderPath, $"import_backup_{DateTime.Now:yyyyMMdd_HHmmss}");
            BackupCurrentData(tempBackupDir);

            OnProgressChanged(30, "正在导入数据...");

            // 从zip解压文件覆盖数据目录
            using var fs = File.OpenRead(zipFilePath);
            using var zipFile = new ZipFile(fs);

            var dataFileEntries = zipFile.Cast<ZipEntry>()
                .Where(e => !e.IsDirectory && e.Name != "manifest.json")
                .ToList();

            var totalEntries = dataFileEntries.Count;
            var processedEntries = 0;

            foreach (var entry in dataFileEntries)
            {
                using var entryStream = zipFile.GetInputStream(entry);
                using var reader = new StreamReader(entryStream);
                var content = reader.ReadToEnd();

                var targetPath = Path.Combine(AppPaths.DataFolderPath, entry.Name);
                await File.WriteAllTextAsync(targetPath, content);

                _logger.LogDebug("已恢复文件: {FileName}", entry.Name);

                processedEntries++;
                var progress = 30 + (double)processedEntries / totalEntries * 60;
                OnProgressChanged(progress, $"正在恢复: {entry.Name}");
            }

            OnProgressChanged(95, "正在重新加载数据...");

            // 通知各服务重新加载数据（通过重新读取文件实现）
            // StudentService 等服务在下次访问时会自动从文件重新读取

            OnProgressChanged(100, "数据导入完成", isCompleted: true);
            _logger.LogInformation("数据已从 {Path} 导入成功", zipFilePath);

            // 导入成功后删除临时备份
            try
            {
                if (Directory.Exists(tempBackupDir))
                {
                    Directory.Delete(tempBackupDir, true);
                }
            }
            catch
            {
                // 忽略清理失败
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入数据失败");

            // 尝试从临时备份恢复
            if (tempBackupDir != null && Directory.Exists(tempBackupDir))
            {
                try
                {
                    OnProgressChanged(0, "导入失败，正在恢复备份...");
                    RestoreFromBackup(tempBackupDir);
                    OnProgressChanged(0, "已从备份恢复数据", isCompleted: true);
                    _logger.LogInformation("导入失败，已从临时备份恢复数据");
                }
                catch (Exception restoreEx)
                {
                    _logger.LogError(restoreEx, "从临时备份恢复数据也失败了");
                    OnProgressChanged(0, $"导入失败且恢复备份也失败: {restoreEx.Message}", isCompleted: true);
                }
            }
            else
            {
                OnProgressChanged(0, $"导入失败: {ex.Message}", isCompleted: true);
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ExportStudentsOnlyAsync(string outputPath)
    {
        try
        {
            OnProgressChanged(0, "正在准备导出学生数据...");

            var students = await _studentService.GetAllStudentsAsync();

            OnProgressChanged(30, "正在生成Excel文件...");

            // 确保输出目录存在
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("学生数据");

            // 表头
            worksheet.Cell(1, 1).Value = "姓名";
            worksheet.Cell(1, 2).Value = "学号";
            worksheet.Cell(1, 3).Value = "性别";
            worksheet.Cell(1, 4).Value = "当前积分";

            // 设置表头样式
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // 填充数据
            var row = 2;
            foreach (var student in students)
            {
                worksheet.Cell(row, 1).Value = student.Name;
                worksheet.Cell(row, 2).Value = student.StudentNumber ?? string.Empty;
                worksheet.Cell(row, 3).Value = student.Gender ?? string.Empty;
                worksheet.Cell(row, 4).Value = student.Score;
                row++;
            }

            // 调整列宽
            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(outputPath);

            OnProgressChanged(100, "学生数据导出完成", isCompleted: true);
            _logger.LogInformation("学生数据已导出到: {OutputPath}，共 {Count} 名学生", outputPath, students.Count);

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出学生数据失败");
            OnProgressChanged(0, $"导出失败: {ex.Message}", isCompleted: true);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ImportStudentsOnlyAsync(string filePath)
    {
        try
        {
            OnProgressChanged(0, "正在准备导入学生数据...");

            if (!File.Exists(filePath))
            {
                _logger.LogError("导入文件不存在: {Path}", filePath);
                OnProgressChanged(0, "导入文件不存在", isCompleted: true);
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            OnProgressChanged(30, "正在导入学生数据...");

            switch (extension)
            {
                case ".xlsx":
                case ".xls":
                    await _studentService.ImportFromExcelAsync(filePath);
                    break;

                case ".csv":
                    await _studentService.ImportFromCsvAsync(filePath);
                    break;

                case ".zip":
                    return await ImportStudentsFromZipAsync(filePath);

                default:
                    _logger.LogError("不支持的文件格式: {Extension}", extension);
                    OnProgressChanged(0, $"不支持的文件格式: {extension}", isCompleted: true);
                    return false;
            }

            OnProgressChanged(100, "学生数据导入完成", isCompleted: true);
            _logger.LogInformation("学生数据已从 {Path} 导入", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入学生数据失败");
            OnProgressChanged(0, $"导入失败: {ex.Message}", isCompleted: true);
            return false;
        }
    }

    /// <summary>
    /// 从zip文件中导入学生数据
    /// </summary>
    private async Task<bool> ImportStudentsFromZipAsync(string zipFilePath)
    {
        try
        {
            using var fs = File.OpenRead(zipFilePath);
            using var zipFile = new ZipFile(fs);

            // 查找zip中的学生数据文件
            var studentsEntry = zipFile.GetEntry("students.json");
            if (studentsEntry != null)
            {
                using var entryStream = zipFile.GetInputStream(studentsEntry);
                using var reader = new StreamReader(entryStream);
                var studentsJson = await reader.ReadToEndAsync();

                var studentsFilePath = Path.Combine(AppPaths.DataFolderPath, "students.json");
                await File.WriteAllTextAsync(studentsFilePath, studentsJson);

                OnProgressChanged(100, "学生数据导入完成", isCompleted: true);
                _logger.LogInformation("已从zip文件恢复学生数据");
                return true;
            }

            // 查找zip中的Excel文件
            var excelEntry = zipFile.Cast<ZipEntry>()
                .FirstOrDefault(e => !e.IsDirectory &&
                    (e.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     e.Name.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)));

            if (excelEntry != null)
            {
                // 解压Excel文件到临时目录后导入
                var tempDir = Path.Combine(Path.GetTempPath(), $"ClassIsScore_Import_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var tempExcelPath = Path.Combine(tempDir, excelEntry.Name);
                    using var entryStream = zipFile.GetInputStream(excelEntry);
                    using var fileStream = File.Create(tempExcelPath);
                    await entryStream.CopyToAsync(fileStream);

                    await _studentService.ImportFromExcelAsync(tempExcelPath);

                    OnProgressChanged(100, "学生数据导入完成", isCompleted: true);
                    return true;
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }

            _logger.LogError("zip文件中未找到学生数据");
            OnProgressChanged(0, "zip文件中未找到学生数据", isCompleted: true);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从zip导入学生数据失败");
            OnProgressChanged(0, $"导入失败: {ex.Message}", isCompleted: true);
            return false;
        }
    }

    /// <summary>
    /// 向zip流中添加一个文件条目
    /// </summary>
    private static void AddEntryToZip(ZipOutputStream zipStream, string entryName, string content)
    {
        var entry = new ZipEntry(entryName);
        zipStream.PutNextEntry(entry);

        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        zipStream.Write(bytes, 0, bytes.Length);
        zipStream.CloseEntry();
    }

    /// <summary>
    /// 备份当前数据到指定目录
    /// </summary>
    private void BackupCurrentData(string backupDir)
    {
        Directory.CreateDirectory(backupDir);

        foreach (var fileName in DataFiles)
        {
            var sourcePath = Path.Combine(AppPaths.DataFolderPath, fileName);
            if (File.Exists(sourcePath))
            {
                var destPath = Path.Combine(backupDir, fileName);
                File.Copy(sourcePath, destPath, overwrite: true);
            }
        }

        _logger.LogDebug("当前数据已备份到: {BackupDir}", backupDir);
    }

    /// <summary>
    /// 从备份目录恢复数据
    /// </summary>
    private void RestoreFromBackup(string backupDir)
    {
        foreach (var fileName in DataFiles)
        {
            var sourcePath = Path.Combine(backupDir, fileName);
            if (File.Exists(sourcePath))
            {
                var destPath = Path.Combine(AppPaths.DataFolderPath, fileName);
                File.Copy(sourcePath, destPath, overwrite: true);
            }
        }
    }
}

/// <summary>
/// 导出清单模型
/// </summary>
internal class ExportManifest
{
    /// <summary>
    /// 清单版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 导出时间
    /// </summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// 包含的数据文件列表
    /// </summary>
    public List<string> Files { get; set; } = new();
}
