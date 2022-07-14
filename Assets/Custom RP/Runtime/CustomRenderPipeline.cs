using UnityEngine;
using UnityEngine.Rendering;


public class CustomRenderPipeline : RenderPipeline
{
    
    private CameraRenderer m_renderer = new CameraRenderer();
    
    private bool m_useDynamicBatching, m_useGPUInstancing;
    private ShadowSettings m_shadowSettings;

    public CustomRenderPipeline(bool useGPUInstancing, bool useDynamicBatching, bool useSRPBatcher, ShadowSettings shadowSettings)
    {
        m_useGPUInstancing = useGPUInstancing;
        m_useDynamicBatching = useDynamicBatching;
        m_shadowSettings = shadowSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            m_renderer.Render(context, camera, m_useDynamicBatching, m_useGPUInstancing, m_shadowSettings);
        }
    }
}
  