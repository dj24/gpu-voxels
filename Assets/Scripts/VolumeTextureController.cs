using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;
using Unity.Collections;

[CustomEditor(typeof(VolumeTextureController))]
[CanEditMultipleObjects]
public class VolumeTextureEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector ();
        if(GUILayout.Button("Generate"))
        {
            ((VolumeTextureController)target).GenerateMesh();
        }
    }
}

public class VolumeTextureController : VoxelMeshController
{
    public Texture3D texture;
    public void GenerateMesh(){
        renderTexture = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32){
            enableRandomWrite = true,
            dimension = TextureDimension.Tex3D,
            volumeDepth = texture.depth
        };
        renderTexture.Create();
        Graphics.CopyTexture(texture,renderTexture);
        base.GenerateMesh();
    }
}
