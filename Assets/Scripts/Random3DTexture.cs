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
            ((Random3DTexture)target).GenerateMesh();
        }
    }
}

[ExecuteInEditMode]
class Random3DTexture : VoxelMeshController {
    public Vector3 offset;
    public Vector3Int dimensions;
    public ComputeShader textureComputeShader;
    public void GenerateMesh(){
        if(dimensions.x < 1 || dimensions.y < 1 || dimensions.z < 1){
            return;
        }
        renderTexture = new RenderTexture(dimensions.x, dimensions.y, 0, RenderTextureFormat.ARGB32){
            enableRandomWrite = true,
            dimension = TextureDimension.Tex3D,
            volumeDepth = dimensions.z
        };
        renderTexture.Create();
        textureComputeShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z, 0));
        textureComputeShader.SetTexture(0, "outputTexture", renderTexture);
        textureComputeShader.Dispatch(0,dimensions.x, dimensions.y, dimensions.z);
        base.GenerateMesh();
    }
}