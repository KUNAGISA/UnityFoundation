# 架构文档索引

本目录记录项目当前有效的框架接口、职责边界、生命周期、扩展方式和排错规则。历史取舍及其原因记录在 [ADR](../ADR/TEMPLATE.md)，角色与实体等领域词汇记录在 [CONTEXT.md](../../CONTEXT.md)。

## 核心架构

- [框架架构](Architecture.md)
- [初始化流程](Initialization.md)
- [Procedure 流程](Procedure.md)

## 框架能力

- [IAssetService](AssetService.md)
- [IStorageService](StorageService.md)
- [配表与生成代码](Tables.md)
- [ConditionSystem](Conditions.md)
- [ActionSystem](Actions.md)
- [InputService](InputService.md)
- [Entity 系统](Entity.md)
- [Gameplay Tag 系统](GameplayTags.md)
- [角色运动](Movement.md)
- [UI 系统](UI.md)
- [CameraSystem](Camera.md)
- [Rendering](Rendering.md)
- [Sequence 框架](Sequence.md)

## 架构决策

- [0001：正式初始化由 Launch 统一编排](../ADR/0001-launch-owned-initialization.md)
- [0002：每帧时间只采样一次](../ADR/0002-single-frame-time-snapshot.md)
- [0003：隔离输入程序集并路由语义命令](../ADR/0003-isolate-input-and-route-semantic-commands.md)
- [0004：存档读取与运行时恢复分离](../ADR/0004-separate-save-load-from-runtime-restore.md)
- [0005：角色面向编码于空间旋转](../ADR/0005-persist-character-facing-in-spatial-rotation.md)
- [0006：Entity 在 Ability 更新后结算本帧运动](../ADR/0006-settle-entity-motion-after-ability-frame-tick.md)
- [0007：角色动画由 AnimationAbility 统一仲裁](../ADR/0007-centralize-character-animation-playback.md)
- [0008：Gameplay Tag 使用稳定 ID、来源计数与同步批次通知](../ADR/0008-use-stable-source-counted-gameplay-tags.md)
- [0009：RuleSystem 统一处理 Condition / Action（已取代）](../ADR/0009-centralize-condition-observation-in-rule-system.md)
- [0010：拆分条件观察与同步行为分发，行为发起即视为成功](../ADR/0010-separate-condition-observation-and-action-dispatch.md)
- [0011：剧情正常结束后执行可选配表行为](../ADR/0011-run-configured-action-after-sequence-completion.md)
- [0012：将剧情脚本加载与编译缓存交给独立对象](../ADR/0012-isolate-sequence-script-loading-and-cache.md)
- [0013：保留剧情播放所有权并复用独立 ADV 状态](../ADR/0013-keep-playback-ownership-and-reuse-adv-state.md)
- [0014：UI 过渡由 View 收束，Entry 决策与 Host 编排分离](../ADR/0014-separate-ui-transition-completion-and-close-decisions.md)

## 新增文档

- 新增或重写框架文档时遵循 [架构文档模板](TEMPLATE.md)。
- 只有难以回退、缺少背景会令人疑惑且存在真实方案取舍的决策才新增 ADR，并遵循 [ADR 模板](../ADR/TEMPLATE.md)。
