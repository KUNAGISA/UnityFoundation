---
status: accepted
---

# Actor 显式调度行为并使用四向朝向

玩家和 NPC 由各自的 Prefab 根组件承担 Entity 生命周期，角色按输入、Performance、Motor、动画的固定顺序调度。角色朝向保存在运行时 Spatial State，由出生点提供初值并供 Animator 选择四向状态。根节点旋转用于实际空间旋转。

## 备选方案

- 保留 Ability 容器和额外的 CharacterActor 绑定。角色行为组合在创建时已知，显式依赖与调用可以直接表达装配和帧顺序。
- 继续用 Y 轴半周旋转编码朝向。这个约定能表示左右镜像，正面和背面需要独立语义，旋转根节点也会影响交互和碰撞子节点。

## 影响

现有 NPC 刷新表的旋转约定在生成入口转换。存档继续使用 v1，空间数据保留路线和位置，四向朝向用于运行时。现行接口见 [Entity 系统](../Architecture/Entity.md) 与 [角色运动](../Architecture/Movement.md)。
