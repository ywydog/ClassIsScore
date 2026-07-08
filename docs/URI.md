# ClassIsScore URI 调用文档

> 外部应用（ClassIsland、命令行、PowerShell、Python 等）通过自定义 URI 协议 `classisscore://` 唤起 ClassIsScore 并跳转到指定页面。

---

## 1. 协议格式

```
classisscore://app/<page>[?param1=value1&param2=value2]
```

| 段 | 说明 |
|---|---|
| `classisscore` | 协议名（已注册的 URI Scheme） |
| `app` | 主机名，固定为 `app` |
| `<page>` | 目标页面标识，对应内部路由 |
| `?param=value` | 可选查询参数 |

支持的 `<page>` 与最终路由（与 [client/src/router/index.ts](../client/src/router/index.ts) 一致）：

### 桌面端（`/admin/...`）

| URI 中的 `<page>` | 实际跳转 |
|---|---|
| `home` | `/admin/dashboard` |
| `dashboard` | `/admin/dashboard` |
| `scores` | `/admin/scores` |
| `students` | `/admin/students` |
| `students/<id>` | `/admin/students/<id>` |
| `groups` | `/admin/groups` |
| `leaderboard` | `/admin/leaderboard` |
| `evaluation` | `/admin/evaluation` |
| `settlement` | `/admin/settlement` |
| `settings` | `/admin/settings` |
| `plugins` | `/admin/plugins` |
| `themes` | `/admin/themes` |
| `admin-settings` | `/admin/admin-settings` |
| `about` | `/admin/about` |

### 移动端（`/m/...`）

URI 解析时若检测到当前平台为 Android / iOS，`/admin/` 前缀自动改为 `/m/`。

### 大屏展示（`/display`）

| URI | 桌面端行为 | 移动端行为 |
|---|---|---|
| `classisscore://app/display` | 创建/唤起独立 `display` 窗口（多窗口模式） | 触发 `navigate` 事件，前端跳 `/display` 路由（单窗口模式） |

实现见 [client/src-tauri/src/commands/app.rs](../client/src-tauri/src/commands/app.rs) 的 `open_display_window`：

```rust
#[tauri::command]
pub async fn open_display_window(app_handle: tauri::AppHandle) -> Result<(), String> {
    #[cfg(not(target_os = "android"))]
    { /* 桌面：WebviewWindowBuilder */ }
    #[cfg(target_os = "android")]
    { app_handle.emit("navigate", "/display")?; }
    Ok(())
}
```

### 外部链接（`open_path` 命令）

`classisscore://app/open-path?url=<url>`（如 `https://` / `tel:` / `mailto:`）会通过系统默认应用打开：

```
classisscore://app/open-path?url=https%3A%2F%2Fexample.com
classisscore://app/open-path?url=tel%3A%2F%2F13800000000
classisscore://app/open-path?url=mailto%3A%2F%2Fhi%40example.com
```

白名单协议：`http` / `https` / `tel` / `mailto`。其他协议会返回 `不支持的协议` 错误。

---

## 2. 命令行调用

> `--uri` 参数会由应用启动时解析，等同于系统协议方式。

### 桌面端

```bash
# Windows
ClassIsScore.exe --uri classisscore://app/scores

# Linux
./classisscore --uri classisscore://app/scores

# macOS
open -a ClassIsScore --args --uri classisscore://app/scores
```

### 移动端

移动端通常通过系统协议唤起（点击外部链接 / 其他 App 调用 Intent），启动时由 Tauri `tauri-plugin-deep-link` 接收 URI。

---

## 3. 编程调用

### 3.1 C# / .NET

```csharp
using System.Diagnostics;

// 进程启动
Process.Start(new ProcessStartInfo("classisscore")
{
    ArgumentList = { "--uri", "classisscore://app/scores" }
});

// 系统协议方式（需先注册 URI Scheme）
Process.Start(new ProcessStartInfo("classisscore://app/scores")
{
    UseShellExecute = true
});
```

### 3.2 PowerShell

```powershell
# 命令行参数
& ".\ClassIsScore.exe" --uri "classisscore://app/scores"

# 系统协议
Start-Process "classisscore://app/scores"
```

### 3.3 Bash

```bash
# 命令行参数
./classisscore --uri classisscore://app/scores

# 系统协议（需先注册）
xdg-open classisscore://app/scores
```

### 3.4 Python

```python
import subprocess, webbrowser

# 命令行参数
subprocess.Popen(["./classisscore", "--uri", "classisscore://app/scores"])

# 系统协议
webbrowser.open("classisscore://app/scores")
```

### 3.5 Node.js

```js
import { spawn, exec } from 'node:child_process'
import { platform } from 'node:process'

// 命令行参数
spawn('classisscore', ['--uri', 'classisscore://app/scores'])

// 系统协议
if (platform === 'win32') exec('start "" "classisscore://app/scores"')
else if (platform === 'darwin') exec('open "classisscore://app/scores"')
else exec('xdg-open "classisscore://app/scores"')
```

---

## 4. 与 ClassIsland 联动

ClassIsland 支持自定义 URI 唤起外部应用，可直接在触发器里写：

```
classisscore://app/scores          → 打开积分管理
classisscore://app/leaderboard     → 打开排行榜
classisscore://app/display         → 打开大屏
classisscore://app/settings        → 打开设置
```

ClassIsland 触发器里推荐勾选「在已运行时激活窗口」避免重复启动。

---

## 5. 注册 URI 协议

> 安装包默认不强制注册 URI Scheme（避免误占用）。需要时按下面手动注册。

### 5.1 Windows

保存为 `classisscore.reg` 双击导入（或安装时通过 NSIS / WiX 写入）：

```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\SOFTWARE\Classes\classisscore]
@="URL:ClassIsScore Protocol"
"URL Protocol"=""

[HKEY_CURRENT_USER\SOFTWARE\Classes\classisscore\shell]

[HKEY_CURRENT_USER\SOFTWARE\Classes\classisscore\shell\open]

[HKEY_CURRENT_USER\SOFTWARE\Classes\classisscore\shell\open\command]
@="\"C:\\Program Files\\ClassIsScore\\ClassIsScore.exe\" --uri \"%1\""
```

### 5.2 Linux

创建 `~/.local/share/applications/classisscore.desktop`：

```ini
[Desktop Entry]
Type=Application
Name=ClassIsScore
Comment=ClassIsScore Protocol Handler
Exec=/usr/bin/classisscore --uri %U
MimeType=x-scheme-handler/classisscore;
NoDisplay=true
```

然后注册：

```bash
update-desktop-database ~/.local/share/applications/
xdg-mime default classisscore.desktop x-scheme-handler/classisscore
```

### 5.3 macOS

Tauri 2 默认会在 `.app` 打包时读取 `Info.plist` 中的 `CFBundleURLTypes`。当前未配置，需要的话在 `tauri.conf.json` 加：

```jsonc
"bundle": {
  "macOS": {
    "frameworks": [],
    "minimumSystemVersion": "10.15"
  }
}
```

并手动往 `Contents/Info.plist` 注入：

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

### 5.4 Android

Tauri 2.x 推荐使用 `tauri-plugin-deep-link` 插件。当前未集成，需要在 `tauri.conf.json` 加：

```jsonc
"plugins": {
  "deep-link": {
    "mobile": [{ "host": "app", "pathPrefix": "/" }],
    "desktop": { "schemes": ["classisscore"] }
  }
}
```

并在 `AndroidManifest.xml` 的主 Activity 加：

```xml
<intent-filter android:autoVerify="false">
  <action android:name="android.intent.action.VIEW" />
  <category android:name="android.intent.category.DEFAULT" />
  <category android:name="android.intent.category.BROWSABLE" />
  <data android:scheme="classisscore" android:host="app" />
</intent-filter>
```

---

## 6. 内部跳转事件

> URI 解析完后，后端会向前端 `emit('navigate', '/<route>')`，前端通过 `listen('navigate', ...)` 接收并 `router.push`（见 [client/src/main.ts](../client/src/main.ts)）。

```ts
import { listen } from '@tauri-apps/api/event'

await listen<string>('navigate', (event) => {
  if (event.payload.startsWith('/')) {
    router.push(event.payload)
  }
})
```

如需在 App 启动时一次性跳转（不通过事件），直接用 `--uri` 参数并由后端在 `setup` 阶段读取。

---

## 7. 错误处理

| 场景 | 行为 |
|---|---|
| 应用未启动 | 启动应用并导航到指定页面 |
| 应用已启动 | 激活窗口并导航到指定页面 |
| 未知 `<page>` | 记录警告日志，跳 `/admin/dashboard` |
| `open-path` 协议不在白名单 | 返回错误，前端弹出提示 |
| 格式错误 | 记录错误日志，忽略该 URI |

---

## 8. 调试

启用 tracing 后，可以在日志中看到 URI 解析过程：

```bash
# 桌面
RUST_LOG=classisscore_lib=debug ./classisscore --uri classisscore://app/scores

# Android (logcat)
adb logcat | grep -i "classisscore\|tauri"
```
