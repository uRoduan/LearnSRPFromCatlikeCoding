using UnityEngine;
using UnityEngine.Rendering;

//TODO: problem 1:how culling distance affect view
//TODO: problem 2:ConvertToAtlasMatrix
public class Shadows
{
    struct ShadowedDirectionalLight
    {
        public int VisibleLightIndex;
        public float SlopeScaleBias;
        public float NearPlaneOffset;
    }
    
    private const string BufferName = "Shadows";
    
    private const int MaxShadowedDirectionalLightCount = 4, MaxCascades = 4;
    
    private static string[] s_directionalFilterKeywords = {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };
    
    private static string[] s_cascadeBlendKeywords = {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    private string[] s_shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    private static readonly int 
        DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        DirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        CascadeCountId = Shader.PropertyToID("_CascadeCount"),
        CascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
        CascadeDataId = Shader.PropertyToID("_CascadeData"),
        ShadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
        ShadowDistanceFadeId  = Shader.PropertyToID("_ShadowDistanceFade");

    private static Vector4[]
        s_cascadeCullingSpheres = new Vector4[MaxCascades],
        s_cascadeData = new Vector4[MaxCascades];
        

    private static Matrix4x4[] s_dirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];

    private CommandBuffer m_buffer = new CommandBuffer()
    {
        name = BufferName
    };
 
    private ScriptableRenderContext m_context;

    private CullingResults m_cullingResults;

    private ShadowSettings m_settings;

    private ShadowedDirectionalLight[] m_shadowedDirectionalLights =
        new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

    private int m_shadowedDirectionalLightCount;

    private bool m_useShadowMask;
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        m_context = context;
        m_cullingResults = cullingResults;
        m_settings = settings;

        m_shadowedDirectionalLightCount = 0;

        m_useShadowMask = false;
    }

    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //Besides that, it's possible that a visible light ends up not affecting any objects that cast shadows,
        //either because they're configured not to or because the light only affects objects beyond the max shadow distance.
        //We can check this by invoking GetShadowCasterBounds on the culling results for a visible light index.
        //It has a second output parameter for the bounds—which we don't need—and returns whether the bounds are valid.
        //If not the there are no shadows to render for this light and it should be ignored.
        if (m_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f)
        {
            float maskChannel = -1;
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
                lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                m_useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            if (!m_cullingResults.GetShadowCasterBounds(visibleLightIndex, out _))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel); //negative for baked shadow, prevent to sample from shadow map
            }
            m_shadowedDirectionalLights[m_shadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                VisibleLightIndex = visibleLightIndex,
                SlopeScaleBias = light.shadowBias,
                NearPlaneOffset = light.shadowNearPlane
            };
            return new Vector4(
                light.shadowStrength,
                m_settings._directional._cascadeCount * m_shadowedDirectionalLightCount++,
                light.shadowNormalBias, maskChannel);
        }
        
        return new Vector4(0f, 0f, 0f, -1f);
    }

    private void ExecuteBuffer()
    {
        
        
        m_context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
    }

    public void Render()
    {
        if (m_shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            m_buffer.GetTemporaryRT(
                DirShadowAtlasId, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
                );
        }
        m_buffer.BeginSample(BufferName);
        SetKeywords(s_shadowMaskKeywords, m_useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 :-1);
        m_buffer.EndSample(BufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows()
    {
        int atlasSize = (int)m_settings._directional._atlasSize;
        m_buffer.GetTemporaryRT(
            DirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
        );
        m_buffer.SetRenderTarget(
                DirShadowAtlasId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
        m_buffer.ClearRenderTarget(true, false, Color.clear);
        ExecuteBuffer();
        m_buffer.BeginSample(BufferName);

        int tiles = m_shadowedDirectionalLightCount * m_settings._directional._cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        
        for (int i = 0; i < m_shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        m_buffer.SetGlobalInt(CascadeCountId, m_settings._directional._cascadeCount);
        m_buffer.SetGlobalVectorArray(CascadeCullingSpheresId, s_cascadeCullingSpheres);
        m_buffer.SetGlobalVectorArray(CascadeDataId, s_cascadeData);
        m_buffer.SetGlobalMatrixArray(DirShadowMatricesId, s_dirShadowMatrices);
        float f = 1f - m_settings._directional._cascadeFade;
        m_buffer.SetGlobalVector(
            ShadowDistanceFadeId,
            new Vector4(1f / m_settings._maxDistance, 1f / m_settings._distanceFade, 1f / (1f - f * f))
        );
        SetKeywords(s_directionalFilterKeywords, (int)m_settings._directional._filter - 1);
        SetKeywords(s_cascadeBlendKeywords, (int)m_settings._directional._cascadeBlend - 1);
        m_buffer.SetGlobalVector(ShadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        m_buffer.EndSample(BufferName);
        ExecuteBuffer();
    }

    private void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++) {
            if (i == enabledIndex) {
                m_buffer.EnableShaderKeyword(keywords[i]);
            }
            else {
                m_buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    private Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, int split) {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        m_buffer.SetViewport(new Rect(
                offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
            ));
        return offset;
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = m_shadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(m_cullingResults, light.VisibleLightIndex);
        int cascadeCount = m_settings._directional._cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = m_settings._directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - m_settings._directional._cascadeFade);

        for (int i = 0; i < cascadeCount; i++)
        {
            m_cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.VisibleLightIndex, i, cascadeCount, ratios, tileSize, light.NearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData
            );// The split data contains information about how shadow-casting objects should be culled.
            shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = shadowSplitData;
            if (index == 0) //culling spheres are only affected by camera
            {
                SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);
            }
            int tileIndex = tileOffset + i;
            s_dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split
            );
            m_buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            m_buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
            ExecuteBuffer();
            m_context.DrawShadows(ref shadowSettings);
            m_buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)m_settings._directional._filter + 1f);
        cullingSphere.w -= filterSize; // avoid to sample outside
        cullingSphere.w *= cullingSphere.w;
        s_cascadeCullingSpheres[index] = cullingSphere;
        s_cascadeData[index] = new Vector4(
            1f / cullingSphere.w,
            filterSize * 1.4142136f // when self occlusion by diagonal, the length should be √2 * texel size
        );
    }

    public void Cleanup()
    {
        m_buffer.ReleaseTemporaryRT(DirShadowAtlasId);
        ExecuteBuffer();
    }
}