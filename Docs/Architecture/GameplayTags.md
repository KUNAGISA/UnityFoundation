# Gameplay Tag 系统

标签框架表示稳定标签身份、父子层级和可取消的来源计数。场景 Entity 当前的 Gameplay Tag State 由其持有的 `GameplayTagContainer` 表达，不属于 Character State 或 Save Data。

## 职责与边界

- `Gameplay.Tags` 程序集位于 `Assets/Framework/Tags/Runtime`，只依赖 Aoiro，提供标签值、Catalog、容器和 Token。
- 项目标签由 XML 生成；共享 Catalog 与常量位于配表程序集 `Gameplay.Tables`，框架不依赖具体标签或 Luban。
- Catalog 持有全局共享的层级信息；每个容器采用以 GameplayTag 为键的稀疏字典，只存储生效标签计数与实际使用的订阅通道，不按 Catalog 大小分配数组或使用索引。
- 标签是正整数 ID 值；`default(GameplayTag)` 表示无标签。运行时不保存路径字符串，不根据 ID 推测父子关系。

## 主要类型与入口

`GameplayTagDefinition` 保存 Tag 和 Parent 两个标签值。`GameplayTagCatalog(ReadOnlySpan<GameplayTagDefinition>)` 在构造时建立层级，不保留调用方的数组引用，允许输入乱序；拒绝无效标签、重复 ID、缺失父标签和循环。没有后续注册或冻结接口。

项目标签通过 `GameplayTags.Control.Block.Movement.Tag` 访问；`GameplayTags.Catalog` 是生成的共享定义。容器构造时显式接收 Catalog，不引入架构 System 或帧调度。

## 生命周期与核心语义

`AddTag(tag, out token)` 每次增加一个独立来源，同时增加自身和所有祖先的计数。可以直接添加父标签；同一来源不能重复移除。Token 及其副本共享容器中的活动记录，重复 Cancel 和默认 Token 的 Cancel 均无效。来源 ID 在容器生命周期内单调递增，Clear 后不复用。

`GetCount` 和 `Has` 使用祖先汇总计数；未知或默认标签查询为零/false。`HasAll(ReadOnlySpan<GameplayTag>)` 的空输入为 true，`HasAny` 的空输入为 false。添加与订阅时要求标签存在于该 Catalog。

添加会先完成整条祖先链的修改，写出 Token，再同步分发通知。因此需要在同步回调中取消本次来源时，必须直接传入持有 Token 的字段；先写入局部变量、返回后再赋字段不能达到相同时序。

`Clear` 先清空全部计数和来源，再分发各生效标签到零的变化，保留订阅；旧 Token 不影响后续来源。Clear 的标签间顺序不作为接口保证。

`Dispose` 静默清空来源、计数、通知及订阅；重复调用和旧 Token 取消安全，查询返回零。Dispose 后添加或订阅抛 ObjectDisposedException。Entity 销毁采用 Dispose，避免销毁通知触发重新添加。

## 使用与扩展规则

通过 `SubscribeCountChanged(tag, Action<GameplayTagChange>)` 和 `SubscribePresenceChanged(tag, Action<GameplayTagChange>)` 监听指定标签，均返回 SubscriptionToken，由订阅所有者在生命周期结束时取消。两者使用相同快照字段 Tag、OldCount、NewCount，不提供注册时立即通知。

数量通知覆盖每次变化；有无通知仅覆盖零与非零之间的转换。一次添加/取消的通知按自身到根节点排序，每个标签先数量后有无。完整变化批次在通知前全部入队，监听器中修改容器立即生效，但后续通知排在当前完整批次之后。

事件参数是历史变化快照，查询容器得到的是最新状态；回调中再次修改后，两者可以不同。订阅增删遵循 Signal 规则。同步监听器异常由 Signal 逐个记录并隔离，不回滚变化、不取消失败监听器；异步回调的异常不在此范围内。

容器不保证调用方初始化的其他字段已经就绪。持有 Token 的业务调用在 AddTag 后仍需判断是否被回调取消、销毁或重新启动；PerformanceAbility 按执行 ID 和状态检查。

## 性能与排错

查询不分配集合；修改以父链遍历更新稀疏计数，零计数删除。通知队列复用容量，监听通道按首次订阅创建。字典与队列首次使用或扩容可以分配，稳定容量下添加/取消不产生逐次临时对象。

单轮最多分发 1024 条标签变化，祖先变化也计入预算。超限清空待发通知并抛异常，已提交状态保持不变，后续操作可以再次分发；该异常表示事件循环问题，不表示修改回滚。Token 在触发此保护前已交付。

## 验证

- 校验乱序层级、缺失父节点、循环、稀疏 ID 和输入所有权。
- 校验祖先汇总、兄弟来源、父标签直接添加、空集合查询和 Token 幂等取消。
- 校验两类通知顺序、重入取消/添加/Clear、监听异常隔离、分发预算和 Dispose。
- 生成、Luban 接入与稳定 ID 的操作规则见 [配表与生成代码](Tables.md)。
