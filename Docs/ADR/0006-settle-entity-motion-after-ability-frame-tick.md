---
status: accepted
---

# Entity 在 Ability 更新后结算本帧运动

Movement 和 Performance 等 Ability 在 FrameTick 提交位移，而 Motor 原本在 LateTick 才更新 Spatial State，使 LateTick 消费者依赖其调用顺序。Entity 现在先更新自身全部 Ability，再调用 `OnFrameTick`；`CharacterEntity` 在此结算 Motor，使后续 LateTick 读取本帧位置。`OnLateTick` 同样位于该 Entity 的 Ability LateTick 之后，保持两个阶段的扩展规则一致。

## 备选方案

- 让 Motor 作为普通 FrameTick Ability 并注册在最后：当前 Ready 阶段仍可取得 Motor，但更新顺序会隐含依赖注册位置，后续新增 Ability 容易破坏结算时机。
- 增加 MotorSystem，在全部 Entity 更新后统一结算：能提供跨 Entity 的统一阶段，但目前只要求单个 Entity 内生产者先于 Motor，额外的注册、注销和全局顺序管理没有对应收益。

## 影响

钩子只约束单个 Entity 内的更新顺序，不提供跨 Entity 的同步屏障。Ability 的 FrameTick 仍读取上次结算的 `ActualDelta`，后续 LateTick 可读取本帧结果；现行接口和生命周期见 [Entity 系统](../Architecture/Entity.md) 与 [角色运动](../Architecture/Movement.md)。
