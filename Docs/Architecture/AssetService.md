# IAssetService

`IAssetService` 是运行时资源与场景加载的统一能力边界，负责隔离 Addressables、表达 operation 所有权，并让调用方按自身生命周期释放资源。

## 职责与边界

业务与框架调用方通过 `IAssetService` 加载资源和场景，不直接依赖 Addressables。服务负责定位资源并返回项目封装的 operation；Prefab 实例化、实例生命周期和业务缓存策略由调用方负责。

运行时的 `AddressableAssetService.OnInitialize` 不加载资源。正式初始化由 Launch 调用 `InitializeAsync` 完成并保存默认 `IResourceLocator`；初始化前调用加载接口会抛出明确异常。`EditorArchitecture` 使用显式的同步初始化选项，在模块初始化时取得 locator，以供编辑模式下同步读取配表。启动依赖见 [初始化流程](Initialization.md)。

## 主要类型与入口

- `IAssetService`：`Assets/Framework/Asset/Runtime/Core/IAssetService.cs`
- `AddressableAssetService`：`Assets/Framework/Asset/Runtime/Addressable/AddressableAssetService.cs`
- `IAssetAsyncOperation<T>`、`ISceneAsyncOperation`：`Assets/Framework/Asset/Runtime/Core/AsyncOperation`

主要入口包括单资源的 `LoadAssetSync<T>` / `LoadAssetAsync<T>`、按 Label 批量加载的 `LoadAssetsSync<T>` / `LoadAssetsAsync<T>`，以及 `LoadSceneAsync`、`UnloadUnusedAssets` 和 `CreateLoader`。

## 生命周期与核心语义

- 单资源加载按 location 定位，批量加载按 Addressables Label 定位。
- 找不到资源时通过 logger 记录错误。
- 加载返回封装 operation，创建者负责在生命周期结束时释放。
- `LoadAssetAsync<T>` 正常返回非空 operation；加载失败通过 operation 的状态、异常或结果表达，也可能在创建 operation 前直接抛出异常，不以返回 null 表示失败。
- 同步加载是 `IAssetService` 的正式接口，由后端实现；返回与异步入口相同类型的 operation。正常返回表示操作已完成，不代表成功，调用方通过 `Status`、`OperationException` 和 `Result` 判断结果。
- `AddressableAssetService` 复用异步入口创建 operation 并同步等待；等待抛异常时释放未交付的 operation，再传播异常。初始化要求、资源定位、类型筛选和批量失败策略与异步入口一致。
- `AssetLoader` 是可重复使用的资源作用域，缓存多个单资源 operation；`Clear()` 会取消未完成等待、释放缓存并允许后续复用。
- `AssetLoader` 同步读取时，缓存未命中调用同步加载接口；命中未完成的 operation 则调用 `WaitForComplete()`，复用原加载，不重新发起请求。
- 场景 operation 由流程或其他明确所有者持有，卸载时使用对应 operation。

## 使用与扩展规则

- 玩法和框架模块统一通过 `IAssetService` 加载资源。
- 需要按调用方生命周期缓存资源时使用 `AssetLoader`，并在所有者退出前调用 `Clear()`。
- Label 可以包含不同 Unity 资源类型，类型专属初始化由调用方处理。
- 流程场景加载优先使用 [Procedure](Procedure.md) 的场景封装。
- 新增通用加载能力放入 `Assets/Framework/Asset`，保持与具体游戏业务解耦。
- operation 生命周期必须与调用方生命周期匹配，不绕过服务直接使用 Addressables。
- 同步入口仅在后端和平台支持的条件下使用，并在 Unity 主线程调用。Addressables 同步等待可能推进其他活跃资源加载并阻塞主线程，不用于依赖远程下载的资源；WebGL 不支持该等待方式，场景尚未完成加载时在 `Awake` 中同步等待可能死锁。
- 场景继续通过异步接口加载与激活，不提供同步场景加载承诺。

## 验证

- 验证初始化前访问会明确失败，初始化成功后 location 与 Label 加载正常。
- 验证同步单资源和 Label 加载返回已完成且可释放的 operation，以及 Loader 复用未完成异步加载后同步读取的行为。
- 覆盖调用方取消、Loader Clear、场景卸载和架构 Shutdown，确认 operation 被释放且旧回调不再生效。
- 资源加载改动后执行 Unity Editor 编译、Addressables Build，并检查运行时引用计数。
