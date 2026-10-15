# Sequence 框架

Sequence 是与具体脚本运行时和表现层解耦的逐帧调度框架，统一管理一次播放中的 Module、Preparation、Action、Yield、Routine、异常和清理。

## 职责与边界

框架负责播放状态机、帧调度、脚本恢复和所有扩展对象的生命周期。具体项目负责提供 `TContext`、脚本运行时适配器、Module 组合以及 Action/Yield 的业务含义；框架不定义 Gameplay Context、UI、输入或资源地址。

## 主要类型与入口

- `SequencePlayer<TContext>`：一次播放的状态与生命周期所有者
- `SequenceModule<TContext>`：播放期间持续存在的扩展模块
- `SequencePreparation`：Module 启动前的异步准备节点
- `SequenceAction<TContext>`：可并行执行的逐帧行为
- `SequenceYield<TContext>`：挂起 Routine 的等待条件，可返回整数结果
- `ISequenceRoutine`：脚本运行时与 Player 的适配边界
- `ISequenceAdvanceable`：Action 或 Yield 可选实现的主动推进能力

Player 在构造时通过 builder 注册 Module，通过 `Start()` 进入播放，通过 Tick 驱动，并通过停止或清理入口结束。

## 生命周期与核心语义

Player 状态为 `Created → Preparing → Running → Completed`：

1. `Start()` 进入 Preparing，按注册顺序调用 Module 的 `OnPrepare`，并行启动返回的 Preparation。
2. 所有 Preparation 完成的同一帧，Player 清理准备节点、调用全部 Module 的 `OnStart` 并进入 Running。
3. Running 每帧先 Tick Module，再启动和更新 Action、更新 Yield，最后按条件恢复 Routine。
4. Routine 结束且没有活动 Action 时自然完成；异常、主动停止和自然完成进入同一清理路径。
5. 清理取消未完成的 Preparation、Action 和 Yield，并按注册反序清理 Module；每个生命周期对象最多清理一次。

普通 `RequestYield` 只允许在 Running 调用，启动依赖使用 Preparation。全部准备节点无需等待时，首次 Module Tick、Action/Yield 调度和 Routine Resume 会在进入 Running 的同一帧发生。

Module 通过 Player 获取其他 Module：必需依赖使用 `GetModule<T>`，可选依赖使用 `TryGetModule<T>`。Module 的 Player 引用只在受控生命周期内有效，Cleanup 后不再保存或访问。

## 使用与扩展规则

- 新 Action、Yield、Module 和 Preparation 继承现有抽象基类，不创建平行生命周期接口。
- `TContext` 由接入方定义，框架不要求实现特定接口。
- Action/Yield 只通过 `ISequencePlayer<TContext>` 获取 Context、Module 和调度能力。
- 启动依赖使用 Preparation，不用普通 Yield 模拟准备阶段。
- 生命周期入口由 Player 驱动，扩展代码只重写受保护 Hook，不主动调用其他对象的生命周期方法。
- 高频通知携带稳定 ID 或紧凑值，避免每帧全量状态、闭包和临时集合。

## 验证

- 覆盖无需等待和跨帧 Preparation、并发 Action、Yield 返回值、Routine 自然结束与主动停止。
- 在 Module、Preparation、Action、Yield 和 Routine 各阶段注入异常，确认统一清理且每个对象只清理一次。
- 验证 Module 正序启动、反序清理，以及 Cleanup 后 Player 引用不可继续使用。
