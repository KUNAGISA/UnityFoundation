# Sequence 框架

Sequence 是与具体脚本运行时和表现层解耦的逐帧调度框架，统一管理一次播放中的 Module、Preparation、Action、Yield、Routine、异常和清理。

## 职责与边界

框架负责播放状态机、帧调度、脚本恢复和所有扩展对象的生命周期。具体项目负责提供 `TContext`、脚本运行时适配器、Module 组合以及 Action/Yield 的业务含义；框架不定义 Gameplay Context、UI、输入或资源地址。

## 主要类型与入口

- `SequencePlayer<TContext>`：一次播放的状态与生命周期所有者
- `SequenceModule<TContext>`：播放期间持续存在的扩展模块
- `SequencePreparation`：Module 启动前的异步准备节点
- `SequenceAction<TContext>`：可并行执行的逐帧行为
- `SequenceYield<TContext>`：挂起 Routine 的等待条件，可返回整数结果
- `ISequenceRoutine`：脚本运行时与 Player 的适配边界
- `ISequenceAdvanceable`：Action 或 Yield 可选实现的主动推进能力

Player 在构造时通过 builder 注册 Module，通过 `Start()` 进入播放，通过 Tick 驱动，并通过停止或清理入口结束。

## 生命周期与核心语义

Player 状态为 `Created → Preparing → Running → Completed`：

1. `Start()` 进入 Preparing，按注册顺序调用 Module 的 `OnPrepare`，并行启动返回的 Preparation。
2. 所有 Preparation 完成的同一帧，Player 清理准备节点、调用全部 Module 的 `OnStart` 并进入 Running。
3. Running 每帧先 Tick Module，再启动和更新 Action、更新 Yield，最后按条件恢复 Routine。
4. Routine 结束且没有活动 Action 时自然完成；异常、主动停止和自然完成进入同一清理路径。
5. 清理取消未完成的 Preparation、Action 和 Yield，并按注册反序清理 Module；每个生命周期对象最多清理一次。

普通 `RequestYield` 只允许在 Running 调用，启动依赖使用 Preparation。全部准备节点无需等待时，首次 Module Tick、Action/Yield 调度和 Routine Resume 会在进入 Running 的同一帧发生。

Module 通过 Player 获取其他 Module：必需依赖使用 `GetModule<T>`，可选依赖使用 `TryGetModule<T>`。Module 的 Player 引用只在受控生命周期内有效，Cleanup 后不再保存或访问。

## Gameplay 配表播放接入

`SequenceSystem.Play(sequenceId)` 从演出表读取脚本、多语言文本路径和可选的 `onCompletedActionId`；结束行为 ID 在本次播放发起时保存。字段留空表示正常结束后不执行行为。

仅当播放器 Tick 正常返回且自然完成时，SequenceSystem 才会在释放旧播放的输入模式、加载任务、播放器、Lua 线程和多语言文本源，并将播放状态设为 `Succeeded` 后，通过 `ActionSystem.Submit` 提交结束行为。每次播放最多提交一次；同一剧情再次播放并正常结束时会再次提交。提交后不再清理旧播放，结束行为可以安全启动下一段剧情。

替换、主动取消、离场、流程退出、系统关闭、加载或播放异常均不执行结束行为；所有收尾路径清空本次保存的行为 ID。演出表重载沿用取消当前播放并清理缓存的语义，不执行结束行为。

结束行为沿用 ActionSystem 的同步分发和失败处理，不等待持续行为结束，执行失败不改变已经成功完成的剧情状态。

System 保持单次播放的完整所有权，不额外创建播放操作对象。`Play` 先停止旧播放，再校验配表，仅在配置有效时创建新播放的 Token。`PreparePlaybackThreadAsync` 加载、创建并接管协程，`StartPlayer` 创建并启动播放器，`StopCurrentPlayback` 统一收尾加载、输入、播放器、协程和文本源。收尾先将旧资源移交到局部变量并清空当前身份，再取消加载和逐项释放。输入模式取消、加载取消、Player 停止、GCHandle 释放和文本源清除遵循各自的不抛异常契约，直接调用。本次私有取消源仅连接资源加载与 UniTask 取消链，取消异常由对应异步等待处理，不注册会抛异常的业务回调。异步结果和 Tick 的正常、异常出口都核对发起时的 Token。共享文本源仅在没有当前播放时清除。

### 脚本加载与缓存

`SequenceSystem` 持有普通对象 `SequenceScriptCache`，向其注入 `IAssetService` 和借用的 Lua 主状态；缓存类不注册为架构模块，不负责文本、立绘、输入或 Player 生命周期。脚本地址继续使用 `Sequences/<location>`。

`PreloadAsync(location, cancellationToken)` 按脚本路径合并共享预加载，缓存 Lua registry 中的编译函数引用，编译后释放原始 TextAsset operation。调用方取消只结束自身等待；共享预加载由缓存作用域管理。加载或编译失败会移除对应条目，后续请求可以重试。

`CreateThreadAsync(location, cancellationToken)` 统一处理命中缓存、等待预加载和未缓存加载，直接返回已装入脚本函数的 Lua 协程。普通播放不自动写入缓存。未缓存 operation 在编译并创建协程后、返回前释放；缓存引用仍归缓存类所有。创建失败时缓存类关闭尚未交接的协程。

`SequenceLuaAPI.RetainThread` 以协程自身为 registry 键建立强引用，创建方法在返回前恢复 Lua 主状态栈。Player 准备好后，`BindPlayer` 将同一条目更新为 GCHandle 指针；等待文本期间也有强引用保护。所有释放路径统一调用 `CloseThread`，先关闭协程，再删除 registry 条目，随后由 System 释放 GCHandle。关闭返回 Lua 错误时只记录日志并继续删除条目，不向调用方抛异常；已关闭的协程不能再次访问。

System 并行开始文本与脚本准备，通过 `UniTask.WhenAll` 同时观察两个任务的取消和异常。脚本任务取得协程后检查播放身份并立即接管，在两项准备完成后于主线程再次确认播放身份，再初始化 Player。任一任务失败都进入播放收尾并取消另一任务。缓存生命周期令牌、代次和播放身份分别阻止使用失效缓存、旧任务回写和启动旧播放。协程在等待文本期间也归 System，通过统一播放收尾处理取消和文本加载失败；已失效的请求在交接前直接关闭协程。System 不读取缓存条目、编译引用或代次。

剧情播放、缓存入口和取消均遵循主线程调用契约，资源完成回调及 PlayerLoop 在主线程恢复等待，不主动切换线程。离场、流程退出及演出表重载先停止播放，再调用缓存 `Clear()`；Clear 取消本批工作、释放编译引用并允许后续复用。旧任务通过取消令牌、代次和条目身份阻止回写或移除新条目。Shutdown 先停止播放并 Dispose 缓存，再关闭 Lua 主状态。

### ADV 接入

`AdvSession` 是随 Player 运行的 Module，负责 View 的准备与释放、本地化订阅、推进请求和选项；其无参数信号 `ChoicesChanged` 只通知选项变化。View 通过 `NotifyViewBound`、`NotifyViewClosed` 回报生命周期，准备节点以 `IsViewBound` 判断绑定是否完成。Session 持有普通对象 `AdvDialogue` 与 `AdvPortraitRegistry`，不将它们注册进架构，每段剧情只创建一份并在其中复用。

Session 在 `BeginDialogue` 解析本地化文本并传入配表参数。`AdvDialogue` 统一拥有对白身份、内容修订、测量、计时、显示及完成状态，不访问架构或 TMP。`DialogueAction` 通过 `BeginDialogue`、`Tick`、`Advance` 接入原有调度时点。`IsCurrent(version)` 只判断对白身份，`IsCompleted` 只读属性表示当前对白的完成状态；Action 在身份失效或对白完成时结束。旧对白 Token 的 Tick 与 Advance 无效。

View 分别订阅选项、对白与立绘通知；`Dialogue.Changed` 使用 `AdvDialogueChange` 的 `Text`、`CharacterName`、`AdvancePrompt` 标记，`All` 仅包含这三项。View 只负责显示与 TMP 测量，显示字数读取 `VisibleCharacterCount`；通过 `ReportCharacterCount(version, contentRevision, characterCount)` 回报具体版本、具体内容的字符数。尚未测量时不推进对白，过期测量不会回写。语言变化由 Session 重新解析内容并调用 `UpdateLocalizedContent`：进行中的对白清空测量并重置进度，已结束但仍留屏的对白保持完成并完整显示新译文，不重新启动 Action。

立绘状态的修改、通道 Token 和通知集中在 `AdvPortraitRegistry`。Lua 与 Tween Action 经其方法写入，View 订阅 `Portraits.Changed` 并读取 `Entries`；`AdvPortraitEntry.VisualState` 对外为只读引用，避免绕过通道与通知修改。`AcquireChannel` 获取指定动画通道的控制权；`GetVisualStateOrDefault` 在立绘缺失时返回默认状态。翻转设置使用 `SetHorizontalFlip`、`SetVerticalFlip`，旋转设置使用 `SetRotationDegrees`，单位为度。相同通道的新动作替换旧动作，不同通道可并行。

Lua 中的 `set` 指令立即设置状态，`move` 等持续动作入队并在启动时读取起点；脚本在同一次恢复中先写 `move` 再写 `set`，表示先设置到该位置，再从该位置向动作目标移动。`wait` 保持 Action/Yield 的既有等待语义。

## 使用与扩展规则

- 新 Action、Yield、Module 和 Preparation 继承现有抽象基类，不创建平行生命周期接口。
- `TContext` 由接入方定义，框架不要求实现特定接口。
- Action/Yield 只通过 `ISequencePlayer<TContext>` 获取 Context、Module 和调度能力。
- 启动依赖使用 Preparation，不用普通 Yield 模拟准备阶段。
- 生命周期入口由 Player 驱动，扩展代码只重写受保护 Hook，不主动调用其他对象的生命周期方法。
- 高频通知携带稳定 ID 或紧凑值，避免每帧全量状态、闭包和临时集合。

## 验证

- 覆盖无需等待和跨帧 Preparation、并发 Action、Yield 返回值、Routine 自然结束与主动停止。
- 在 Module、Preparation、Action、Yield 和 Routine 各阶段注入异常，确认统一清理且每个对象只清理一次。
- 验证 Module 正序启动、反序清理，以及 Cleanup 后 Player 引用不可继续使用。
- 验证配表结束行为的空值、正常完成且仅提交一次、重复播放、取消及异常不提交，以及结束行为启动下一段剧情时不会被旧播放清理。
- 验证未缓存、已预加载和预加载中三条脚本准备路径、同地址预加载合并、单个等待者取消、失败重试，以及 Clear/Dispose 后旧请求不回写且资源正确释放。
- 验证反复播放后主状态栈深度和 registry 协程条目恢复、等待文本期间协程不被 GC、Lua 关闭错误只记录日志，以及取消加载后的异步结果不启动旧播放。
- 验证对白替换、过期测量、手动推进、自动结束、语言切换与已完成对白留屏，以及立绘即时设置、同通道替换、不同通道并行和通知订阅清理。
