# ConditionSystem

ConditionSystem 集中管理条件总表的求值、按需观察、依赖索引、结果缓存和结果变化通知，消费方只维护 ConditionId 与自身业务对象的映射。

## 职责与边界

System 负责按 ID 查表和观察生命周期；ConditionResolver 接收 ConditionDef 与只读 ConditionContext，负责具体类型的求值。条件解析不执行写操作，也不携带调用来源；当前结果可以按 ConditionId 共享。引入依赖来源的条件时，需重新定义观察身份与缓存键。

条件依赖变化由 ConditionSystem 监听对应状态源并在内部汇总失效；ConditionResultChangedEvent 是消费方使用的结果输出。当前条件只有常量和组合，尚无业务状态监听。具体业务响应由消费系统负责，Action 分发见 [ActionSystem](Actions.md)。

## 主要类型与入口

- `WatchCondition(int conditionId, out bool initialResult)`：登记观察并返回当前结果和专用 ConditionWatchToken。
- `ConditionWatchToken.Cancel()`：幂等撤销自身登记；Token 副本共享同一登记身份，不实现 ICancelToken，也不接入通用 Token 托管。
- `Evaluate(int conditionId, bool useCache = true)`：统一求值入口。默认优先读取已登记条件的缓存，没有观察记录时即时只读求值；传入 `useCache: false` 时始终即时求值。即时求值不建立观察、更新缓存或发布结果事件，找不到配置或求值失败时记录错误并返回 false。
- `ConditionResultChangedEvent`：携带 ConditionId 和 Result，仅在已观察条件的布尔结果变化时发布。

## 生命周期与核心语义

架构初始化时绑定上下文并订阅条件表重载。WatchCondition 和 Evaluate 遵循架构生命周期契约，由调用方保证只在初始化完成后、Shutdown 前调用。消费方先建立业务映射，再按不重复的 ConditionId 登记，使用初始结果初始化状态；首次登记不发布结果变化事件。

同 ID 的观察共享依赖和结果，每次登记独立计数。最后一个登记取消后释放该条件的依赖与缓存。消费方离场、重建或业务结束时显式取消自己的 Token，关闭时取消事件订阅；System Shutdown 释放全部登记。

内部 `Refresh(ConditionChangeType type)` 登记按类型刷新请求，由 `ProcessRefreshes()` 同步处理。空闲时立即处理，通知期间在本轮通知结束后继续执行；按类型请求遵循 FIFO，相同类型也不合并。每次刷新先提交全部受影响缓存，再完整发布本轮结果变化；通知前已释放并重新登记的同 ID 条件不接收旧观察记录的通知。

全量刷新仅由条件表重载直接执行，重建所有正在观察的依赖并重新求值。配表重载统一由 EarlyTick 发起，遵循不重入条件刷新的调用约定，不维护全量刷新标记、优先级或请求合并。全量刷新通知期间产生的按类型请求仍进入 FIFO，完整发布本轮通知后由 `ProcessRefreshes()` 继续处理，不丢弃这些请求。

首次登记不存在的 ID 得到 false；保留登记期间后续条件表重载可恢复该条件，仍只通知结果变化。按类型处理器统一计入首次和后续请求，单轮最多处理 1024 次；超限时记录通知循环错误并清空剩余请求，Shutdown 同样清空待处理刷新。

## 使用与扩展规则

- 消费方通过 System 的 ID 入口读取条件；具体 Resolver 使用定义入口，不重复查询总表。
- 新条件沿用 DefinitionResolverAttribute 和生成模板，分别实现只读求值和依赖收集。
- 新增业务状态依赖时，由 ConditionSystem 集中订阅状态源，在对应回调中调用内部 `Refresh(ConditionChangeType type)`；消费方只订阅条件结果变化。
- 保持 All/Any 短路与现有空组合语义；不扫描整张总表或永久缓存无人使用的条件。
- 条件批次保证缓存通知顺序，不将多个业务状态写入变成事务；需要整体状态提交时由状态所有者提供批次能力。

## 验证

验证共享登记、Token 副本重复取消、最后登记释放、未登记时求值兜底、强制即时求值、不自动建立观察或更新缓存、初始结果、结果不变不通知、按类型与全量刷新、批次缓存一致性、本轮通知完整性、类型刷新重入 FIFO、重复类型请求不合并、全量刷新通知后继续处理类型请求、循环保护和条件表热重载。
