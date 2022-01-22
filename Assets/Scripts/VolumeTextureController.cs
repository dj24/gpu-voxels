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
            ((VolumeTextureController)target).GenerateTexture();
        }
    }
}

public class VolumeTextureController : MonoBehaviour
{
    public Texture3D texture;
    public void GenerateTexture(bool save = false){
        var rt = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32){
            enableRandomWrite = true,
            dimension = TextureDimension.Tex3D,
            volumeDepth = texture.depth
        };
        rt.Create();
        Graphics.CopyTexture(texture,rt);
        GetComponent<ChunkController>().texture = rt;
        GetComponent<ChunkController>().GenerateMesh();
    }
}
