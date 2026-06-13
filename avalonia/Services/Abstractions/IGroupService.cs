using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 小组服务接口，提供小组的创建、删除及成员管理功能
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// 获取所有小组
    /// </summary>
    Task<List<StudentGroup>> GetAllGroupsAsync();

    /// <summary>
    /// 创建小组
    /// </summary>
    /// <param name="name">小组名称</param>
    /// <param name="studentIds">初始成员ID列表</param>
    Task<StudentGroup> CreateGroupAsync(string name, List<Guid> studentIds);

    /// <summary>
    /// 删除小组
    /// </summary>
    /// <param name="id">要删除的小组ID</param>
    Task DeleteGroupAsync(Guid id);

    /// <summary>
    /// 向小组添加学生
    /// </summary>
    /// <param name="groupId">小组ID</param>
    /// <param name="studentIds">要添加的学生ID列表</param>
    Task AddStudentsToGroupAsync(Guid groupId, List<Guid> studentIds);

    /// <summary>
    /// 从小组移除学生
    /// </summary>
    /// <param name="groupId">小组ID</param>
    /// <param name="studentIds">要移除的学生ID列表</param>
    Task RemoveStudentsFromGroupAsync(Guid groupId, List<Guid> studentIds);
}
