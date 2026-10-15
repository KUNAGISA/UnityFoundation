# 框架架构

项目使用 Aoiro 组织运行时与编辑器模块，通过明确的分层、能力接口和生命周期控制依赖方向。本文记录模块接入及协作时必须保持的框架契约。

## 职责与边界

- `Service` 封装底层能力或外部服务，通过接口注册和访问。
- `Model` 持有共享状态与运行时数据。
- `System` 承担业务能力和长期生命周期，不默认拥有访问其他 System 的权限。
- `Command` 编排无状态写操作，尤其适合需要多个 System 协作的流程。
- `Query<TResult>` 承担读操作或无副作用推导。

架构初始化只注册模块、创建纯内存状态并绑定依赖。运行时资源由 Launch 流程初始化，契约见 [初始化流程](Initialization.md) 和 [ADR-0001](../ADR/0001-launch-owned-initialization.md)。

## 主要类型与入口

- 运行时入口：`Assets/Gameplay/Scripts/Architecture/Core/GameplayArchitecture.cs`
- Gameplay 能力入口：`Assets/Gameplay/Scripts/Architecture/Core/IGameplayController.cs`
- 编辑器入口：`Assets/Editor/Scripts/Architecture/Core/EditorArchitecture.cs`
- Aoiro 基础实现：`Packages/com.kunagisatomo.aoiro/Runtime`

架构通过 `Initialize()` 创建，通过 `Shutdown()` 关闭。`Instance` 在实例不存在时按需初始化；关闭阶段读取现有实例使用 `TryGetInstance`，避免重新触发初始化。

## 生命周期与核心语义

模块在 `OnInitialize` 获取依赖，在 `OnShutdown` 释放自身资源。初始化顺序为 Service → Model → System，同层按注册顺序；关闭顺序为 System → Model → Service，同层按注册反序。需要在常规关闭前释放跨模块状态时实现 `IPreShutdownModule`。

动态模块通过 `RegisterModule`、`GetModule`、`ContainsModule` 和 `UnregisterModule` 管理。编辑器诊断工具使用 `EnumerateModules<TModule>()` 获取只读枚举，不访问内部注册表。

### 帧驱动与时间

`GameplayArchitecture` 在每帧 EarlyTick 开始时生成一次不可变 `FrameTime`，并将同一快照传给 Procedure、Scheduler 和 Camera 的 EarlyTick、FrameTick、LateTick。`IFrameTickable` 只接收 `in FrameTime`，Tick 中不重新采样 Unity 时间。

全局 `FrameTime` 使用 Unity 缩放时间；需要暂停或独立缩放的领域从全局快照派生并持有自己的时间域。`ProcedureSystem.Time` 是流程时间域，流程暂停、切换或未运行时停止推进。决策背景见 [ADR-0002](../ADR/0002-single-frame-time-snapshot.md)。

### 能力接口

Aoiro 使用小接口限制访问范围：

- `IServiceAccessor`
- `IModelAccessor`
- `ISystemAccessor`
- `ICommandExecutor`
- `IQueryExecutor`
- `IEventPublisher`
- `IEventSubscriber`

`SystemBase` 默认不实现 `ISystemAccessor`。确需访问其他 System 的具体 System 必须显式声明该能力。模块外的 View、MonoBehaviour、Editor 工具和适配器应组合实际需要的能力，并通过 `IArchitectureProvider.GetArchitecture()` 返回对应架构。

### Command、Query 与事件

- 写操作调用 `ExecuteCommand`，读操作调用 `ExecuteQuery`。
- 架构事件通过 `SubscribeEvent` 和 `PublishEvent` 订阅、发布。
- 订阅返回 `SubscriptionToken`，持有者在自身生命周期结束时取消。
- 同类型事件按 QueuedSignal FIFO 派发；不同类型事件允许同步嵌套。
- Signal 逐个捕获订阅者的同步异常，通过 `Debug.LogException` 记录后继续派发，不自动取消失败订阅者，也不回滚其副作用；发布者不能依靠 `Emit` 或 `PublishEvent` 抛异常判断订阅者是否成功。异步回调恢复后的异常不在此隔离范围内。
- Signal 禁止同实例重入；由回调触发的重入异常会被外层 callback 捕获并记录，外层派发继续。QueuedSignal 的排队事件不因订阅者异常而丢弃；单轮超过 1024 次派发仍清空待派发队列并向外抛出异常。

### BindableProperty

需要监听单值变化时使用 `BindableProperty<T>`；对外只读暴露使用 `IReadOnlyBindableProperty<T>`。通过 `Subscribe(callback, invokeImmediately)` 监听，通过 `SetValueWithoutNotify` 静默写入，自定义比较器仅作用于当前实例。

属性更新通知遵循 Signal 的异常隔离契约，通知失败不会回滚已写入的属性值。`invokeImmediately: true` 保持先通知再注册订阅的顺序；立即通知失败时记录异常，随后仍注册订阅并返回可取消的令牌。

同一 BindableProperty 在回调中再次写入不同值会触发 Signal 非重入异常，该异常由外层 callback 捕获并记录，外层派发继续，但再次写入的值不会回滚。规范化与校验应在赋值入口完成。

## 使用与扩展规则

- 新模块沿用现有层级，不创建平行全局单例或重复框架能力。
- 多 System 写流程优先由 Command 编排；只有无法合理拆分时才让 System 显式访问其他 System。
- 注册模块前确认初始化依赖，释放时由模块撤销其订阅、Token 和资源所有权。
- 新增帧驱动模块时接收架构分发的 `FrameTime`，独立时间语义通过明确的时间域表达。

## 验证

- 检查模块注册类型、初始化顺序和关闭反序是否正确。
- 检查新增访问能力是否为最小集合，System 间协作是否经过既有 Command 或显式接口。
- 检查事件订阅、动态模块和帧任务在关闭后均已失效。
- C# 修改后执行 Unity Editor 编译；帧驱动改动还需验证同一帧各阶段收到相同时间快照。
