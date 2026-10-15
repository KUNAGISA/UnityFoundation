# IStorageService

`IStorageService` 是业务文件 IO 的通用边界，统一限制可访问路径、写入模式和异步取消语义，使上层不依赖具体文件系统 API。

## 职责与边界

业务系统通过 `IStorageService` 读写文件，不直接依赖 `File` 或 `Directory`。服务负责根目录约束、目录创建、同步与异步 IO，以及原子写入；序列化格式和业务数据校验由调用方负责。

## 主要类型与入口

- `IStorageService`：`Assets/Framework/Storage/Runtime/Core/IStorageService.cs`
- `FileStorageService`：`Assets/Framework/Storage/Runtime/FileSystem/FileStorageService.cs`
- `StorageWriteMode`：`Assets/Framework/Storage/Runtime/Core/StorageWriteMode.cs`

入口覆盖文件与目录检查、删除、字节和文本读写，以及分段读取；同步和异步版本保持相同的路径与范围语义。

## 生命周期与核心语义

`FileStorageService` 将所有操作限制在 `RootPath`：

- 只接受相对路径，路径片段不允许为 `.` 或 `..`。
- 写入前确保父目录存在，初始化时创建根目录和 `Temp`。
- 默认使用 `StorageWriteMode.Atomic`，先写入 Temp 临时文件再替换目标；`Direct` 直接覆盖目标。
- 分段读取接受非负 `offset` 和 `count`，超过文件尾时返回实际读取的数据。

异步 IO 使用 `UniTask.RunOnThreadPool` 执行文件操作，并在开始及长读取过程中检查 `CancellationToken`。回到 Unity 对象操作前由调用方切换到主线程。

## 使用与扩展规则

- 关键业务文件优先使用 Atomic 写入。
- 长生命周期读写传入调用方的取消令牌。
- 新增存储能力放在 `Assets/Framework/Storage`，保持与序列化格式和游戏业务解耦。
- 路径校验由服务统一完成，调用方不拼接可逃逸 RootPath 的路径。

## 验证

- 覆盖合法相对路径、绝对路径、`.`、`..` 和越界分段读取。
- 验证 Atomic 写入成功替换目标，失败时不留下半写入目标文件。
- 覆盖异步取消与线程切换，确认取消后没有继续提交写入。
