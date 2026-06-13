using System.Reflection;
using System.Runtime.Loader;

namespace ClassIsScore.Services;

/// <summary>
/// 插件程序集隔离加载上下文，支持依赖解析和卸载
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// 创建插件加载上下文
    /// </summary>
    /// <param name="pluginPath">插件入口程序集路径</param>
    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 优先从插件目录解析依赖
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // 回退到默认上下文（共享主应用的程序集）
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
