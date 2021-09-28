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
    public Planet planet;
    public bool use32IndexFormat;
    public bool invert_normals;
    public bool flat_shade;

    public MeshBuilder(Planet planet, bool use32IndexFormat, bool invert_normals, bool flat_shade)
    {
        this.planet = planet;
        this.use32IndexFormat = use32IndexFormat;
        this.invert_normals = invert_normals;
        this.flat_shade = flat_shade;

        meshShader = new MeshShader(planet.grid.gridBuffer,
                                    planet.mc,
                                    planet.iso,
                                    planet.interpolate);

        meshShader.Initialize(planet.global_res,
                                    planet.global_grid_res,
                                    planet.increments,
                                    planet.start_coordinates);

        chunkShader = new ChunkShader(planet, planet.chunk_shader);
        chunkShader.Initialize(planet.global_res,
                                    planet.global_grid_res,
                                    planet.increments,
                                    planet.start_coordinates);
    }

    public Mesh GetMesh(Chunk chunk, Transform parent)
    {
        var mesh = new Mesh();

        // support for ~4.3M vs ~65k vertices
        if (use32IndexFormat)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        var chunkBuffer = chunkShader.GetChunk(chunk);

        var (tris,verts) = meshShader.GetTriangles(chunk, chunkBuffer);
        ConvertTrisToMesh(tris, verts, ref mesh, parent);

        if (flat_shade)
            mesh.RecalculateNormals();

        return mesh;
    }

    private void ConvertTrisToMesh(TriStruct[] tris, Vertex[] verts, ref Mesh mesh, Transform parent)
    {
        var triLen = tris.Length;
        var vertLen = verts.Length;

        var vertices = new Vector3[vertLen];
        var normals = new Vector3[vertLen];
        var colors = new Color[vertLen];
        var triangles = new int[triLen*3];


        for (int i = 0; i < vertLen; i++)
        {
            var vertex = verts[i];
            vertices[i] = parent.InverseTransformPoint(vertex.position);
            normals[i] = parent.InverseTransformDirection(vertex.normal);
            colors[i] = (EvalVertColor(vertex));
        }

        for (int j = 0; j < triLen; j++)
        {
            var i = j * 3;
            TriStruct tri = tris[j];

            if (invert_normals)
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
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
    }

    private Color EvalVertColor(Vertex vert)
    {
        var pos = vert.position;
        var height = vert.position.magnitude;
        var normal = vert.normal;

        var gravity = -pos.normalized;
        // 0 Means they are perpendicular (i.e. the normal is perpendicular and the terrain is parallel -> steep)... 1 means the ground is flat
        
        if (height <= planet.sea_level_radius)
            return Color.blue;

        if (height >= planet.snowcap_radius)
            return Color.white;

        return planet.gradient.Evaluate(Mathf.Abs(Vector3.Dot(gravity,normal.normalized)));
    }
}
