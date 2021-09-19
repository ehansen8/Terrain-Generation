using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
public class MeshGen : MonoBehaviour
{ 
    Mesh mesh;

    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Color[] colors;
    public Vector2[] uv;

    public int vertCount;
    public float radius = 5.0f;
    public float depth = 5.0f;
    public int radialRes = 20;
    public int depthRes = 20;
    public bool rebuildMesh = false;

    [Range(0f, 10f)]
    public float strength = 1f;

    public bool damping;

    public float frequency = 1f;

    [Range(1, 8)]
    public int octaves = 1;

    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Range(0f, 1f)]
    public float persistence = 0.5f;
    [Range(1, 3)]
    public int dimensions = 3;

    public NoiseMethodType type;
    public Gradient coloring;



    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape(radius, depth, radialRes, depthRes);
        UpdateMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (rebuildMesh)
        {
            CreateShape(radius, depth, radialRes, depthRes);
            rebuildMesh = false;
        }
        UpdateMesh();
    }

    void CreateShape(float radius, float length, int radialRes, int depthRes)
    {
        NoiseMethod method = Noise.methods[(int)type][dimensions - 1];

        // need plus 1 because we have a single line of overlapping points
        List<int> tris = new List<int>();
        vertices = new Vector3[(radialRes) * depthRes];
        colors = new Color[vertices.Length];
        normals = new Vector3[vertices.Length];
        uv = new Vector2[vertices.Length];

        float angleIncrement = 2*Mathf.PI / (radialRes-1);
        float depthIncrement = length / depthRes;
        float maxAmp = 0;
        float minAmp = 1000000;
        for (int v = 0, depth = 0; depth < depthRes; depth++)
        {
            for (int a = 0; a < radialRes; a++, v++)
            {
                float cos = Mathf.Cos(a * angleIncrement);
                float sin = Mathf.Sin(a * angleIncrement);
                float x = radius * cos;
                float y = radius * sin;
                float z = depth * depthIncrement;

                Vector3 point = new Vector3(x, y, z);
                NoiseSample sample = Noise.Sum(method, point, frequency, octaves, lacunarity, persistence);
                colors[v] = coloring.Evaluate(sample.value/0.5f + 0.5f);
                float amplitude = damping ? strength / frequency : strength;
                sample *= amplitude;

                if (sample.value > maxAmp)
                {
                    Debug.Log(sample.value);
                    maxAmp = sample.value;
                }

                if (sample.value < minAmp)
                {
                    Debug.Log(sample.value);
                    minAmp = sample.value;
                }

                float newRad = radius + sample.value;
                float newX = newRad * cos;
                float newY = newRad * sin;
                vertices[v] = new Vector3(newX, newY, z);
                normals[v] = 1*sample.derivative.normalized;
                // if last row (i == n-1), then skip creating triangles
                if (depth != depthRes - 1)
                {
                    // if first only go across
                    if (a == 0)
                    {
                        tris.AddRange(createTri(v, false, radialRes));
                    }

                    // if last only go diagonal
                    else if (a == radialRes)
                    {
                        tris.AddRange(createTri(v, true, radialRes));
                    }

                    // otherwise go diagonal and then across
                    else
                    {
                        tris.AddRange(createTri(v, true, radialRes));
                        tris.AddRange(createTri(v, false, radialRes));
                    }
                }
            }
        }

        triangles = tris.ToArray();
        
    }

    // isDiagonal means up up from current diagonal, otherwise go across
    // wrap is the line count to add to go directly across the grid
    int[] createTri(int position, bool isDiagonal, int wrap)
    {
        int across = position + wrap;
        int acrossLeft = across - 1;
        int right = position + 1;
        if (isDiagonal)
        {
            return new int[] { position, acrossLeft, across };
        }
        else
        {
            return new int[] { position, across, right };
        }
    }
        

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.colors = colors;
        mesh.normals = normals;
        mesh.uv = uv;
    }
}
