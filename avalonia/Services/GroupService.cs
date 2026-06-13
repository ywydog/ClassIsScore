using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 小组服务实现，使用 JSON 文件存储小组数据
/// </summary>
public class GroupService : IGroupService
{
    private readonly ILogger<GroupService> _logger;
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // 用于同步访问的锁对象
    private readonly object _lock = new();

    public GroupService(ILogger<GroupService> logger)
    {
        _logger = logger;
        _dataFilePath = Path.Combine(AppPaths.DataFolderPath, "groups.json");
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
    /// 从 JSON 文件读取所有小组数据
    /// </summary>
    private List<StudentGroup> ReadFromFile()
    {
        try
        {
            var json = File.ReadAllText(_dataFilePath);
            return JsonSerializer.Deserialize<List<StudentGroup>>(json, _jsonOptions) ?? new List<StudentGroup>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取小组数据文件失败");
            return new List<StudentGroup>();
        }
    }

    /// <summary>
    /// 将小组数据写入 JSON 文件
    /// </summary>
    private void WriteToFile(List<StudentGroup> groups)
    {
        try
        {
            var json = JsonSerializer.Serialize(groups, _jsonOptions);
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入小组数据文件失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<List<StudentGroup>> GetAllGroupsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(ReadFromFile());
        }
    }

    /// <inheritdoc/>
    public Task<StudentGroup> CreateGroupAsync(string name, List<Guid> studentIds)
    {
        lock (_lock)
        {
            var groups = ReadFromFile();
            var group = new StudentGroup
            {
                Id = Guid.NewGuid(),
                Name = name,
                StudentIds = studentIds,
                CreatedAt = DateTime.Now
            };
            groups.Add(group);
            WriteToFile(groups);
            _logger.LogInformation("创建小组: {Name}，成员数: {Count}", name, studentIds.Count);
            return Task.FromResult(group);
        }
    }

    /// <inheritdoc/>
    public Task DeleteGroupAsync(Guid id)
    {
        lock (_lock)
        {
            var groups = ReadFromFile();
            var removed = groups.RemoveAll(g => g.Id == id);
            if (removed > 0)
            {
                WriteToFile(groups);
                _logger.LogInformation("删除小组: {Id}", id);
            }
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc/>
    public Task AddStudentsToGroupAsync(Guid groupId, List<Guid> studentIds)
    {
        lock (_lock)
        {
            var groups = ReadFromFile();
            var group = groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null)
            {
                throw new InvalidOperationException($"未找到ID为 {groupId} 的小组");
            }

            foreach (var studentId in studentIds)
            {
                if (!group.StudentIds.Contains(studentId))
                {
                    group.StudentIds.Add(studentId);
                }
            }

            WriteToFile(groups);
            _logger.LogInformation("向小组 {GroupId} 添加 {Count} 名学生", groupId, studentIds.Count);
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc/>
    public Task RemoveStudentsFromGroupAsync(Guid groupId, List<Guid> studentIds)
    {
        lock (_lock)
        {
            var groups = ReadFromFile();
            var group = groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null)
            {
                throw new InvalidOperationException($"未找到ID为 {groupId} 的小组");
            }

            foreach (var studentId in studentIds)
            {
                group.StudentIds.Remove(studentId);
            }

            WriteToFile(groups);
            _logger.LogInformation("从小组 {GroupId} 移除 {Count} 名学生", groupId, studentIds.Count);
            return Task.CompletedTask;
        }
    }
}
