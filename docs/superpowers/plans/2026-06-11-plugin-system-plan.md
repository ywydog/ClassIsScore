# Plugin System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a dynamic plugin system for ClassIsScore that loads `.cispg` (ZIP) or folder-based plugins using `AssemblyLoadContext` and dependency injection.

**Architecture:** Plugins will have a `manifest.json` for metadata. The `PluginService` will use `PluginLoadContext` to isolate assemblies, discover the `[PluginEntrance]` class extending `PluginBase`, and call `Initialize` during the application's service registration phase.

**Tech Stack:** .NET 8, `System.Runtime.Loader.AssemblyLoadContext`, `System.Text.Json`, `Microsoft.Extensions.DependencyInjection`

---

### Task 1: Create Plugin Core Abstractions

**Files:**
- Create: `Models/PluginManifest.cs`
- Create: `Abstractions/PluginBase.cs`
- Create: `Attributes/PluginEntranceAttribute.cs`
- Modify: `Models/PluginInfo.cs`

- [ ] **Step 1: Write the failing tests for Plugin Core**
```csharp
// tests/ClassIsScore.Tests/PluginCoreTests.cs
using System;
using ClassIsScore.Models;
using ClassIsScore.Abstractions;
using ClassIsScore.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ClassIsScore.Tests;

public class PluginCoreTests
{
    [PluginEntrance]
    public class TestPlugin : PluginBase
    {
        public override void Initialize(HostBuilderContext context, IServiceCollection services) {}
    }

    [Fact]
    public void PluginEntranceAttribute_CanBeApplied()
    {
        var attr = (PluginEntranceAttribute?)Attribute.GetCustomAttribute(typeof(TestPlugin), typeof(PluginEntranceAttribute));
        Assert.NotNull(attr);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test ClassIsScore.Tests --filter PluginCoreTests`
Expected: FAIL due to missing classes

- [ ] **Step 3: Write minimal implementation**
```csharp
// Models/PluginManifest.cs
namespace ClassIsScore.Models;
public class PluginManifest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string EntranceAssembly { get; set; } = "";
}

// Abstractions/PluginBase.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassIsScore.Models;
namespace ClassIsScore.Abstractions;
public abstract class PluginBase
{
    public PluginInfo Info { get; internal set; } = null!;
    public abstract void Initialize(HostBuilderContext context, IServiceCollection services);
}

// Attributes/PluginEntranceAttribute.cs
using System;
namespace ClassIsScore.Attributes;
[AttributeUsage(AttributeTargets.Class)]
public class PluginEntranceAttribute : Attribute { }
```

- [ ] **Step 4: Update PluginInfo**
```csharp
// Models/PluginInfo.cs
// (Replace contents or add properties)
namespace ClassIsScore.Models;
public class PluginInfo
{
    public PluginManifest Manifest { get; set; } = new();
    public string PluginFolderPath { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public object? Instance { get; set; }
}
```

- [ ] **Step 5: Run test to verify it passes**
Run: `dotnet test ClassIsScore.Tests`
Expected: PASS

- [ ] **Step 6: Commit**
```bash
git add ClassIsScore.Tests/PluginCoreTests.cs Models/PluginManifest.cs Abstractions/PluginBase.cs Attributes/PluginEntranceAttribute.cs Models/PluginInfo.cs
git commit -m "feat(plugin): add core abstractions for plugin system"
```

### Task 2: Implement PluginLoadContext

**Files:**
- Create: `Services/PluginLoadContext.cs`

- [ ] **Step 1: Write the failing test**
```csharp
// tests/ClassIsScore.Tests/PluginLoadContextTests.cs
using ClassIsScore.Services;
using Xunit;
namespace ClassIsScore.Tests;

public class PluginLoadContextTests
{
    [Fact]
    public void Context_CanBeCreated()
    {
        var context = new PluginLoadContext("test_path");
        Assert.NotNull(context);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test ClassIsScore.Tests --filter PluginLoadContextTests`
Expected: FAIL

- [ ] **Step 3: Write minimal implementation**
```csharp
// Services/PluginLoadContext.cs
using System;
using System.Reflection;
using System.Runtime.Loader;

namespace ClassIsScore.Services;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        return null;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test ClassIsScore.Tests`
Expected: PASS

- [ ] **Step 5: Commit**
```bash
git add ClassIsScore.Tests/PluginLoadContextTests.cs Services/PluginLoadContext.cs
git commit -m "feat(plugin): implement PluginLoadContext for isolated loading"
```

### Task 3: Implement PluginService Loading Logic

**Files:**
- Modify: `Services/PluginService.cs`

- [ ] **Step 1: Write the failing test**
```csharp
// tests/ClassIsScore.Tests/PluginServiceTests.cs
// (Mocking file system might be complex, so we'll test the interface implementation)
using System.Threading.Tasks;
using ClassIsScore.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace ClassIsScore.Tests;

public class PluginServiceTests
{
    [Fact]
    public async Task LoadPluginsAsync_DoesNotThrow()
    {
        var service = new PluginService(NullLogger<PluginService>.Instance);
        await service.LoadPluginsAsync();
        Assert.NotNull(service.LoadedPlugins);
    }
}
```

- [ ] **Step 2: Run test to verify it fails/passes**
Run: `dotnet test ClassIsScore.Tests --filter PluginServiceTests`

- [ ] **Step 3: Write minimal implementation**
Modify `Services/PluginService.cs` to scan `data/Plugins`, read `manifest.json`, load assembly via `PluginLoadContext`, and find the `[PluginEntrance]` type. (Provide code in implementation phase).

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test ClassIsScore.Tests`
Expected: PASS

- [ ] **Step 5: Commit**
```bash
git add ClassIsScore.Tests/PluginServiceTests.cs Services/PluginService.cs
git commit -m "feat(plugin): implement plugin scanning and loading logic"
```