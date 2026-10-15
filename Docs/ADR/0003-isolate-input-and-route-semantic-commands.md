---
status: accepted
---

# 隔离输入程序集并路由语义命令

输入资产、模式计算和设备状态需要独立于具体玩法与 View，因此它们位于 `Gameplay.Input` 程序集，主 `Gameplay` 只通过公开输入能力单向引用。`InputService` 的模式栈统一计算通道状态并绑定 UI Module，View 通过焦点路由接收语义命令；UI 导航和玩法输入阻断是两个可独立组合的策略。

## 备选方案

- 由业务 System 或 View 直接启停 ActionMap、订阅 Input Actions：会形成多个输入状态所有者，并把底层设备细节扩散到业务层。
- 将“显示可导航界面”自动等同于“阻断玩法输入”：配置更少，但无法表达只导航、不暂停玩法或只阻断玩法的界面。

## 影响

业务代码必须保存并取消模式与绑定 Token，不能从程序集外初始化或启停 Channel。新增设备动作先归属到稳定通道，UI 特有行为转换成语义命令后再路由；当前接口见 [InputService](../Architecture/InputService.md)。
