using System;

namespace ClassIsScore.Attributes;

/// <summary>
/// 标记插件入口类的特性，用于反射查找插件入口
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class PluginEntranceAttribute : Attribute
{
}
