# 角色运动

角色运动由 `CharacterMotor` 统一执行。玩家输入与 Performance 提交本帧期望位移，Motor 负责仲裁、路线求解以及最终空间状态写入。

## 职责与边界

`CharacterMotor` 是运行时 `SpatialState` 与角色 Root Transform 的唯一连续运动写入者。它负责收集本帧运动、按模式与优先级合并、调用 `MovementRouteModel` 求解，并公开最近一次结算的实际位移 `ActualDelta` 和当前路线位置的只读查询。Motor 根据实际位移更新 `SpatialState.Facing`，动画据此选择四向片段。角色根节点以单位旋转生成。

`SpatialState` 对外只读，通过程序集内的 `SetPosition` 提交路线 ID 和世界位置，通过 `SetFacing` 提交朝向。位置写入发生在 Entity 生成前、存档恢复和 Motor 结算时。空间存档保存路线 ID 和世界位置，格式版本沿用 v1。

运动生产者负责速度、加速度和状态规则，将结果换算为 Delta 后提交。Motor 不持有移动速度等玩法参数，也不产生重力、惯性或平台速度。

Motor 在构造时接收已求解的初始路线位置。运行时传送、路线切换和场景重新放置使用 Warp。Warp 不参与运动仲裁，并会清除尚未结算的运动和 `ActualDelta`。

## 主要类型与入口

- `CharacterMotor`：角色运动执行入口，位于 `Assets/Gameplay/Scripts/Character/Components`。
- `SubmitDelta`：提交本帧期望位移。
- `WarpToRoute`、`WarpToWorld`：设置路线内或自由世界位置。
- `MotionMode.Override`：只采用最高优先级请求，同优先级请求相加。
- `MotionMode.Additive`：所有请求相加，不参与优先级竞争。
- `ActualDelta`：最近一次 Motor 结算产生的实际世界位移。
- `TryGetRouteLocation`：只读查询当前 `RouteLocation`；角色不在路线内时返回 `false`。
- `Facing`、`SetFacing`、`FaceTowards`：读取当前四向面向，或在不移动时指定面向、朝向世界坐标。

## 生命周期与核心语义

输入在 EarlyTick 写入角色输入状态。FrameTick 中 PlayerActor 提交输入位移并推进 Performance，CharacterActor 统一结算 Motor，再根据本帧的 ActualDelta 更新基础动画。后续 LateTick 读取本帧最终位置。

SceneSpawnPoint 使用「初始朝向」配置角色的出生姿态。四向朝向在运行时保存在 Spatial State。NPC 刷新入口将现有表中的旧镜像约定转换成初始左右朝向。

每次 Motor 结算都会刷新 `ActualDelta`，并在成功、无运动或求解失败后清空全部本帧累计值。Warp 会清空累计值，运动请求不会跨帧残留。

Performance 的路线移动在进入时将目标投影到当前路线；目标不在到达容差内或角色没有路线位置时失败。每次 FrameTick 按当前位置与目标的路线距离截取步长，并通过前进方向的短距离路线采样确定运动方向；下一次 FrameTick 根据路线距离是否缩短判断进展。到达需要同时满足路线距离与世界距离容差；空间位置在提交当帧的 Motor 结算时生效。

Motor 取实际世界位移绝对值更大的轴。X 轴对应左右，Y 轴正向对应背面。静止时保留最近朝向。显式转向立即生效，后续有效位移可以再次更新朝向。指定 Facing 的 Warp 用于出生和刷新，其他 Warp 保留当前朝向。

## 使用与扩展规则

- 运行时连续移动必须通过 `SubmitDelta`，不得由调用方直接写入 Spatial State 或 Transform。
- `SpatialState.SetPosition` 的调用范围限于 Motor、生成前初始化和存档恢复。
- 普通移动使用 Override 优先级 `0`；脚本化 Performance 移动使用 Override 优先级 `100`；Additive 的优先级参数不生效。
- 对话等原地动作通过 Motor 的 `SetFacing` 或 `FaceTowards` 转向，不需要提交零位移请求。
- 当前 Motor 只执行路线约束移动。空中运动、碰撞、Ground 和移动平台能力落地时扩展 Motor 内部实现，不预先增加抽象求解器。
- 只有出现至少两种实际运动求解实现时，才在 Motor 内部建立 Adapter seam；调用方仍只依赖 `CharacterMotor` 的公开接口。

## 性能与排错

Motor 使用固定字段增量归约请求，不创建请求列表，不产生逐帧集合分配。发生输入有效但角色不移动时，依次检查角色是否已登记、是否具有有效 RouteLocation、路线是否存在，以及 `ActualDelta` 是否为零。

## 验证

- 验证玩家输入、路线端点、BlockMovement 和 Performance 移动下的位置与动画状态；受阻时应显示静止姿态。
- 验证 Performance 在直线、曲线和反向路线上的到达，以及目标偏离路线、路线切换和持续无法前进时的失败结果。
- 验证普通移动、演出移动和原地转向的四向朝向，以及静止时保留上次朝向。
- 验证玩家出生、存档恢复、路线切换以及路线内外 NPC 放置。
- 验证指定和默认出生点读取四向配置，Warp 和路线切换遵守朝向参数。
- 修改仲裁规则或 Tick 顺序后，验证同优先级 Override 相加、高优先级覆盖低优先级、Additive 累加，以及所有请求在结算后清空。
