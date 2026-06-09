using System;
using System.IO;

namespace ClassIsScore.Helpers;

/// <summary>
/// 应用路径管理，提供所有数据目录的统一访问入口
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// 应用数据根目录（应用目录下的 data 文件夹）
    /// </summary>
    public static string AppRootFolderPath { get; private set; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "data"));

    /// <summary>
    /// 数据目录
    /// </summary>
    public static string DataFolderPath => Path.Combine(AppRootFolderPath, "Data");

    /// <summary>
    /// 日志目录
    /// </summary>
    public static string LogFolderPath => Path.Combine(AppRootFolderPath, "Logs");

    /// <summary>
    /// 备份目录
    /// </summary>
    public static string BackupFolderPath => Path.Combine(AppRootFolderPath, "Backup");

    /// <summary>
    /// 插件目录
    /// </summary>
    public static string PluginFolderPath => Path.Combine(AppRootFolderPath, "Plugins");

    /// <summary>
    /// 配置目录
    /// </summary>
    public static string ConfigFolderPath => Path.Combine(AppRootFolderPath, "Config");

    /// <summary>
    /// 确保所有必要目录存在
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        EnsureDirectoryExists(AppRootFolderPath);
        EnsureDirectoryExists(DataFolderPath);
        EnsureDirectoryExists(LogFolderPath);
        EnsureDirectoryExists(BackupFolderPath);
        EnsureDirectoryExists(PluginFolderPath);
        EnsureDirectoryExists(ConfigFolderPath);
    }

    /// <summary>
    /// 确保指定目录存在，不存在则创建
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
