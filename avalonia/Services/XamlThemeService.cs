using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 自定义 XAML 主题服务实现，管理 .cisui 主题包的加载、启用和卸载
/// </summary>
public class XamlThemeService : IXamlThemeService
{
    private readonly ILogger<XamlThemeService> _logger;
    private readonly List<IStyle> _loadedStyles = new();

    public ObservableCollection<ThemeInfo> Themes { get; } = new();

    public XamlThemeService(ILogger<XamlThemeService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 加载所有已安装的主题元数据
    /// </summary>
    public async Task LoadAllThemesAsync()
    {
        Themes.Clear();

        var themesDir = AppPaths.ThemesFolderPath;
        if (!Directory.Exists(themesDir))
        {
            _logger.LogInformation("主题目录不存在，跳过加载");
            return;
        }

        foreach (var dir in Directory.GetDirectories(themesDir))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("跳过无 manifest.json 的目录: {Dir}", dir);
                continue;
            }

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ThemeManifest>(json);
                if (manifest == null || string.IsNullOrEmpty(manifest.Id))
                {
                    _logger.LogWarning("主题清单无效: {Path}", manifestPath);
                    continue;
                }

                var themeInfo = new ThemeInfo
                {
                    Manifest = manifest,
                    ThemeFolderPath = dir,
                    IsEnabled = false
                };

                Themes.Add(themeInfo);
                _logger.LogInformation("发现主题: {Name} ({Id})", manifest.Name, manifest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载主题清单失败: {Path}", manifestPath);
            }
        }

        // 加载已启用的主题样式
        foreach (var theme in Themes.Where(t => t.IsEnabled))
        {
            ApplyThemeStyles(theme);
        }
    }

    /// <summary>
    /// 导入 .cisui 主题包
    /// </summary>
    public async Task ImportThemeAsync(string packagePath)
    {
        if (!File.Exists(packagePath))
        {
            _logger.LogWarning("主题包不存在: {Path}", packagePath);
            return;
        }

        try
        {
            // 临时解压到内存以读取 manifest
            using var archive = ZipFile.OpenRead(packagePath);
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
            {
                _logger.LogError("主题包缺少 manifest.json: {Path}", packagePath);
                return;
            }

            ThemeManifest manifest;
            using (var stream = manifestEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                manifest = JsonSerializer.Deserialize<ThemeManifest>(json)
                           ?? throw new InvalidOperationException("无法解析 manifest.json");
            }

            if (string.IsNullOrEmpty(manifest.Id))
            {
                _logger.LogError("主题清单缺少 Id: {Path}", packagePath);
                return;
            }

            // 解压到 data/Themes/{Id}/
            var targetDir = Path.Combine(AppPaths.ThemesFolderPath, manifest.Id);
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

            ZipFile.ExtractToDirectory(packagePath, targetDir, overwriteFiles: true);

            var themeInfo = new ThemeInfo
            {
                Manifest = manifest,
                ThemeFolderPath = targetDir,
                IsEnabled = false
            };

            Themes.Add(themeInfo);
            _logger.LogInformation("已导入主题: {Name} ({Id})", manifest.Name, manifest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入主题包失败: {Path}", packagePath);
        }
    }

    /// <summary>
    /// 启用指定主题
    /// </summary>
    public void EnableTheme(string themeId)
    {
        var theme = Themes.FirstOrDefault(t => t.Manifest.Id == themeId);
        if (theme == null)
        {
            _logger.LogWarning("未找到主题: {Id}", themeId);
            return;
        }

        theme.IsEnabled = true;
        ApplyThemeStyles(theme);
        _logger.LogInformation("已启用主题: {Name}", theme.Manifest.Name);
    }

    /// <summary>
    /// 禁用指定主题
    /// </summary>
    public void DisableTheme(string themeId)
    {
        var theme = Themes.FirstOrDefault(t => t.Manifest.Id == themeId);
        if (theme == null) return;

        theme.IsEnabled = false;
        RemoveThemeStyles(theme);
        _logger.LogInformation("已禁用主题: {Name}", theme.Manifest.Name);
    }

    /// <summary>
    /// 删除指定主题
    /// </summary>
    public async Task DeleteThemeAsync(string themeId)
    {
        var theme = Themes.FirstOrDefault(t => t.Manifest.Id == themeId);
        if (theme == null) return;

        RemoveThemeStyles(theme);

        try
        {
            if (Directory.Exists(theme.ThemeFolderPath))
            {
                await Task.Run(() => Directory.Delete(theme.ThemeFolderPath, true));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除主题文件夹失败: {Path}", theme.ThemeFolderPath);
        }

        Themes.Remove(theme);
        _logger.LogInformation("已删除主题: {Name}", theme.Manifest.Name);
    }

    /// <summary>
    /// 应用主题样式到 Application
    /// </summary>
    private void ApplyThemeStyles(ThemeInfo theme)
    {
        var axamlPath = Path.Combine(theme.ThemeFolderPath, "Theme.axaml");
        if (!File.Exists(axamlPath))
        {
            _logger.LogWarning("主题缺少 Theme.axaml: {Path}", axamlPath);
            return;
        }

        try
        {
            var app = Application.Current;
            if (app == null) return;

            var uri = new Uri($"file:///{axamlPath.Replace('\\', '/')}");
            var loaded = AvaloniaXamlLoader.Load(uri);

            if (loaded is IStyle style)
            {
                app.Styles.Add(style);
                _loadedStyles.Add(style);
                _logger.LogInformation("已应用主题样式: {Name}", theme.Manifest.Name);
            }
            else if (loaded is ResourceDictionary resources)
            {
                app.Resources.MergedDictionaries.Add(resources);
                _logger.LogInformation("已合并主题资源: {Name}", theme.Manifest.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用主题样式失败: {Name}", theme.Manifest.Name);
        }
    }

    /// <summary>
    /// 移除主题样式
    /// </summary>
    private void RemoveThemeStyles(ThemeInfo theme)
    {
        var app = Application.Current;
        if (app == null) return;

        foreach (var style in _loadedStyles.ToList())
        {
            app.Styles.Remove(style);
        }
        _loadedStyles.Clear();
    }
}
