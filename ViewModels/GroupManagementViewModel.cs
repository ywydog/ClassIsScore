using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.ViewModels;

/// <summary>
/// 小组管理页面 ViewModel
/// </summary>
public partial class GroupManagementViewModel : ObservableObject
{
    private readonly IGroupService _groupService;
    private readonly IStudentService _studentService;
    private readonly IScoreService _scoreService;
    private readonly ILogger<GroupManagementViewModel> _logger;

    /// <summary>
    /// 所有小组列表
    /// </summary>
    public ObservableCollection<StudentGroup> Groups { get; } = new();

    /// <summary>
    /// 当前选中的小组
    /// </summary>
    [ObservableProperty]
    private StudentGroup? _selectedGroup;

    /// <summary>
    /// 未分组学生列表（可添加到小组的学生）
    /// </summary>
    public ObservableCollection<Student> AvailableStudents { get; } = new();

    /// <summary>
    /// 当前选中小组的成员列表
    /// </summary>
    public ObservableCollection<Student> GroupMembers { get; } = new();

    /// <summary>
    /// 新小组名称
    /// </summary>
    [ObservableProperty]
    private string _newGroupName = string.Empty;

    /// <summary>
    /// 选中的未分组学生（用于添加到小组）
    /// </summary>
    [ObservableProperty]
    private Student? _selectedAvailableStudent;

    /// <summary>
    /// 选中的小组成员（用于从小组移除）
    /// </summary>
    [ObservableProperty]
    private Student? _selectedGroupMember;

    /// <summary>
    /// 整组评价的积分变化值
    /// </summary>
    [ObservableProperty]
    private double _groupScoreValue = 1;

    /// <summary>
    /// 整组评价原因
    /// </summary>
    [ObservableProperty]
    private string _groupScoreReason = string.Empty;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    public GroupManagementViewModel(
        IGroupService groupService,
        IStudentService studentService,
        IScoreService scoreService,
        ILogger<GroupManagementViewModel> logger)
    {
        _groupService = groupService;
        _studentService = studentService;
        _scoreService = scoreService;
        _logger = logger;
    }

    /// <summary>
    /// 加载所有小组和未分组学生
    /// </summary>
    [RelayCommand]
    private async Task LoadGroupsAsync()
    {
        try
        {
            IsLoading = true;
            var groups = await _groupService.GetAllGroupsAsync();
            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }

            await RefreshAvailableStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载小组列表失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建小组命令
    /// </summary>
    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName)) return;

        try
        {
            await _groupService.CreateGroupAsync(NewGroupName, new());
            _logger.LogInformation("创建小组: {Name}", NewGroupName);
            NewGroupName = string.Empty;
            await LoadGroupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建小组失败");
        }
    }

    /// <summary>
    /// 删除小组命令
    /// </summary>
    [RelayCommand]
    private async Task DeleteGroupAsync(StudentGroup? group)
    {
        if (group == null) return;

        try
        {
            await _groupService.DeleteGroupAsync(group.Id);
            _logger.LogInformation("删除小组: {Name}", group.Name);

            if (SelectedGroup?.Id == group.Id)
            {
                SelectedGroup = null;
            }

            await LoadGroupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除小组失败");
        }
    }

    /// <summary>
    /// 添加成员到小组命令
    /// </summary>
    [RelayCommand]
    private async Task AddMemberToGroupAsync(Student? student)
    {
        if (student == null || SelectedGroup == null) return;

        try
        {
            await _groupService.AddStudentsToGroupAsync(SelectedGroup.Id, new() { student.Id });

            // 更新学生的 GroupId
            student.GroupId = SelectedGroup.Id;
            await _studentService.UpdateStudentAsync(student);

            _logger.LogInformation("将学生 {Name} 添加到小组 {GroupName}", student.Name, SelectedGroup.Name);
            await LoadGroupsAsync();
            await RefreshGroupMembersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加成员到小组失败");
        }
    }

    /// <summary>
    /// 从小组移除成员命令
    /// </summary>
    [RelayCommand]
    private async Task RemoveMemberFromGroupAsync(Student? student)
    {
        if (student == null || SelectedGroup == null) return;

        try
        {
            await _groupService.RemoveStudentsFromGroupAsync(SelectedGroup.Id, new() { student.Id });

            // 清除学生的 GroupId
            student.GroupId = null;
            await _studentService.UpdateStudentAsync(student);

            _logger.LogInformation("将学生 {Name} 从小组 {GroupName} 移除", student.Name, SelectedGroup.Name);
            await LoadGroupsAsync();
            await RefreshGroupMembersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从小组移除成员失败");
        }
    }

    /// <summary>
    /// 整组加分命令
    /// </summary>
    [RelayCommand]
    private async Task AddScoreToGroupAsync()
    {
        if (SelectedGroup == null) return;

        try
        {
            await _scoreService.AddScoreToGroupAsync(
                SelectedGroup.Id,
                GroupScoreValue,
                string.IsNullOrWhiteSpace(GroupScoreReason) ? "整组加分" : GroupScoreReason);

            _logger.LogInformation("小组 {GroupName} 加分: {Score}", SelectedGroup.Name, GroupScoreValue);
            await RefreshGroupMembersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "整组加分失败");
        }
    }

    /// <summary>
    /// 整组减分命令
    /// </summary>
    [RelayCommand]
    private async Task SubtractScoreFromGroupAsync()
    {
        if (SelectedGroup == null) return;

        try
        {
            await _scoreService.AddScoreToGroupAsync(
                SelectedGroup.Id,
                -GroupScoreValue,
                string.IsNullOrWhiteSpace(GroupScoreReason) ? "整组减分" : GroupScoreReason);

            _logger.LogInformation("小组 {GroupName} 减分: {Score}", SelectedGroup.Name, GroupScoreValue);
            await RefreshGroupMembersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "整组减分失败");
        }
    }

    /// <summary>
    /// 选中小组变更时，刷新小组成员和未分组学生
    /// </summary>
    partial void OnSelectedGroupChanged(StudentGroup? value)
    {
        _ = RefreshGroupMembersAsync();
        _ = RefreshAvailableStudentsAsync();
    }

    /// <summary>
    /// 刷新当前选中小组的成员列表
    /// </summary>
    private async Task RefreshGroupMembersAsync()
    {
        GroupMembers.Clear();

        if (SelectedGroup == null) return;

        try
        {
            var allStudents = await _studentService.GetAllStudentsAsync();
            var members = allStudents.Where(s => SelectedGroup.StudentIds.Contains(s.Id)).ToList();

            foreach (var member in members)
            {
                GroupMembers.Add(member);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新小组成员列表失败");
        }
    }

    /// <summary>
    /// 刷新未分组学生列表
    /// </summary>
    private async Task RefreshAvailableStudentsAsync()
    {
        try
        {
            var allStudents = await _studentService.GetAllStudentsAsync();
            var ungrouped = allStudents.Where(s => s.GroupId == null).ToList();

            AvailableStudents.Clear();
            foreach (var student in ungrouped)
            {
                AvailableStudents.Add(student);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新未分组学生列表失败");
        }
    }
}
