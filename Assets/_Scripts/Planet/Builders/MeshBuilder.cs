using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Takes in a Grid and outputs meshes of the grid
/// </summary>
public class MeshBuilder
{
    public MeshShader meshShader;
    public ChunkShader chunkShader;
    public MeshParameters meshParameters;
    

    public MeshBuilder(MeshParameters meshParameters, NoiseParameters chunkNoiseParameters, ComputeBuffer gridBuffer)
    {
        this.meshParameters = meshParameters;

        chunkShader = new ChunkShader(gridBuffer, chunkNoiseParameters);
        meshShader = new MeshShader(meshParameters);
    }

    public Mesh Build(Chunk chunk, Transform parent)
    {
        var mesh = new Mesh();

        // support for ~4.3M vs ~65k vertices
        if (meshParameters.use32IndexFormat)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        var osChunkBuffer = chunkShader.GetChunk(chunk);

        var (tris,verts) = meshShader.GetTriangles(chunk, osChunkBuffer);
        ConvertTrisToMesh(tris, verts, ref mesh, parent);

        if (meshParameters.flat_shade)
            mesh.RecalculateNormals();

        return mesh;
    }

    private void ConvertTrisToMesh(TriStruct[] tris, Vertex[] verts, ref Mesh mesh, Transform parent)
    {
        var triLen = tris.Length;
        var vertLen = verts.Length;

        var vertices = new Vector3[vertLen];
        var normals = new Vector3[vertLen];
        var triangles = new int[triLen*3];


        for (int i = 0; i < vertLen; i++)
        {
            var vertex = verts[i];
            vertices[i] = parent.InverseTransformPoint(vertex.position);
            normals[i] = parent.InverseTransformDirection(vertex.normal);
        }

        for (int j = 0; j < triLen; j++)
        {
            var i = j * 3;
            TriStruct tri = tris[j];

            if (meshParameters.invert_normals)
            {
                triangles[i] = tri.v3;
                triangles[i + 1] = tri.v2;
                triangles[i + 2] = tri.v1;

            }
            else
            {
                triangles[i] = tri.v1;
                triangles[i + 1] = tri.v2;
                triangles[i + 2] = tri.v3;
            
            }
            
        }
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
    }
}
