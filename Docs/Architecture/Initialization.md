# 初始化流程

初始化流程负责把同步的架构创建与异步的运行时资源准备分离，并统一启动依赖、进度、取消、失败和呈现交接。其所有权决策记录在 [ADR-0001](../ADR/0001-launch-owned-initialization.md)。

## 职责与边界

架构创建阶段只注册模块、创建纯内存状态并绑定依赖，不加载运行时资源。正式初始化由 Launch 统一编排；启动入口只负责显示无资源依赖的启动呈现、请求进入 Launch，并在成功或失败后完成交接。

正式启动从专用 StartupScene 进入。普通场景不补齐全局初始化，也不提供同步 Ensure；需要独立预览的内容使用专用预览入口。

编辑器工具栏的“启动”沿用 Launch 到 MainMenu 的正式路径。“预览”先从当前活动场景的 Addressables 地址匹配场景配表；匹配到多个场景 ID 时由用户在弹出菜单中选择，取消选择则不启动。选定的场景 ID 作为一次性启动参数传给 Launch；Launch 完成全局初始化后直接进入 Game，Game 首次加载该场景并生成玩家。无存档、无预览参数时，Game 的首次场景 ID 取自全局表 `InitialSceneId`；读档仍使用存档中的场景 ID。进入 MainMenu 或 Game 成功后卸载 StartupScene。

## 主要类型与入口

- 启动入口：`Assets/Startup/GameplayStartup.cs`
- Launch 流程：`LaunchProcedure`
- 启动呈现边界：`IStartupSplash`
- 需要运行时资源的模块：公开 `InitializeAsync(CancellationToken, IProgress<float>)`

## 生命周期与核心语义

- `OnInitialize` 只绑定架构依赖、创建取消源和纯内存对象。
- Launch 按依赖顺序调用模块的异步初始化；互不依赖的全局任务可以并行。
- 成功后的顺序重复初始化可以立即返回，但模块不支持并发初始化。
- 未就绪时，功能接口返回明确状态或抛出 `InvalidOperationException`，不隐式同步加载。
- Launch token 直接取消模块的实际初始化；架构关闭先取消 Launch Context，模块 Shutdown 再释放 operation、实例和订阅。
- 模块初始化失败时回滚本次创建的资源并原样传播异常，以允许后续顺序重试。

启动呈现至少保持到最短展示时间结束。若此时初始化尚未完成，运行时加载呈现接管；若任务已经完成，则直接完成成功交接。初始化失败会立即取消并收束其余任务，保留当前呈现并使流程进入 `Failed`。

## 使用与扩展规则

- 只依赖资源服务的任务放入资源服务完成后的并行组。
- 依赖其他模块或运行时 Root 的任务在对应依赖就绪后启动。
- 仅由具体 Procedure 或场景使用的资源归属对应生命周期，不加入全局启动。
- 模块持有 operation 和清理逻辑；Launch 只表达依赖、启动任务和进度。
- 初始化链路保持异步，不使用 `WaitForCompletion`。

## 验证

- 快速路径不加载运行时加载呈现，启动呈现保持到成功交接。
- 慢速路径等待加载呈现可见后再隐藏启动呈现，不出现空白帧或瞬时切换。
- 初始化退出时没有旧 continuation、`MissingReferenceException` 或未释放 operation。
- 对每个初始化任务注入失败，确认其余任务被取消、模块 operation 收束且流程进入 `Failed`。
- 执行 Unity Editor 编译、Player 编译和资源构建，并扫描正式初始化链路中不存在 `WaitForCompletion`。
