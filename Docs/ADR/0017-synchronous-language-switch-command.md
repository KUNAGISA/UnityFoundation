---
status: accepted
---

# 主动语言切换同步执行并由 Command 编排

语言切换是低频操作，当前语言文本使用独立的本地资源分组，允许为简化流程接受短暂主线程停顿。最初决定由 `SetLanguageCommand` 同步调用 `GlobalSettingModel` 保存语言，再调用 `LocalizationSystem` 重载文本；设置的写入与跨模块编排归 Command，System 只负责文本加载、生效和刷新通知。语言资源属于开发期受控内容，加载错误直接暴露，不引入 Prepare/Apply、多阶段准备作用域或失败回滚协议。

后续将设置修改与显式保存分离的决定及取舍见 [ADR-0020](0020-separate-global-settings-mutation-from-persistence.md)，同步语言切换与 Command 编排的决定保持有效。

现行接口、通知与生命周期约束见 [Sequence 框架](../Architecture/Sequence.md)，同步加载的资源所有权与后端限制见 [IAssetService](../Architecture/AssetService.md)。

后续剧情预加载、同步启动及文本源激活所有权的调整见 [ADR-0018](0018-preload-sequence-resources-and-activate-text-with-token.md)。

## 备选方案

- 异步加载并暂停流程、阻断输入：可避免同步等待导致的停顿，但需要维护跨帧状态、取消及恢复；当前优先采用同步流程，后续实际体验测试确认卡顿影响体验时再评估异步方案。
- 由 LocalizationSystem 保存设置并完成全部切换：调用入口较少，但将全局设置写入和文本加载职责混在同一 System；选择让 Command 明确编排 Model 与 System。
- Prepare/Apply 分阶段提交：能够在加载失败时保持原设置，但增加中间状态与调用协议；当前选择开发期解决资源错误，不为切换失败增加恢复流程。

## 影响

- 启动初始化和显式剧情预加载继续异步。后续剧情播放改为同步启动，首次文本未预加载时同步读取；该调整延续本决策优先简化流程、实际卡顿影响体验时再评估异步的取舍。
- 最初先保存再重载时，文本重载失败不会回滚已保存的语言。持久化时机后续由 [ADR-0020](0020-separate-global-settings-mutation-from-persistence.md) 调整，语言切换继续直接传播重载异常，不引入回滚。
- 切换语言会同步重载全部预加载剧情目录及当前文本源，保持当前激活 Token 有效。正式流程保证预加载先完成，移除原剧情异步激活的请求版本检查；保留异步预加载取消、资源 operation 释放和缓存清理，同步执行不替代资源与生命周期管理。
