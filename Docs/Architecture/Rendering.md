# Rendering

Rendering 模块提供与 Gameplay 和 UI 解耦的通用 URP 渲染能力；当前核心实现是可由任意相机 RendererData 独立启停的全屏 Dual Kawase Blur。

## 职责与边界

`Assets/Framework/Rendering/Runtime` 编译为独立程序集 `Gameplay.Rendering`，只依赖 Unity Rendering Core 与 URP。它不理解 UI Layer、View、Option 或业务生命周期；Gameplay 决定何时启停 Feature 和如何组织 Camera Stack。

当前模糊能力处理整张 Camera Color，不导出全局纹理、不提供局部遮罩，也不替调用方筛选 Layer。局部 UI 模糊、背景纹理采样或不同区域不同半径需要单独设计。

## 主要类型与入口

- `DualKawaseBlurRendererFeature`：Renderer Feature 与运行时启停入口
- Blur Settings：注入时机、Levels、Radius 和 Shader
- Blur Pass：金字塔 RTHandle、采样和最终合成
- Shader：`Hidden/Gameplay/Rendering/DualKawaseBlur`

`Intensity` 为 0–1 的运行时合成强度；`SetActive(false)` 是移除 Pass 成本的入口。每份 RendererData 拥有独立 Feature 实例。

可序列化配置：

| 配置 | 范围 | 默认值 |
| --- | --- | --- |
| Injection Point | Before Transparents / Before Post Processing / After Post Processing | Before Post Processing |
| Levels | 1–6 | 3 |
| Radius | 大于等于 0.1 | 0.5 |
| Shader | `Hidden/Gameplay/Rendering/DualKawaseBlur` | RendererData 显式引用 |

## 生命周期与核心语义

Feature 在 `AddRenderPasses` 配置并入队 Pass，在 `SetupRenderPasses` 绑定有效的 `cameraColorTargetHandle`。Pass 从 Camera Color 逐层降采样，再沿原金字塔上采样，最终将完整模糊结果按 Intensity 混合回 Camera Color，不申请全分辨率模糊 RT。

运行时根据相机尺寸限制有效层数，较高层 RT 在上采样阶段复用。Before Post Processing 的结果继续进入该相机后处理；After Post Processing 处理已经完成后处理的颜色。

在 URP Camera Stack 中，Overlay Camera 的输入已包含此前 Base/Overlay 的合成结果。Feature 原地写回，后续 Overlay 继续合成，因此调用方通过 Stack 顺序决定哪些内容参与模糊。

Feature 从序列化 Shader 创建 Material；Shader 缺失或不支持时不创建 Pass。Feature 重建、销毁或层数减少时释放 Material 和不再使用的 RTHandle。RTHandle 使用相机颜色格式，关闭 MSAA、Depth、MipMap 和 Random Write，稳定描述下复用现有资源。

## 使用与扩展规则

- 在目标 RendererData 添加独立 Feature，显式引用 Shader 并配置注入点、Levels 与 Radius。
- 需要独立启停状态的相机使用不同 RendererData，不共享同一 Feature 子资源。
- 调整 Camera Stack 顺序后重新确认模糊输入和清晰前景的位置。
- 运行时强度归零后调用 `SetActive(false)`，只有停用才能移除 Pass、复制和临时 RT 成本。
- 通用 Shader、Feature、Settings 和 Pass 保持在 Rendering 程序集，不引用 Gameplay/UI 类型。

## 性能与排错

GPU 成本随 Levels 增长，但各级像素数逐级缩小。移动端优先从 2 层开始，1080p Standalone 默认 3 层；画质不足时先增加层数，再调整 Radius。`0 < Intensity < 1` 仍生成完整模糊结果，成本不会按强度同比降低。

- 无模糊：检查 Feature Active、Shader、Renderer 索引和注入点。
- 前景被模糊：检查清晰内容是否由模糊相机之后的相机绘制。
- 场景未参与：检查模糊 Overlay 之前是否已有 Base Camera 内容。
- 重复模糊：检查 Stack 中是否有多台相机启用了该 Feature。
- 分辨率或 HDR 异常：检查金字塔描述、颜色格式和 RTHandle 重分配。

## 验证

- 使用 Frame Debugger 确认 Downsample、Upsample、Composite 顺序及写回目标。
- 使用 Profiler 确认稳定分辨率下无逐帧托管分配，停用后无额外 Pass 和临时 RT 活动。
- 在目标设备记录不同 Levels/Radius 组合的 GPU 时间。
- 覆盖 Base/Overlay 顺序、HDR、分辨率切换、Feature 重建和销毁。
