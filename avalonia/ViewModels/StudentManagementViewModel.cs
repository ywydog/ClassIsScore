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
/// 学生管理页面 ViewModel
/// </summary>
public partial class StudentManagementViewModel : ObservableObject
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentManagementViewModel> _logger;

    /// <summary>
    /// 学生列表
    /// </summary>
    public ObservableCollection<Student> Students { get; } = new();

    /// <summary>
    /// 当前选中的学生
    /// </summary>
    [ObservableProperty]
    private Student? _selectedStudent;

    /// <summary>
    /// 排序选项索引：0=按姓名，1=按学号
    /// </summary>
    [ObservableProperty]
    private int _sortOptionIndex;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 添加/编辑学生对话框中使用的临时学生对象
    /// </summary>
    [ObservableProperty]
    private Student? _editingStudent;

    /// <summary>
    /// 是否显示编辑对话框
    /// </summary>
    [ObservableProperty]
    private bool _isEditDialogOpen;

    /// <summary>
    /// 编辑对话框标题
    /// </summary>
    [ObservableProperty]
    private string _editDialogTitle = string.Empty;

    /// <summary>
    /// 所有可用宠物类型
    /// </summary>
    public PetTypeInfo[] PetTypes => PetSystem.AllPetTypes;

    /// <summary>
    /// 编辑中学生选中的宠物类型ID
    /// </summary>
    [ObservableProperty]
    private string? _editingPetType;

    public StudentManagementViewModel(
        IStudentService studentService,
        ILogger<StudentManagementViewModel> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// 加载学生列表
    /// </summary>
    [RelayCommand]
    private async Task LoadStudentsAsync()
    {
        try
        {
            IsLoading = true;
            var students = await _studentService.GetAllStudentsAsync();
            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }
            ApplyCurrentSort();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载学生列表失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 添加学生命令
    /// </summary>
    [RelayCommand]
    private void AddStudent()
    {
        EditingStudent = new Student();
        EditingPetType = null;
        EditDialogTitle = "添加学生";
        IsEditDialogOpen = true;
    }

    /// <summary>
    /// 编辑学生命令
    /// </summary>
    [RelayCommand]
    private void EditStudent(Student? student)
    {
        if (student == null) return;
        // 创建副本进行编辑
        EditingStudent = new Student
        {
            Id = student.Id,
            Name = student.Name,
            StudentNumber = student.StudentNumber,
            Gender = student.Gender,
            Avatar = student.Avatar,
            Score = student.Score,
            GroupId = student.GroupId,
            PetType = student.PetType,
            PetExp = student.PetExp,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
        EditingPetType = student.PetType;
        EditDialogTitle = "编辑学生";
        IsEditDialogOpen = true;
    }

    /// <summary>
    /// 保存学生（添加或更新）
    /// </summary>
    [RelayCommand]
    private async Task SaveStudentAsync()
    {
        if (EditingStudent == null) return;
        if (string.IsNullOrWhiteSpace(EditingStudent.Name)) return;

        try
        {
            // 同步宠物类型选择
            EditingStudent.PetType = EditingPetType;

            // 判断是新增还是更新
            var existing = await _studentService.GetStudentByIdAsync(EditingStudent.Id);
            if (existing != null)
            {
                // 更新
                await _studentService.UpdateStudentAsync(EditingStudent);
                _logger.LogInformation("更新学生: {Name}", EditingStudent.Name);
            }
            else
            {
                // 新增
                await _studentService.AddStudentAsync(EditingStudent);
                _logger.LogInformation("添加学生: {Name}", EditingStudent.Name);
            }

            IsEditDialogOpen = false;
            await LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存学生失败");
        }
    }

    /// <summary>
    /// 删除学生命令
    /// </summary>
    [RelayCommand]
    private async Task DeleteStudentAsync(Student? student)
    {
        if (student == null) return;

        try
        {
            await _studentService.DeleteStudentAsync(student.Id);
            _logger.LogInformation("删除学生: {Name}", student.Name);
            await LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除学生失败");
        }
    }

    /// <summary>
    /// 从Excel导入学生
    /// </summary>
    [RelayCommand]
    private async Task ImportExcelAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            IsLoading = true;
            var imported = await _studentService.ImportFromExcelAsync(filePath);
            _logger.LogInformation("从Excel导入 {Count} 名学生", imported.Count);
            await LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Excel导入学生失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 从CSV导入学生
    /// </summary>
    [RelayCommand]
    private async Task ImportCsvAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            IsLoading = true;
            var imported = await _studentService.ImportFromCsvAsync(filePath);
            _logger.LogInformation("从CSV导入 {Count} 名学生", imported.Count);
            await LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从CSV导入学生失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 排序选项变更时重新排序
    /// </summary>
    partial void OnSortOptionIndexChanged(int value)
    {
        ApplyCurrentSort();
    }

    /// <summary>
    /// 应用当前排序
    /// </summary>
    private void ApplyCurrentSort()
    {
        var studentList = Students.ToList();
        var sorted = SortOptionIndex switch
        {
            0 => _studentService.SortByName(studentList),
            1 => _studentService.SortByStudentNumber(studentList),
            _ => studentList
        };

        Students.Clear();
        foreach (var student in sorted)
        {
            Students.Add(student);
        }
    }
}
