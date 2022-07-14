using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool _useDynamicBatching = true, _useGPUInstancing = true, _useSRPBatcher = true;

    [SerializeField] 
    private ShadowSettings _shadowSettings = default;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            _useGPUInstancing, _useDynamicBatching, _useSRPBatcher, _shadowSettings
            );
    }
}