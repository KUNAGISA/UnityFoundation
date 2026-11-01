# ActionSystem

ActionSystem 是行为总表的统一执行入口，以同步 FIFO 分发保证当前 Action 的同步逻辑完成后再执行重入提交的行为。

## 职责与边界

System 负责按 ActionId 查询配置、同步重入队列、失败日志及异常隔离；ActionResolver 接收 ActionDef 与 ActionContext，返回 bool。持续行为的驱动、资源独占、替换、取消和清理由 SequenceSystem 等所属系统管理。

Resolver 返回 true 表示行为已成功发起或完成空操作，不表示剧情等持续行为已经结束。System 不向提交方反馈执行结果；后续播放失败、被替换或离场取消不触发回滚或自动重试。

## 主要类型与入口

`void Submit(int actionId)` 只提交行为，不返回执行结果或提供结果回调。

空闲提交立即执行。Resolver 及其同步通知期间的重入提交按 FIFO 排队，最外层 Submit 在正常情况下处理完本轮全部提交后返回；重入 Submit 返回不代表该项已执行。不等待下一帧、不使用 UniTask、不等待持续行为结束。

## 生命周期与核心语义

System 作为常规 SystemBase 注册，在初始化时绑定 ActionContext，不持有流程帧任务。Submit 遵循架构生命周期契约，由调用方保证只在初始化完成后、Shutdown 前调用。每次实际执行查询最新配置，已经发起的行为归所属系统管理，行为表重载只影响后续执行。

缺失 ID、未知定义、同步发起失败或异常记录日志后继续下一项；生成分发器保留空定义作为空操作成功的语义，调用具体 Resolver 时定义已保证非空。ActionResolver 返回 false 时，ActionSystem 统一记录包含 ActionId 的发起失败日志，并保留具体 Resolver 的错误与异常诊断，调用方无需重复记录失败。执行异常由 ActionSystem 隔离，不阻塞下一项。

单轮最多执行 1024 个行为；超限时记录触发循环错误并清空未执行项。Shutdown 直接清空未执行项，不撤销已发起行为。

## 使用与扩展规则

- 配表行为统一经 Submit 发起，消费方不直接调用生成分发器绕开队列。
- 入队代表已决定执行，出队不重新判断触发条件或调用来源；提交前完成业务判断。队列只保证单个 Action 不被重入 Action 打断，不提供状态事务。
- SceneEventSystem 的登记初始化与条件批次应用同步完成，状态接收方及行为分发期间不允许同步清空或重建场景事件登记，不维护登记版本。登记持有的 Point 在登记有效期内保持有效，随离场或重建统一清理，不支持独立销毁 Point 后保留登记。条件变化回调由 ConditionSystem 的登记取消契约保护，不重复做消费方版本校验。
- 多次提交相同 ActionId 是独立执行；场景事件不维护完成记录或临时提交标记，是否可再次触发由入口时机、条件和业务变量决定。同一事件在前一次尚未分发完成时再次提交，也作为独立请求处理。
- SceneEventSystem 不跟踪分发结果或持续行为生命周期；条件状态由同步初始化和 LateTick 观察批次维护。Trigger 为 void，提交前即时复核条件，不更新观察结果或表现。
- 新行为使用 ActionResolverAttribute，通过专用 Generate Action 菜单生成，返回同步 bool，不返回执行监视器。生成与条件观察分别见[代码生成](DefinitionGeneration.md)、[条件模块](Conditions.md)。
- 设置游戏/全局变量、开关的四种行为分别调用对应 Command，配置校验与全局持久化继续沿用[变量与开关模块](Variables.md)。
- 独占冲突按所属系统的语义处理；剧情沿用新播放取消旧播放。角色运动能力与 Sequence 内部 Action/Yield 的生命周期不受此接口影响。

## 验证

验证当前 Resolver 完成后再分发重入提交项、跨来源 FIFO、同 ID 独立执行、缺失配置、发起失败、执行异常、循环保护和 Shutdown 清理。验证场景事件初始 Auto、条件 false→true、玩家进入触发及离场或重建时取消旧观察并清理登记；验证同批前一个行为修改条件后，后一个事件在提交前即时复核并拒绝失效条件。验证连续剧情请求仍立即替换，不等待旧播放结束。
