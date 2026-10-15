# UI 系统

UI 框架以 `UIViewHost` 统一编排 View 实例、可见性、过渡、Layer 和横切 Extension；Gameplay 通过 `UISystem` 接入资源、相机、输入与具体 UI 配置。

## 职责与边界

- `UIViewHost` 位于 Framework，是生命周期的唯一编排者，通过 `IUIViewInstanceProvider` 请求和归还实例。
- `UIViewEntry` 只保存一次打开的生命周期、可见性、Data 版本和等待状态，不反向驱动 Host、Layer、View 或 Extension。
- `UILayer` 维护层内顺序、Root、遮挡和目标可见性；实际 Show/Hide 由 Host 执行。
- `UISystem` 位于 Gameplay，创建 UIRoot、持有 Prefab 缓存、注册 UI Camera Stack、注入 Extension 并提供业务入口。
- 横切行为由 `UIViewExtension` 承接，不写入 Framework 的 View 基类。

业务代码只通过 `UISystem` 打开、查询和关闭 UI，不直接创建 Host、Entry 或驱动 View 生命周期。

## 主要类型与入口

- `UIViewHost`、`UIViewEntry`、`UIViewToken`、`UIView<TData>`
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

Host 通过 Provider 请求实例，并传入目标 Layer 的 Primary Root 和 Host 生命周期 token。Provider 成功返回的必须是目标 Parent 下、未激活的根实例；返回成功即把所有权交给 Host，Host 最终按 Location 恰好调用一次 `Release`。Provider 在交付前取消或失败时自行清理已创建实例。

Loading Entry 被关闭后 Host 立即释放 Entry；共享 Provider 请求可以继续，晚到实例会被立即归还。Host Shutdown 统一取消仍在运行的请求。

Gameplay 的 `UIViewPrefabCache` 缓存每个 Location 的 Prefab operation；当前实例释放只销毁实例，不立即释放缓存。`TrimUnusedViewAssets` 根据 Host 存活 Entry 收集在用 Location，再释放未使用 operation，不维护 View 级引用计数。

### View Data 与 Option

每个 View 定义自己的 `UIViewData` 子类。`Location`、`LayerId`、`Options`、`AllowMultiple` 和 `HideViewsBelow` 在 Entry 创建时快照；刷新只替换业务 Data，不重新应用配置。持续共享状态放在 Model 或 System，不把 Data 当作打开后的命令通道。

- `LayerId` 未指定时由 Host 解析到默认业务层。
- `Options` 默认为空 Mask，只表达布尔横切能力。
- `AllowMultiple` 默认为 false；同 Location 默认单实例。
- `HideViewsBelow` 只影响同一业务 Layer 中更早打开的 View。
- 需要参数的能力通过 Data、Model、System 或独立配置表达。

这些 Option 均由 Gameplay Extension 实现，Framework 不理解其业务含义。

### 打开、显示与释放

`OpenView` 的稳定阶段为：校验与解析配置 → Extension `BeforeOpen` → 复用刷新或创建 Entry → Provider 实例化 → Bind/Initialize → 提交 Layer → Show/Hide → 最终 Release。

Provider、Prefab、Data、Bind 或 Initialize 失败会使本次打开失败并完整回滚。Show/Hide 表现异常只记录日志，Host 仍完成逻辑状态；各 Extension 回调独立捕获异常，单个 Extension 失败不阻断后续 Extension。

Extension 注册顺序执行打开与显示阶段，释放和 Shutdown 按反序。`Released` 撤销单 Entry 所有权，`OnShutdown` 清理输入模式、Tween 等全局残留。回调允许重入 Close、Refresh 和可见性重算；Host 通过 Entry 状态、`TransitionVersion` 和 dirty 重算使旧任务失效。

### Layer 与实例约束

`UILayerRoots` 支持单 Root 或 Primary/Secondary 双 Root。Layer 负责换 Root、物理 Root 内稳定 sibling 排序和目标可见性；逻辑 Layer 不依赖物理 Root。

同 Location 且 `AllowMultiple == false` 时，顶部实例被刷新复用；非顶部实例由新实例替代。`AllowMultiple == true` 时每次创建独立 Entry。关闭同层全部 View、隐藏 HUD 等行为由 Gameplay Extension 实现，不属于 Framework 实例规则。

### View 状态与等待

Entry 生命周期为 Loading、Opened、Closing、Releasing、Released；可见性独立为 Hidden、Showing、Shown、Hiding。`UIView` 是动画版本的唯一所有者，Host 只消费有效完成结果。

- `WaitForReadyAsync` 等待实例化、校验、Bind 与 Initialize，不等待 Show。
- `WaitForShownAsync` 等待有效 Show 及 `ShowCompleted`；被 Hide、Close 或 Shutdown 打断时取消。
- `WaitForClosedAsync` 等待 Hide、Extension、Layer、输入和 Provider Release，不承诺池化实例已销毁。
- 等待 token 只取消调用方等待，不取消 UI 实例。

永久关闭执行有效 Hide、`OnClose` 和 Release；普通遮挡 Hide 不执行 `OnClose`。Immediate Close 终止当前过渡并立即完成清理。业务通过 `UIViewToken`、`UISystem.CloseAll` 或 `CloseSelf` 关闭 View，不手动销毁或归还 GameObject。

### 模糊与 Camera Stack

UIRoot 使用 Background/Foreground Canvas 与两台 Overlay Camera，把模糊边界以下内容和清晰前景分开。`UIViewBlurExtension` 从所有可见 Blur View 中选择排序最高的边界，路由 Root 并启停通用模糊 Feature 与前景相机；边界切换不重播淡入淡出。

隐藏主相机只暂时移除 Base Camera 的场景 CullingMask，不禁用 Base Camera，确保 Overlay Stack 继续合成。多个所有者通过 Token 计数，最后一个撤销后恢复。渲染实现见 [Rendering](Rendering.md)。

### 输入与导航

`UIViewInputBlockExtension` 在 `BeforeShow` 获取玩法输入阻断，在 `BeforeHide` 和 `Released` 撤销；Loading Entry 不阻断输入，多 View 通过 Token 所有权合并。

需要导航的 View 在根节点配置 `UIViewNavigation` 和根 `UIFocusScope`，并指定默认 `Selectable`。Extension 按 Layer 排序和 ViewId 选择当前活动 View；每个可见 View 都是路由边界，即使没有导航组件也会阻断低层 View 的语义命令。

子窗口通过 `PushFocusScope` 建立独占焦点作用域并保存 `UIFocusScopeToken`。取消非栈顶 Token 会同时移除其后代作用域。Scope 只管理 EventSystem 选择，不改变 `Selectable.interactable` 或鼠标遮挡。

Universal Fancy 适配器作为 Selectable 接入 EventSystem，复用列表已有的 Selection 和 ItemInteraction，不修改第三方包。Pointer hover、导航候选和业务激活保持三个独立状态。

### UI 绑定生成

`Gameplay.UI.Editor` 收集 `UIBindTag` 和 `UIBindCollectionTag` 生成绑定代码。Project Settings 统一配置 Binding、View、Widget 模板和多态规则；稳定 Key 通过固定哈希生成只读 ID，修改 Key 会使现有 Prefab 引用失效。

集合根的直接子节点按 Hierarchy 顺序组成数组项；每个数组字段必须在每项中恰好出现一次，类型和事件配置保持一致。集合根不嵌套。事件监听通过 lambda 调用 partial 方法，使业务回调尚未实现时生成代码仍可编译。

## 使用与扩展规则

- 新 View 继承 `UIView<TData>`，资源 Location 只写在 Data；复用子控件继承 `UIWidget`。
- 新横切行为定义不可变 Option 和单一职责 Extension，不把业务枚举或行为加入 Framework。
- View 保存打开 Token，并在 `OnClose` 清理订阅与长生命周期工作；池化 View 必须支持完整重复生命周期。
- Extension 不跨生命周期保存 Context、Host 或其中的 Span，也不调用 `Host.Shutdown()`。
- Provider 必须遵守交付前后所有权边界，并保证成功交付的实例可由 Release 恰好归还一次。
- 新导航界面显式配置默认焦点；View 通过语义路由接收 Cancel 和上下文命令。

## 性能与排错

- Prefab 冷打开可能同步等待 Addressables；按 Location 缓存 operation，流程稳定点调用 Trim。
- 可见性重算和 Location 收集复用集合，不在热路径使用 LINQ 或临时集合。
- 焦点丢失时检查当前路由边界、Scope 默认项和目标是否属于活动 Scope。
- 模糊异常时检查 Camera Stack、Root 分区、Feature Active 和所有权 Token。

## 验证

- 覆盖首次打开、单实例刷新、多实例、加载中关闭、初始化失败、动画反转和 Shutdown。
- 验证 Extension 正序进入、反序释放、异常隔离和重入后的最终状态。
- 验证双 Root 排序、模糊边界、主相机隐藏和多所有者恢复。
- 验证键鼠/手柄导航、嵌套 Scope、语义命令路由和玩法输入阻断。
- UI 修改后执行 Unity Editor 编译、绑定代码生成、Prefab 引用检查及 PlayMode 验证。
