# AI 代码修改规则

本文件只保留所有代码任务都必须遵守的规则；模块细节按需从项目文档读取。

## 文档路由

- 修改架构、初始化、Procedure、资源、存储、配表、输入、Entity、UI、镜头、渲染或 Sequence 前，从 [架构文档索引](Docs/Architecture/Overview.md) 进入对应文档；新增或重写框架文档遵循 [架构文档模板](Docs/Architecture/TEMPLATE.md)。
- 新增、修订或质疑既有架构取舍前，检查 [Docs/ADR](Docs/ADR/TEMPLATE.md)；只有难以回退、缺少背景会令人疑惑且存在真实方案取舍的决定才按模板新增 ADR。
- 涉及 Character State、Entity、Save Data、Spatial State 或 Gameplay Tag State 时，使用 [CONTEXT.md](CONTEXT.md) 中的规范术语。
- 架构文档只记录当前有效的接口、边界、生命周期、扩展方式、约束和排错；历史取舍写入 ADR，具体玩法流程、业务状态和界面说明不与框架文档混写。

## 工作流

- 修改前审计相关调用链、模块注册、生命周期和邻近实现，优先复用现有对象池、调度器、取消令牌、日志与框架辅助能力。
- 采用完成任务所需的最小改动，不做无关重构、格式化、依赖升级或历史问题清理；仅在减少真实重复、保护清晰边界或匹配现有模式时新增抽象。
- 已有未提交改动默认属于用户；保留无关改动，无法安全绕开重叠时先说明冲突。
- 信任项目已明确的契约和不变量，避免过度防御；正常流程下不会出现的情况，不添加防御性判断或兜底。仅在真实输入边界、可预期失败点或现有约定要求处增加校验与兜底。
- 最终回复列出修改文件、验证方式、未验证项和风险。

## 文件边界

- 配表修改 `Design` 下的 Luban 源数据并重新生成；不手工编辑 `Assets/Gameplay/Tables/Generated`。
- 项目侧优先提供第三方能力的适配或扩展；除非用户明确要求或不存在项目侧方案，不修改 `Assets/Plugins`、`Packages/luban_unity`、`Packages/com.singularitygroup.hotreload`、`Packages/com.github.messagepack-csharp` 等第三方源码。
- 不编辑 Unity 临时或用户目录：`Library`、`Temp`、`Logs`、`obj`、`UserSettings`。
- Unity Editor、批处理和测试日志输出到项目根目录 `Logs/`。
- AI 临时工具、脚本和一次性工程放在项目根目录 `.ai-workspace/`；该目录只存放可丢弃产物，不承载提交代码或资源，也不写入 Unity、SDK、Codex、插件安装目录或用户全局目录。
- 修改 Prefab、Scene、Addressables 或其他序列化资源时保持 `.meta`、GUID 和引用完整，并只保留任务相关的序列化 diff。

## 代码边界

- 优先使用 Aoiro 的 `System`、`Model`、`Service`、`Command`、`Query`、`Controller` 分层，并遵循对应架构文档。
- 多 System 写流程优先由 Command 编排；确实无法合理拆分时，具体 System 显式声明访问其他 System 的能力。
- 复用现有架构入口与生命周期，不创建平行全局单例或重复框架能力。
- 通用框架代码放入现有 Framework 程序集和目录；具体玩法运行时代码放入 Gameplay，不因调用方位置改变能力归属。
- 编辑器代码放入 `Gameplay.Editor` 程序集和命名空间，或使用 `#if UNITY_EDITOR` 隔离。
- Gameplay 类型和成员优先使用 `public` 或 `private`；只有明确程序集边界需要时使用 `internal`。Editor 访问 Gameplay 内部 API 时通过 `InternalsVisibleTo`，不为测试或编辑器便利扩大实现可见性。
- 小型无状态 Command、Query、事件和值对象可以使用 `struct`，前提是生命周期仍然清晰。

## 异步

- Unity 异步代码使用 `UniTask`；只有接入必要外部 API 时引入 `Task`。
- 使用可返回的异步签名；仅 Unity 或事件入口无法更改签名时使用 `async void`。
- 长生命周期异步操作接受或持有 `CancellationToken`，所有者销毁时取消并收束自身工作。
- 只有取消和异常处理明确，或与现有局部模式一致时调用 `.Forget()`。
- CPU 密集的序列化或转换可以使用 `UniTask.RunOnThreadPool`；触碰 Unity 对象前回到主线程。

## 性能

- 新增代码默认不使用 LINQ；已有代码只在任务需要、性能修复或局部风格强相关时调整 LINQ。
- 热路径不引入 LINQ、闭包、临时集合、字符串拼接、装箱或逐帧堆分配。

## 命名与格式

- 新增架构模块使用与层级一致的 `System`、`Model`、`Service`、`Command`、`Query` 或 `View` 后缀；Controller 按职责和现有风格命名。
- 业务类型不使用 `Runtime` 前后缀；非必要不使用 `Manager`；类型不以 `Handle` 结尾；`Module` 只用于现有模式或真实扩展点。
- 类型和成员使用 PascalCase，局部变量和参数使用 camelCase，私有字段使用 `_camelCase`，`static readonly` 字段使用 PascalCase；C# 文件名与主类型一致。
- 方法、类型和控制语句使用独占行的左右花括号；只有简短函数或匿名函数使用表达式体。
- 方法定义与调用参数、条件、三元表达式和 LINQ 默认保持单行，只在过长或明显影响可读性时换行。
- 注释解释意图、约束或不明显行为，不复述代码。
- 编辑器工具优先使用已有 Odin Inspector/Editor API，前提是程序集已经引用且适合任务。

## 验证

- C# 修改后至少执行一次编译级检查；优先使用 Unity Editor，无法使用时执行解决方案或 MSBuild 检查并说明限制。
- 修改后检查编译错误、命名空间、asmdef 引用和 Unity 序列化 diff。
- 修改存档、配表、资源加载、UI 生命周期、输入或流程切换时，最终回复单独说明对应验证情况。
- 默认不新增测试；只有用户或任务明确要求时添加。
- 只有确实执行过对应检查时，才声称通过 Unity Editor、PlayMode、Prefab 或 Scene 验证。

## 项目事实

- Unity 版本：`2022.3.62f3`。
- 主运行时程序集：`Assets/Gameplay/Scripts/Gameplay.asmdef`，根命名空间 `Gameplay`。
- 主编辑器程序集：`Assets/Editor/Scripts/Gameplay.Editor.asmdef`，根命名空间 `Gameplay.Editor`。
- 配表源数据位于 `Design`，生成代码位于 `Assets/Gameplay/Tables/Generated`。
- 项目自有可复用框架代码位于 `Assets/Framework`。
