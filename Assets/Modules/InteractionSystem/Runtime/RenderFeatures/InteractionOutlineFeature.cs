using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace InteractionSystem.Rendering
{
    public sealed class InteractionOutlineFeature : ScriptableRendererFeature
    {
        [Header("References")]
        [SerializeField] private Material _outlineMaterial;

        [Header("Outline Layer")]
        [SerializeField] private LayerMask _outlineLayer;
        [SerializeField] private Material _maskMaterial;

        [Header("Default Material Settings")]
        [SerializeField] private RenderPassEvent _renderEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [SerializeField, ColorUsage(false, true)] private Color _outlineColor = Color.black;
        [SerializeField, Min(0f)] private float _thickness = 1f;
        [SerializeField, Range(0f, 1f)] private float _maskThreshold = 1f;
        [SerializeField, Range(0f, 1f)] private float _normalThreshold = 1f;

        private Material _fallBackMaskMaterial;

        private InteractionOutlinePass _outlinePass;
        private InteractionOutlineMaskPass _maskPass;


        private void CreateFallbackMaskMaterial()
        {
            Shader maskShader = Shader.Find("Hidden/UnlitWhite");
            if (maskShader != null)
                _fallBackMaskMaterial = new Material(maskShader);
        }

        private void UpdateMaskMaterial()
        {
            if (_maskMaterial == null)
            {
                if (_fallBackMaskMaterial == null) CreateFallbackMaskMaterial();
                _maskMaterial = _fallBackMaskMaterial;
            }

            _maskPass?.UpdateMaskMaterial(_maskMaterial);       
        }

        public override void Create()
        {
            if (_fallBackMaskMaterial == null) CreateFallbackMaskMaterial();

            if (_outlineMaterial == null)
                _outlineMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("InteractionSystem/OutlineShader"));

            if (_maskPass == null)
            {
                _maskPass = new InteractionOutlineMaskPass(_outlineLayer);
                UpdateMaskMaterial();
            }
            else
                _maskPass.SetLayerMask(_outlineLayer);

            _outlinePass ??= new InteractionOutlinePass()
            {
                renderPassEvent = _renderEvent
            };

            _outlinePass.ConfigureInput(
                ScriptableRenderPassInput.Normal |
                ScriptableRenderPassInput.Depth |
                ScriptableRenderPassInput.Color
            );
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_outlineMaterial == null || _outlinePass == null)
                return;

            if (_maskPass == null)
            {
                _maskPass = new InteractionOutlineMaskPass(_outlineLayer);
            }
            else
            {
                _maskPass.SetLayerMask(_outlineLayer);
            }

            UpdateMaskMaterial();

            renderer.EnqueuePass(_maskPass);

            _outlineMaterial.SetColor("_OutlineColor", _outlineColor);
            _outlineMaterial.SetFloat("_Thickness", _thickness);
            _outlineMaterial.SetFloat("_MaskThreshold", _maskThreshold);
            _outlineMaterial.SetFloat("_NormalThreshold", _normalThreshold);

            _outlinePass.Setup(_outlineMaterial);
            renderer.EnqueuePass(_outlinePass);
        }

        public void SetOutlineLayer(LayerMask mask)
        {
            _outlineLayer = mask;

            if (_maskPass != null)
            {
                _maskPass.SetLayerMask(mask);
            }
        }
    }
}