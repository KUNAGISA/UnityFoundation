---
status: accepted
---

# 每帧时间只采样一次

同一逻辑帧内的子系统必须观察一致的时间，因此 `GameplayArchitecture` 在 EarlyTick 开始时只从 Unity 时间生成一次不可变 `FrameTime`，并把同一快照传给 Procedure、Scheduler 和 Camera 的各 Tick 阶段。需要暂停或独立缩放的领域从全局快照派生并持有自己的时间域，而不是重新采样 Unity 时间。

## 备选方案

- 各子系统在 Tick 中直接读取 `UnityEngine.Time`：调用简单，但会隐藏时间来源，并使测试、暂停和跨阶段一致性难以保证。
- 由一个 System 暴露全局时间供其他 System 查询：会扩大跨 System 访问并模糊帧驱动边界。

## 影响

所有帧驱动接口都显式接收 `in FrameTime`，调用方需要明确选择全局时间或派生时间域。新增时间域必须定义暂停、缩放和累计时间语义；当前规则见 [框架架构](../Architecture/Architecture.md)。
