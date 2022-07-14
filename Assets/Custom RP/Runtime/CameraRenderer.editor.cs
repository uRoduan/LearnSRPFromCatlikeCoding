using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    partial void DrawGizmos();
    partial void DrawUnsupportedShaders ();
    partial void PrepareForSceneWindow ();

    partial void PrepareBuffer();
    
#if UNITY_EDITOR
    public string SampleName { get; set; }
    
    private static readonly ShaderTagId[] LegacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    private static Material s_errorMaterial;

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            m_context.DrawGizmos(m_camera, GizmoSubset.PreImageEffects);
            m_context.DrawGizmos(m_camera, GizmoSubset.PostImageEffects);
        }
    }
    partial void DrawUnsupportedShaders()
    {
        s_errorMaterial ??= new Material(Shader.Find("Hidden/InternalErrorShader"));
        
        var drawingSettings = new DrawingSettings(
            LegacyShaderTagIds[0], new SortingSettings(m_camera)
        )
        {
            overrideMaterial = s_errorMaterial
        };
        for (int i = 1; i < LegacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, LegacyShaderTagIds[i]);
        }
        var filteringSettings = FilteringSettings.defaultValue;
        m_context.DrawRenderers(
            m_cullingResults,  ref drawingSettings, ref filteringSettings
        );
        
    }

    partial void PrepareForSceneWindow()
    {
        if (m_camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(m_camera); // Emits UI geometry into the Scene view for rendering.
        }
    }
    
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        m_buffer.name = SampleName = m_camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = m_bufferName;
#endif
}