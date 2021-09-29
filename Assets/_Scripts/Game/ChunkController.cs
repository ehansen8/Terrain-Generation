using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ChunkController : MonoBehaviour
{
    //make sure dimensions are uniform for now so global chunk size is consistant
    public Vector3 min_bounds;
    public Vector3 max_bounds;
    public bool takeStep;
    public int simSteps;
    public bool buildChunks;
    public bool clearGrid;
    public bool redrawMesh;
    public bool alwaysRedraw;
    public bool drawParticles;
    public bool drawNormals;
    public int resolution_factor;
    public bool addNoise;

    // Chunk parameters
    public int chunkRes = 32;
    public int chunkSize = 4;  // chunk dimensions in unit length;
    public int globalRes;
    public int globalGridRes;

    // MC parameters
    public float isoLevel = 0;
    public bool drawGrid = true;
    public bool onlyDrawInside = true;
    public bool drawVertices = true;
    public bool invertNormals = true;
    public bool interpolateVertices = false;

    // Set in Editor
    public Material meshMaterial;
    public ComputeShader densityShader;
    public ComputeShader marchShader;

    public GameObject planet_go;
    Planet planet;
    public Transform player;


    MarchingCubes MC;
    Dictionary<(int,int,int),Transform> activeChunks;
    List<Vector3Int> radialChunks;
    public Transform chunk_go;
    public Vector3Int chunkDims;

    // Debugging
    public List<(Vector3 center, Vector3 size)> boxCoords;
    //public List<EdgeStruct[]> gridList;
    public List<Vector4[]> gridList;
    public List<List<Water>> particlesList;
    public Gradient particleGradient;


    private Vector4[] grid;


    void Start()
    {
        planet = planet_go.GetComponent<Planet>();
        particlesList = new List<List<Water>>();

        RefreshGlobalParameters();

        activeChunks = new Dictionary<(int, int, int), Transform>();
        
        ConfigurePlanet();
        radialChunks = GetRadialChunks();
    }

    void RefreshGlobalParameters()
    {
        min_bounds = -1 * planet.bounds / 2;
        max_bounds = planet.bounds / 2;

        chunkDims = RoundUpVector((max_bounds - min_bounds) / chunkSize);
        globalRes = Mathf.Max(chunkDims.x, chunkDims.y, chunkDims.z) * chunkRes;
        globalGridRes = globalRes + 1;
    }

    private void Update()
    {
        if (clearGrid || alwaysRedraw)
        {
            clearGrid = false;
            RefreshGlobalParameters();
            planet.ClearGrid();
            ConfigurePlanet();
            foreach (var p in activeChunks.Values)
            {
                Destroy(p.gameObject);
            }
            activeChunks.Clear();
        }

        if(redrawMesh || alwaysRedraw)
        {
            redrawMesh = false;
            foreach (var p in activeChunks.Values)
            {
                Destroy(p.gameObject);
            }
            activeChunks.Clear();
        }
        if (buildChunks)
            BuildClosestChunks();

        if (takeStep)
        {
            for (int j = 0; j < simSteps; j++)
            {
                takeStep = false;
                //Water[] particles = 
                planet.manager.sim.RunSimulation(1);

                //for (int i = 0; i < particles.Length; i++)
                //{
                //    if (this.particlesList.Count <= i)
                //    {
                //        this.particlesList.Insert(i, new List<Water>());
                //        this.particlesList[i].Add(particles[i]);
                //    }
                //    else
                //    {
                //        this.particlesList[i].Add(particles[i]);
                //    }
                //} 
            }
            redrawMesh = true;
                
        }

    }

    void ConfigurePlanet()
    {
        planet.ConfigurePlanet(globalRes, globalGridRes, isoLevel, interpolateVertices);
    }

    void GenerateChunks(Vector3Int c)
    {
        var view = 2;
        var mins = Vector3Int.Max(c - Vector3Int.one*view, Vector3Int.zero);
        var maxs = Vector3Int.Min(c + Vector3Int.one * (1+view), chunkDims);
        for (int i = mins.x; i < maxs.x; i++)
        {
            for (int j = mins.y; j < maxs.y; j++)
            {
                for (int k = mins.z; k < maxs.z; k++)
                {
                    if (activeChunks.ContainsKey((i,j,k)))
                    {
                        continue;
                    }
                    else
                    {
                        var localStartBounds = min_bounds + (new Vector3(i, j, k) * chunkSize);
                        var localEndBounds = localStartBounds + (Vector3.one * chunkSize);
                        Vector3 center = (localStartBounds + localEndBounds) / 2;
                        var chunk_go = GameObject.Instantiate(this.chunk_go, center, Quaternion.identity, planet.transform);
                        var chunk = chunk_go.GetComponent<Chunk>();
                        chunk.InitChunk(localStartBounds, 
                                        localEndBounds, 
                                        new int[] { i * chunkRes, j * chunkRes, k * chunkRes },
                                        chunkRes, 
                                        chunkRes+1,
                                        resolution_factor,
                                        addNoise);

                        var mesh = planet.GetChunkMesh(chunk, chunk_go);
                        chunk_go.GetComponent<MeshFilter>().mesh = mesh;
                        chunk_go.GetComponent<MeshCollider>().sharedMesh = mesh;
                        activeChunks.Add((i, j, k), chunk_go);
                    }
                    
                }
            }
        }
    }

    void GenerateChunksV2(List<Vector3Int> chunkList, Vector3Int c)
    {
        var range = 4;
        foreach (var id in chunkList)
        {
            if (Mathf.Abs(id.x-c.x) > range || 
                Mathf.Abs(id.y-c.y) > range ||
                Mathf.Abs(id.z-c.z) > range)
            {
                if (activeChunks.ContainsKey((id.x,id.y,id.z)))
                {
                    Destroy(activeChunks[(id.x,id.y,id.z)].gameObject);
                    activeChunks.Remove((id.x,id.y,id.z));
                }

                continue;   
            }
            if (activeChunks.ContainsKey((id.x,id.y,id.z)))
            {
                continue;
            }
            else
            {
                var localStartBounds = min_bounds + (id * chunkSize);
                var localEndBounds = localStartBounds + (Vector3.one * chunkSize);
                Vector3 center = (localStartBounds + localEndBounds) / 2;
                var chunk_go = GameObject.Instantiate(this.chunk_go, center, Quaternion.identity, planet.transform);
                var chunk = chunk_go.GetComponent<Chunk>();
                chunk.InitChunk(localStartBounds,
                                localEndBounds,
                                new int[] { id.x * chunkRes, id.y * chunkRes, id.z * chunkRes },
                                chunkRes,
                                chunkRes + 1,
                                resolution_factor,
                                addNoise);

                var mesh = planet.GetChunkMesh(chunk, chunk_go);
                chunk_go.GetComponent<MeshFilter>().mesh = mesh;
                chunk_go.GetComponent<MeshCollider>().sharedMesh = mesh; 
                activeChunks.Add((id.x, id.y, id.z), chunk_go);
            }
        }
            
    }

    void BuildClosestChunks()
    {
        var closestChunkIdx = RoundDownVector((player.position-min_bounds) / chunkSize);
        //GenerateChunks(closestChunkIdx);
        GenerateChunksV2(radialChunks, closestChunkIdx);
    }

    

    Vector3Int RoundDownVector(Vector3 pos)
    {
        return new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
    }
    Vector3Int RoundUpVector(Vector3 pos)
    {
        return new Vector3Int(Mathf.CeilToInt(pos.x), Mathf.CeilToInt(pos.y), Mathf.CeilToInt(pos.z));
    }

    private void OnDrawGizmos()
    {
        if (drawNormals)
        {
            foreach (var chunk in activeChunks.Values)
            {
                var mesh = chunk.GetComponent<MeshFilter>().mesh;
                Gizmos.color = Color.green;
                for (int i = 0; i < mesh.normals.Length; i++)
                {
                    var v = chunk.TransformPoint(mesh.vertices[i]);
                    var n = v + mesh.normals[i];
                    Gizmos.DrawLine(v, n);
                }
            }
        }
        if (drawGrid)
        {
            var grid = planet.grid.GetGridArray();
            for (int i = 0; i < grid.Length; i++)
            {
            var point = grid[i];
            var r = globalGridRes;
            var r2 = r * r;
            var z = i / (r2);
            var temp_i = i - z * (r2);
            var y = temp_i / r;
            var x = temp_i - y * r;
            var c = GridToWorld(new Vector3(x, y, z));
            if (onlyDrawInside)
                {
                    if (point < isoLevel)
                        continue;
                }
                Gizmos.color = point > isoLevel ? Color.red : Color.blue;
                Gizmos.DrawSphere(c, 0.5f);
            }
        }

        if (drawParticles && particlesList != null)
        {
            foreach (var particles in particlesList)
            {
                var c = particles.Count;
                for (int i = 0; i < c; i++)
                {
                    var p = particles[i];
                    var pos = GridToWorld(p.pos);

                

                    if (i == c-1) // last particle
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(pos, 0.1f);
                        //UnityEditor.Handles.Label(pos - Vector3.one * 3, $"water: {p.water} \n" +
                        //                                                $"sediment: {p.sediment} \n" +
                        //                                                $"phi: {p.phi} \n" +
                        //                                                $"vel: {p.vel.magnitude} \n" +
                        //                                                $"air_f: {p.tangent.magnitude} \n" +
                        //                                                $"surf_f: {p.normal.magnitude} \n" +
                        //                                                $"c: {p.c} \n" +
                        //                                                $"delta_s: {p.delta_s}");

                        //Gizmos.color = Color.white;
                        //Gizmos.DrawRay(pos, p.vel);

                        //Gizmos.color = Color.red;
                        //Gizmos.DrawRay(pos, p.tangent); // friction

                        //Gizmos.color = Color.green;
                        //Gizmos.DrawRay(pos, p.normal); //air
                        //DrawSurroundingGrid(p.pos);
                            
                    }
                    else
                    {
                        var p1 = particles[i + 1];
                        var pos1 = GridToWorld(p1.pos);
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(pos, pos1);
                    }
                }
            }


        }
    }

    Vector3 GridToWorld(Vector3 gridPos)
    {
        return (gridPos * chunkSize/globalRes) + min_bounds; 
    }

    public void DrawSurroundingGrid(Vector3 gridPos)
    {
        var id = RoundDownVector(gridPos);
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    var idx = id + new Vector3Int(i, j, k);
                    var p = grid[GetGridIndex(idx)];

                    Gizmos.color = p.w > 0 ? Color.black : Color.white;
                    Gizmos.DrawSphere(p, 0.2f);
                    //Handles.Label(p, $"{System.Math.Round(p.w,2)}");
                    
                }
            }
        }
    }

    public int GetGridIndex(Vector3Int id)
    {
        return id.x + (id.y * globalGridRes) + (id.z * globalGridRes * globalGridRes);
    }

    public List<Vector3Int> GetRadialChunks()
    {
        var max_radius = (planet.radius+planet.radialRange) / chunkSize;
        var min_radius = (planet.radius-planet.radialRange) / chunkSize;
        var r2_max = max_radius*max_radius;
        var r2_min =min_radius*min_radius;

        var center = WorldToGrid(planet.coordinates);
        var z_min = (int)(center.z - max_radius);
        var z_max = (int)(z_min + 2 * max_radius);
        var z_rows = z_max - z_min;
        var chunks = new List<Vector3Int>();

        for (int k = 0; k <= z_rows; k ++)
        {
            var row_z = z_min + k;
            float dzh = 0;

            if (row_z > center.z)
                    dzh = center.z - (row_z);
            else
                dzh = center.z - (row_z+1);
            
            var y_radius_max = Mathf.Sqrt(r2_max - (dzh * dzh));

            float y_radius_min = 0;
            if(dzh < min_radius)
                y_radius_min = Mathf.Sqrt(r2_min - (dzh * dzh));

            var y_r2_max = y_radius_max*y_radius_max;
            var y_r2_min = y_radius_min*y_radius_min;

            var y_min = (int)(center.y - y_radius_max);
            var y_max = (int)(y_min + 2 * y_radius_max);
            var rows = y_max - y_min;
            
            for (int i = 0; i <= rows; i++)
            {
                var row_y = y_min + i;
                float dh_max = 0;
                float dh_min = 0;

                if (row_y > center.y)
                {
                    dh_max = center.y - (row_y);
                    dh_min = Mathf.Min((row_y+1) - center.y, y_radius_max);
                }
                else
                {
                    dh_max = center.y - (row_y+1);
                    dh_min = Mathf.Min(center.y - (row_y), y_radius_max);
                }

                float dx_min = 0;
                if(dh_min < y_radius_min)
                    dx_min = Mathf.Sqrt(y_r2_min - (dh_min * dh_min));

                var dx_max = Mathf.Sqrt(y_r2_max - (dh_max * dh_max));


                //chunks.Add(RoundDownVector(new Vector3((int)(center.x-dx), row_y, row_z)));
                //chunks.Add(RoundDownVector(new Vector3((int)(center.x+dx), row_y, row_z)));
                for (int x = (int)(center.x-dx_max); x <= center.x-dx_min; x++)
                {
                    chunks.Add(RoundDownVector(new Vector3(x, row_y, row_z)));
                }
                for (int x = (int)(center.x+dx_min); x <= center.x+dx_max; x++)
                {
                    chunks.Add(RoundDownVector(new Vector3(x, row_y, row_z)));
                }
            }
        }

        return chunks;
    }

    public Vector3 WorldToGrid(Vector3 dims)
    {
        return (dims - min_bounds) / chunkSize;
    }
}
