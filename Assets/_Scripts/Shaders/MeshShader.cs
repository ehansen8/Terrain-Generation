using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Multi-Pass shader to
/// A: Calculate chunk normals;
/// B: Calculate interpolated vertices + normals
/// C: Build Triangle from said vertices
/// D: Returns TriangleStruct[]
/// </summary>
public class MeshShader
{
    public ComputeShader shader;

    public int vertex_kernel;
    public int normal_kernel;
    public int tri_kernel;

    public MeshShader(MeshParameters mesh_params)
    {
        shader = Utilities.FindShader("MarchCubes");

        normal_kernel = shader.FindKernel("CalculateNormals");
        vertex_kernel = shader.FindKernel("CalculateVertices");
        tri_kernel = shader.FindKernel("CalculateTriangles");

        SetMeshVariables(mesh_params);
    }

    private void SetMeshVariables(MeshParameters mesh)
    {
        shader.SetBool(Shader.PropertyToID("interpolate"), mesh.interpolate);
    }

    /// Gets the list of triangles that make up the mesh for the passed in chunk
    public (TriStruct[], Vertex[]) GetTriangles(Chunk chunk, ComputeBuffer osChunkBuffer)
    {
        
        
        SetChunkVariables(chunk);
        var buffers = SetChunkBuffers(chunk, osChunkBuffer);
        
        DispatchShader(chunk.final_res);

        //Get Data
        var triangles = new TriStruct[GetTriangleCount(buffers.triBuffer)];
        var vertices = new Vertex[GetVertexCount(buffers.vertexCountBuffer)];

        buffers.orderedVertexBuffer.GetData(vertices);
        buffers.triBuffer.GetData(triangles);

        //Release all buffers
        buffers.ReleaseAll();
        osChunkBuffer.Release();

        return (triangles, vertices);
    }

    private void SetChunkVariables(Chunk chunk)
    {
        //Set instance variables
        shader.SetFloats(Shader.PropertyToID("gridPosOffset"), chunk.pos_offset);
        shader.SetInt(Shader.PropertyToID("chunk_grid_res"), chunk.final_grid_res);
        shader.SetInt(Shader.PropertyToID("oversize_chunk_grid_res"), chunk.final_grid_res + 2);
        shader.SetFloat(Shader.PropertyToID("increment"), chunk.increment);
    }

    private MeshBuffers SetChunkBuffers(Chunk chunk, ComputeBuffer osChunkBuffer)
    {
        var buffers = new MeshBuffers(chunk);
        //Pass 1
        shader.SetBuffer(normal_kernel, "normalBuffer", buffers.normalBuffer);
        shader.SetBuffer(normal_kernel, "chunkBuffer", buffers.chunkBuffer);
        shader.SetBuffer(normal_kernel, "osChunkBuffer", osChunkBuffer);    //This is passed in

        //Pass 2
        shader.SetBuffer(vertex_kernel, "normalBuffer", buffers.normalBuffer);
        shader.SetBuffer(vertex_kernel, "vertexBuffer", buffers.vertexBuffer);
        shader.SetBuffer(vertex_kernel, "chunkBuffer", buffers.chunkBuffer);

        //Pass 3
        shader.SetBuffer(tri_kernel, "vertexBuffer", buffers.vertexBuffer);
        shader.SetBuffer(tri_kernel, "triBuffer", buffers.triBuffer);
        shader.SetBuffer(tri_kernel, "ordered_vertex_buffer", buffers.orderedVertexBuffer);
        shader.SetBuffer(tri_kernel, "vCount", buffers.vertexCountBuffer);
        shader.SetBuffer(tri_kernel, "chunkBuffer", buffers.chunkBuffer);

        return buffers;
    }

    private void DispatchShader(int chunk_res)
    {
        int vertex_groups = (chunk_res / 8) + 1;
        int mc_groups = chunk_res / 8;

        shader.Dispatch(normal_kernel, vertex_groups, vertex_groups, vertex_groups);
        shader.Dispatch(vertex_kernel, vertex_groups, vertex_groups, vertex_groups);
        shader.Dispatch(tri_kernel, mc_groups, mc_groups, mc_groups);
    }
    
    private int GetTriangleCount(ComputeBuffer triBuffer)
    {

        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(triBuffer, countBuffer, 0);

        int[] counter = new int[1] { 0 };
        countBuffer.GetData(counter);

        countBuffer.Release();

        return counter[0];
    }

    private int GetVertexCount(ComputeBuffer vCount)
    {
        int[] counter = new int[1] { 0 };
        vCount.GetData(counter);

        return counter[0];
    }
    
    private struct MeshBuffers
    {
        public ComputeBuffer vertexBuffer;
        public ComputeBuffer normalBuffer;
        public ComputeBuffer chunkBuffer;
        public ComputeBuffer triBuffer;
        public ComputeBuffer orderedVertexBuffer;
        public ComputeBuffer vertexCountBuffer;

        public MeshBuffers(Chunk chunk)
        {
            //Set up Instance Buffers
            var vert_len = (int)Mathf.Pow(chunk.final_grid_res, 3);
            var tri_len = (int)Mathf.Pow(chunk.final_res, 3) * 5;

            vertexBuffer = new ComputeBuffer(vert_len, EdgeStruct.GetSize());
            normalBuffer = new ComputeBuffer(vert_len, sizeof(float) * 3);
            chunkBuffer = new ComputeBuffer(vert_len, sizeof(float) * 3);
            triBuffer = new ComputeBuffer(tri_len, TriStruct.GetSize(), ComputeBufferType.Append);
            orderedVertexBuffer = new ComputeBuffer(vert_len, Vertex.GetSize());
            vertexCountBuffer = new ComputeBuffer(1, sizeof(int));

            triBuffer.SetCounterValue(0);
            vertexCountBuffer.SetData(new int[1] { 0 });
        }

        public void ReleaseAll()
        {
            normalBuffer.Release();
            vertexBuffer.Release();
            triBuffer.Release();
            orderedVertexBuffer.Release();
            vertexCountBuffer.Release();
            chunkBuffer.Release();
        }
    }

    private void DebugNormals(ComputeBuffer normalBuffer)
    {
        var data = new Vector3[normalBuffer.count];
        normalBuffer.GetData(data);
    }
    private void DebugChunk(ComputeBuffer buffer, ComputeBuffer gridBuffer)
    {
        var data = new float[buffer.count];
        var grid = new float[gridBuffer.count];
        gridBuffer.GetData(grid);
        buffer.GetData(data);
        var combo = new List<Dictionary<string, object>>();
        for (int i = 0; i < data.Length; i++)
        {

            var w = data[i];
            var z = i / (17 * 17);
            var temp_i = i - z * (17 * 17);
            var y = temp_i / 17;
            var x = temp_i - y * 17;

            var new_id = new Vector3(x, y, z);
            var thread_id = new Vector3(x / 2, y / 2, z / 2);
            var dir = new_id - thread_id * 2;

            var lerp = thread_id + dir;
            var lerp_id = lerp.x + lerp.y * 9 + lerp.z * (9 * 9);
            var grid_id = thread_id.x + thread_id.y * 9 + thread_id.z * (9 * 9);
            var d = new Dictionary<string, object>
{
    {"phi", w },
    {"new_id", new_id },
    {"thread_id", thread_id},
    {"dir", dir },
    {"lerp", lerp},
    {"chunk_idx", i },
    {"grid_idx", grid_id },
    {"lerp_idx", lerp_id },
    {"grid_val", grid[(int)grid_id] },
    {"lerp_val", grid[(int)lerp_id] },

};

            combo.Add(d);
        }

    }

    private void DebugVerts(ComputeBuffer vertexBuffer)
    {
        var data = new EdgeStruct[vertexBuffer.count];
        vertexBuffer.GetData(data);
    }
}