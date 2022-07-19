using System;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private static readonly ShaderTagId
        UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        LitShaderTagId = new ShaderTagId("CustomLit");

    private ScriptableRenderContext m_context;

    private Camera m_camera;

    private CullingResults m_cullingResults;
    
    private const string BufferName = "Render Camera";

    private CommandBuffer m_buffer = new CommandBuffer {
        name = BufferName
    };

    private Lighting m_lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera,
        bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        m_context = context;
        m_camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings._maxDistance))
        {
            return;
        }
        m_buffer.BeginSample(SampleName);
        ExecuteBuffer();
        m_lighting.Setup(context, m_cullingResults, shadowSettings);
        m_buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        m_lighting.Cleanup();
        Submit();
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(m_camera) {
            criteria = SortingCriteria.CommonOpaque //
        };
        var drawingSettings = new DrawingSettings(UnlitShaderTagId, sortingSettings) //todo： what is shader pass
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume
        };
        drawingSettings.SetShaderPassName(1, LitShaderTagId);
        // indicate which render queues are allowed
        // because transparent objects will not write to z buffer, so we do not draw them firstly.
        var filteringSettings  = new FilteringSettings(RenderQueueRange.opaque); 
        
        m_context.DrawRenderers(
            m_cullingResults, ref drawingSettings, ref filteringSettings
        );
        
        m_context.DrawSkybox(m_camera);
        
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        m_context.DrawRenderers(
            m_cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    private void Setup()
    {
        // First setting camera properties and second clearing render target, we can get a quick way to clear.
        // The ture reason may be that they are both before the invoking of execute buffer function.
        m_context.SetupCameraProperties(m_camera);
        CameraClearFlags flags = m_camera.clearFlags;
        // Whatever was drawn to that target earlier is still there,
        // which could interfere with the image that we are rendering now.
        // To guarantee proper rendering we have to clear the render target to get rid of its old contents.
        // CameraClearFlags: 1~4, Skybox, Color, Depth, Nothing
        // Link: https://blog.lujun.co/2019/06/02/unity_camera_clear_flags/
        // Tutorial： We only really need to clear the color buffer when flags are set to Color,
        // because in the case of Skybox we end up replacing all previous color data anyway.
        // Link: https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/
        // My try:
        //  skybox: will invoke draw skybox -> "in the case of Skybox we end up replacing all previous color data anyway"
        //  color : the draw skybox function will not be invoked
        m_buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, 
            flags == CameraClearFlags.Color, 
                     flags == CameraClearFlags.Color ? m_camera.backgroundColor.linear : Color.clear
        );
        m_buffer.BeginSample(SampleName);
        ExecuteBuffer();
        
    }

    bool Cull(float maxShadowDistance)
    {
        if (m_camera.TryGetCullingParameters(out var p))
        {
            p.shadowDistance = maxShadowDistance;
            p.shadowDistance = Mathf.Min(maxShadowDistance, m_camera.farClipPlane);
            m_cullingResults = m_context.Cull(ref p);
            return true;
        }

        return false;
    }

    private void Submit()
    {
        m_buffer.EndSample(SampleName);
        ExecuteBuffer();
        m_context.Submit();
    }

    void ExecuteBuffer()
    {
        m_context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
    }
    
}