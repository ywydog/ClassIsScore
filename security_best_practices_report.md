# 安全最佳实践审查报告 — ClassIsScore

> **生成时间**：2026-07-10
> **审查范围**：`/workspace/classisscore/client`
> **审查方法**：基于 `security-best-practices` skill 的参考文档（`javascript-typescript-vue-web-frontend-security.md`、`javascript-general-web-frontend-security.md`、`javascript-express-web-server-security.md`）进行静态扫描
> **覆盖栈**：Vue 3 + TypeScript + Vite 前端 + Tauri 2 (Rust) 后端 + Axum HTTP 伺服

> **重要说明**：该 skill 仅直接支持 Python / JavaScript / TypeScript / Go 四个语言。后端为 Rust + Axum（**不在 skill 明确支持范围**），本报告对 Rust 后端部分基于通用安全知识（OWASP、Axum/tower-http 官方建议）进行审查，并明示非 skill 直接覆盖的范围。
>
> 本仓库整体为**本地单用户桌面应用 + 局域网网络伺服**，不直接暴露公网，所以"机密面"小于典型 Web 应用。但仍存在以下值得注意的问题，按严重度排序。

---

## 摘要 (Executive Summary)

* 整体安全态势：中等偏好。密码使用 SHA-256 单轮（无 salt）存储 + 错误信息泄露 / 网络伺服 CORS `Any` / 跨 index.html 与 tauri.conf.json 的 CSP 不一致是主要风险。
* 发现 2 个 High、3 个 Medium、3 个 Low 级别问题。
* 没有发现 Critical 级别的注入类漏洞（无 `v-html` / `innerHTML` / `eval` / `new Function` 等危险 sink 的实际使用）。
* 自动评估规则（auto_score）使用全 `i64` 主键通过 URL/命令参数传递，符合规范中的"避免使用自增 ID"提醒；建议改用 UUID 以增加不可猜测性。

---

## Critical（无）

未发现 Critical 级别问题。

---

## High

### [H-1] 密码使用 SHA-256 单轮散列且无 salt

* **Rule ID**：JS-AUTH-Hash-001（不在 skill 文档中显式编号，对应通用 OWASP "Password Storage" 章节）
* **位置**：[client/src-tauri/src/commands/auth.rs:17-21](file:///workspace/classisscore/client/src-tauri/src/commands/auth.rs#L17-L21)
* **证据**：
  ```rust
  fn hash_password(password: &str) -> String {
      let mut hasher = Sha256::new();
      hasher.update(password.as_bytes());
      hex::encode(hasher.finalize())
  }
  ```
  存储在 `admin_settings.setting_value` 字段（[client/src-tauri/src/commands/auth.rs:71-79](file:///workspace/classisscore/client/src-tauri/src/commands/auth.rs#L71-L79)），整个文件没有 salt 字段。
* **影响**：攻击者拿到 SQLite 文件即可在彩虹表 / 离线 GPU 上秒破。管理员密码通常是设备主人常用的密码，命中率较高。
* **修复**：改用 `argon2`（推荐）或 `bcrypt`（最低门槛），输出包含 salt 与参数；并显式迁移历史 hash。
* **缓解**：Tauri 2 默认对应用数据目录有 OS 级权限隔离；同时应用仅本机使用，暴露面较小。
* **误报可能**：无（确实是无 salt SHA-256）。

### [H-2] 网络伺服 CORS `allow_origin(Any)`，且没有鉴权

* **Rule ID**：EXPRESS-CORS-001（类比，借用）
* **位置**：[client/src-tauri/src/server/mod.rs:89-94](file:///workspace/classisscore/client/src-tauri/src/server/mod.rs#L89-L94)
* **证据**：
  ```rust
  .layer(
      CorsLayer::new()
          .allow_origin(Any)
          .allow_methods(Any)
          .allow_headers(Any),
  )
  ```
  整个 axum 路由除了 `/api/health` 没有鉴权中间件（[client/src-tauri/src/server/mod.rs:86-88](file:///workspace/classisscore/client/src-tauri/src/server/mod.rs#L86-L88)）；`tauri.conf.json` 中没有声明网络伺服专用的 Tauri capability（仅 `shell:allow-open` 等桌面能力）。
* **影响**：开启网络伺服后，**同一局域网内任何设备**都可以：
  1. 访问应用前端页面（hash 路由下的内容）；
  2. 因为前端默认会把路由守卫的"是否已登录"交给后端 `settings_get_all` / `auth_verify`（参见 [client/src/router/index.ts:209-242](file:///workspace/classisscore/client/src/router/index.ts#L209-L242)），攻击者有可能在同网段探查到设备上的 token / 业务数据（详见 H-2 子项）。
  3. 由于 CORS 是 `Any`，也允许第三方站点在用户浏览器内跨域读取伺服返回的响应。
* **修复**：
  * 至少为 `/api/*` 加一个共享口令（PIN）中间件，与 admin 口令独立。
  * 缩小 CORS：`allow_origin` 改为具体的开发域名列表，或仅在 LAN 内 hostname 列表。
  * 把"未鉴权"的服务（如纯静态页）与"会泄露状态"的 `/api/*` 拆开。
* **缓解**：默认仅绑定 `127.0.0.1`，需用户主动开启"网络伺服"才暴露到 `0.0.0.0`（[client/src-tauri/src/commands/server.rs:64-65](file:///workspace/classisscore/client/src-tauri/src/commands/server.rs#L64-L65)），故 LAN 外 / 互联网攻击面小。

---

## Medium

### [M-1] 登录失败信息泄露账户设置状态

* **Rule ID**：VUE-AUTH-001 / 通用错误处理原则
* **位置**：[client/src-tauri/src/commands/auth.rs:76-95](file:///workspace/classisscore/client/src-tauri/src/commands/auth.rs#L76-L95)
* **证据**：
  ```rust
  match setting {
      Some(model) => {
          // 校验密码
          if stored_hash == hash_password(&password) { Ok(成功) }
          else { Ok(AuthResult { success: false, message: "密码错误" }) }
      }
      None => Ok(AuthResult { success: false, message: "管理员密码未设置" }),
  }
  ```
* **影响**：消息区分"未设置密码"与"密码错误"，是低风险但符合规范中"避免指纹"原则的改进点；未授权用户可借此判断设备是否初始化。
* **修复**：统一返回"账号或密码不正确"（设备为本地使用，影响极小，但保持一致更专业）。
* **缓解**：本地单用户场景，影响很小。

### [M-2] CSP 在 `index.html` 与 `tauri.conf.json` 中不一致

* **Rule ID**：JS-CSP-001 / VUE-HEADERS-001
* **位置**：
  * [client/index.html:10](file:///workspace/classisscore/client/index.html#L10)
  * [client/src-tauri/tauri.conf.json:27](file:///workspace/classisscore/client/src-tauri/tauri.conf.json#L27)
* **证据**：
  * `index.html` 规定：`script-src 'self' 'unsafe-inline'`（**没有 `unsafe-eval`**），`connect-src 'self' http://localhost:18888 ws://localhost:18888`（**不包含 5173 端口**）
  * `tauri.conf.json` 规定：`script-src 'self' 'unsafe-eval' 'unsafe-inline'`，`connect-src ipc: http://ipc.localhost https://fonts.googleapis.com https://fonts.gstatic.com`（**不包含 localhost:18888**）
  Tauri 2 在生产构建中应用 `tauri.conf.json` 的 `security.csp`，开发时倾向使用 `index.html` 的 `<meta>`（参见 Tauri 文档）。结果：
    * 生产端启用了 `unsafe-eval`，与 index.html 的策略不一致；
    * 局域网网络伺服期望 `connect-src` 包含伺服端口（默认 6806），但当前两条 CSP 都没覆盖。
  * 额外：`connect-src` 同时存在 `https://fonts.googleapis.com`（line 27），但代码注释里"字体已本地自托管"（[client/index.html:12-13](file:///workspace/classisscore/client/index.html#L12-L13)），构成不必要的外部访问白名单。
* **影响**：
  1. 启用 `unsafe-eval` 显著削弱了 XSS 防御（一旦前端出现 DOM XSS 漏洞，攻击者可直接 `eval`）。
  2. 网络伺服开启后，前端若尝试用 `fetch` 访问局域网 URL，可能被 CSP 阻塞（取决于取哪条策略）。
  3. 出现"自托管字体却仍允许 googleapis 域名"的明显疏忽。
* **修复**：
  * 移除 `unsafe-eval`（vue-tsc/vite 不需要 eval，devtools 由 dev-only 控制）；
  * 同步两条 CSP 规则，至少 `connect-src` 包含网络伺服端口（`http://*:6806 ws://*:6806` 或 `'self'` + 显式 `http://192.168.0.0/16`）；
  * 删除 `https://fonts.googleapis.com` `https://fonts.gstatic.com` 两条（已自托管）。
* **缓解**：应用仅本地 / 局域网使用；目前没有发现 DOM XSS 漏洞。

### [M-3] 自动评估规则使用自增主键 `i64` 作为公开 ID

* **Rule ID**：通用 "Avoid Using Incrementing IDs"（skill 总论 §General Security Advice）
* **位置**：[client/src-tauri/src/commands/auto_score.rs:12-24](file:///workspace/classisscore/client/src-tauri/src/commands/auto_score.rs#L12-L24)
* **证据**：`AutoScoreRuleInput` / `auto_score_*` 命令中的所有 `id` 字段都是 `i64`，且这些 ID 在前端日志、错误信息、URL fragment 中出现。
* **影响**：攻击者可以从某条规则的 ID 推算其他规则的 ID（数量 + 内容）；不构成直接漏洞，但是低成本的加固项。
* **修复**：在 `auto_evaluation_config` 实体中加 `uuid: String UNIQUE`，把 `id` 仅作为内部使用，外部 API 返回 / 接收 UUID。
* **缓解**：所有变更操作仍需 `auth_verify`，且仅 LAN 内可访问。

---

## Low

### [L-1] 路由守卫把后端鉴权响应也存进 localStorage

* **Rule ID**：VUE-AUTH-001 / JS-STORAGE-001
* **位置**：[client/src/router/index.ts:222-238](file:///workspace/classisscore/client/src/router/index.ts#L222-L238)
* **证据**：当后端返回 `onboardingCompleted=true` 时，前端写 `localStorage.setItem('onboardingCompleted', 'true')` 作为缓存。逻辑只缓存"是否完成过引导"，并非凭据，所以风险低；但当 `tauri-plugin-store` / `pinia-plugin-persistedstate` 后续接入时需注意不要把 `auth` 类的 state 持久化到 storage。
* **影响**：当前无敏感数据落入 storage，但代码模式会成为未来错误引入凭据持久化的诱因。
* **修复**：保持现状，并加注释说明"只允许缓存非敏感开关位状态"。

### [L-2] `document.execCommand('copy')` 仍在使用

* **Rule ID**：JS-XSS-001（不直接相关）/ 通用弃用 API
* **位置**：[client/src/components/common/NetworkServeToggle.vue:94](file:///workspace/classisscore/client/src/components/common/NetworkServeToggle.vue#L94)
* **证据**：
  ```ts
  const input = document.createElement('input')
  input.value = statusUrl.value
  document.body.appendChild(input)
  input.select()
  document.execCommand('copy')
  document.body.removeChild(input)
  ```
* **影响**：当前 `statusUrl` 是后端返回值（IP + 端口）拼接出来的，不含用户输入，**没有直接 XSS 风险**；但 `document.execCommand` 已被主流浏览器标记为 deprecated，主要问题是行为不一致。在 Clipboard API 不可用时作为 fallback 是可接受的。
* **修复**：保留 fallback，但增加注释说明 Clipboard API 失败时使用。

### [L-3] 网络伺服没有请求体大小限制

* **Rule ID**：EXPRESS-BODY-001（类比 Axum）
* **位置**：[client/src-tauri/src/server/mod.rs:86-98](file:///workspace/classisscore/client/src-tauri/src/server/mod.rs#L86-L98)
* **证据**：axum `Router` 没有任何 `RequestBodyLimitLayer` / `DefaultBodyLimit::max(...)`，`/api/health` 也无大小限制。
* **影响**：理论上攻击者可发送大 body 耗内存；由于未鉴权，但暴露面是 LAN，影响很小。
* **修复**：用 `tower_http::limit::RequestBodyLimitLayer` 设置上限（如 1 MiB），或将所有 `api` 路由限制到 64 KiB。

---

## 已确认无问题（Negative Findings）

* **DOM XSS sinks**：全项目 `grep` 未发现 `v-html` / `innerHTML` / `insertAdjacentHTML` / `document.write` / `eval(` / `new Function` / `setTimeout("...")` / `setInterval("...")`。安全。
* **子进程注入**：Rust 后端没有 `Command::new` / `std::process::Command` 使用；唯一使用 `shell.open` 的地方（[client/src-tauri/src/commands/app.rs:32-34](file:///workspace/classisscore/client/src-tauri/src/commands/app.rs#L32-L34)）做了协议白名单（`http/https/tel/mailto`），符合规范。
* **外部链接**：[client/src/views/mobile/About.vue:11-12](file:///workspace/classisscore/client/src/views/mobile/About.vue#L11-L12) 显式加了 `rel="noopener"`；[client/src/views/admin/About.vue:39](file:///workspace/classisscore/client/src/views/admin/About.vue#L39) 缺少 `rel="noopener noreferrer"`，属于 Element Plus `<el-link>` 默认行为。**追加 noreferrer 即可**，但严重度极低。
* **依赖锁定**：`package.json` 中有 `package-lock.json`、`Cargo.lock` 都在仓库，符合 VUE-SUPPLY-001 / 通用建议。
* **环境变量前缀**：`vite.config.ts:37` 仅暴露 `VITE_` / `TAURI_ENV_*`，无泛化。`process.env.TAURI_DEV_HOST` 是 Node 端读 env，不会进 client bundle，符合 VUE-SECRETS-002。
* **open redirect**：在路由中未发现 `route.query.next` / `redirect` / `return_to` 流入 `router.push` 或 `window.location` 的情况；`window.location.hash = '#/display'`（[client/src/components/layout/MobileLayout.vue:229](file:///workspace/classisscore/client/src/components/layout/MobileLayout.vue#L229)）是常量。

---

## 报告位置

`/workspace/classisscore/security_best_practices_report.md`

---

## 建议优先修复顺序

1. **H-1**（密码 hash）—— 一行依赖 + 迁移函数，影响最大。
2. **H-2**（伺服 CORS / 鉴权）—— 加一个 PIN 中间件。
3. **M-2**（CSP 同步）—— 单文件修改，风险最低。
4. **M-1 / M-3 / L-*** —— 顺带修复。

如需对其中某项进行修复，请告诉我具体编号，我可以分次单 commit 提交。
