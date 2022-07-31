using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    enum ShadowMode {
        On, Clip, Dither, Off
    }
    
    private MaterialEditor m_editor;
    private Object[] m_materials;
    private MaterialProperty[] m_properties; 
    private bool m_showPresets;
    
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUI.BeginChangeCheck();
        base.OnGUI(materialEditor, properties);
        m_editor = materialEditor;
        m_materials = materialEditor.targets;
        m_properties = properties;

        BakeEmission();
        
        EditorGUILayout.Space();
        m_showPresets = EditorGUILayout.Foldout(m_showPresets, "Presets", true);
        if (m_showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }
        if (EditorGUI.EndChangeCheck()) {
            SetShadowCasterPass();
            CopyLightMappingProperties();
        }
    }

    private void CopyLightMappingProperties()
    {
        MaterialProperty mainTex = FindProperty("_MainTex", m_properties, false);
        MaterialProperty baseMap = FindProperty("_BaseMap", m_properties, false);
        if (mainTex != null && baseMap != null)
        {
            mainTex.textureValue = baseMap.textureValue;
            mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
        }
        MaterialProperty color = FindProperty("_Color", m_properties, false);
        MaterialProperty baseColor = FindProperty("_BaseColor", m_properties, false);
        if (color != null && baseColor != null)
        {
            color.colorValue = baseColor.colorValue;
        }
    }

    private void BakeEmission()
    {
        EditorGUI.BeginChangeCheck();
        m_editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material m in m_editor.targets)
            {
                m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }

    private bool SetProperty(string name, float value)
    {
        var property = FindProperty(name, m_properties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }

        return false;
    }
    bool HasProperty (string name) =>
        FindProperty(name, m_properties, false) != null;
    
    bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

    void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material material in m_materials)
            {
                material.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material material in m_materials)
            {
                material.DisableKeyword(keyword);
            }
        }
    }
    
    
    ShadowMode Shadows {
        set {
            if (SetProperty("_Shadows", (float)value))
            {
                SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
            }
        }
    }
    
    
    void SetShadowCasterPass () {
        MaterialProperty shadows = FindProperty("_Shadows", m_properties, false);
        if (shadows == null || shadows.hasMixedValue) { // has mixed value: if all selected materials are set to the same mode
            return;
        }
        bool enabled = shadows.floatValue < (float)ShadowMode.Off;
        foreach (Material m in m_materials) {
            m.SetShaderPassEnabled("ShadowCaster", enabled);
        }
    }

    void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyword(keyword, value);
        }
    }
    
    RenderQueue RenderQueue {
        set {
            foreach (Material m in m_materials) {
                m.renderQueue = (int)value;
            }
        }
    }
    
    bool Clipping {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    bool PremultiplyAlpha {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    BlendMode SrcBlend {
        set => SetProperty("_SrcBlend", (float)value);
    }

    BlendMode DstBlend {
        set => SetProperty("_DstBlend", (float)value);
    }

    bool ZWrite {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }
    
    bool PresetButton (string name) {
        if (GUILayout.Button(name)) {
            m_editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }
    
    void OpaquePreset () {
        if (PresetButton("Opaque")) {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    void ClipPreset () {
        if (PresetButton("Clip")) {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }
    
    void FadePreset () {
        if (PresetButton("Fade")) {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    void TransparentPreset () {
        if (HasPremultiplyAlpha && PresetButton("Transparent")) {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
}