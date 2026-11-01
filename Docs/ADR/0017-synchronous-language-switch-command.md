---
status: accepted
---

# 主动语言切换同步执行并由 Command 编排

语言切换是低频操作，当前语言文本使用独立的本地资源分组，允许为简化流程接受短暂主线程停顿。决定由 SetLanguageCommand 同步更新 GlobalSettingModel 的内存语言，再调用 LocalizationSystem 重载文本；设置修改与跨模块编排归 Command，System 只负责文本加载、生效和刷新通知。语言资源属于开发期受控内容，加载错误直接暴露，不引入 Prepare/Apply、多阶段准备作用域或失败回滚协议。

设置修改与显式保存的取舍见 [ADR-0020](0020-separate-global-settings-mutation-from-persistence.md)，剧情预加载、同步启动及文本源激活所有权的取舍见 [ADR-0018](0018-preload-sequence-resources-and-activate-text-with-token.md)。现行接口、通知与生命周期约束见 [Sequence 框架](../Architecture/Sequence.md)，同步加载的资源所有权与后端限制见 [IAssetService](../Architecture/AssetService.md)。

## 备选方案

- 异步加载并暂停流程、阻断输入：可避免同步等待导致的停顿，但需要维护跨帧状态、取消及恢复；当前优先采用同步流程，实际体验测试确认卡顿影响体验时再评估异步方案。
- 由 LocalizationSystem 修改设置并完成全部切换：调用入口较少，但将全局设置修改和文本加载职责混在同一 System；选择让 Command 明确编排 Model 与 System。
- Prepare/Apply 分阶段提交：能够在加载失败时保持原设置，但增加中间状态与调用协议；当前选择开发期解决资源错误，不为切换失败增加恢复流程。

## 影响

- 启动初始化和显式剧情预加载继续异步；正式流程保证预加载完成后才播放或切换语言，不为预加载期间切语言增加并发协议。
- 文本重载异常直接传播，内存语言不回滚；是否落盘由显式保存入口决定，语言切换 Command 不承担保存。
- 切换语言同步重载全部预加载剧情目录及当前文本源，保持当前激活 Token 有效，Lua 缓存不变。同步执行不替代异步预加载取消、资源 operation 释放和缓存清理。
