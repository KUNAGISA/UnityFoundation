# Entity 系统

Entity 框架管理场景里的运行时 MonoBehaviour。Gameplay 通过 Actor Prefab 装配角色表现，通过显式依赖组合角色行为。领域词汇见 [CONTEXT.md](../../CONTEXT.md)。

## 职责与边界

- EntityWorld 接管已实例化的 Entity 根组件，负责分配运行时 EntityId、登记、初始化、Tick 和释放。
- EntitySystem 持有 World，通过 IAssetService 按角色配置地址预加载 Prefab，并在场景加载阶段注入角色状态和运动路线。
- CharacterActor 执行角色内部的帧顺序，PlayerActor 负责输入位移和 Performance。Character State 的寿命独立于 Actor。

## 生命周期与核心语义

EntitySystem 确认 Prefab 根节点的 Actor 类型，实例化后注入 Character State、Spatial State、路线模型和起始路线位置。Actor 在 Prepare 阶段直接创建 Motor 并交付路线位置。World 分配 EntityId 后初始化动画和角色行为，成功后提供查询和 Tick。初始化失败时注销并释放该实例。重复登记保留已有实例，由所属 World 继续管理。

移除请求会立即使 Entity 失效，LateTick 再注销和销毁。场景离场会清理并释放所有 Actor。释放先取消角色工作并回收 Token，再销毁 GameObject。流程时间由 Procedure 提供。

## 使用与扩展规则

- CharacterActor 先收集位移请求，再由 CharacterMotor 结算，随后以实际位移和朝向更新基础动画。
- PlayerActor 持有 Gameplay Tag State。Performance 获取自己的移动阻断 Token，并在完成、失败、取消和离场时释放。PerformanceToken 对应单次运行，节点通过 PerformanceContext 获取 Motor 和路线。
- CharacterAnimation 在角色根节点及子节点查找 Animator，并在第 0 层按动作和朝向播放状态。直接在第 0 层配置 `idle`、`walk`、`talk`、`interact`、`stairs_up`、`stairs_down`，后缀为 `_left`、`_right`、`_front`、`_back`。基础状态允许用 `Idle` 和 `Move` 回退。角色没有 Animator Controller 时仍可移动并显示静态 Sprite。
- `BeginOverride(action, facing)` 启动临时覆盖。缺少目标状态或重复申请时记录错误并返回无效 Token。结束或取消时调用 Token 的 `Cancel`，恢复最新基础动作。同朝向切换混合 0.2 秒，跨朝向立即切换。循环和自然播放结束时的表现由 Animator Controller 与片段设置控制。
- 实体 ID 只用于当前运行时实例的查询。角色状态和持久化数据继续按角色 ID 关联。
