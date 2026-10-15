---
status: accepted
---

# 角色动画由 AnimationAbility 统一仲裁

输入移动与演出移动都可能提出动画请求。若各处直接调用 Animator，调用顺序会决定最终画面，演出结束也难以恢复当前基础动画。因此由 `AnimationAbility` 统一处理基础请求与可取消的临时覆盖；`CharacterEntity` 在 Motor 结算后按实际位移设置基础请求。

## 备选方案

- 各运动生产者直接调用 Animator：实现简单，但路线阻挡和优先级覆盖时，动画可能与最终位移不符。
- 建立完整角色动画状态机：能支持复杂转换，但当前只有基础移动和临时覆盖，不需要额外状态层。

## 影响

播放接口与 YH 的 Spine 版本一致，Animator 的 `Idle`、`Move` 及覆盖状态需由 Controller 配置。过渡通过 Animator 第 0 层的 `CrossFadeInFixedTime` 实现；每次请求的 `loop` 意图不能覆盖 AnimationClip 的循环设置，实际循环与非循环末帧保持由动画资源配置。现有测试角色尚无 Animator，动画请求不会阻止其运动；现行接口见 [Entity 系统](../Architecture/Entity.md#角色动画)。
