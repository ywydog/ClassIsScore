# ClassIsScore URI 调用文档

ClassIsScore 支持通过自定义 URI 协议 `classisscore://` 从外部启动应用并导航到指定页面。

## 协议格式

```
classisscore://app/{page}[?param1=value1&param2=value2]
```

| 部分 | 说明 |
|------|------|
| `classisscore` | 协议名（已注册的 URI Scheme） |
| `app` | 导航主机名 |
| `{page}` | 目标页面路径 |
| `?param=value` | 可选查询参数 |

## 命令行调用

```bash
# Windows
ClassIsScore.exe --uri classisscore://app/scores

# Linux
./ClassIsScore --uri classisscore://app/scores

# macOS
open -a ClassIsScore --args --uri classisscore://app/scores
```

## 支持的页面路径

### 基础导航

| URI | 页面 | 说明 |
|-----|------|------|
| `classisscore://app/home` | 主页 | 积分显示主页 |
| `classisscore://app/students` | 学生管理 | 学生列表与导入 |
| `classisscore://app/scores` | 积分管理 | 加减分操作 |
| `classisscore://app/settlement` | 结算 | 结算与导出 |
| `classisscore://app/leaderboard` | 排行榜 | 个人/小组排行 |
| `classisscore://app/settings` | 设置 | 应用设置 |

### 扩展导航（预留）

| URI | 页面 | 说明 |
|-----|------|------|
| `classisscore://app/groups` | 小组管理 | 小组创建与成员管理 |
| `classisscore://app/auto-evaluation` | 自动评价 | 定时评价配置 |
| `classisscore://app/admin-settings` | 管理员设置 | 验证方式配置 |

## 编程调用

### C# 调用示例

```csharp
// 通过进程启动
Process.Start(new ProcessStartInfo("ClassIsScore.exe")
{
    ArgumentList = { "--uri", "classisscore://app/scores" }
});

// 通过系统 Shell 调用（需先注册 URI 协议）
Process.Start(new ProcessStartInfo("classisscore://app/scores")
{
    UseShellExecute = true
});
```

### PowerShell 调用示例

```powershell
# 命令行参数方式
& ".\ClassIsScore.exe" --uri "classisscore://app/scores"

# 系统协议方式
Start-Process "classisscore://app/scores"
```

### Bash 调用示例

```bash
# 命令行参数方式
./ClassIsScore --uri classisscore://app/scores

# 系统协议方式（需注册 URI 协议）
xdg-open classisscore://app/scores
```

### Python 调用示例

```python
import subprocess

# 命令行参数方式
subprocess.Popen(["./ClassIsScore", "--uri", "classisscore://app/scores"])

# 系统协议方式
import webbrowser
webbrowser.open("classisscore://app/scores")
```

## 与 ClassIsland 联动

ClassIsScore 的 URI 协议设计参考了 ClassIsland，可通过 IPC 或 URI 协议实现联动：

```
# ClassIsland 中通过组件调用 ClassIsScore
classisscore://app/scores           → 打开积分管理
classisscore://app/leaderboard      → 打开排行榜
```

## 注册 URI 协议

### Windows

在安装时写入注册表：

```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\SOFTWARE\Classes\classisscore]
@="URL:ClassIsScore Protocol"
"URL Protocol"=""

[HKEY_CURRENT_USER\SOFTWARE\Classes\classisscore\shell\open\command]
@="\"C:\\Program Files\\ClassIsScore\\ClassIsScore.exe\" --uri \"%1\""
```

### Linux

创建 `.desktop` 文件：

```ini
[Desktop Entry]
Type=Application
Name=ClassIsScore
Exec=/usr/bin/classisscore --uri %U
MimeType=x-scheme-handler/classisscore;
NoDisplay=true
```

然后运行：
```bash
xdg-mime default classisscore.desktop x-scheme-handler/classisscore
```

### macOS

在 `Info.plist` 中注册：

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>ClassIsScore URI</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>classisscore</string>
        </array>
    </dict>
</array>
```

## 错误处理

| 场景 | 行为 |
|------|------|
| 应用未启动 | 启动应用并导航到指定页面 |
| 应用已启动 | 激活窗口并导航到指定页面 |
| 无效路径 | 记录警告日志，停留在当前页面 |
| 格式错误 | 记录错误日志，忽略该 URI |
