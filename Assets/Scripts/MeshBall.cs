using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int s_baseColorId = Shader.PropertyToID("_BaseColor"),
		s_metallicId = Shader.PropertyToID("_Metallic"),
		s_smoothnessId = Shader.PropertyToID("_Smoothness");

    private const int MAX_INSTANCE_COUNT = 500;

    private Matrix4x4[] m_matrices = new Matrix4x4[MAX_INSTANCE_COUNT];
    private Vector4[] m_baseColors = new Vector4[MAX_INSTANCE_COUNT];
    private readonly float[] m_metallic = new float[MAX_INSTANCE_COUNT];
    private readonly float[] m_smoothness = new float[MAX_INSTANCE_COUNT];


    private MaterialPropertyBlock m_block;

    [SerializeField]
    private Mesh _mesh = default;

    [SerializeField] 
    private Material _material = default;

    private void Awake()
    {
        for (int i = 0; i < m_matrices.Length; i++) {
            m_matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10f,
                Quaternion.Euler(
                    Random.value * 360f, Random.value * 360f, Random.value * 360f
                ),
                Vector3.one * Random.Range(0.5f, 1.5f)
            );
            m_baseColors[i] =
                new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
            m_metallic[i] = Random.value < 0.25f ? 1f : 0f;
            m_smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_block == null) {
            m_block = new MaterialPropertyBlock();
            m_block.SetVectorArray(s_baseColorId, m_baseColors);
			m_block.SetFloatArray(s_metallicId, m_metallic);
			m_block.SetFloatArray(s_smoothnessId, m_smoothness);
        }
        Graphics.DrawMeshInstanced(_mesh, 0, _material, m_matrices, MAX_INSTANCE_COUNT, m_block);
    }
}
