# Procedure 流程

Procedure 提供互斥流程切换、流程生命周期、帧时间、取消和按流程启停能力系统的统一框架。具体游戏流程只组合这些能力，不复制框架的切换事务。

## 职责与边界

`ProcedureSystem` 是流程切换、当前 Procedure 与 Context 的唯一所有者。Procedure 负责一个流程内的进入、就绪、运行和退出逻辑；只在部分流程启用的长期能力由 `ProcedureCapacitySystem` 承担。

流程切换统一处理呈现、临时 UI、资源清理和失败状态。具体 Procedure 不直接创建或销毁其他 Procedure，也不自行复制全局切换表现。

## 主要类型与入口

- `ProcedureSystem`
- `Procedure` / `Procedure<TOptions>`
- `ProcedureContext`
- `ProcedureCapacitySystem`
- `TransitionProcedureRequest`
- `ProcedureType`

流程切换入口为 `ProcedureSystem.Transition(in TransitionProcedureRequest request)`。新流程类型需要在 `ProcedureType` 和 `ProcedureSystem.CreateProcedure` 中注册。

`Procedure<TOptions>` 在进入具体 Hook 前拒绝 null 或不匹配类型的参数，具体 `OnEnterAsync(TOptions)` 可直接访问 options。参数成员自身是否可空由对应 Options 类型约定；`LaunchProcedureOptions` 构造时还会保证 Splash 非空。

## 生命周期与核心语义

Procedure 生命周期顺序为：

1. `OnInitialize`
2. `OnEnterAsync`
3. `OnReadyAsync`
4. `OnFrameTick` / `OnLateTick`
5. `OnTeardownAsync`
6. `OnExitAsync`
7. `OnDestroy`

切换会先退出并销毁当前 Procedure 与 Context，再创建目标流程，完成 Enter、下一帧 Ready、资源收束和呈现交接后进入 `Running`，并发布 `EnterProcedureSuccessEvent`。

### ProcedureContext

`ProcedureContext` 提供当前流程时间源、只读暂停状态、生命周期取消令牌、帧任务、Timer、外部 Token 托管和所需的 Aoiro 能力接口。创建时由所属 ProcedureSystem 通过构造函数注入自身，IsPaused 直接读取该所有者，不通过架构查询模块。Enter/Ready 中的长生命周期异步操作直接使用 `Context.LifetimeCancellationToken`。

暂停通过 `ProcedureSystem.Pause` 申请并返回 `ProcedurePauseToken`。同一暂停原因只能有一个持有者，每次申请只包含一个原因；Token 取消后释放暂停。暂停期间流程时间、Context 帧任务、Timer 和 Procedure 帧回调停止推进，并通过对应输入模式阻断输入。

暂停申请与释放在 Running 状态下的 EarlyTick 统一生效。`IProcedureContext.IsPaused` 提供当前已生效暂停状态的只读视图，直接读取 ProcedureSystem 的实际状态，不在 Context 中维护副本；暂停原因与 Token 仍由 ProcedureSystem 管理。整体状态从运行变为暂停或从暂停变为运行时，在对应 Procedure Hook 完成后发布 `ProcedurePauseChangedEvent(IsPaused)`，暂停原因之间的变更不重复通知。该事件不表示流程进入、切换、失败或退出，也不由清理暂停状态触发。

### Capacity System

Procedure 通过 `EnsureCapacity<TSystem>()` 附加能力系统：

- 附加时执行 `OnProcedureAttach`。
- Ready 阶段按注册顺序执行 `OnProcedureReadyAsync`。
- Teardown 阶段按注册反序执行 `OnProcedureTeardownAsync`。
- Procedure 销毁时按注册反序执行 `OnProcedureDetach`。

只在部分流程工作、需要流程帧任务或持有流程资源的系统应实现为 Capacity System，而不是常驻后再判断当前流程。

需要共用全局时间片预算的 Capacity 可继续向 SchedulerSystem 注册分帧任务，在 OnProcedureAttach 订阅 ProcedurePauseChangedEvent 并通过传入的 context.IsPaused 读取初值，在 OnProcedureDetach 取消订阅和任务。暂停与恢复通过 TimeSliceToken.Pause/Resume 保留任务及游标；暂停期间新建或重建任务时立即同步暂停状态。该机制只约束增量调度，不阻止显式场景初始化或外部业务入口。

### 场景加载

`Procedure` 通过 `LoadSceneAsync` 和 `UnloadSceneAsync` 封装 `IAssetService` 的 Additive 场景加载、Active Scene 设置及 operation 所有权。具体 Procedure 使用这些入口，不散落直接的 SceneManager 或 Addressables 调用。

### 切换事务与失败

同一时刻只运行一个切换 runner。切换期间的新请求进入 pending：Priority 数值越小优先级越高，同优先级时后请求覆盖前请求；pending 只在后续 EarlyTick 启动。

`ProcedureState` 是唯一状态源：runner 启动前进入 `Transitioning`，成功后进入 `Running`，普通异常进入终止态 `Failed`。`Failed` 会清除 pending 并拒绝后续切换，只有重新初始化架构才能恢复；PreShutdown/Shutdown 进入 `ShuttingDown` 并同步收束当前状态。

普通异常由 ProcedureSystem 记录并保留当前 Procedure 与 Context，直至架构 Shutdown 同步销毁。只有确认不可恢复的独立异步 operation 才通过 `IProcedureContext.Fail` 报告终止失败。

### 生命周期取消

- Procedure 与 Capacity 的 Enter、Ready、Teardown、Exit Hook 不重复接收 `CancellationToken`。
- 正常切换期间 Context lifetime 保持有效；Teardown/Exit 主动取消并等待自身 operation 收敛。
- PreShutdown/Shutdown 是硬取消边界，会取消 Context lifetime 并同步销毁当前 Procedure。
- 场景加载入口继续显式接收 token，以组合场景 operation 等独立取消来源。

## 使用与扩展规则

- 新 Procedure 继承 `Procedure` 或 `Procedure<TOptions>`，并通过统一 Transition 入口切换。
- Procedure 内逐帧逻辑使用自身 Tick 或 `ProcedureContext` 调度器。
- 流程资源由创建它们的 Procedure 或 Capacity 持有，并在退出前收束。
- 生命周期 Hook 不把调用方 token 传入清理阶段；清理由所有者自己的生命周期驱动。
- 可恢复业务错误不进入 `Failed`，终止失败只用于无法继续运行的流程状态。

## 验证

- 覆盖正常切换、切换中 pending 覆盖、暂停恢复、退出和 Shutdown。
- 为 Enter、Ready、Teardown 与场景加载注入异常或取消，确认状态、资源和呈现保持一致。
- 检查 Capacity 的 Ready 为正序、Teardown/Detach 为反序。
- C# 修改后执行 Unity Editor 编译，并对实际流程切换执行 PlayMode 验证。
