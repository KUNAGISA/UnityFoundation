# UnityFoundation

基于 Unity 与 [Aoiro](https://github.com/KUNAGISA/Aoiro) 的游戏项目基础工程，提供从启动、资源准备、流程切换到场景玩法与 UI 的一套实现。项目包含场景、界面、配置数据和编辑器工具，可作为阅读架构、验证功能及继续开发的起点。

**首次使用需要自行安装 Odin Inspector、DOTween Pro 和 Quantum Console。** 仓库保留对这些插件的代码与资源引用，但不分发插件本体；仅克隆仓库尚不足以完成编译。Hot Reload 是可选开发工具，不是运行游戏的前置条件。

## 目录

- [环境要求](#环境要求)
- [功能与架构](#功能与架构)
- [项目结构](#项目结构)
- [依赖与插件](#依赖与插件)
- [首次运行](#首次运行)
- [日常开发与生成工具](#日常开发与生成工具)
- [本地文件与版本控制](#本地文件与版本控制)
- [常见问题](#常见问题)
- [文档与验证范围](#文档与验证范围)
- [许可证](#许可证)

## 环境要求

| 项目 | 仓库记录 / 要求 |
| --- | --- |
| 主开发环境 | Windows 11 |
| Unity Editor | `2022.3.62f3`，以 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 为准 |
| .NET | .NET 8 SDK；用于 Gameplay Tag 生成器和 Luban 工具链 |
| Git | 需要命令行 Git，并可访问 manifest 中引用的 Git 仓库 |
| 网络 | 首次导入需要访问 Unity Package Registry、GitHub、OpenUPM；构建生成工具时需要 NuGet |
| 文件编码 | UTF-8 |

[manifest.json](Packages/manifest.json) 与 [packages-lock.json](Packages/packages-lock.json) 记录当前包配置及解析结果，其中包括 URP `17.1.0`、Cinemachine `3.1.5`、Addressables `2.6.0` 等。这里列出的是仓库实际配置，不代表这些版本与上述 Editor 的全新安装组合均已验证。遇到包版本约束时，应先核对 Package Manager 报错与对应包的 Unity 版本要求；不要以清空 Library 或随意升级所有依赖代替诊断。

Windows 是本文命令示例的主要环境。Design 同时提供 Bash 脚本，但其他操作系统与 Player 平台的完整流程尚未在本次整理中验证。

## 功能与架构

### 分层与生命周期

项目使用 Aoiro 组织运行时和编辑器能力：

| 层级 | 职责 |
| --- | --- |
| `Service` | 封装资源、存储、输入等底层能力，通过接口提供访问 |
| `Model` | 持有共享状态和运行时数据 |
| `System` | 承担业务能力及其长期生命周期 |
| `Command` | 编排写操作，尤其是多个 System 协作的流程 |
| `Query<TResult>` | 读取数据或执行无副作用推导 |
| Controller / View | 将 Unity 对象、用户操作和架构能力连接起来 |

运行时入口为 `GameplayArchitecture`，编辑器入口为 `EditorArchitecture`。架构初始化阶段注册模块、建立纯内存状态与依赖；正式资源加载由 Launch 编排。模块持有自己的资源、订阅和取消令牌，并在关闭时收束工作。业务访问使用所需的能力接口，多 System 写流程优先通过 Command 协调。

正式启动路径：

```text
StartupScene
    → GameplayStartup 请求进入 Launch
    → Launch 初始化资源服务及运行时模块
    → MainMenu（普通启动）或 Game（场景预览）
    → 卸载 StartupScene
```

### 已有能力

| 能力 | 内容与文档 |
| --- | --- |
| 初始化与 Procedure | 异步初始化、取消、失败收束、流程及场景切换；[初始化](Docs/Architecture/Initialization.md)、[Procedure](Docs/Architecture/Procedure.md) |
| 资源 | 通过 `IAssetService` 接入 Addressables，以 operation 和 Loader 明确资源所有权；[资源服务](Docs/Architecture/AssetService.md) |
| 存储与存档 | 存储服务、Save Data 读取与运行时恢复边界；[存储](Docs/Architecture/StorageService.md) |
| 数据与本地化 | Luban 配表、二进制数据生成、表模型与本地化数据；[配表](Docs/Architecture/Tables.md) |
| 条件与行为 | 条件查询、变化观察，以及同步行为分发；[Condition](Docs/Architecture/Conditions.md)、[Action](Docs/Architecture/Actions.md) |
| 输入 | 独立输入程序集、输入模式和语义命令路由；[输入](Docs/Architecture/InputService.md) |
| Entity 与运动 | Entity 生命周期、Ability、角色运动与动画仲裁；[Entity](Docs/Architecture/Entity.md)、[运动](Docs/Architecture/Movement.md) |
| Gameplay Tag | 稳定 ID、来源计数、层级标签和同步批次通知；[Gameplay Tag](Docs/Architecture/GameplayTags.md) |
| UI | View 生命周期、Layer、异步加载、导航焦点、输入阻断、绑定代码生成；[UI](Docs/Architecture/UI.md) |
| 镜头与渲染 | Player / Art Camera 模块、相机优先级与混合，以及 UI 模糊效果；[镜头](Docs/Architecture/Camera.md)、[渲染](Docs/Architecture/Rendering.md) |
| Sequence | 将序列执行接入游戏流程与业务能力；[Sequence](Docs/Architecture/Sequence.md) |
| 编辑器工具 | 启动 / 预览工具栏、架构诊断、场景出生点 / 移动路径 / 事件编辑、配置选择与生成工具 |

角色与场景对象的命名遵循 [CONTEXT.md](CONTEXT.md)：Character State 表示会话内持续存在的角色业务状态，Entity 表示角色进入场景后产生的实例，Save Data 表示持久化传输形式，Spatial State 表示位置与旋转。

## 项目结构

```text
Assets/
  Framework/            项目自有可复用框架与程序集
  Gameplay/
    Scripts/            Gameplay 架构、业务系统与 View
    Input/              输入适配与独立程序集
    GM/                 调试命令，受 GAMEPLAY_GM 编译符号约束
    Tables/             运行时表模型、生成代码和二进制数据
    Scenes/             项目场景
    Prefabs/            玩法与 UI 预制体
    Settings/           项目配置资源
    Sequences/          Sequence 资源
  Editor/Scripts/       Gameplay.Editor 编辑器工具
  Startup/              启动场景和启动入口
  AddressableAssetsData/ Addressables 分组、地址和 LayoutRule
  Plugins/              随仓库保留的第三方组件及本地插件安装位置
Design/                 Luban 源数据、定义、模板、本地化和 Gameplay Tag 源文件
Tools/                  Luban、校验工具与 Gameplay Tag 生成器
Packages/               UPM manifest、锁文件及已纳入仓库的 NuGet 包
ProjectSettings/        Unity 项目设置
Docs/Architecture/      当前架构接口、边界、生命周期与排错说明
Docs/ADR/               架构决策及其背景
```

通用框架能力放在 `Assets/Framework`，具体玩法放在 Gameplay。主运行时程序集为 `Gameplay`，主编辑器程序集为 `Gameplay.Editor`；各框架模块有各自的程序集边界。编辑器能力不能直接进入 Player 程序集。

## 依赖与插件

### UPM 自动恢复

在 Unity 打开项目后，由 Package Manager 按 manifest 和锁文件恢复；不需要把包缓存复制进 Assets。

| 依赖 | 当前配置 / 来源 | 用途 |
| --- | --- | --- |
| [Aoiro](https://github.com/KUNAGISA/Aoiro) | Git，固定提交 `7b05851773b024ebdd786fbf6398b507a3219d54` | 架构与模块基础 |
| [Sequence](https://github.com/KUNAGISA/Sequence) | Git | 序列执行框架 |
| [LiteLua](https://github.com/KUNAGISA/LiteLua) | Git | Lua 接入 |
| [Universal Fancy ScrollView](https://github.com/KUNAGISA/UniversalFancyScrollView) | Git，依赖 FancyScrollView `1.9.0` | 列表与导航适配 |
| [Luban Unity](https://github.com/focus-creative-games/luban_unity) | Git | Luban 运行时接入 |
| [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) | Git，UnityClient 子目录 | 序列化 |
| [Smart Addresser](https://github.com/CyberAgentGameEntertainment/SmartAddresser) | Git，Assets 子目录 | Addressables 地址配置工具 |
| Unity Addressables | `2.6.0` | 资源和场景寻址 |
| Unity Cinemachine | `3.1.5` | 镜头支持 |
| Unity Input System | `1.14.2` | 输入后端 |
| Universal RP | `17.1.0` | 渲染管线 |
| TextMesh Pro / uGUI | `3.0.9` / `2.0.0` | UI |
| Timeline / 2D Feature | `1.8.9` / `2.0.1` | Unity 时间线与 2D 工具 |

未固定提交的 Git 依赖，其当前解析提交记录在锁文件中。完整依赖清单以 manifest 为准；FancyScrollView 使用 manifest 中配置的 OpenUPM registry。

### 随仓库保留

| 组件 | 位置 / 版本依据 |
| --- | --- |
| [UniTask](https://github.com/Cysharp/UniTask) | `Assets/Plugins/UniTask`，本地 package.json 标注 `2.5.10` |
| [DOTween 基础部分](https://dotween.demigiant.com/) | `Assets/Plugins/Demigiant/DOTween`，版本以组件自身信息为准 |
| NuGet 接入与已安装包 | `Assets/Plugins/NuGet`、`Packages/nuget-packages`，依赖记录见其中的 packages.config |
| [Luban 工具链](https://github.com/focus-creative-games/luban) | `Tools/Luban`，供 Design 脚本调用 |

### 需要自行安装

从插件官方渠道获取适用版本并导入以下位置，保留插件自带的 `.meta` 和程序集定义。项目内的代码及序列化资源依赖这些组件；更换插件版本后应检查编译结果与 Prefab 引用。

| 插件 | 安装位置 | 版本记录与要求 |
| --- | --- | --- |
| [Odin Inspector](https://odininspector.com/) | `Assets/Plugins/Sirenix` | 本地可读清单未明确标注产品版本；需要 Inspector / Editor API 及相关程序集，仅安装独立 Odin Serializer 不足以满足当前项目 |
| [DOTween Pro](https://dotween.demigiant.com/pro.php) | `Assets/Plugins/Demigiant/DOTweenPro`，以及发行包随附的 DemiLib | 本地可读清单未明确标注产品版本；Gameplay 显式引用 `DOTweenPro.dll`，需与 DOTween 基础部分匹配 |
| [Quantum Console](https://qfsw.co.uk/docs/QC/) | `Assets/Plugins/QFSW/Quantum Console` | 本地 changelog 首项为 `2.6.7`；ConsoleSystem 直接依赖，关闭 GM 编译符号并不能移除该依赖 |

这些插件及其随附示例、相关目录 `.meta` 已配置为 Git 忽略。仓库中的项目代码、生成模板、Console 配置和 Prefab 引用仍保留。

### 可选开发工具

[Hot Reload](https://hotreload.net/) 可安装在 `Packages/com.singularitygroup.hotreload`，本地 package.json 记录版本为 `1.13.20`。包目录及 `Assets/Settings/HotReload` 已忽略；不安装也无需给 manifest 添加占位依赖。

## 首次运行

### 1. 克隆与环境准备

```powershell
git clone https://github.com/KUNAGISA/UnityFoundation.git
Set-Location UnityFoundation
dotnet --info
```

仓库访问需要对应权限。通过 Unity Hub 安装项目记录的 Editor，将仓库根目录作为现有项目添加，确认打开的是包含 Assets、Packages 和 ProjectSettings 的目录。

### 2. 导入必要插件并完成编译

首次打开时等待 UPM 恢复依赖，然后从自己的插件发行包导入 Odin Inspector、DOTween Pro、Quantum Console。安装完成前出现 Sirenix、QFSW 或 DOTweenPro 缺失错误是预期的依赖缺失现象。

打开 `Tools > Demigiant > DOTween Utility Panel`，按插件提供的 `Setup DOTween...` 和模块配置流程完成设置，保留项目使用的 UI / TextMesh Pro 集成。检查导入后的程序集名与项目 asmdef 引用一致，避免在多个目录重复安装同一插件。

等待 Unity 编译结束，先处理 Console 中的编译与包恢复错误，再运行场景。导入过程若要求重启 Editor，应按插件自身提示操作。

### 3. 检查 Addressables

打开 `Window > Asset Management > Addressables > Groups`，使用仓库已有的 AddressableAssetSettings 和分组，不重新创建一套设置。

- 首次在 Editor 中运行可选择 `Play Mode Script > Use Asset Database (fastest)`，从项目资源读取，避免依赖尚未生成的 Bundle。
- 检查现有分组与资源引用，尤其是 `Global/Console`、`Configs/...`、`Cameras/...`、界面与场景地址；安装 Quantum Console 后还需确认项目 Console Prefab 的脚本引用有效。
- 使用打包内容或构建 Player 前，在当前目标平台执行 `Build > New Build > Default Build Script`，确认构建成功；`Use Existing Build` 需要先有对应的内容构建结果。
- 资源整理沿用 `Assets/AddressableAssetsData/LayoutRule` 中已有的 Smart Addresser 规则，避免任意改动业务代码使用的地址。

### 4. 从正式入口启动

打开 `Assets/Startup/StartupScene.unity` 后进入 Play Mode，正常路径由 Launch 完成初始化并进入 MainMenu。Build Settings 已将该场景配置为启用的启动场景。

也可以使用编辑器工具栏的 `▶ 启动` 从正式入口启动。`▶ 预览` 会将已保存的当前活动场景匹配到场景表，完成正式初始化后直接进入 Game 并加载该场景；匹配多个场景 ID 时需选择目标。

普通玩法场景不会自动补齐全局初始化。直接打开普通场景并按 Play 不能替代 Startup / Launch 流程。工具栏不可用时，先确认编译已完成，再直接使用 StartupScene。

## 日常开发与生成工具

以下命令从仓库根目录执行。生成会修改源文件或生成输出，执行后应审核 Git diff。

### Luban 配表与本地化

在 `Design/Datas`、`Design/Defines`、`Design/Localizations` 及相应模板中修改数据和结构，然后执行：

```powershell
.\Design\gen.bat
```

脚本先生成 Gameplay Tag，再校验配置、生成 C# 与二进制配置数据，最后生成 `zh` / `en` 本地化数据；Windows 脚本结束时会暂停。Bash 入口为 `bash Design/gen.sh`。

主要输出包括 `Assets/Gameplay/Tables/Generated`、`Assets/Gameplay/Tables/Data/Config` 和 `Assets/Gameplay/Tables/Data/Localization`。不要手工修补 Generated；运行时代码通过 TablesModel 及既有封装读取数据。变更资源地址后同步检查 Addressables。

### Gameplay Tag

修改 `Design/GameplayTags.xml` 后可单独生成：

```powershell
.\Design\gen-tags.bat
```

Bash 入口为 `bash Design/gen-tags.sh`。生成器项目位于 `Tools/GameplayTags.Generator`，使用 .NET 8 与 Scriban；生成 C# Catalog 和供 Luban 使用的常量定义。已有标签重命名或移动时保留 ID，删除后不复用 ID，不手动降低 `nextId`。

### Condition / Action 分发器

新增定义并配置 `DefinitionResolverAttribute` 后，在 Unity 中执行 `Tools > Gameplay > Generate Definition Resolvers`。条件和行为总表在 Design 中维护，沿用 Luban 引用校验；不要手工维护生成的分发器。条件观察与行为执行的契约见对应架构文档。

### UI 绑定

1. 在 `Edit > Project Settings > UI Binding` 配置 Binding、View、Widget 模板及规则，可使用 `Validate` 检查。
2. 使用 `Assets > Create > Scripting > View Script` 或 `Widget Script` 创建脚本。
3. 在界面 Hierarchy 配置 `UIBindTag` / `UIBindCollectionTag`。
4. 在 View / Widget 组件上下文菜单执行 `生成绑定代码`，审核生成区与 Prefab 引用。

业务回调沿用生成器的 partial 方法约定。Binding Key 会生成稳定 ID，已有 Prefab 使用后不要随意修改 Key。集合绑定的直接子节点顺序与字段一致性要求见 [UI 文档](Docs/Architecture/UI.md)。

### 诊断入口

- `Window > Architecture > Gameplay`：查看架构诊断。
- `Tools > Gameplay > Reload Localization Table`：编辑器本地化表重载入口。
- `GameObject > 场景`：创建出生点、移动路径和场景事件点。
- GM 命令位于 `Assets/Gameplay/GM`；程序集受 `GAMEPLAY_GM` 约束，控制台本身仍属于启动依赖。

## 本地文件与版本控制

- Hot Reload、Odin、DOTween Pro、DemiLib、Quantum Console 及相应目录 `.meta` 保留在安装者本地，不纳入 Git。不要使用 `git add -f` 将它们重新加入。
- DOTween 基础部分、UniTask、项目适配、现有场景和配置继续跟踪；忽略规则没有覆盖整个 Plugins 或 Packages。
- 本地保留 Hot Reload 嵌入包时，Unity 可能在 `Packages/packages-lock.json` 重新写入该包记录。提交前审核锁文件，仅排除本地 Hot Reload 引起的变化，保留其他真实依赖更新；不要忽略整个锁文件或把所有锁文件变化一并丢弃。
- Library、Temp、obj、Logs、UserSettings、IDE 工程文件和本地 `SaveData` 已忽略。当前存储服务使用相对项目工作目录的 `./SaveData`。
- Unity 批处理与验证日志写入根目录 `Logs/`。可丢弃的 AI 辅助脚本和检查结果写入 `.ai-workspace/`；它们不作为提交代码或正式资源。
- Unity 资源提交时保留 `.meta`、GUID 与引用完整，检查 Prefab、Scene 和 Addressables 的序列化 diff。

## 常见问题

| 现象 | 优先检查 |
| --- | --- |
| 找不到 Sirenix 命名空间或 Odin DLL | 是否已安装完整 Odin Inspector；安装目录、程序集名及 asmdef 引用是否一致 |
| 找不到 QFSW / QuantumConsole | 是否已安装 Quantum Console；关闭 GAMEPLAY_GM 不能替代安装 |
| 找不到 DOTweenPro.dll 或 DOTween 模块 | 是否已安装 Pro，是否完成 DOTween Setup，是否重复导入或与基础版不匹配 |
| Prefab 出现 Missing Script | 插件是否导入完整、原始 `.meta` 是否保留，以及所用版本的脚本 GUID 是否与引用一致 |
| Git 依赖恢复失败 | Git 命令是否可用、仓库是否有访问权限、网络是否能访问 GitHub / OpenUPM |
| Package Manager 提示 Unity 版本不兼容 | 对照 ProjectVersion、manifest、锁文件与包要求；本次整理未升级 Editor 或调整包版本 |
| 找不到资源地址、Catalog 或 Bundle | 使用的 Addressables Play Mode、当前平台的内容构建、分组资源与地址是否匹配 |
| 普通场景直接运行后缺少全局状态 | 从 StartupScene 或 `▶ 预览` 启动，确认 Launch 完成 |
| 表生成失败 | .NET 8 SDK、Design 源数据、Luban ref 校验和 Gameplay Tag 定义；以脚本中的首个错误为起点 |
| 本地插件又出现在待提交列表 | 用 `git check-ignore -v <路径>` 检查规则；已跟踪文件需要先取消跟踪，仅增加 ignore 不会移除索引记录 |

## 文档与验证范围

从 [架构文档索引](Docs/Architecture/Overview.md) 进入模块说明。阅读或扩展架构时同时参考 [领域术语](CONTEXT.md)、[ADR](Docs/ADR) 和 [项目修改规则](AGENTS.md)。架构文档描述当前有效契约，ADR 保留设计取舍背景。

本次仓库整理的验证结果：

- 本地插件文件完整性、Git 忽略规则、发布快照的文件清单、JSON 格式及 README 本地链接均按整理流程检查。
- Unity Editor 批处理导入 / 编译结果见下方记录；本地环境包含自行安装的插件与已有包缓存，该结果不能证明裸克隆无需安装依赖即可运行。
- 本次不新增测试，不将编译等同于 PlayMode、场景、Prefab、Addressables 内容构建或 Player 验证。

2026-10-15：使用本地 Unity `2022.3.62f3` 完成批处理导入 / 编译检查，日志记录构建成功及退出码 `0`，未发现 C# 编译错误。日志中仍有 Cinemachine 的 `HDRP-Editor-ref.asmref` 无目标程序集提示，本次未修改该第三方包。日志保存在本地 `Logs/repository-release-compile.log`，不随仓库提交。未执行 PlayMode、Prefab / Scene 功能检查、Addressables 内容构建、Player 构建或全新无缓存环境验证。

## 许可证

项目自有代码采用 [MIT License](LICENSE)，Copyright (c) 2026 KUNAGISA。

第三方代码、插件和资源遵循各自许可证及版权声明；本项目的 MIT 授权不替代它们的授权条款，也不授予重新分发被排除插件的权限。Odin Inspector、DOTween Pro、Quantum Console 和可选的 Hot Reload 请从各自官方渠道获取并遵守对应条款。仓库随附第三方组件保留其原有声明；项目中的美术、字体和其他资源不因代码使用 MIT 自动获得相同授权。
