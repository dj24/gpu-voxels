using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

public enum VoxelRenderType {
    Blocks, MarchingCubes
}

[ExecuteInEditMode]
public class VoxelMeshController : MonoBehaviour
{   
    int indexCount, vertexCount, voxelCount, width, height, depth;
    public VoxelRenderType renderType;
    public ComputeShader marchingCubesComputeShader;
    public ComputeShader marchingCubeTrianglesComputeShader;
    public ComputeShader computeShader;
    public ComputeShader faceComputeShader;
    ComputeBuffer argsBuffer, faceBuffer, triBuffer;
    MeshFilter meshFilter { get => GetComponent<MeshFilter>(); }
    protected RenderTexture renderTexture;
    void Start(){
        GenerateMesh();
    }

    void GenerateBlocks(){
        var faceCount = 6 * voxelCount;
        vertexCount = faceCount * 6; // 6 Verts per face 6 faces per voxel
        indexCount = faceCount * 6; // 6 Tris per face

        faceBuffer = new ComputeBuffer(voxelCount * 6, 16, ComputeBufferType.Append);
        faceBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] {1,1,1,1});
        faceComputeShader.SetTexture(0, "inputTexture", renderTexture);
        faceComputeShader.SetBuffer(0,"faces", faceBuffer);
        faceComputeShader.SetVector("dimensions", new Vector4(width, height, depth, 0));
        faceComputeShader.Dispatch(0, width, height, depth);
        ComputeBuffer.CopyCount(faceBuffer, argsBuffer, 0);

        AsyncGPUReadback.Request(argsBuffer, (AsyncGPUReadbackRequest request) => {
            NativeArray<int> args = request.GetData<int>(0);
            vertexCount = args[0] * 6;
            indexCount = args[0] * 6;   
            args.Dispose();
            Debug.Log(vertexCount);
            var mesh = new Mesh();

            mesh.SetVertexBufferParams(
                vertexCount, 
                new VertexAttributeDescriptor(VertexAttribute.Position, stream:0), 
                new VertexAttributeDescriptor(VertexAttribute.Normal, stream:1),
                new VertexAttributeDescriptor(VertexAttribute.Color, stream:2)
            );
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0,indexCount, MeshTopology.Triangles), MeshUpdateFlags.DontRecalculateBounds);
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            mesh.bounds = new Bounds(new Vector3(width, height, depth) / 2, new Vector3(width, height, depth));

            var indexBuffer = mesh.GetIndexBuffer();
            var vertexBuffer = mesh.GetVertexBuffer(0);
            var normalBuffer = mesh.GetVertexBuffer(1);
            var colourBuffer = mesh.GetVertexBuffer(2);

            computeShader.SetBuffer(0,"faceBuffer", faceBuffer);
            computeShader.SetBuffer(0, "indexBuffer", indexBuffer);
            computeShader.SetBuffer(0, "vertexBuffer", vertexBuffer);
            computeShader.SetBuffer(0, "normalBuffer", normalBuffer);
            computeShader.SetBuffer(0, "colourBuffer", colourBuffer);
            computeShader.SetTexture(0, "inputTexture", renderTexture);
            computeShader.DispatchIndirect(0, argsBuffer);
            
            var vertices = new Vector3[vertexCount];
            vertexBuffer.GetData(vertices);
            Debug.Log(vertices[0]);

            meshFilter.mesh = mesh;
            
            mesh.UploadMeshData(true);
            indexBuffer.Dispose();
            vertexBuffer.Dispose();
            normalBuffer.Dispose();
            colourBuffer.Dispose();
            faceBuffer.Dispose();
            argsBuffer.Dispose();
        });
    }

    // void OnDrawGizmosSelected()
    // {
    //     // Draw a semitransparent blue cube at the transforms position
    //     Gizmos.color = new Color(1, 0, 0, 0.25f);
    //     for(int x = 0; x < renderTexture.width; x ++){
    //         for(int y = 0; y < renderTexture.height; y ++){
    //             for(int z = 0; z < renderTexture.volumeDepth; z ++){
    //                 Gizmos.DrawWireCube(new Vector3(x,y,z), new Vector3(1, 1, 1));
    //             }
    //         }
    //     }
    // }

    void MarchCubes(){
        var faceCount = 6 * voxelCount;
        vertexCount = faceCount * 6; // 6 Verts per face 6 faces per voxel
        indexCount = faceCount * 6; // 6 Tris per face
        int maxTrisPerVoxel = 4;
        int triangleStride = 12 * 5; // float3 (12bytes) * 5
        triBuffer = new ComputeBuffer(voxelCount * 4, triangleStride, ComputeBufferType.Append);
        triBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] {1,1,1,1});
        marchingCubeTrianglesComputeShader.SetTexture(0, "inputTexture", renderTexture);
        marchingCubeTrianglesComputeShader.SetBuffer(0,"triangleBuffer", triBuffer);
        marchingCubeTrianglesComputeShader.Dispatch(0, width + 1, height + 1, depth + 1);
        ComputeBuffer.CopyCount(triBuffer, argsBuffer, 0);

        AsyncGPUReadback.Request(argsBuffer, (AsyncGPUReadbackRequest request) => {
            NativeArray<int> args = request.GetData<int>(0);
            vertexCount = args[0] * 3;
            Debug.Log(vertexCount);
            indexCount = args[0] * 3;   
            args.Dispose();

            var mesh = new Mesh();

            mesh.SetVertexBufferParams(
                vertexCount, 
                new VertexAttributeDescriptor(VertexAttribute.Position, stream:0), 
                new VertexAttributeDescriptor(VertexAttribute.Normal, stream:1),
                new VertexAttributeDescriptor(VertexAttribute.Color, stream:2)
            );
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0,indexCount, MeshTopology.Triangles), MeshUpdateFlags.DontRecalculateBounds);
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            mesh.bounds = new Bounds(new Vector3(width, height, depth) / 2, new Vector3(width, height, depth));

            var indexBuffer = mesh.GetIndexBuffer();
            var vertexBuffer = mesh.GetVertexBuffer(0);
            var normalBuffer = mesh.GetVertexBuffer(1);
            var colourBuffer = mesh.GetVertexBuffer(2);

            marchingCubesComputeShader.SetBuffer(0,"triBuffer", triBuffer);
            marchingCubesComputeShader.SetBuffer(0, "indexBuffer", indexBuffer);
            marchingCubesComputeShader.SetBuffer(0, "vertexBuffer", vertexBuffer);
            marchingCubesComputeShader.SetBuffer(0, "normalBuffer", normalBuffer);
            marchingCubesComputeShader.SetBuffer(0, "colourBuffer", colourBuffer);
            marchingCubesComputeShader.SetTexture(0, "inputTexture", renderTexture);
            marchingCubesComputeShader.DispatchIndirect(0, argsBuffer);

            var vertices = new Vector3[vertexCount];
            vertexBuffer.GetData(vertices);
            // foreach(var vert in vertices){
            //     Debug.Log(vert);
            // }
            
            meshFilter.mesh = mesh;
           
            mesh.UploadMeshData(true);
            indexBuffer.Dispose();
            vertexBuffer.Dispose();
            normalBuffer.Dispose();
            colourBuffer.Dispose();
            triBuffer.Dispose();
            argsBuffer.Dispose();
        });
    }

    public void GenerateMesh()
    {
        if(renderTexture == null){
            return;
        }
        width = renderTexture.width;
        height = renderTexture.height;
        depth = renderTexture.volumeDepth;
        voxelCount = width * height * depth;
        
        switch(renderType){
            case VoxelRenderType.Blocks:
                GenerateBlocks();
                break;
            case VoxelRenderType.MarchingCubes:
                MarchCubes();
                break;
        }
    }
    
}
