using Unity.Collections;
using UnityEngine;
using Unity.Rendering;
using UnityEngine.Rendering;

public class Lighting
{
    private const string BufferName = "Lighting";

    private const int MaxDirLightCount = 4;

    private static readonly int
        DirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        DirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
        DirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        DirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    private static readonly Vector4[]
        DirLightColors = new Vector4[MaxDirLightCount],
        DirLightDirections = new Vector4[MaxDirLightCount],
        DirLightShadowData = new Vector4[MaxDirLightCount];

    private CommandBuffer m_buffer = new CommandBuffer
    {
        name = BufferName
    };

    private CullingResults m_cullingResults;

    private Shadows m_shadows = new Shadows();
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        m_cullingResults = cullingResults;
        m_buffer.BeginSample(BufferName);
        m_shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        m_shadows.Render();
        m_buffer.EndSample(BufferName);
        context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
    }

    private void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = m_cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++) {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType != LightType.Directional) continue;
            SetupDirectionalLight(dirLightCount++, ref visibleLight);
            if (dirLightCount >= MaxDirLightCount) break;
        }
        
        m_buffer.SetGlobalInt(DirLightCountId, dirLightCount);
        m_buffer.SetGlobalVectorArray(DirLightColorsId, DirLightColors);
        m_buffer.SetGlobalVectorArray(DirLightDirectionsId, DirLightDirections);
        m_buffer.SetGlobalVectorArray(DirLightShadowDataId, DirLightShadowData);
    }

    private void SetupDirectionalLight (int index, ref VisibleLight visibleLight)
    {
        DirLightColors[index] = visibleLight.finalColor;
        // Z direction is light direction.
        DirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        DirLightShadowData[index] = m_shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    public void Cleanup()
    {
        m_shadows.Cleanup();
    }
}