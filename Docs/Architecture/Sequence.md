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

仅当播放器 Tick 正常返回且自然完成时，SequenceSystem 才会在释放旧播放的输入模式、播放器、Lua 线程和文本源 Token，并将播放状态设为 `Succeeded` 后，通过 `ActionSystem.Submit` 提交结束行为。每次播放最多提交一次；同一剧情再次播放并正常结束时会再次提交。提交后不再清理旧播放，结束行为可以安全启动下一段剧情。

替换、主动取消、离场、流程退出、系统关闭、加载或播放异常均不执行结束行为；所有收尾路径清空本次保存的行为 ID。演出表重载沿用取消当前播放并清理缓存的语义，不执行结束行为。

结束行为沿用 ActionSystem 的同步分发和失败处理，不等待持续行为结束，执行失败不改变已经成功完成的剧情状态。

System 保持单次播放的完整所有权，不额外创建播放操作对象。`Play` 先停止旧播放，再校验配表，仅在配置有效时创建新播放的 Token；随后同步创建并接管 Lua 协程，通过 `ActivateSequenceTextSource` 激活文本源并保存 Token，最后创建 Player 并调用 `Start()`。启动失败记录异常并统一收尾，将本次播放标记为 `Failed`。同步启动不承诺对白已显示，View Preparation 和后续剧情仍由 Tick 推进。

`StopCurrentPlayback` 统一收尾输入、播放器、协程和文本源 Token。收尾先将旧资源移交到局部变量并清空当前身份，再逐项释放；文本源 Token 只能解除自己的激活，不会清除后续播放的文本源。输入模式取消、Player 停止、GCHandle 释放和文本源 Token 取消遵循各自的不抛异常契约，直接调用。Tick 的正常、异常出口保留本次播放 Token 检查。

### 脚本加载与缓存

`SequenceSystem` 持有普通对象 `SequenceScriptCache`，向其注入 `IAssetService` 和借用的 Lua 主状态；缓存类不注册为架构模块，不负责文本、立绘、输入或 Player 生命周期。脚本地址继续使用 `Sequences/<location>`。

`PreloadAsync(location, cancellationToken)` 按脚本路径合并共享预加载，缓存 Lua registry 中的编译函数引用，编译后释放原始 TextAsset operation。调用方取消只结束自身等待；共享预加载由缓存作用域管理。加载或编译失败会移除对应条目，后续请求可以重试。

`CreateThread(location)` 同步返回已装入脚本函数的 Lua 协程：命中预加载缓存时使用编译引用，未命中时经 `IAssetService.LoadAssetSync` 加载并编译。普通播放不自动写入缓存。未缓存 operation 在编译并创建协程后、返回前释放；缓存引用仍归缓存类所有。创建失败时缓存类关闭尚未交接的协程。

`SequenceLuaAPI.RetainThread` 以协程自身为 registry 键建立强引用，创建方法在返回前恢复 Lua 主状态栈。Player 创建后，`BindPlayer` 将同一条目更新为 GCHandle 指针。所有释放路径统一调用 `CloseThread`，先关闭协程，再删除 registry 条目，随后由 System 释放 GCHandle。关闭返回 Lua 错误时只记录日志并继续删除条目，不向调用方抛异常；已关闭的协程不能再次访问。

`SequenceSystem.PreloadAsync(sequenceId, cancellationToken)` 从演出表取得路径，通过 `UniTask.WhenAll` 同时等待脚本和剧情文本预加载。找不到演出配置时记录 Error 并直接返回，不抛异常；资源加载或编译失败继续向调用方传播。调用方取消只终止自身等待，共享加载由各自缓存作用域管理。System 不读取缓存条目、编译引用或代次。后续场景 NPC 对话可在既有 `IScenePreload` 阶段调用该入口，本框架不自行推导场景的剧情清单。

剧情播放、缓存入口和取消均遵循主线程调用契约，资源完成回调及 PlayerLoop 在主线程恢复等待，不主动切换线程。正式流程从 Launch 完成全局初始化，并在场景预加载完成后才允许播放或切换语言；播放、语言重载不与预加载并发执行，不同步阻塞等待未完成的 UniTask。

离场、流程退出及演出表重载先停止播放，再清理脚本和文本缓存。清理取消本批工作、释放编译引用并允许后续复用；旧预加载通过取消令牌、脚本缓存代次及条目身份阻止回写或移除新条目。Shutdown 先停止播放、清理文本缓存并 Dispose 脚本缓存，再关闭 Lua 主状态。

### 剧情文本缓存与激活

`LocalizationSystem.PreloadSequenceTextAsync(location, cancellationToken)` 按路径合并当前语言的共享预加载，缓存已解析的 `LocalizationCatalog`，不改变当前文本源、不发送 `CatalogChanged`。加载或解析失败移除条目，后续可以重试。原始 TextAsset operation 在解析后释放；动态文本仍在 `Get` 时求值。

`ActivateSequenceTextSource(location)` 命中缓存时直接引用目录，未命中时同步加载，仅供当前激活使用、不自动写入预加载缓存。每次激活替换当前目录、发送一次 `CatalogChanged` 并返回新的 `SequenceTextSourceToken`；相同路径也产生新的激活身份。

`SequenceTextSourceToken` 是普通只读结构体，不实现 `Aoiro.ICancelToken`，通过 `IsValid` 和 `Cancel()` 管理本次激活。默认、过期和重复取消无效果；有效取消只解除当前绑定并发送刷新通知，不删除预加载缓存、不恢复旧文本源。`ClearSequenceTextCache()` 取消共享预加载、清空缓存并解除当前激活，使旧 Token 失效。

### ADV 接入

`Module/Adv` 按 `Dialogue`、`Choice`、`Portrait`、`View` 分类，Session 与对应的通知、Token 和局部状态就近放置；命名空间统一为 `Gameplay.Sequence`。

按能力拆分及保留 Action 驱动、Preparation 等待的取舍见 [ADR-0019](../ADR/0019-split-adv-sessions-and-widgets-with-action-driven-dialogue.md)。

ADV 按顺序注册 `AdvDialogueSession`、`AdvChoiceSession`、`AdvPortraitSession` 和 `AdvViewSession`，均为随 Player 运行的 `SequenceModule<ISequenceContext>`，每段剧情各创建一份并复用，不注册为全局架构模块。前三者集中各自状态与操作，Action、Yield 和 Lua 指令直接获取对应 Session；`AdvViewSession` 只负责 View 的准备与释放、界面生命周期和推进请求，并向 View 提供三个 Session 的引用。

`AdvViewPreparation` 只等待 View 就绪并检查 `IsViewBound`，准备完成即清理自身等待；View 的 `UIViewToken` 仍归 `AdvViewSession` 持有，直到播放自然完成、停止或异常时取消。三个 Session 先完成准备，`AdvViewSession` 再打开 View；清理按注册反序关闭 View 并解绑 Widget，再释放各能力的状态和本地化订阅。View 通过 `NotifyViewBound`、`NotifyViewClosed` 回报生命周期，全部 Widget 绑定和首次刷新成功后才报告绑定完成；播放期间意外关闭 View 仍使播放失败。

`AdvDialogueSession.Begin` 解析本地化文本和配表参数，并统一拥有对白身份、内容修订、测量、计时、显示及完成状态，不依赖 TMP。`DialogueAction` 通过 `Begin`、`Tick`、`Advance` 接入原有调度时点，Session 的 Module Tick 不重复计时。`IsCurrent(version)` 只判断对白身份，`IsCompleted` 只读属性表示当前对白的完成状态；Action 在身份失效或对白完成时结束。旧对白 Token 的 Tick 与 Advance 无效。

`AdvView` 组合三个独立的嵌套 Prefab：`AdvDialogueWidget`、`AdvChoiceWidget`、`AdvPortraitWidget`。Widget 通过显式 `Bind`、`Unbind` 管理各自订阅，支持重复绑定与部分初始化后的清理；不相互引用。对白 Widget 负责正文、姓名、继续提示和 TMP 测量，选项 Widget 负责选项实例复用及列表内部导航，立绘 Widget 负责资源加载、实例、视觉状态应用与排序。解绑清理创建的实例及加载资源。各 Widget 在自身 Prefab 内维护生成绑定，View 仅序列化三个 Widget 引用及协调根导航。

`AdvDialogueSession.Changed` 使用 `AdvDialogueChangeFlags` 的 `Text`、`CharacterName`、`AdvancePrompt` 标记，`All` 仅包含这三项。对白 Widget 的显示字数读取 `VisibleCharacterCount`，通过 `ReportCharacterCount(version, contentRevision, characterCount)` 回报具体版本、具体内容的字符数。尚未测量时不推进对白，过期测量不会回写。Session 订阅语言变化并重新解析内容：进行中的对白清空测量并重置进度，已结束但仍留屏的对白保持完成并完整显示新译文，不重新启动 Action。

`AdvChoiceSession` 私有保存选项 Key，对外只提供 `Count`、`Token` 和选择结果，通过 `Show`、`Select`、`TryGetSelectedIndex`、`Hide` 管理选择，通过 `GetText(index)` 解析当前语言文本；其无参数信号 `Changed` 通知选项与语言刷新。旧 Token 不能选择或隐藏新选项。选项 Widget 先订阅并刷新，View 再订阅同一信号，确保焦点指向已可用的目标。有有效选项时，View 禁用对白推进并聚焦首个选项；选项为空或隐藏时恢复推进按钮及焦点。对白 Widget 的点击经 `AdvViewSession.RequestAdvance` 缓存，在 Module Tick 中调用 `Player.Advance()`，保留对当前 Action 和 Yield 的统一推进语义。

主动切换语言由同步 `SetLanguageCommand` 编排：相同语言直接返回，否则先调用 `GlobalSettingModel.SetLanguage(language)` 原子保存语言设置，再调用 `LocalizationSystem.Reload()` 按当前设置同步加载全局、所有预加载剧情目录及未缓存的当前剧情目录。相同路径只加载一次，完成后替换目录并统一发送 `CatalogChanged`；当前文本源 Token 保持有效，Lua 缓存不变。System 不写入语言设置，不拆分准备与提交接口，不做失败回滚；保存或加载异常直接向调用方传播，资源 operation 始终释放。切换不申请专用输入模式或 Procedure 暂停。启动初始化和显式预加载继续异步，剧情文本激活同步执行；不为预加载期间切语言增加请求版本或并发状态。

同步切换与 Command 编排的决策背景见 [ADR-0017](../ADR/0017-synchronous-language-switch-command.md)，剧情预加载、同步启动与文本源 Token 的取舍见 [ADR-0018](../ADR/0018-preload-sequence-resources-and-activate-text-with-token.md)。

立绘状态的修改、通道 Token 和通知集中在 `AdvPortraitSession`。Lua 与 Tween Action 经其方法写入，立绘 Widget 订阅 `Changed` 并读取 `Entries`；`AdvPortraitEntry.VisualState` 对外为只读引用，避免绕过通道与通知修改。`AcquireChannel` 获取指定动画通道的控制权；`GetVisualStateOrDefault` 在立绘缺失时返回默认状态。翻转设置使用 `SetHorizontalFlip`、`SetVerticalFlip`，旋转设置使用 `SetRotationDegrees`，单位为度。`AdvPortraitVisualState.Default` 提供初始视觉状态（不透明、白色、单位缩放、无翻转）。零版本通道 Token 无效，不能修改包括 0 号立绘在内的任何条目；相同通道的新动作替换旧动作，不同通道可并行。

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
- 验证未缓存同步播放、预加载后播放、同地址预加载合并、单个等待者取消、失败重试，以及 Clear/Dispose 后旧请求不回写且资源正确释放。
- 验证反复播放后主状态栈深度和 registry 协程条目恢复、文本或 Player 启动失败时协程被关闭，以及 Lua 关闭错误只记录日志。
- 验证文本预加载不改变当前目录；旧 Token、重复取消不影响当前激活，解除后再次激活仍命中缓存。
- 验证切语言重载全部预加载目录，同路径只加载一次，当前 Token 仍有效；离场、表重载及 Shutdown 清理两类缓存。
- 验证对白替换、过期测量、手动推进、自动结束、语言切换与已完成对白留屏，以及立绘即时设置、同通道替换、不同通道并行和通知订阅清理。
