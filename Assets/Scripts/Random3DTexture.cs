using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System;
using Unity.Collections;

[CustomEditor(typeof(Random3DTexture))]
[CanEditMultipleObjects]
public class Random3DTextureEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector ();
        if(GUILayout.Button("Generate"))
        {
            ((Random3DTexture)target).GenerateTexture();
        }
    }
}

[ExecuteInEditMode]
class Random3DTexture : MonoBehaviour {
    public Vector3 offset;
    public Vector3Int dimensions;

    [SerializeField]
    RenderTexture texture;
    public ComputeShader textureComputeShader;
    public void GenerateTexture(bool save = false){
        if(dimensions.x < 1 || dimensions.y < 1 || dimensions.z < 1){
            return;
        }
        texture = new RenderTexture(dimensions.x, dimensions.y, 0, RenderTextureFormat.ARGB32){
            enableRandomWrite = true,
            dimension = TextureDimension.Tex3D,
            volumeDepth = dimensions.z
        };
        texture.Create();
        textureComputeShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z, 0));
        textureComputeShader.SetTexture(0, "outputTexture", texture);
        textureComputeShader.Dispatch(0,dimensions.x, dimensions.y, dimensions.z);
        GetComponent<ChunkController>().texture = texture;
        GetComponent<ChunkController>().GenerateMesh();
    }
}