using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 学生服务实现，使用 JSON 文件存储学生数据
/// </summary>
public class StudentService : IStudentService
{
    private readonly ILogger<StudentService> _logger;
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // 用于同步访问的锁对象
    private readonly object _lock = new();

    public StudentService(ILogger<StudentService> logger)
    {
        _logger = logger;
        _dataFilePath = Path.Combine(AppPaths.DataFolderPath, "students.json");
        EnsureDataFileExists();
    }

    /// <summary>
    /// 确保数据文件存在，不存在则创建空文件
    /// </summary>
    private void EnsureDataFileExists()
    {
        if (!File.Exists(_dataFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataFilePath)!);
            File.WriteAllText(_dataFilePath, "[]");
        }
    }

    /// <summary>
    /// 从 JSON 文件读取所有学生数据
    /// </summary>
    private List<Student> ReadFromFile()
    {
        try
        {
            var json = File.ReadAllText(_dataFilePath);
            return JsonSerializer.Deserialize<List<Student>>(json, _jsonOptions) ?? new List<Student>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取学生数据文件失败");
            return new List<Student>();
        }
    }

    /// <summary>
    /// 将学生数据写入 JSON 文件
    /// </summary>
    private void WriteToFile(List<Student> students)
    {
        try
        {
            var json = JsonSerializer.Serialize(students, _jsonOptions);
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入学生数据文件失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<List<Student>> GetAllStudentsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(ReadFromFile());
        }
    }

    /// <inheritdoc/>
    public Task<Student?> GetStudentByIdAsync(Guid id)
    {
        lock (_lock)
        {
            var students = ReadFromFile();
            return Task.FromResult(students.FirstOrDefault(s => s.Id == id));
        }
    }

    /// <inheritdoc/>
    public Task<Student> AddStudentAsync(Student student)
    {
        lock (_lock)
        {
            var students = ReadFromFile();
            student.Id = Guid.NewGuid();
            student.CreatedAt = DateTime.Now;
            student.UpdatedAt = DateTime.Now;
            students.Add(student);
            WriteToFile(students);
            _logger.LogInformation("添加学生: {Name}", student.Name);
            return Task.FromResult(student);
        }
    }

    /// <inheritdoc/>
    public Task<Student> UpdateStudentAsync(Student student)
    {
        lock (_lock)
        {
            var students = ReadFromFile();
            var index = students.FindIndex(s => s.Id == student.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"未找到ID为 {student.Id} 的学生");
            }
            student.UpdatedAt = DateTime.Now;
            students[index] = student;
            WriteToFile(students);
            _logger.LogInformation("更新学生: {Name}", student.Name);
            return Task.FromResult(student);
        }
    }

    /// <inheritdoc/>
    public Task DeleteStudentAsync(Guid id)
    {
        lock (_lock)
        {
            var students = ReadFromFile();
            var removed = students.RemoveAll(s => s.Id == id);
            if (removed > 0)
            {
                WriteToFile(students);
                _logger.LogInformation("删除学生: {Id}", id);
            }
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc/>
    public Task<List<Student>> ImportFromExcelAsync(string filePath)
    {
        try
        {
            var importedStudents = new List<Student>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.First();
            var rows = worksheet.RangeUsed()?.RowsUsed();

            if (rows == null)
            {
                return Task.FromResult(importedStudents);
            }

            // 跳过标题行
            foreach (var row in rows.Skip(1))
            {
                var name = row.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    StudentNumber = row.Cell(2).GetString().Trim(),
                    Gender = row.Cell(3).GetString().Trim(),
                    Score = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                importedStudents.Add(student);
            }

            // 将导入的学生保存到数据文件
            lock (_lock)
            {
                var existingStudents = ReadFromFile();
                existingStudents.AddRange(importedStudents);
                WriteToFile(existingStudents);
            }

            _logger.LogInformation("从Excel导入 {Count} 名学生", importedStudents.Count);
            return Task.FromResult(importedStudents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Excel导入学生失败: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<List<Student>> ImportFromCsvAsync(string filePath)
    {
        try
        {
            var importedStudents = new List<Student>();
            var lines = File.ReadAllLines(filePath);

            // 跳过标题行
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                var name = parts.ElementAtOrDefault(0)?.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    StudentNumber = parts.ElementAtOrDefault(1)?.Trim(),
                    Gender = parts.ElementAtOrDefault(2)?.Trim(),
                    Score = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                importedStudents.Add(student);
            }

            // 将导入的学生保存到数据文件
            lock (_lock)
            {
                var existingStudents = ReadFromFile();
                existingStudents.AddRange(importedStudents);
                WriteToFile(existingStudents);
            }

            _logger.LogInformation("从CSV导入 {Count} 名学生", importedStudents.Count);
            return Task.FromResult(importedStudents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从CSV导入学生失败: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public List<Student> SortByName(List<Student> students)
    {
        return students
            .OrderBy(s => s.Name, new ChinesePinyinComparer())
            .ToList();
    }

    /// <inheritdoc/>
    public List<Student> SortByStudentNumber(List<Student> students)
    {
        return students
            .OrderBy(s => s.StudentNumber ?? string.Empty)
            .ToList();
    }
}

/// <summary>
/// 中文拼音排序比较器，按姓氏首字母排序
/// </summary>
internal class ChinesePinyinComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        // 使用中文区域信息的排序规则
        var compareInfo = CultureInfo.GetCultureInfo("zh-CN").CompareInfo;
        return compareInfo.Compare(x, y, CompareOptions.StringSort);
    }
}
