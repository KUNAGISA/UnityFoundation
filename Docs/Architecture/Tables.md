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

运行时 ID 入口由 [ConditionSystem](Conditions.md) 和 [ActionSystem](Actions.md) 提供。System 从表模型查找定义，生成的 ConditionResolver / ActionResolver / ConditionDependencyResolver 保留接收定义的类型分发入口，不再重复提供总表 ID 查询。找不到 ID 时记录错误，条件返回 false，行为结果回调反馈 false。

上下文复用 Aoiro 的架构能力和绑定生命周期，目前不携带调用来源。ConditionSystem 持有只读 ConditionContext；ActionSystem 持有 ActionContext，以同步 FIFO 防止重入插队。ActionResolver 返回 bool，true 表示已成功发起；持续行为的状态、替换和取消归所属系统，不向调用方返回 Action 监视器。场景事件不记录执行历史，不配置 repeatMode；是否可触发由条件决定，只保留行为提交期间的临时防重入。需要限制重复执行时，通过变量条件与业务行为表达，决策背景见 [ADR-0022](../ADR/0022-condition-driven-scene-event-availability.md)。

游戏/全局各有整数、布尔两张[变量表](Variables.md)，字段为 id、desc、defaultValue，ID 在各表内独立。设置变量行为、变量判断条件均通过 Luban ref 引用对应的强类型变量表。

进入触发器模式只响应玩家的 OnTriggerEnter2D，通过 SceneEventSystem.Trigger(SceneEventPoint) 发起触发请求，该入口返回 void，不向调用方反馈是否提交。SceneEventTrigger 根据条件结果开关 Collider；提交期间的临时防重入不影响对外可触发状态、Collider 或表现对象。SceneEventSystem 的分发回调忽略行为成败，仅释放当前登记的临时防重入标记；条件状态由登记初始化和条件变化通知更新，Trigger 使用已维护的 IsTriggerable，触发尝试和分发结束均不重复求值或通知表现对象。开发期配置重载后需执行 condition.refresh 更新观察结果，触发尝试不会绕过观察状态读取最新配置。条件持续成立时，提交与分发结束不会重新产生进入事件；条件从不成立恢复为成立时，重新启用 Collider 可以让仍在区域内的玩家再次触发。行为发起成功不代表剧情播放完成，需要在剧情正常结束后通过业务行为修改变量，使条件失效。

ConditionSystem 不订阅条件总表或变量表重载。开发期配置重载后，通过 condition.refresh GM 显式重建已观察条件的依赖并重新求值，仅通知布尔结果变化；即时求值始终读取最新配置。行为每次实际执行时查询最新定义，行为总表重载不替换已经发起的行为。

新增定义沿用 `DefinitionResolverAttribute` 和现有生成模板，通过 `Tools/Gameplay/Generate Definition Resolvers` 重新生成统一分发器，不手工编辑生成结果。配表不再按调用领域限定定义类型，Luban 标准 ID、格式和定义内部引用校验继续生效。新增总表数据资源沿现有 `Configs/<table>` Addressables 规则注册，并由正式初始化流程加载。

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
