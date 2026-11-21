using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gameplay.Rendering
{
    internal sealed class DualKawaseBlurPass : ScriptableRenderPass
    {
        private const int DownsamplePass = 0;
        private const int UpsamplePass = 1;
        private const int CompositePass = 2;

        private static readonly int BlurRadiusId = Shader.PropertyToID("_DualKawaseBlurRadius");
        private static readonly int BlurIntensityId = Shader.PropertyToID("_DualKawaseBlurIntensity");
        private static readonly string[] PyramidNames =
        {
            "_DualKawaseBlurPyramid0",
            "_DualKawaseBlurPyramid1",
            "_DualKawaseBlurPyramid2",
            "_DualKawaseBlurPyramid3",
            "_DualKawaseBlurPyramid4",
            "_DualKawaseBlurPyramid5"
        };

        private readonly Material _material;
        private readonly RTHandle[] _pyramid = new RTHandle[DualKawaseBlurSettings.MaxLevels];
        private RTHandle _source;
        private int _levels;
        private int _activeLevels;
        private float _radius;
        private float _intensity;

        internal DualKawaseBlurPass(Material material)
        {
            _material = material;
            profilingSampler = new ProfilingSampler(nameof(DualKawaseBlurRendererFeature));
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        internal void Configure(DualKawaseBlurSettings settings, float intensity)
        {
            renderPassEvent = settings.RenderPassEvent;
            _levels = settings.Levels;
            _radius = settings.Radius;
            _intensity = intensity;
        }

        internal void SetSource(RTHandle source) => _source = source;

        public override void OnCameraSetup(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.enableRandomWrite = false;

            _activeLevels = 0;
            for (var index = 0; index < _levels; index++)
            {
                if (descriptor.width <= 1 || descriptor.height <= 1)
                {
                    break;
                }

                descriptor.width = Mathf.Max(1, descriptor.width / 2);
                descriptor.height = Mathf.Max(1, descriptor.height / 2);
                RenderingUtils.ReAllocateIfNeeded(ref _pyramid[index], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: PyramidNames[index]);
                _activeLevels++;
            }

            ReleasePyramidFrom(_activeLevels);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_source == null || _activeLevels == 0 || _material == null)
            {
                return;
            }

            var commandBuffer = CommandBufferPool.Get();
            try
            {
                using (new ProfilingScope(commandBuffer, profilingSampler))
                {
                    commandBuffer.SetGlobalFloat(BlurRadiusId, _radius);
                    var source = _source;
                    for (var index = 0; index < _activeLevels; index++)
                    {
                        var destination = _pyramid[index];
                        Blitter.BlitCameraTexture(commandBuffer, source, destination, _material, DownsamplePass);
                        source = destination;
                    }

                    for (var index = _activeLevels - 1; index > 0; index--)
                    {
                        Blitter.BlitCameraTexture(commandBuffer, _pyramid[index], _pyramid[index - 1], _material, UpsamplePass);
                    }

                    commandBuffer.SetGlobalFloat(BlurIntensityId, _intensity);
                    Blitter.BlitCameraTexture(commandBuffer, _pyramid[0], _source, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, _material, CompositePass);
                }

                context.ExecuteCommandBuffer(commandBuffer);
            }
            finally
            {
                CommandBufferPool.Release(commandBuffer);
            }
        }

        public override void OnCameraCleanup(CommandBuffer commandBuffer) => _source = null;

        internal void Dispose()
        {
            ReleasePyramidFrom(0);
            _activeLevels = 0;
        }

        private void ReleasePyramidFrom(int startIndex)
        {
            for (var index = startIndex; index < _pyramid.Length; index++)
            {
                _pyramid[index]?.Release();
                _pyramid[index] = null;
            }
        }
    }
}
