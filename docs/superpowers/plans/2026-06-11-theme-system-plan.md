# Theme System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a theme system that loads custom XAML themes from `.cisui` archives.

**Architecture:** Custom themes are packaged in `.cisui` files containing a `manifest.json` and a `Theme.axaml` file. The `XamlThemeService` extracts these, reads the metadata, and merges the XAML dictionaries into the Avalonia application styles.

**Tech Stack:** Avalonia UI, `System.IO.Compression.ZipFile`, `System.Text.Json`

---

### Task 1: Theme Core Abstractions

**Files:**
- Create: `Models/ThemeManifest.cs`
- Create: `Models/ThemeInfo.cs`

- [ ] **Step 1: Write the failing test**
```csharp
// tests/ClassIsScore.Tests/ThemeCoreTests.cs
using ClassIsScore.Models;
using Xunit;
namespace ClassIsScore.Tests;

public class ThemeCoreTests
{
    [Fact]
    public void ThemeManifest_CanBeInstantiated()
    {
        var manifest = new ThemeManifest { Id = "test", Name = "Test" };
        Assert.Equal("test", manifest.Id);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test ClassIsScore.Tests --filter ThemeCoreTests`
Expected: FAIL

- [ ] **Step 3: Write minimal implementation**
```csharp
// Models/ThemeManifest.cs
namespace ClassIsScore.Models;
public class ThemeManifest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string TargetApiVersion { get; set; } = "";
}

// Models/ThemeInfo.cs
namespace ClassIsScore.Models;
public class ThemeInfo
{
    public ThemeManifest Manifest { get; set; } = new();
    public string ThemeFolderPath { get; set; } = "";
    public bool IsEnabled { get; set; } = false;
}
```

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test ClassIsScore.Tests`
Expected: PASS

- [ ] **Step 5: Commit**
```bash
git add ClassIsScore.Tests/ThemeCoreTests.cs Models/ThemeManifest.cs Models/ThemeInfo.cs
git commit -m "feat(theme): add core abstractions for theme system"
```

### Task 2: Implement XamlThemeService

**Files:**
- Create: `Services/Abstractions/IXamlThemeService.cs`
- Create: `Services/XamlThemeService.cs`

- [ ] **Step 1: Write the failing test**
```csharp
// tests/ClassIsScore.Tests/XamlThemeServiceTests.cs
using System.Threading.Tasks;
using ClassIsScore.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace ClassIsScore.Tests;

public class XamlThemeServiceTests
{
    [Fact]
    public async Task Service_CanBeCreated()
    {
        var service = new XamlThemeService(NullLogger<XamlThemeService>.Instance);
        Assert.NotNull(service.Themes);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test ClassIsScore.Tests --filter XamlThemeServiceTests`

- [ ] **Step 3: Write minimal implementation**
```csharp
// Services/Abstractions/IXamlThemeService.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClassIsScore.Models;
namespace ClassIsScore.Services.Abstractions;

public interface IXamlThemeService
{
    ObservableCollection<ThemeInfo> Themes { get; }
    Task LoadAllThemesAsync();
    Task ImportThemeAsync(string packagePath);
    void EnableTheme(string themeId);
    void DisableTheme(string themeId);
}

// Services/XamlThemeService.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

public class XamlThemeService : IXamlThemeService
{
    private readonly ILogger<XamlThemeService> _logger;
    public ObservableCollection<ThemeInfo> Themes { get; } = new();

    public XamlThemeService(ILogger<XamlThemeService> logger)
    {
        _logger = logger;
    }

    public Task LoadAllThemesAsync() => Task.CompletedTask;
    public Task ImportThemeAsync(string packagePath) => Task.CompletedTask;
    public void EnableTheme(string themeId) {}
    public void DisableTheme(string themeId) {}
}
```

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test ClassIsScore.Tests`

- [ ] **Step 5: Commit**
```bash
git add ClassIsScore.Tests/XamlThemeServiceTests.cs Services/Abstractions/IXamlThemeService.cs Services/XamlThemeService.cs
git commit -m "feat(theme): implement basic XamlThemeService"
```