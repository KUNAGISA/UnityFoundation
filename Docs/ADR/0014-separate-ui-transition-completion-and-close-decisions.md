---
status: accepted
---

# UI 过渡由 View 收束，Entry 决策与 Host 编排分离

UIViewHost 同时解释单次打开的状态组合、观察动画任务并协调实例与 Layer，使关闭和立即关闭路径容易重复。决定由 UIView 基类等待动画、校验过渡版本并通过绑定 Token 报告有效完成，由 UIViewEntry 集中关闭状态决策，Host 按动作统一执行 View、Extension、Layer 和 Provider 操作。具体 View 保留返回 UniTask 的动画扩展协议，资源加载和外部顺序等待继续使用 UniTask，使职责收敛而不增加动画实现者的完成通知义务。

当前接口、生命周期与等待语义见 [UI 系统](../Architecture/UI.md)。

## 备选方案

- Host 继续等待 Show/Hide 并解释所有状态组合：执行顺序直接，但动画观察与关闭判断跨越 Host、Entry 和 View，普通关闭与立即关闭需要分别维护。
- 动画扩展改为普通方法并要求具体 View 显式报告完成：便于直接接入动画回调，但每个实现都要保证完成通知不遗漏、不重复，并处理异常和打断；选择由基类统一完成这些工作。
- Entry 主动执行整个生命周期：可以缩短 Host，但 Layer、Extension 和资源仍需全局协调，会引入反向调用和重复所有权；Entry 保留一次打开记录的身份，只管理局部状态与决策。

## 影响

- 完成通知允许同步执行，通知返回前实例就可能已被释放；View 必须先完成自身收尾，通知后不再访问实例。
- 取消等待者也可能同步重入 Close 或 Shutdown；Entry 在取消 Shown 等待后、Host 在隐藏回调前后复查状态，旧过渡由 View 的版本校验失效。
- 关闭决策区分首次立即隐藏与立即结束已有隐藏，避免重复派发 BeforeHide 和 OnHide；OnClose 与最终归还仍经过 Host 的唯一释放入口，Closed 等待完整释放后才完成。
