# InputService

InputService 统一拥有输入模式、通道、UI Module 绑定和设备表现，把底层 Input System 状态转换成稳定的项目能力。程序集与语义路由的决策见 [ADR-0003](../ADR/0003-isolate-input-and-route-semantic-commands.md)。

## 职责与边界

`Gameplay.Input` 是独立运行时程序集，由主 `Gameplay` 单向引用。输入程序集负责创建和启停 Channel、计算模式栈、绑定 UI Module，并暴露只读输入状态与语义命令；业务 System、Procedure 和 View 只使用公开能力。

View 不直接订阅 Input Actions。UI 上下文命令由 `UICommand` 信号进入 `UIViewNavigationExtension`，再发送给当前活动 View 的栈顶焦点作用域。界面导航与玩法输入阻断是独立策略。

## 主要类型与入口

- `IInputService` / `InputService`：`Assets/Gameplay/Input/Runtime`
- `InputModeCatalog`、`InputModeDefinition`、`InputChannelMask`、模式栈：`Assets/Gameplay/Input/Runtime/Mode`
- `InputChannel`、`PlayerInputChannel`、`InputButtonState`：`Assets/Gameplay/Input/Runtime/Channel`
- `InputModeToken`、`UIInputModuleToken`：输入模式和 UI Module 绑定的强类型取消凭证
- `UIInputCommand`：Cancel、上下文和翻页等 UI 语义命令

主要入口包括 `PushMode`、`BindUIInputModule`、两个强类型 `Cancel`、`Player`、`UICommand`、`CurrentPresentation` 和 `InputPresentationChanged`。

## 生命周期与核心语义

### 输入模式

`PushMode(InputModeDefinition)` 将语义化模式压栈并返回独立 Token。模式可以并存：优先级高者先决定通道状态，同优先级时最后压入者优先。模式定义至少启用或禁用一个通道，同一通道不能同时出现在启用和禁用掩码中。

调用方保存 Token 并在生命周期结束时取消。重复压入同一模式产生不同 Token；`default`、旧 Token 和重复取消均无操作。View、Procedure 和 Capacity System 不直接启停底层 `InputAction`。

### 输入通道

当前通道为 Player、HotKey、UIGlobal 和 UIModal。每个具体 Channel 只声明一个 Mask 位和默认状态；`InputService.Compute` 根据模式栈计算整体 `IsEnabled`。`InputChannelMask.All` 表示当前及后续可表示的全部通道。

`IsEnabled` 是模式计算出的权威状态，不能用 `InputActionMap.enabled` 代替。通道启用时先启用完整 ActionMap、再标记状态、最后挂接 UI Module 引用；禁用时按相反语义先撤销状态与 UI 引用、再禁用 ActionMap，避免 UI Module 在禁用期间重新激活 Action。

### UI Module 绑定

`BindUIInputModule` 同时只接受一个实例。InputService 设置 Actions Asset，UIGlobal 和 UIModal Channel 分别维护自己拥有的 ActionReference；解绑时 Channel 先撤销引用，服务再清除 Actions Asset。

UIRoot 中的 `InputSystemUIInputModule` 默认禁用且不序列化 ActionReference。UISystem 创建 Root 后先绑定再启用；初始化失败或关闭时先禁用再取消绑定，最后销毁 Root。Cancel 不绑定到 UI Module，避免 EventSystem 与 View 语义路由重复处理。

### 输入状态与表现

`InputPresentation` 只暴露设备种类、UI 交互来源和稳定设备信息，不泄露底层 InputSystem 对象。设备识别只接收当前资产中已启用 Action 的有效输入；零值导航、轻微漂移、Canceled 和持续姿态不会切换表现。

Gameplay 输入由 Channel 回调写入状态，再由 System 在既定 Tick 主动读取。`InputButtonState` 的 Pressed、Released 是帧内累计瞬时状态，Held 是最终持续状态，同一帧可以同时 Pressed 和 Released；LateTick 清除瞬时状态，通道禁用时清空全部玩家输入状态。

## 使用与扩展规则

- 新输入场景优先复用或新增 `InputModeDefinition`；只有出现新的动作所有权时才新增 Channel。
- `Gameplay.Input` 不引用主 `Gameplay`，程序集外不初始化或启停 Channel。
- View 通过 `UIFocusScope` 和 `IUIInputCommandTarget` 接收语义命令，不读取底层 Action。
- 与 Procedure 同生命周期的输入逻辑放入 Capacity System，由 Procedure 附加。
- 所有临时模式和 UI Module 绑定都保存 Token 并显式取消。
- 动作事件不从 Player Channel 直接广播给 Entity 或 View；设备表现变化使用独立只读信号。

## 性能与排错

- 通道整体已禁用但 Action 仍活跃时，检查 UI Module 是否绕过 InputService 保存了 ActionReference。
- 输入模式未恢复时，检查所有权 Token 是否在 Hide、Teardown、异常和 Shutdown 路径释放。
- 设备表现频繁切换时，检查零值、漂移和 Canceled 输入是否被错误计入。

## 验证

- 覆盖多模式优先级、同优先级后压入、Token 重复取消和模式退出恢复。
- 验证 UI Module 绑定唯一性、绑定/解绑顺序以及初始化失败回滚。
- 覆盖键鼠与手柄表现切换、帧内 Press/Release 累计和 LateTick 清理。
- 输入或 UI 路由修改后执行 Unity Editor 编译，并在 PlayMode 验证 EventSystem、焦点与玩法阻断。
