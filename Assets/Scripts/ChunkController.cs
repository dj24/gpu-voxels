using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

[ExecuteInEditMode]
public class ChunkController : MonoBehaviour
{   
    int indexCount;
    [SerializeField]
    int vertexCount;
    [SerializeField]
    int voxelCount;
    public ComputeShader computeShader;
    public ComputeShader faceComputeShader;
    ComputeBuffer argsBuffer;
    ComputeBuffer faceBuffer;
    MeshFilter meshFilter { get => GetComponent<MeshFilter>(); }
    public RenderTexture texture;
    void Start(){
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        var width = texture.width;
        var height = texture.height;
        var depth = texture.volumeDepth;

        voxelCount = width * height * depth;
        
        var faceCount = 6 * voxelCount;
        vertexCount = faceCount * 6; // 6 Verts per face 6 faces per voxel
        indexCount = faceCount * 6; // 6 Tris per face

        faceBuffer = new ComputeBuffer(voxelCount * 6, 16, ComputeBufferType.Append);
        faceBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] {1,1,1,1});
        faceComputeShader.SetTexture(0, "inputTexture", texture);
        faceComputeShader.SetBuffer(0,"faces", faceBuffer);
        faceComputeShader.SetVector("dimensions", new Vector4(width, height, depth, 0));
        faceComputeShader.Dispatch(0, width, height, depth);
        ComputeBuffer.CopyCount(faceBuffer, argsBuffer, 0);

        AsyncGPUReadback.Request(argsBuffer, (AsyncGPUReadbackRequest request) => {
            NativeArray<int> args = request.GetData<int>(0);
            vertexCount = args[0] * 6;
            indexCount = args[0] * 6;   
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
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
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
            computeShader.SetTexture(0, "inputTexture", texture);
            computeShader.DispatchIndirect(0, argsBuffer);
            
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
    
}
