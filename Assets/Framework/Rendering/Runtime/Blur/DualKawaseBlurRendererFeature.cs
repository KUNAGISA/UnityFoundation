using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gameplay.Rendering
{
    public sealed class DualKawaseBlurRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private Shader _shader = null;

        [SerializeField, InlineProperty]
        private DualKawaseBlurSettings _settings = new();

        private Material _material;
        private DualKawaseBlurPass _pass;
        private float _intensity = 1f;

        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp01(value);
        }

        public override void Create()
        {
            DisposeResources();
            _settings ??= new DualKawaseBlurSettings();
            if (_shader == null || !_shader.isSupported)
            {
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(_shader);
            _pass = new DualKawaseBlurPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData))
            {
                return;
            }

            _pass.Configure(_settings, _intensity);
            renderer.EnqueuePass(_pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData))
            {
                return;
            }

            _pass.SetSource(renderer.cameraColorTargetHandle);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeResources();
        }

        private void DisposeResources()
        {
            _pass?.Dispose();
            _pass = null;
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            return _pass != null && renderingData.cameraData.cameraType is not (CameraType.Preview or CameraType.Reflection);
        }
    }
}
