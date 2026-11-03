# UI 系统

UI 框架由 `UIViewHost` 协调多个界面，`UIViewSession` 拥有一次打开的完整生命周期，`UILayer` 管理层内可见性。Gameplay 通过 `UISystem` 接入资源、相机、输入与具体 UI 配置。

## 职责与边界

- `UIViewHost` 位于 Framework，负责配置校验、顶部复用、注册查询、单实例冲突、批量关闭和整体 Shutdown，拥有共享 Provider 和 Extension 的生命周期。
- `UIViewSession` 管理一次打开的加载、初始化、Data 刷新、显隐、关闭和释放，只通过 Host 协调跨界面冲突及注销；Provider 成功交付的实例由 Session 恰好归还一次。
- `UIView` 执行表现、等待动画并收束立即隐藏，通过内部绑定的 Session 直接报告有效完成；具体 View 保留返回动画 UniTask 的接口，Token 仅提供对外查询与操作。
- `UILayer` 维护层内顺序、Root、遮挡和目标可见性，并拥有 dirty 重算循环；只向 Session 发出可见性请求，不直接执行 View 生命周期或 Extension。
- `UISystem` 位于 Gameplay，创建 UIRoot、持有 Prefab 缓存、注册 UI Camera Stack、注入 Extension 并提供业务入口。
- 横切行为由 `UIViewExtension` 承接，不写入 Framework 的 View 基类。

业务代码只通过 `UISystem` 打开、查询和关闭 UI，不直接创建 Host、Session 或驱动 View 生命周期。

## 主要类型与入口

- `UIViewHost`、`UIViewSession`、`UIViewToken`、`UIViewStatus`、`UIViewTokenExtensions`、`UIView<TData>`
- `UIViewData`、`UIViewOptionMask`、`UILayerId`
- `UIViewExtension`、`UIViewExtensionContext`
- `IUIViewInstanceProvider`、`UILayer`、`UILayerRoots`
- `UISystem`、`UIViewPrefabCache`、`UIRoot`

Framework 的 LayerId 和 Option 只包装整数，不定义业务枚举。Gameplay 定义具体 Layer 与 `[Flags] UIViewOptionFlags`，通过转换扩展传入 Framework。

Gameplay 的逻辑 Layer 从低到高为 HUD、View、Adv、Popup、Loading、Modal、Overlay。当前共享 Option 为：

- `ClosePopup`：打开前关闭现有 Popup。
- `HideHUD`：可见期间阻断 HUD Layer。
- `Blur`：成为一个模糊边界候选。
- `HideMainCamera`：Show 完成后隐藏主相机场景输出。
- `BlockGameplayInput`：可见期间阻断 Player 和 HotKey，并启用 UIModal。

## 生命周期与核心语义

### 实例与 Prefab 所有权

Session 借用 Host 的 Provider 请求实例，传入目标 Layer 的 Primary Root 和 Host 生命周期 token。Provider 成功返回的必须是目标 Parent 下、未激活的根实例；返回成功即将实例交给 Session，Session 最终按 Location 恰好调用一次 `Release`。Provider 在交付前取消或失败时自行清理已创建实例。

Loading Session 尚未附加 View 时，关闭会立即完成逻辑释放；共享 Provider 请求可以继续，加载操作捕获的 Provider 负责立即归还晚到实例。已进入初始化栈的关闭先标记 Closing，退出调用后统一释放。Host Shutdown 先禁止新打开和 Layer 可见性重算，再关闭 Session、取消仍在运行的请求，并清理共享 Extension 与 Layer。初始化调用栈内重入 Shutdown 时，共享清理延迟到最后一个 Session 归还实例并注销，避免 Released 晚于 OnShutdown。

Gameplay 的 `UIViewPrefabCache` 缓存每个 Location 的 Prefab operation；当前实例释放只销毁实例，不立即释放缓存。`TrimUnusedViewAssets` 根据 Host 存活 Session 收集在用 Location，再释放未使用 operation，不维护 View 级引用计数。

### View Data 与 Option

每个 View 定义自己的 `UIViewData` 子类。`Location`、`LayerId`、`Options`、`AllowMultiple` 和 `HideViewsBelow` 在 Session 创建时快照；刷新只替换业务 Data，不重新应用配置。持续共享状态放在 Model 或 System，不把 Data 当作打开后的命令通道。

View 与其 Data 类型一一对应；初始化和刷新直接转换为声明的 Data 类型，不重复做类型预检。类型不匹配属于配置或代码 Bug，由转换抛错；初始化失败仍走统一释放路径。

加载期间的重复打开只替换待初始化的 Data，实例交付后使用最新值初始化一次。已打开界面的每次刷新同步执行一次 View.Refresh，未被关闭时再通知 Extension.Refreshed；不维护 Data 版本或刷新追赶循环。

- `LayerId` 未指定时由 Host 解析到默认业务层。
- `Options` 默认为空 Mask，只表达布尔横切能力。
- `AllowMultiple` 默认为 false；同 Location 默认单实例。
- `HideViewsBelow` 只影响同一业务 Layer 中更早打开的 View。
- 需要参数的能力通过 Data、Model、System 或独立配置表达。

这些 Option 均由 Gameplay Extension 实现，Framework 不理解其业务含义。

### 打开、显示与释放

`OpenView` 的稳定阶段为：校验与解析配置 → Extension `BeforeOpen` → 复用刷新或创建 Session → Provider 实例化 → Bind/Initialize → 提交 Layer → Show/Hide → 最终 Release。

Provider、Prefab、Data、Bind 或 Initialize 失败会记录日志并完整释放，清理完成后状态为 Failed。Show/Hide 表现异常属于实现 Bug：记录日志，不发送成功完成通知，不强制应用终态或自动恢复；关闭与取消仍按显式请求执行。BeforeOpen 抛出异常会直接拒绝打开，其余 Extension 回调独立捕获异常，单个 Extension 失败不阻断后续 Extension。

Extension 注册顺序执行打开与显示阶段，释放和 Shutdown 按反序。`Released` 撤销单 Session 所有权，`OnShutdown` 清理输入模式、Tween 等全局残留。生命周期回调允许关闭当前界面和触发可见性重算，但不得重入打开或刷新同一 Session；Session 通过自身状态检查收束关闭，Layer 通过 dirty 重算重新应用目标可见性，UIView 通过 VersionGate 使旧过渡失效。

### Layer 与实例约束

`UILayerRoots` 支持单 Root 或 Primary/Secondary 双 Root。Layer 负责换 Root、物理 Root 内稳定 sibling 排序和目标可见性；逻辑 Layer 不依赖物理 Root。

同 Location 且 `AllowMultiple == false` 时，顶部实例被刷新复用；非顶部实例由新实例替代。`AllowMultiple == true` 时每次创建独立 Session。关闭同层全部 View、隐藏 HUD 等行为由 Gameplay Extension 实现，不属于 Framework 实例规则。

### View 状态与等待

`UIViewToken.GetStatus()` 返回无堆分配的只读 `UIViewStatus`，包含生命周期 `UIViewLifecycle` 与可见性 `UIViewVisibilityState`：

- 生命周期：None、Loading、Opened、Closing、Closed、Failed。
- 可见性：Hidden、Showing、Shown、Hiding。
- Opened 表示实例校验、初始化、最新 Data 应用和 Layer 提交完成，不要求当前可见。Failed 只表达打开失败，不表达动画 Bug。
- Closed、Failed 在 View、Layer、Extension、Provider 和 Host 注册清理完成后发布，可见性为 Hidden。释放阶段内部防止重复进入，对外仍为 Closing。
- 默认 Token 返回 None；IsValid 表示曾指向一次打开，不代表仍存活。Token 直接引用 Session，Host 注销后仍可查询终态；Session 清空 View、Data 和共享依赖引用，不保留公开异常对象。

`UIView.IsTargetVisible` 表示 View 的目标可见性，在 Show 开始时为 true、Hide 开始时即为 false，不能据此判断动画是否完成；显示完成应查询 Token 状态的 Shown。

`UIView` 是动画版本的唯一所有者，在生命周期回调、异步动画及显隐切换后校验版本，有效完成直接调用 Session 的 `CompleteShow` 或 `CompleteHide`。无动画时允许同步完成；View 通知前完成自身收尾，通知后不再访问可能已经释放的实例。

普通遮挡 Hide 不永久关闭。永久关闭先进入 Closing，再完成有效 Hide、OnClose、Layer 移除与可见性重算、Extension 反序释放、Provider Release 和 Host 注销。Immediate Close 同步使旧过渡失效并收束隐藏；立即结束已有隐藏不重复派发 BeforeHide 和 OnHide。业务通过 Token、UISystem.CloseAll 或 CloseSelf 关闭，不手动销毁或归还 GameObject。

### 状态轮询与等待扩展

核心不维护完成源、等待者或历史完成记录。`UIViewTokenExtensions` 提供返回 `UniTask<bool>` 的可选等待方法：先同步检查，再按 Update 采样当前状态。

| 方法 | true | false | 继续等待 |
| --- | --- | --- | --- |
| WaitForReadyAsync | Opened | None、Closing、Closed、Failed | Loading |
| WaitForShownAsync | Opened 且 Shown | None、Closing、Closed、Failed | 其余状态 |
| WaitForClosedAsync | Closed、Failed | None | 其余状态 |

等待采用当前状态语义：两次采样之间短暂出现的 Shown 可以被错过；普通遮挡后可继续等待下一次显示。调用方必须处理 false，不把无法达到的前置状态当作成功。调用方 token 只取消自身等待，遵循 UniTask 取消约定，不取消 UI 实例。逐帧驱动方直接调用 GetStatus，不必再启动等待任务。

### 模糊与 Camera Stack

UIRoot 使用 Background/Foreground Canvas 与两台 Overlay Camera，把模糊边界以下内容和清晰前景分开。`UIViewBlurExtension` 从所有可见 Blur View 中选择排序最高的边界，路由 Root 并启停通用模糊 Feature 与前景相机；边界切换不重播淡入淡出。

隐藏主相机只暂时移除 Base Camera 的场景 CullingMask，不禁用 Base Camera，确保 Overlay Stack 继续合成。多个所有者通过 Token 计数，最后一个撤销后恢复。渲染实现见 [Rendering](Rendering.md)。

### 输入与导航

`UIViewInputBlockExtension` 在 `BeforeShow` 获取玩法输入阻断，在 `BeforeHide` 和 `Released` 撤销；Loading Session 不阻断输入，多 View 通过 Token 所有权合并。

需要导航的 View 在根节点配置 `UIViewNavigation` 和根 `UIFocusScope`，并指定默认 `Selectable`。Extension 按 Layer 排序和 ViewId 选择当前活动 View；每个可见 View 都是路由边界，即使没有导航组件也会阻断低层 View 的语义命令。

子窗口通过 `PushFocusScope` 建立独占焦点作用域并保存 `UIFocusScopeToken`。取消非栈顶 Token 会同时移除其后代作用域。Scope 只管理 EventSystem 选择，不改变 `Selectable.interactable` 或鼠标遮挡。

Universal Fancy 适配器作为 Selectable 接入 EventSystem，复用列表已有的 Selection 和 ItemInteraction，不修改第三方包。Pointer hover、导航候选和业务激活保持三个独立状态。

### UI 绑定生成

`Gameplay.UI.Editor` 收集 `UIBindTag` 和 `UIBindCollectionTag` 生成绑定代码。Project Settings 统一配置 Binding、View、Widget 模板和多态规则；稳定 Key 通过固定哈希生成只读 ID，修改 Key 会使现有 Prefab 引用失效。

集合根的直接子节点按 Hierarchy 顺序组成数组项；每个数组字段必须在每项中恰好出现一次，类型和事件配置保持一致。集合根不嵌套。事件监听通过 lambda 调用 partial 方法，使业务回调尚未实现时生成代码仍可编译。

## 使用与扩展规则

- 新 View 继承 `UIView<TData>`，资源 Location 只写在 Data；复用子控件继承 `UIWidget`。
- 新横切行为定义不可变 Option 和单一职责 Extension，不把业务枚举或行为加入 Framework。
- View 的 Session 引用由框架绑定并在归还实例前解除，不向业务暴露；View 在 `OnClose` 清理订阅与长生命周期工作，池化 View 必须支持完整重复生命周期。
- View 与 Extension 的生命周期回调不得直接或间接重入打开、刷新同一个存活 Session；加载等待期间的普通重复打开仍会合并最新 Data。
- Extension 不跨生命周期保存 Context、Host 或其中的 Span，也不调用 `Host.Shutdown()`。
- Provider 必须遵守交付前后所有权边界，并保证成功交付的实例可由 Release 恰好归还一次。
- 新导航界面显式配置默认焦点；View 通过语义路由接收 Cancel 和上下文命令。

## 性能与排错

- Prefab 冷打开可能同步等待 Addressables；按 Location 缓存 operation，流程稳定点调用 Trim。
- Layer 可见性重算和 Location 收集复用集合，等待扩展不使用闭包或逐帧临时集合。
- 焦点丢失时检查当前路由边界、Scope 默认项和目标是否属于活动 Scope。
- 模糊异常时检查 Camera Stack、Root 分区、Feature Active 和所有权 Token。

## 验证

- 覆盖首次打开、单实例刷新、多实例、加载中关闭、初始化失败、动画反转和 Shutdown。
- 验证 Extension 正序进入、反序释放、异常隔离和重入后的最终状态。
- 验证双 Root 排序、模糊边界、主相机隐藏和多所有者恢复。
- 验证键鼠/手柄导航、嵌套 Scope、语义命令路由和玩法输入阻断。
- UI 修改后执行 Unity Editor 编译、绑定代码生成、Prefab 引用检查及 PlayMode 验证。
