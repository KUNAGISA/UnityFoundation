# Entity 系统

Entity 框架管理场景内运行时实例及其 Ability 生命周期；具体游戏负责构造实体、定义领域状态和选择能力组合。领域词汇边界见 [CONTEXT.md](../../CONTEXT.md)。

## 职责与边界

`EntityWorld` 只接管已经构造完成的 Entity 与 Root，负责分配 `EntityId`、注册、初始化、Tick 和释放。它不实例化具体 Entity，也不定义业务状态。

`EntityId` 是 World 分配的运行时实例标识，使用 `long` 且 `0` 无效。角色或其他领域标识由具体状态定义，不与 `EntityId` 混用。

## 主要类型与入口

- `Entity`、`EntityWorld`、`EntityContext`、`Ability`：`Assets/Framework/Entities/Runtime`
- Gameplay 具体 Entity：负责构造参数、领域状态和 Ability 组合
- `EntitySystem`：项目侧 World 生命周期与场景接入入口

## 生命周期与核心语义

`AddEntity` 后 Root 的释放责任归 World 所有；参数错误、重复添加或初始化失败时也只释放一次。初始化失败会注销 Entity、销毁已建立的 Ability，并确保 Entity 不进入活动列表。

Entity 初始化顺序为：构建 Ability → 全部 Ability `OnInitialize` → Entity `OnInitialize` → 全部 Ability `OnReady` → 根据启用状态执行 `OnEnabled`。`OnReady` 时所有 Ability 都已注册并完成初始化，同层依赖不依赖彼此的 Ready 顺序。

`RemoveEntity` 将 Ready Entity 标记为 PendingRemoval，实际销毁在 LateTick flush。`IsValid` 只对 Ready 返回 true。销毁时按反序停止 Ability 启用阶段，再按反序执行 Ability 与 Entity 的 `OnDestroy`。

`OnEnabled` 抛出异常时 Ability 回滚为停用并继续传播异常，Ability 自身清理启用过程中已经产生的副作用。

每个 Entity 的 FrameTick 先更新全部启用的 `IAbilityFrameTickable`，再调用 Entity 的 `OnFrameTick`；LateTick 先更新全部启用的 `IAbilityLateTickable`，再调用 Entity 的 `OnLateTick`。这两个钩子只保证当前 Entity 内 Ability 与 Entity 的顺序，不构成跨 Entity 的同步阶段。

## 使用与扩展规则

- 具体业务 Entity 继承 `Entity`，在构造阶段提供必需状态和 Ability。
- 共享状态放在 Entity 或明确的 State；具有独立生命周期的行为实现为 Ability。
- Ability 的状态依赖通过构造函数注入，同层 Ability 依赖通过 `Owner.RequireAbility<T>()` 获取。
- Entity 初始化保持同步和原子化，失败后不得残留在 World。
- 高频 Ability 逻辑避免 LINQ、临时集合和逐帧分配。

## 角色动画

`AnimationAbility` 是角色 Animator 第 0 层的播放入口。它在角色 Root 下查找 Animator；当前测试角色没有 Animator 或 Controller 时，动画请求只更新内部仲裁状态，不影响 Entity 初始化和运动。配置角色动画资源后，Controller 需提供 `Idle`、`Move` 及业务传入的覆盖状态。

`CharacterEntity` 在 Motor 结算后根据本帧 `ActualDelta` 更新基础移动动画；Motor 停用或实际位移不足阈值时请求 `Idle`。特殊演出可通过 `BeginAnimationOverride` 获取覆盖 token，在退出或取消时释放。覆盖期间基础请求继续更新，释放后恢复最新基础请求。同一时刻只支持一个覆盖；名称和循环意图相同的重复请求不会重播。普通 Performance 路线移动不申请覆盖。

`SetBaseAnimation(null)` 映射为 Animator 的 `Idle` 状态；过渡时间默认 `0.2f`，正值使用 `CrossFadeInFixedTime`，零值使用 `Play`。接口中的 `loop` 参数用于记录播放意图与重复请求判断；实际循环由 Controller 状态及 AnimationClip 配置，非循环动画要保持末帧时也应在 Controller 中避免自动转出。

- PlayerEntity 持有 GameplayTagContainer 表达 Gameplay Tag State；使用项目共享 Catalog，Entity 销毁时静默 Dispose。来源与订阅语义见 [Gameplay Tag 系统](GameplayTags.md)。

## 验证

- 覆盖成功创建、初始化各阶段异常、重复添加、延迟移除和 World Shutdown。
- 检查 Root 恰好释放一次，失败或 PendingRemoval Entity 不再可查询和 Tick。
- 检查 Ability `OnReady`/`OnEnabled` 为正序、停用和销毁为反序。
