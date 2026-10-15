# CameraSystem

CameraSystem 统一管理主相机资源、Camera Stack、镜头模块、固定优先级仲裁、Cinemachine Brain 更新和镜头 Session 生命周期。

## 职责与边界

`CameraSystem` 是项目侧组装入口；内部 `CameraDirector` 管理模块、Brain、Provider、Blend、Selected 通知和诊断，`CameraOutputRegistry` 管理输出与 Active/Selected/Live 状态。具体镜头能力由模块实现，当前包含 PlayerCamera 和 ArtCamera。

Director 是 internal 核心，不作为业务入口。镜头模块只在初始化或激活冷路径通过 `ICameraContext` 获取架构依赖，并缓存实际使用的 Service、System 或时间源；帧更新、选择、Blend 和绑定同步等热路径不查找架构模块。

## 主要类型与入口

- `CameraSystem`：模块注册、初始化、Camera Stack 和外部调用入口
- `CameraDirector`：内部模块与 Brain 驱动
- `CameraOutputRegistry`：固定优先级输出仲裁
- `ICameraContext`：模块依赖、输出注册和调度能力
- `PlayerCamera`、`ArtCamera`：具体镜头模块
- `CameraSessionToken`、`ArtCameraPlayback`、`OverlayCameraToken`：强类型生命周期凭证

模块在 CameraSystem 初始化前通过 `RegisterCameraModule` 注册。Provider ID、模块类型和 Priority Range 必须唯一，Range 不重叠。

固定 Addressable 地址为：

- `Global/CameraSystemSettings`
- `Cameras/PlayerCameraSettings`
- `Cameras/ArtCameraSettings`
- `Cameras/PlayerCameraRig`

默认资产位于 `Assets/Gameplay/Settings/Camera`，Addressables 分组和地址由 SmartAddressable 规则生成。

## 生命周期与核心语义

Director 按注册顺序初始化模块、按反序关闭，初始化失败也会清理已成功初始化的模块。

顶层输出通过 `ICameraContext.RegisterOutput` 注册，由系统设置 Priority 和 Output Channel：

- Active：Session 存在且参与固定优先级仲裁。
- Selected：当前最高 Priority 的 Active 输出；同优先级不按激活顺序决胜，Provider 必须主动避免冲突。
- Live：Brain 当前输出或 Blend 的 From/To 仍引用该虚拟相机。

每帧顺序为 FrameTick → 第一次 Selected 解析 → Provider LateTick → 第二次解析 → Brain ManualUpdate → AfterCameraUpdate。临时 Rig 退出仲裁后只有在不再 Live 时才能销毁；系统 Shutdown 是唯一例外。

常规激活顺序为 Load → StagingRoot 实例化 → Validate → Bind → Evaluate/Snap → 设置输出 → Enable。激活失败不影响当前镜头。Session Token 携带 ID，旧 Token 不能取消后续 Session。

### 时间

CameraSystem 使用架构分发的同一份 `FrameTime`，Brain、Blend、参数过渡和释放等待共享其 DeltaTime。ArtCamera 每次激活显式选择 Camera 时间或 `ProcedureSystem.Time`；流程暂停只停止选择 Procedure 时间的内容，不停止 CameraSystem、Brain、绑定同步或 Session 检查。统一时间决策见 [ADR-0002](../ADR/0002-single-frame-time-snapshot.md)。

Loading Session 从 Activate 起按所选内容时间累计，资源完成后追到对应播放位置。加载超过 Clip 时按 CompletionPolicy 结束、保持末帧或循环。

### PlayerCamera

`PlayerCamera.ActivateAsync` 接收有效 PlayerEntity。Rig 首次加载后常驻，Session 结束只禁用并清理玩家绑定。PlayerCameraRig Prefab 是镜头参数的唯一来源，运行时不覆盖 OrthographicSize、FollowOffset、Damping 或 Composition；初次激活或空间连续性中断时通过 Snap 丢弃旧帧状态。

### ArtCamera

每个 Art Prefab 对应独立 Session 和主 AnimationClip，根节点挂 ArtCameraRig 与 Animator，运行时使用 Manual PlayableGraph。Layer、时间、遮挡策略和完成策略均由每次 Activate 的 Options 提供，不写入 Prefab，也不由 Layer 隐式推导。

Art Rig 只包含一个无父 Cinemachine Camera 的顶层 OutputCamera；多子镜头由 MixingCamera 或 SequencerCamera 组织。Clip 可以动画子镜头、权重、Transform、Lens 和表现参数，但不动画 Rig 根、顶层 Priority、Output Channel 或 Session 生命周期字段。

Binding 使用固定 Slot ID 和 `BindingProxy → AnimatedOffset` 两层结构。目标丢失策略为 CancelSession、FreezeLastPose 或 ClearBinding。

同一 ArtCameraLayer 只允许一个 Visual Session，不同层可以并行；固定层级从低到高为 Ambient、Interaction、MainSequence、CriticalCutscene。CoveredPolicy 控制被更高层覆盖后继续、暂停或取消，CompletionPolicy 控制完成后释放、保持末帧或循环到取消。

每个 Art Session 独立持有 Prefab operation，结束时释放，不使用对象池或显式暖缓存。

### Camera Stack

`AddOverlayCamera` 返回 `OverlayCameraToken`。同一 Camera 可以由多个 Token 持有，但 Stack 只保留一项；最后一个所有者取消时移除。Shutdown 清空注册并使旧 Token 失效，调用方不直接修改 Camera Stack。

## 使用与扩展规则

- 新模块在 CameraSystem 初始化前注册，并声明唯一 Provider ID 与不重叠的 Priority Range。
- 业务代码不修改顶层输出的 Priority、Output Channel 或 Camera Stack。
- 同层冲突由 Provider 主动禁止，不使用“后激活者获胜”或临时抬高优先级。
- 运行时镜头参数来源于 Prefab 或 Settings；Session 清理不污染常驻 Rig。
- Art 内容的时间、覆盖和完成语义在每次 Activate 时显式指定。
- 临时 Rig 保持到不再 Live，避免在 Brain Blend 中提前销毁。

## 性能与排错

`GetDebugSnapshot()` 提供 Selected、Blend、Live Camera 及各输出状态，只用于显式诊断，不逐帧调用。`ArtCameraSettings.LiveWarningInterval` 控制 Deactivating Session 长时间 Live 时的重复警告间隔，不强制销毁。

镜头冲突先检查 Priority Range 和同层 Session；播放暂停异常先检查内容时间域；Rig 释放异常检查 Live 状态和 Brain Blend；运行时参数漂移检查是否直接修改了常驻 Prefab 实例。

## 验证

- 覆盖模块初始化失败、输出仲裁、同层冲突、跨 Provider Blend 和 Shutdown 反序。
- 验证 Player Rig 常驻复用、绑定清理和 Snap。
- 验证 Art 加载追帧、三种完成策略、覆盖策略、Binding 丢失和取消。
- 验证 Overlay Token 多所有者以及 UI Camera Stack 的加入、移除顺序。
- Camera 修改后执行 Unity Editor 编译，并在 PlayMode 检查 Cinemachine Brain、Blend 和 Rig 生命周期。
