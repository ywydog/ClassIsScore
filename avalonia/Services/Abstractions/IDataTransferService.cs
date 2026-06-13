using System;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 数据传输服务接口，提供数据导出和导入功能
/// </summary>
public interface IDataTransferService
{
    /// <summary>
    /// 导出所有数据到zip文件
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>实际导出的文件路径</returns>
    Task<string> ExportAllDataAsync(string outputPath);

    /// <summary>
    /// 从zip文件导入所有数据
    /// </summary>
    /// <param name="zipFilePath">zip文件路径</param>
    /// <returns>是否导入成功</returns>
    Task<bool> ImportAllDataAsync(string zipFilePath);

    /// <summary>
    /// 仅导出学生数据到Excel文件
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>实际导出的文件路径</returns>
    Task<string> ExportStudentsOnlyAsync(string outputPath);

    /// <summary>
    /// 仅导入学生数据（支持Excel/CSV/zip）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否导入成功</returns>
    Task<bool> ImportStudentsOnlyAsync(string filePath);

    /// <summary>
    /// 数据传输进度变更事件
    /// </summary>
    event EventHandler<DataTransferProgressEventArgs>? ProgressChanged;
}
