# 角色运动

角色运动由 Entity 上的 `CharacterMotorAbility` 统一执行。`PlayerInputMovementAbility`、Performance 等运动生产者只提交本帧期望位移，Motor 负责仲裁、路线求解以及最终空间状态写入。

## 职责与边界

`CharacterMotorAbility` 是运行时 `SpatialState` 与角色 Root Transform 的唯一连续运动写入者。它负责收集本帧运动、按模式与优先级合并、调用 `MovementRouteModel` 求解，并公开最近一次结算的实际位移 `ActualDelta` 和当前路线位置的只读查询。Motor 同时统一更新角色左右面向，通过 Root 的 Y 轴半周旋转表示翻转，并将完整旋转写入 `SpatialState.Rotation`；玩家存档沿用该旋转恢复面向。当前角色的 X 轴旋转保持为零，Y 轴用于左右面向，Z 轴可用于场景姿态。

`SpatialState` 对外只读，并通过程序集内的 `SetPose` 原子更新 Route、Position 和 Rotation。该入口只用于 Entity 生成前的初始化或存档恢复，以及 Entity 生成后由 Motor 提交最终空间结果。

运动生产者负责速度、加速度和状态规则，将结果换算为 Delta 后提交。Motor 不持有移动速度等玩法参数，也不产生重力、惯性或平台速度。

出生、路线切换和场景放置使用 Warp 接口表达非连续位移。Warp 不参与运动仲裁，并会清除尚未结算的运动和 `ActualDelta`。

## 主要类型与入口

- `CharacterMotorAbility`：角色运动执行入口，位于 `Assets/Gameplay/Scripts/Entities/Abilities`。
- `SubmitDelta`：提交本帧期望位移。
- `WarpToRoute`、`WarpToWorld`：设置路线内或自由世界位置。
- `MotionMode.Override`：只采用最高优先级请求，同优先级请求相加。
- `MotionMode.Additive`：所有请求相加，不参与优先级竞争。
- `ActualDelta`：最近一次 Motor 结算产生的实际世界位移。
- `TryGetRouteLocation`：只读查询当前 `RouteLocation`；角色不在路线内时返回 `false`。
- `Facing`、`SetFacing`、`FaceTowards`：读取当前左右面向，或在不移动时指定面向、朝向世界坐标。

## 生命周期与核心语义

输入在 EarlyTick 写入角色输入状态。`PlayerInputMovementAbility`、Performance 等 Ability 在 FrameTick 计算并提交 Delta；所有 Ability 的 FrameTick 完成后，`CharacterEntity.OnFrameTick` 调用 Motor 合并请求、完成路线求解并更新空间状态。后续 LateTick 因此可以读取本帧最终位置。

`CharacterEntity` 先注册 Motor 和 Animation，再构建具体角色 Ability，供它们在 Ready 阶段取得 Motor。`PlayerInputMovementAbility` 在 FrameTick 只提交输入产生的位移；Motor 结算后，`CharacterEntity` 根据本帧 `ActualDelta` 更新基础移动动画。Motor 停用或实际位移不足阈值时使用静止姿态。

每次 Motor 结算都会刷新 `ActualDelta`，并在成功、无运动或求解失败后清空全部本帧累计值。禁用、销毁和 Warp 也会清空累计值，运动请求不会跨帧残留。

Performance 的路线移动在进入时将目标投影到当前路线；目标不在到达容差内或角色没有路线位置时失败。每次 FrameTick 按当前位置与目标的路线距离截取步长，并通过前进方向的短距离路线采样确定运动方向；下一次 FrameTick 根据路线距离是否缩短判断进展。到达需要同时满足路线距离与世界距离容差；空间位置在提交当帧的 Ability 更新结束后生效。

Motor 根据胜出的 Override 请求方向更新面向，先排除当前左右翻转，再按 Root 其余旋转转换到局部空间判断水平分量；即使路线求解失败也可转向。Additive 位移、纯竖直位移和停止不改变面向。未指定旋转的 Warp 保留面向；指定旋转的 Warp 应用完整旋转，Performance 路线切换会恢复进入节点前的面向。显式转向立即生效；同帧若有水平 Override 位移，Motor 结算时仍会按位移方向自动转向。

## 使用与扩展规则

- 运行时连续移动必须通过 `SubmitDelta`，不得由调用方直接写入 Spatial State 或 Transform。
- 业务代码不得调用 `SpatialState.SetPose`；生成前初始化和存档恢复是 Motor 之外仅有的合法调用场景。
- 普通移动使用 Override 优先级 `0`；脚本化 Performance 移动使用 Override 优先级 `100`；Additive 的优先级参数不生效。
- 对话等原地动作通过 Motor 的 `SetFacing` 或 `FaceTowards` 转向，不需要提交零位移请求。
- 左右面向由当前 `SpatialState.Rotation` 推导，不另存独立状态；调用 `SetFacing` 会同步更新 Root 和 Spatial State。
- 当前 Motor 只执行路线约束移动。空中运动、碰撞、Ground 和移动平台能力落地时扩展 Motor 内部实现，不预先增加抽象求解器。
- 只有出现至少两种实际运动求解实现时，才在 Motor 内部建立 Adapter seam；调用方仍只依赖 `CharacterMotorAbility` 的公开接口。

## 性能与排错

Motor 使用固定字段增量归约请求，不创建请求列表，不产生逐帧集合分配。发生输入有效但角色不移动时，依次检查 Motor 是否启用、角色是否具有有效 RouteLocation、路线是否存在，以及 `ActualDelta` 是否为零。

## 验证

- 验证玩家输入、路线端点、BlockMovement 和 Performance 移动下的位置与动画状态；受阻时应显示静止姿态。
- 验证 Performance 在直线、曲线和反向路线上的到达，以及目标偏离路线、路线切换和持续无法前进时的失败结果。
- 验证普通移动、演出移动和原地转向的左右面向，以及 Additive 位移不影响面向。
- 验证玩家出生、存档恢复、路线切换以及路线内外 NPC 放置。
- 验证玩家朝右存档后读档仍朝右，路线切换保留面向。
- 修改仲裁规则或 Tick 顺序后，验证同优先级 Override 相加、高优先级覆盖低优先级、Additive 累加，以及所有请求在结算后清空。
