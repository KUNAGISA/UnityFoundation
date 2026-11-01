# ConditionSystem

ConditionSystem 提供即时条件求值和按条件 ID 共享的条件观察。观察依赖由生成代码收集，数据源分类和变更事件映射由项目人工维护；运行时在 LateTick 合并刷新。决策背景见 [ADR-0025](../ADR/0025-declarative-condition-sources-and-batched-observation.md)。

## 职责与边界

System 持有只读 ConditionContext、观察缓存、来源集合和观察登记。ConditionResolver 只读求值，CollectSources 只收集配置依赖；两者都不执行业务写操作。NPC、场景事件等消费方拥有业务状态、观察 Token 与实际应用逻辑。Context 是查询入口，不是事务快照。

ConditionSource 为手工维护的 Flags 枚举，当前包含 None、GameIntVariables、GameBoolVariables、GlobalIntVariables、GlobalBoolVariables，不细化到变量 ID。ConditionSystem 手工订阅四类变量变更及其默认值表重载，各自标记对应来源；同一来源可由多个业务事件标记失效。

## 主要类型与入口

- `Evaluate(int conditionId)`：即时读取当前定义和状态，不使用或更新观察缓存；缺失配置或求值失败返回 false 并记录错误。
- `Observe(int conditionId, Action<bool> onChanged, out bool initialValue)`：即时返回初值，登记时不调用回调；相同 ID 共享后续求值，各观察者独立保存最后通知值。
- `ConditionObservationToken.Cancel/Pause/Resume`：取消、暂停和恢复单次观察。默认 Token 和重复取消为空操作；恢复后安排重新检查，相对最后通知值决定是否通知。
- `ConditionBatchCompletedEvent`：本批所有变化通知完成后发布，供消费方统一应用已收集的变化。

## 生命周期与核心语义

架构初始化绑定 Context 并订阅状态和表重载。Shutdown 取消全部订阅，释放观察缓存；消费方在离场、重建、流程脱离时取消自己的 Token。内部登记 ID 在 System 实例内不复用，取消或重建不会让旧通知命中新登记；最后一个观察者取消时释放该条件的观察状态。

FlushDirty 在有效架构生命周期内同步完成，刷新及回调期间不执行 Shutdown；关闭由架构停止帧驱动后统一收束。ConditionSystem 信任该约定，不维护关闭版本；通知派发前仍检查观察是否已取消或暂停。

GameplayArchitecture 在 LateTick 的 Procedure、Scheduler 之后、Camera 之前调用 FlushDirty。没有待处理变化时直接返回；刷新开始截取来源和待检查条件，先完成本批全部求值并提交缓存，再派发变化回调，最后发布批次完成事件。回调或业务应用产生的新 dirty 留到下一次 LateTick，不递归刷新，不使用分帧时间片。

FlushDirty 统一管理批次截取、完成事件和集合清理；内部按 CollectDirtyConditions、EvaluateBatch、DispatchNotifications 三个阶段执行。各观察的 LastNotifiedValue 以注册初值为比较基准，仅在通知派发时推进；暂停期间保留该基准。

暂停的观察者不接收变化；其他观察者仍可正常更新。消费方在流程暂停事件中操作自身 Token，暂停期间重建的新 Token 立即暂停。同步初始化和显式 Evaluate 继续按入口契约执行。

条件总表重载在下一次刷新重建所有已观察条件的来源并重新求值，包括 None 来源条件。默认值表重载只标记对应变量来源。观察注册读取最新初值，不吞掉既有观察者尚未收到的变化。

## 使用与扩展规则

- 使用 `[ConditionResolver(typeof(SomeConditionDef), ConditionSource.SomeSource)]` 声明条件与来源，通过专用生成菜单更新求值分发和来源收集，详见[生成流程](DefinitionGeneration.md)。来源必填；常量条件显式写 None。
- All/Any 保持求值短路，收集来源则遍历所有子条件；空 All 成立、空 Any 不成立。
- CollectSources 对未支持的条件类型记录 Error 并返回 None，不中断组合中其余分支的收集；None 不提供来源驱动的自动刷新，需要修正对应声明并重新生成。
- 新增数据源时人工维护枚举及通知映射，不从事件名称推断来源，不要求 Model 发布条件框架专用事件。
- 观察回调只更新本地结果、收集待处理对象；在批次完成事件中去重应用。提交业务行为前使用 Evaluate 即时复核，复核不改变观察缓存。
- 批处理可能合并两次刷新之间的短暂真假变化；一次性事实使用业务事件或所属系统的持续状态。LateTick 之后及刷新回调内的变化延至下一批，不承诺 setter 返回前已响应。

## 性能与排错

只维护当前已观察条件，不扫描整张条件总表；同 ID 在一个 dirty 批次中只求值一次。集合在热路径复用，闭包仅在注册阶段创建。集中刷新不限制固定 CPU 预算，昂贵求值和业务应用需要分别测量。

状态未更新时，依次检查 Token 生命周期/暂停状态、Resolver 来源声明、变更事件映射和最新生成产物。来源声明和事件覆盖仍属于人工契约，生成器不分析 Evaluate 的全部实际读取。

## 验证

验证四类来源独立失效、组合完整收集、共享求值、初始化无回调、独立暂停恢复、取消待派发观察、重建/关闭失效、同批合并与回调新增 dirty 延后；验证变量默认值和条件表重载，以及消费方业务批处理。编译、隔离探针与 Unity 场景验证分别报告。
