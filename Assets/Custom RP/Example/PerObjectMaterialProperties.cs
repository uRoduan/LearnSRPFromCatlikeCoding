using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
    private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");


    private static MaterialPropertyBlock s_block;

    [SerializeField, Range(0f, 1f)] 
    private float _cutoff = 0.5f;

    [SerializeField, Range(0f, 1f)] 
    private float _metallic = 0f;

    [SerializeField, Range(0f, 1f)] 
    private float _smoothness = 0.5f;

    [SerializeField] 
    private Color _baseColor = Color.white;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        s_block ??= new MaterialPropertyBlock();
        
        s_block.SetColor(BaseColorId, _baseColor);
        s_block.SetFloat(CutoffId, _cutoff);
        s_block.SetFloat(MetallicId, _metallic);
        s_block.SetFloat(SmoothnessId, _smoothness);
        GetComponent<Renderer>().SetPropertyBlock(s_block);
    }
}