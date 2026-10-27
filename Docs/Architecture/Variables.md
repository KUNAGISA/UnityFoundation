# 游戏变量与全局变量

变量模块提供整数、布尔覆盖值，以及游戏存档与全局持久化生命周期。配表拥有默认值，Query 组合默认值和覆盖值，Model 不缓存配置。架构取舍见 [ADR-0021](../ADR/0021-variable-overrides-and-query-defaults.md)。

## 职责与边界

- GameVariableModel 管理当前游戏进度的覆盖值，新游戏清空，普通场景切换保留，读档整体替换。
- GlobalVariableModel 管理跨存档覆盖值，独立持久化，新游戏和游戏读档不覆盖；用户偏好仍由 GlobalSettingModel 管理。
- 两个 Model 共用普通对象 VariableStore，不访问同层 Model，包括 TablesModel；不持有默认值目录或初始化标记。
- 读取由 Query 提供当前配表默认值，业务设置经 Command 校验和编排。GM 清除直接调用 Model，不新增变量 System。

## 主要类型与入口

GameIntVariableTable、GameBoolVariableTable、GlobalIntVariableTable、GlobalBoolVariableTable 分别配置 id、desc、defaultValue，ID 在各表内独立，描述不参与求值或存档。

四个 Get…VariableQuery 先校验对应表，取得默认值，再调用 Model 的 `GetInt(id, defaultValue)` / `GetBool(id, defaultValue)`。读取不创建覆盖记录；不存在的配置 ID 抛出异常，不能作为合法的 0 或 false 返回。业务读取通过 Query，传给 Model 的默认值由对应配置决定。

四个 Set…VariableCommand 分别对应游戏/全局、int/bool，不使用作用域参数分流。设置先检查配置，不存在时记录 Error 并返回 false；通过对应 Query 比较有效值，相同时不新增覆盖、不通知。需要取消已有赋值时清除覆盖，不通过设置默认值代替清除。

## 生命周期与核心语义

架构初始化只创建空覆盖集合并绑定存储服务。变量没有默认数据初始化流程，配表就绪后即可查询。新游戏调用 GameVariableModel.Clear；读档经现有 RestoreGameSaveDataCommand 整体恢复，不先清除再恢复；普通切场景不改动覆盖集合。

GlobalVariableModel 完整负责独立文件的加载、保存和脏标记。Launch 在原有配表加载完成的位置等待 LoadAsync；LoadAsync 在入口检查取消，异步读取的取消由 IStorageService 负责；读取成功后同步解析临时数据并整体替换两个覆盖字典、清除脏标记，再按变化类型通知。文件缺失时应用空覆盖值；文件内容空白、解析结果为空或任一变量集合缺失时直接跳过，保留已有覆盖值和脏标记，不通知、不写文件。读取、解析失败、重复 ID 或取消均保留已有状态。加载不自动写文件，也不访问 TablesModel。

游戏与全局变量的持久化不检查配置 ID，覆盖记录原样保存和恢复，失效 ID 不报配置错误、不自动丢弃。游戏存档中的变量数据或任一字典为空时只跳过变量恢复，其他存档检查与角色等数据的恢复照常执行。两个 Model 对完整但没有记录的快照正常恢复，可清空旧覆盖值；缺失数据与合法空集合含义不同。业务 Query 和设置 Command 的配置校验保持不变。同表内复用 ID 会使旧覆盖值重新生效。

全局设置或 GM 清除先更新内存并通知，再通过 SaveIfDirty 原子保存至 Variables/global.json。同值设置仍重试此前的脏数据，写入失败保留已生效的内存和脏标记。正常关闭在存储服务释放前再次尝试保存。

四类变量分别发布 GameIntVariableChangedEvent、GameBoolVariableChangedEvent、GlobalIntVariableChangedEvent、GlobalBoolVariableChangedEvent。事件表示覆盖记录变化：新增、改值、删除或快照内容改变；有效值相同也可通知。清除不存在的覆盖、重复应用相同快照不通知。批量清除、恢复先完成两个字典的全部提交，再按变化类型通知。捕获、恢复均不与调用方共享可修改字典。

## 使用与扩展规则

- 新变量修改 Design 下对应源表并重新生成，沿用 Configs 地址和正式配表加载；Model 无需同步默认值。
- 四种设置变量 ActionDef 分别引用对应表，经 Command 执行；四种变量 ConditionDef 经 Query 读取。bool 判断与配置值是否相等；整数支持六种常规比较。
- ConditionSystem 只监听运行时覆盖变化，不监听配置重载，也不更新 Model。开发期修改配置后执行 condition.refresh GM，手动重建已观察条件的依赖并刷新缓存；只有条件布尔结果变化才通知消费方。
- GM 使用 variable.game、variable.global 前缀，各提供 get-int、get-bool、set-int、set-bool、reset-int、reset-bool 和 reset-all。reset 直接调用 ClearInt、ClearBool、Clear；允许清理配置已删除的 ID，不存在的记录为空操作。全局清除后立即尝试保存。
- 未覆盖变量随 Query 使用最新默认值；已有覆盖值不受默认值修改影响。存档只保存覆盖值，不冻结未赋值变量当时的默认值。

## 验证

覆盖无需默认值初始化的读取、配置 ID 校验、覆盖优先、同值设置、清除失效 ID、覆盖变化通知、批量提交、存档及失效 ID 往返、全局加载整体提交、缺失变量数据跳过恢复、合法空快照清空覆盖、失败和取消不修改状态、保存失败重试、配置重载不自动刷新、condition.refresh、运行时类型刷新 FIFO 和正式游戏流程。
