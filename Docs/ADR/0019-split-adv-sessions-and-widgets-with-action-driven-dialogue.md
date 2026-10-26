---
status: accepted
---

# 按能力拆分 ADV Session 与 Widget，并保留 Action 驱动对白

ADV 的对白、选项和立绘已有不同的状态规则与显示资源，继续由一个 Session 和一个 View 集中维护，会使本地化、输入协调、资源释放和显示逻辑相互牵连。决定将三类能力分别组织为随 Player 存活的 Session 和独立 Widget，由 AdvViewSession 与 AdvView 负责界面生命周期及跨 Widget 的输入协调；对白仍由 DialogueAction 驱动，启动就绪仍使用 SequencePreparation，以保留既有动作等待、推进和清理语义。

本决策替代 [ADR-0013](0013-keep-playback-ownership-and-reuse-adv-state.md) 中由单个 Session 持有普通对白和立绘状态对象的组织方式，继续沿用 SequenceSystem 对单次播放资源的完整所有权。现行接口、注册顺序与生命周期见 [Sequence 框架](../Architecture/Sequence.md)，Widget 和 View 的显示、导航及释放契约见 [UI 系统](../Architecture/UI.md)。

## 备选方案

- 保留单个 AdvSession 和 AdvView，仅在内部增加辅助对象或拆分文件：可以减少注册类型，但选项、本地化和界面资源仍集中在同一入口，调用方也需要经总 Session 访问各能力；选择按能力直接获取 Session，并让各 Widget 管理自己的显示资源与订阅。
- 由对白 Session 自行 Tick，保留 Action 作为等待入口：能够保留动作计数，但计时提前到 Module 阶段，使新对白启动当帧以及手动推进与计时的先后关系发生变化。当前没有脱离对白 Action 持续计时的需求，因此由 Action 提供时间，Session 集中状态和计时算法。
- 由对白 Session 自行 Tick 并删除 DialogueAction：接口看似更少，但对白将不再参与动作计数，现有 wait()、自然完成和统一推进都需要额外接入协议；保留 DialogueAction 表达一次仍在执行的对白。
- 将界面就绪等待与长期资源持有合并到外部传入的对象：外部需要承担准备失败、取消和所有权交接，且与现有 Player 准备阶段重复；保留短期 Preparation 等待和长期界面 Token 各自的生命周期。

## 影响

- 能力 Session 每段剧情各创建一份并复用，注册到 Player，不扩大为全局架构模块。界面 Session 在能力 Session 之后准备，清理时先处理界面释放，保持显示与状态的所有权清晰。
- DialogueAction 负责一次对白的启动、动作计数、完成判断和主动推进，Session 保留对白规则及完成后的留屏状态；拆分不会产生第二个对白计时入口。
- Preparation 完成只结束自身等待，不释放界面；界面资源继续由 AdvViewSession 持有，播放结束、停止或异常时统一收尾。
- 三个 Widget 以独立嵌套 Prefab 维护绑定、订阅及实例资源；跨 Widget 的推进开关和导航焦点由 AdvView 统一协调，不建立 Widget 之间的引用。
- 保留对白身份与内容修订、选项 Token 和立绘通道 Token 的既有隔离语义。目录和接口可以收敛，但不会据此修改脚本命令、时序或 Sequence 框架。
