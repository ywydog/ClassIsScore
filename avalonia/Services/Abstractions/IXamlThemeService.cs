using System.Collections.ObjectModel;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 自定义 XAML 主题服务接口，管理 .cisui 主题包的加载、启用和卸载
/// </summary>
public interface IXamlThemeService
{
    /// <summary>
    /// 已安装的主题列表
    /// </summary>
    ObservableCollection<ThemeInfo> Themes { get; }

    /// <summary>
    /// 加载所有已安装的主题元数据
    /// </summary>
    Task LoadAllThemesAsync();

    /// <summary>
    /// 导入 .cisui 主题包
    /// </summary>
    /// <param name="packagePath">主题包文件路径</param>
    Task ImportThemeAsync(string packagePath);

    /// <summary>
    /// 启用指定主题
    /// </summary>
    /// <param name="themeId">主题唯一标识</param>
    void EnableTheme(string themeId);

    /// <summary>
    /// 禁用指定主题
    /// </summary>
    /// <param name="themeId">主题唯一标识</param>
    void DisableTheme(string themeId);

    /// <summary>
    /// 删除指定主题
    /// </summary>
    /// <param name="themeId">主题唯一标识</param>
    Task DeleteThemeAsync(string themeId);
}
