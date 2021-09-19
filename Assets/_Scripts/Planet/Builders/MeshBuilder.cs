using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Takes in a Grid and outputs meshes of the grid
/// </summary>
public class MeshBuilder
{
    public MeshShader meshShader;
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
    }

    public Mesh GetMesh(Chunk chunk, Transform parent)
    {
        var mesh = new Mesh();

        // support for ~4.3M vs ~65k vertices
        if (use32IndexFormat)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        var tris = meshShader.GetTriangles(chunk);
        ConvertTrisToMesh(tris, ref mesh, parent);

        if (flat_shade)
            mesh.RecalculateNormals();

        return mesh;
    }

    private void ConvertTrisToMesh(TriStruct[] tris, ref Mesh mesh, Transform parent)
    {
        var triLen = tris.Length;
        var vertLen = triLen * 3;

        var vertices = new Vector3[vertLen];
        var normals = new Vector3[vertLen];
        var colors = new Color[vertLen];
        var triangles = new int[vertLen];


        for (int j = 0; j < tris.Length; j++)
        {
            TriStruct tri = tris[j];

            int i = j * 3;

            vertices[i] = parent.InverseTransformPoint(tri.v1.position);

            colors[i] = (EvalVertColor(tri.v1));


            vertices[i + 1] = parent.InverseTransformPoint(tri.v2.position);

            colors[i + 1] = (EvalVertColor(tri.v2));


            vertices[i + 2] = parent.InverseTransformPoint(tri.v3.position);

            colors[i + 2] = (EvalVertColor(tri.v3));


            if (invert_normals)
            {
                triangles[i] = (i + 2);
                triangles[i + 1] = (i + 1);
                triangles[i + 2] = (i);

                normals[i] = parent.InverseTransformDirection(tri.v1.normal.normalized);
                normals[i + 1] = parent.InverseTransformDirection(tri.v2.normal.normalized);
                normals[i + 2] = parent.InverseTransformDirection(tri.v3.normal.normalized);

            }
            else
            {
                triangles[i] = (i);
                triangles[i + 1] = (i + 1);
                triangles[i + 2] = (i + 2);

                normals[i] = parent.InverseTransformDirection(tri.v1.normal.normalized);
                normals[i + 1] = parent.InverseTransformDirection(tri.v2.normal.normalized);
                normals[i + 2] = parent.InverseTransformDirection(tri.v3.normal.normalized);
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
