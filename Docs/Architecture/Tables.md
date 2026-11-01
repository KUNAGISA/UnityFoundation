# 配表与生成代码

配表流程以 `Design` 中的 Luban 源数据为唯一编辑入口，生成代码只作为构建产物，由 `TablesModel` 向运行时代码提供稳定访问边界。

## 职责与边界

设计数据和结构在 `Design` 修改，通过项目既有 Luban 流程生成运行时代码。玩法代码通过表模型或现有封装读取，不直接依赖设计源文件，也不手工维护生成输出。

## 主要类型与入口

- 设计源数据：`Design`
- 生成代码：`Assets/Gameplay/Tables/Generated`
- 表模型：`Assets/Gameplay/Tables/Runtime/TablesModel.cs`

## 生命周期与核心语义

表结构或数据变化先修改源定义，再重新生成。生成结果进入运行时程序集，读取方通过 `TablesModel` 获取已经加载的表数据；表的运行时初始化由 [初始化流程](Initialization.md) 统一编排。

## 使用与扩展规则

- 配表结构和数据变更只修改 `Design` 下的源文件。
- 不手写修补 `Assets/Gameplay/Tables/Generated` 下的输出。
- 运行时代码通过 `TablesModel` 或现有表封装访问数据，不直接读取生成类静态状态、Excel、XML 或其他设计文件。
- 新增字段时同步检查默认值、空值、资源地址和存档兼容性。

## Condition / Action 总表

`ConditionTable` 和 `ActionTable` 各按稳定整数 ID 索引。每条记录包含 ID、用于编辑器展示的描述和一个多态 `ConditionDef` / `ActionDef` 配置；描述不参与条件求值、行为执行或存档。

业务表使用必填 `conditionId` / `actionId` 并声明对应总表的 Luban `ref`。相同配置可共享 ID；两张总表使用独立 ID 空间。All/Any 的子条件仍直接内嵌配置，不通过总表引用；求值保持短路，空 All 成立、空 Any 不成立。

演出表的 `onCompletedActionId` 是可选的行为总表引用，类型为 `int?#ref=ActionTable`，生成成员为 `SequenceEntry.OnCompletedActionId`。留空表示无结束行为，非空值必须通过行为总表引用校验；运行时在本次剧情正常完成并清理后经 ActionSystem 提交，取消和失败不提交，生命周期详见 [Sequence 框架](Sequence.md)。

运行时 ID 入口由 [ConditionSystem](Conditions.md) 和 [ActionSystem](Actions.md) 提供。System 从表模型查找定义，生成的 ConditionResolver / ActionResolver 保留接收定义的类型分发入口，不再重复提供总表 ID 查询。找不到 ID 时记录错误，条件返回 false，行为结果回调反馈 false。

上下文复用 Aoiro 的架构能力和绑定生命周期，目前不携带调用来源。ConditionSystem 持有只读 ConditionContext；ActionSystem 持有 ActionContext，以同步 FIFO 防止重入插队。ActionResolver 返回 bool，true 表示已成功发起；持续行为的状态、替换和取消归所属系统，不向调用方返回 Action 监视器。场景事件不记录执行历史，不配置 repeatMode；是否可触发由条件决定，只保留行为提交期间的临时防重入。需要限制重复执行时，通过变量条件与业务行为表达，决策背景见 [ADR-0022](../ADR/0022-condition-driven-scene-event-availability.md)。

游戏/全局各有整数、布尔两张[变量表](Variables.md)，字段为 id、desc、defaultValue，ID 在各表内独立。设置变量行为、变量判断条件均通过 Luban ref 引用对应的强类型变量表。

SceneEventSystem 在场景进入及业务表重建时同步初始化全部事件状态，再按 EventId 顺序提交初始成立的 Auto。之后通过条件观察维护本地结果，在 ConditionBatchCompletedEvent 中去重、按 EventId 应用；持续成立不重复提交，只在 false→true 时再次自动提交。

进入触发器只响应玩家 OnTriggerEnter2D；提交前即时 Evaluate，拒绝已失效条件，不刷新观察缓存。提交临时防重入不改变 Collider 或表现，分发回调只释放当前登记的标记。真实条件变化控制 Collider，恢复后区域内玩家可以再次触发；限制重复由变量和业务行为表达，不记录事件完成历史。

SceneNpcSystem 观察当前场景全部候选配置条件，批次结束按 NPC ID 重新选择最高优先级有效配置。配置不变不重复定位，无有效配置时移除实体。配置重建或条件结果变化时可为缺失实体补建；条件不变且 Entity 被外部移除时，不再由持续轮询自动补建。

NPC 候选配置按 NPC ID 分组，每条 SceneNpcConfigState 一并保存配置、条件结果和观察 Token；重建先取消旧观察再清理配置状态，已登记 Entity 保留到配置刷新时比较。两个消费方将 dirty ID 去重收集到本批列表，排序后统一应用。

两个消费系统随 Procedure Attach/Detach 订阅/解除业务表、暂停与批次事件；观察 Token 随场景登记创建和取消。暂停时暂停 Token，恢复后重新检查，暂停期间新登记立即暂停。初始化与显式提交维持同步入口语义；取消后的旧条件通知由 ConditionSystem 在派发前排除，消费方不重复校验版本。

SceneNpcSystem 的配置选择、位置应用和 Entity 登记沿同步调用链完成，不维护额外登记版本。SceneEventSystem 的状态接收方和 Action 分发是同步业务重入入口：调用返回后检查 _sceneEventVersion 以停止旧遍历，不可取消的 Action 分发回调也检查登记版本，避免操作重建后的防重入集合。

ConditionSystem 手工维护变量事件和来源映射，在 LateTick 集中刷新观察缓存；变量默认值表重载使对应来源失效，条件总表重载重建全部观察依赖。详见[条件观察](Conditions.md)。ActionSystem 每次实际执行查询最新行为定义，不替换已经发起的行为。

新增定义使用专用 ActionResolverAttribute/ConditionResolverAttribute，通过两个独立菜单生成。Condition 来源显式声明，枚举与事件映射不自动推断，详见[代码生成](DefinitionGeneration.md)。配表不按调用领域限定定义类型，继续使用 Luban 标准 ID、格式、定义内部引用校验和既有 Configs Addressables 注册/加载流程。

## Gameplay Tag 生成

标签源文件为 `Design/GameplayTags.xml`，使用 `<gameplayTags nextId="...">` 和嵌套 `<tag name="..." id="...">`。每个节点都是标签；名称逐层拼成路径。新增节点只需填写 name，CLI 自动补齐缺失 ID 并保存 nextId。重命名或移动原节点保留其 id；删除不回退 nextId，不复用 ID。

从任意工作目录调用 `Design/gen-tags.bat`（Windows）或 `bash Design/gen-tags.sh` 可独立生成；完整 gen.bat/gen.sh 在 Luban 校验前自动执行。需要 .NET 8 运行时和 .NET SDK；工具项目为 `Tools/GameplayTags.Generator`，构建产物写入 `.ai-workspace`。

CLI 也可直接调用：

```text
dotnet run --project Tools/GameplayTags.Generator/GameplayTags.Generator.csproj -c Release -- --input Design/GameplayTags.xml --template Design/Template/gameplay-tags/catalog.sbn --output Assets/Gameplay/Tables/Runtime/Tags/Generated/GameplayTags.cs --template Design/Template/gameplay-tags/constalias.sbn --output Design/Defines/gameplaytags.xml
```

工具先校验整份定义，再补齐 ID 并生成。重复 ID、同级重名、非法 C# 名称、与 Tag/Catalog 或所属生成类同名、ID 耗尽均报错，不自动改名。XML 使用 UTF-8，回写保留注释、空白和换行风格；已有 ID 不重算，无变化的文件不写入。nextId 只能递增；手工降低它会破坏已删除 ID 不复用的契约。

生成器统一使用 Scriban 的 `.sbn` 模板，通过 `--template <sbn> --output <file>` 选择输出格式，可指定一组或多组。所有模板渲染成功后才写入源文件和输出，模板语法或变量错误不会补写 ID 或覆盖已有输出。模板模型提供 `tags`（前序扁平列表）、`roots`（树的根节点）和 `next_id`；节点包含 `name`、`path`、`id`、`parent_id`（根节点为 0）和 `children`。

`Design/Template/gameplay-tags/catalog.sbn` 生成项目 C# 常量/Catalog，`constalias.sbn` 生成 Luban 常量定义。前者位于配表程序集独立目录，不受 Luban 生成输出目录清理影响；后者被现有 Defines 目录加载。路径字符串只存在于源文件和 Luban 别名定义，不进入运行时 Catalog。

builtin 类型 `gameplaytag` 包含整数 id；Excel/CSV 可填数字或路径常量（例如 `Control.Block.Movement`），JSON 整数字段需填数字，不能把别名当作 JSON 字符串填写。通过 `BuildIn.ToGameplayTag` 映射为 `Gameplay.Tags.GameplayTag`。0 表示无标签，非零未知 ID 在转换时抛异常。可空字段应使用 `gameplaytag?`，集合使用 Luban 现有容器语法。生成代码只做反序列化及 mapper 调用，不手工修改生成结果。

## 验证

- 重新执行 Luban 生成并检查生成命令成功。
- 执行 Unity Editor 编译，确认引用代码与新生成类型一致。
- 验证关联 Addressables 路径以及受影响的数据兼容性。
