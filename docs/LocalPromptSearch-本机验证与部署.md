# LocalPromptSearch 本机验证与部署

## 当前结论

当前机器上已经确认：

- 已安装 Flow Launcher，安装目录为：
  - `C:\Users\Administrator\AppData\Local\FlowLauncher\app-2.1.3`
- 内置插件目录为：
  - `C:\Users\Administrator\AppData\Local\FlowLauncher\app-2.1.3\Plugins`
- 已安装 Visual Studio 自带的 `MSBuild.exe`

但当前机器缺少可用的 `.NET SDK`，因此暂时无法把 `Flow.Launcher.Plugin.LocalPromptSearch` 编译成可部署的插件产物。

## 为什么现在还不能直接验证

已经尝试用以下工具构建：

- `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe`

但构建失败，原因是：

- 缺少 `Microsoft.NET.Sdk`
- 当前环境中也没有可用的 `dotnet` 命令

这意味着：

- 现在不能只把源码目录复制到 Flow Launcher 的 `Plugins` 目录
- 必须先完成编译，生成 `.dll`、`.deps.json`、`plugin.json` 等文件后，才能部署

## 插件正确的部署形态

Flow Launcher 需要的是“编译后的插件目录”，而不是源码目录。

目标目录结构应类似：

```text
Flow.Launcher.Plugin.LocalPromptSearch/
  Flow.Launcher.Plugin.LocalPromptSearch.dll
  Flow.Launcher.Plugin.LocalPromptSearch.deps.json
  Flow.Launcher.Plugin.LocalPromptSearch.pdb
  plugin.json
  prompts.json
  prompts.sample.json
  Views/...
  其它依赖文件
```

## 当前源码目录

当前插件源码位于：

```text
D:\Flow.Launcher\Plugins\Flow.Launcher.Plugin.LocalPromptSearch
```

## 预期构建输出目录

根据项目文件配置，`Debug` 构建产物应输出到：

```text
D:\Flow.Launcher\Output\Debug\Plugins\Flow.Launcher.Plugin.LocalPromptSearch
```

## 正确的本机验证路径

### 1. 安装 .NET SDK

需要先让以下命令可用：

```powershell
dotnet --info
```

或至少让 Visual Studio 的 MSBuild 能解析 `Microsoft.NET.Sdk`。

### 2. 编译插件

推荐命令：

```powershell
dotnet build D:\Flow.Launcher\Plugins\Flow.Launcher.Plugin.LocalPromptSearch\Flow.Launcher.Plugin.LocalPromptSearch.csproj -c Debug
```

### 3. 确认输出目录

确认下面目录存在：

```text
D:\Flow.Launcher\Output\Debug\Plugins\Flow.Launcher.Plugin.LocalPromptSearch
```

### 4. 部署到本机安装版 Flow Launcher

把整个输出目录复制到：

```text
C:\Users\Administrator\AppData\Local\FlowLauncher\app-2.1.3\Plugins\Flow.Launcher.Plugin.LocalPromptSearch
```

### 5. 重启 Flow Launcher

重启后测试：

- 输入 `pt`
- 输入 `pt 周报`
- 回车复制 Prompt
- `Shift + Enter` 打开上下文菜单
- 输入 `pt reload`

## 目前推荐的手动验证清单

编译成功后，逐项确认：

1. `pt` 能显示模板列表
2. `pt 周报` 能搜出“周报总结模板”
3. 回车后剪贴板中出现对应 Prompt 内容
4. 最近使用列表会在空查询时优先显示
5. `pt reload` 可重新加载模板
6. 设置页可修改模板路径
7. 上下文菜单可复制标题、切换收藏、打开模板目录

## 当前阻塞点

当前唯一真正阻塞验证的点是：

- 本机没有可用的 `.NET SDK`

只要补齐这一步，后续的编译、复制到插件目录、重启验证这条链路就可以继续执行。
