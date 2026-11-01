# Action / Condition 代码生成

两个独立 Unity 菜单根据 Gameplay 程序集中的专用 Attribute 生成类型分发；Condition 同时生成依赖分类收集。生成器位于 Gameplay.Editor，运行时仅执行已生成的静态代码。

## 职责与边界

ActionGenerator、ConditionGenerator 各自收集、校验和渲染；共用工具只负责类型名格式化、Scriban 渲染、UTF-8 差异写入和所属目录的旧文件清理。不使用 Roslyn，不保留程序集级可配置方法签名规则。配表仍由 Design/Luban 管理，生成器不修改表结构。

ResolverTemplateItem 只包含共用的定义类型、Resolver 类型和类型 ID 表达式；ConditionResolverTemplateItem 继承这些数据并提供 SourcesExpression。两个生成器各自在 GenerateContent 中构造模板数据，共用层不持有 ConditionSource。

## 使用与扩展规则

- 行为使用 `[ActionResolver(typeof(SomeActionDef))]`，实现 `public static bool Execute(SomeActionDef action, ICommandContext context)`。
- 条件使用 `[ConditionResolver(typeof(SomeConditionDef), ConditionSource.GameVariables)]`，实现 `public static bool Evaluate(SomeConditionDef condition, IQueryContext context)`；来源支持按位组合，True/False 显式写 None。
- All/Any 为内置组合类型，不另行注册 Resolver；依赖收集遍历全部子条件，不沿用求值短路。
- 运行时 CollectSources 遇到未支持类型时记录 Error 并返回 None，不抛出缺失分支异常；组合继续收集其余子条件。生成期仍校验声明与定义覆盖，运行时返回 None 不代替重新生成。
- 使用 `Tools/Gameplay/Generate Action` 或 `Tools/Gameplay/Generate Condition`。后者在一次操作中生成求值分发和 CollectSources，不单独生成枚举或订阅。
- 声明和生成结果一起维护；不手工修改生成代码。新增数据源枚举、变更事件与来源的映射仍由 ConditionSystem 人工维护。

## 生命周期与核心语义

菜单扫描当前已编译 Gameplay 程序集。需先保证声明和生成器能编译，不让手写声明引用尚不存在的生成类型。校验绑定类型、静态 Resolver、必需方法签名、重复绑定、定义覆盖和来源合法性；全部校验与渲染成功后才写入结果。

输出保持稳定排序和原有路径，无内容变化不写文件；各生成器只清理自己 Generated 目录内带自身标记的旧文件。构建前重新计算预期内容并只读比较；产物缺失、过期或残留时阻止构建并提示对应菜单，不在构建中自动改写代码。

## 验证

验证分别生成不会改动另一类产物、连续生成不产生 diff 或更新时间、异常校验不写入部分结果，以及构建前过期检查。修改模板后同样重新执行对应菜单。
