using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 学生服务接口，提供学生数据的增删改查及导入排序功能
/// </summary>
public interface IStudentService
{
    /// <summary>
    /// 获取所有学生
    /// </summary>
    Task<List<Student>> GetAllStudentsAsync();

    /// <summary>
    /// 根据ID获取学生
    /// </summary>
    /// <param name="id">学生ID</param>
    Task<Student?> GetStudentByIdAsync(Guid id);

    /// <summary>
    /// 添加学生
    /// </summary>
    /// <param name="student">要添加的学生对象</param>
    Task<Student> AddStudentAsync(Student student);

    /// <summary>
    /// 更新学生信息
    /// </summary>
    /// <param name="student">要更新的学生对象</param>
    Task<Student> UpdateStudentAsync(Student student);

    /// <summary>
    /// 删除学生
    /// </summary>
    /// <param name="id">要删除的学生ID</param>
    Task DeleteStudentAsync(Guid id);

    /// <summary>
    /// 从Excel文件导入学生
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    Task<List<Student>> ImportFromExcelAsync(string filePath);

    /// <summary>
    /// 从CSV文件导入学生
    /// </summary>
    /// <param name="filePath">CSV文件路径</param>
    Task<List<Student>> ImportFromCsvAsync(string filePath);

    /// <summary>
    /// 按姓氏首字母排序
    /// </summary>
    /// <param name="students">要排序的学生列表</param>
    List<Student> SortByName(List<Student> students);

    /// <summary>
    /// 按学号排序
    /// </summary>
    /// <param name="students">要排序的学生列表</param>
    List<Student> SortByStudentNumber(List<Student> students);
}
