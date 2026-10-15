# 角色与实体

本上下文描述角色长期状态与场景实体之间的关系，用于区分业务身份、场景实例和持久化表示。

## Language

**Character State**:
一次游戏会话内持续存在的角色业务状态，不依赖角色是否位于当前场景。
_Avoid_: Character Data, Entity Data

**Entity**:
角色进入场景后产生的可更新实例，拥有独立的运行时 Entity ID 和场景生命周期。
_Avoid_: Character State, Save Data

**Ability**:
附着于 Entity、具有独立生命周期的行为。
_Avoid_: State, Aspect

**Save Data**:
角色状态在存档协议中的传输表示，不作为游戏运行时的领域对象。
_Avoid_: Character State, Entity

**Spatial State**:
角色在某个空间中的位置和旋转；玩家空间状态长期保存，NPC 空间状态仅属于当前场景 Entity。
_Avoid_: Transform Data

**Gameplay Tag State**:
场景 Entity 当前生效的标签及其来源计数。
_Avoid_: Gameplay Tag Set, Gameplay Tag Ability
