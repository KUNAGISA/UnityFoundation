---
status: accepted
---

# 单次 UI 生命周期由 Session 收束，外部按状态轮询等待

Host 执行动作、Entry 决定状态的拆分使一次打开或关闭需要跨对象多次往返，Host 仍需理解局部状态组合。决定以 UIViewSession 直接替代 Entry，集中单次打开的状态与执行；Host 只负责全局协调，Layer 独立重算可见性，Token 引用 Session 并保留终态。外部等待改为读取当前状态的扩展方法，移除核心完成源，逐帧调用方直接查询状态。

当前接口和生命周期见 [UI 系统](../Architecture/UI.md)。本决策替代 [ADR-0014](0014-separate-ui-transition-completion-and-close-decisions.md)。

## 备选方案

- 保留 Entry 的决策与 Host 的执行拆分：局部动作需要共享分步协议，继续放大 Host；选择由 Session 一处完成状态与副作用。
- 使用 UIViewController：与主体框架 Controller 分层含义重叠，采用表达单次打开会话的 Session。
- 将等待完成源移到单独辅助对象：保留了核心通知等待者的义务；当前调用方可以按状态采样，因此选择轮询扩展，不维护历史完成记录。
- 将动画改为协程或每个 View 手动通知：不会消除打断和重入规则；保留 View 基类的 UniTask 动画协议及版本校验。

## 影响

- 等待返回 bool；无法达到目标的关闭、打开失败或无效凭证需要调用方处理，主动取消只结束自身等待。
- 状态采样可以错过短暂 Shown，普通遮挡也不再取消 Show 等待；需要逐帧准备的消费方直接查询，不承诺捕获历史过渡。
- Token 可保留 Session 终态；永久释放必须清空实例、Data 和共享依赖引用，晚到实例通过请求捕获的 Provider 归还。
- 动画异常属于 Bug，内部记录后不发送成功通知，也不增加恢复路径；等待可以持续至调用方取消或显式关闭。
- 初始化调用栈内重入 Shutdown 时，关闭先标记、实例退出初始化后释放；共享 Extension 与 Layer 必须等最后一个 Session 注销后再清理。
