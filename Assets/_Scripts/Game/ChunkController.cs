using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ChunkController : MonoBehaviour
{
    //make sure dimensions are uniform for now so global chunk size is consistant
    public Vector3 min_bounds;
    public Vector3 max_bounds;
    public Vector3 totalSize;
    public bool takeStep;
    public int simSteps;
    public bool takeThermalStep = false;
    public bool calculate_height_delta = true;
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
    public PlanetBehavior planetBehavior;
    Planet planet;
    public Transform player;

    Dictionary<(int,int,int),Transform> activeChunks;
    List<Vector3Int> radialChunks;
    List<Vector3Int> allChunks;
    public Transform chunk_go;
    public Vector3Int chunkDims;
    public List<Vector4[]> gridList;
    public Water[] waterParticles;

    private Vector4[] grid;

    private float lastRedrawTime = 0;
    public float timeBetweenRedraw;

    public float min_vertex_radius = Mathf.Infinity;
    public float max_vertex_radius = 0;
    public float max_height_delta;


    void Start()
    {
        planetBehavior = planet_go.GetComponent<PlanetBehavior>();
        RefreshGlobalParameters();
        ConfigurePlanet();
        planet = planetBehavior.CreatePlanet();

        waterParticles = new Water[0];

        

        activeChunks = new Dictionary<(int, int, int), Transform>();

        //radialChunks = GetRadialChunks();

        allChunks = GetAllChunks();
    }

    void RefreshGlobalParameters()
    {
        min_bounds = -1 * planetBehavior.planet_params.bounds / 2;
        max_bounds = planetBehavior.planet_params.bounds / 2;
        totalSize = (max_bounds - min_bounds);
        chunkDims = RoundUpVector( totalSize / chunkSize);
        
        globalRes = Mathf.Max(chunkDims.x, chunkDims.y, chunkDims.z) * chunkRes;
        globalGridRes = globalRes + 1;

    }

    private void Update()
    {
        if (clearGrid || (alwaysRedraw && CanRedraw()))
        {
            calculate_height_delta = true;
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

        if(redrawMesh || (alwaysRedraw && CanRedraw()))
        {
            calculate_height_delta = true;
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

                planet.manager.RunHydraulicSimulation();
                waterParticles = planet.manager.GetHydraulicParticles();
            }
            redrawMesh = true;
                
        }

        if (takeThermalStep)
        {
            takeThermalStep = false;
            planet.manager.RunThermalSimulation();
            redrawMesh = true;
        }

        if (calculate_height_delta)
        {
            calculate_height_delta = false;
            max_vertex_radius = 0;
            min_vertex_radius = Mathf.Infinity;
            foreach (var chunk in activeChunks.Values)
            {
                var mesh = chunk.GetComponent<MeshFilter>().mesh;
                foreach (var v in mesh.vertices)
                {
                    var mag = v.magnitude;
                    if(mag > max_vertex_radius)
                        max_vertex_radius = mag;
                    
                    if(mag < min_vertex_radius)
                        min_vertex_radius = mag;
                }
            }
            max_height_delta = max_vertex_radius - min_vertex_radius;
        }

    }

    void ConfigurePlanet()
    {
        planetBehavior.planet_params.global_res = globalRes;
        planetBehavior.planet_params.global_grid_res = globalGridRes;
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
                        var chunk_go = GameObject.Instantiate(this.chunk_go, center, Quaternion.identity, planet_go.transform);
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
                var chunk_go = GameObject.Instantiate(this.chunk_go, center, Quaternion.identity, planet_go.transform);
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
    void GenerateChunksV3(List<Vector3Int> chunkList, Vector3Int c)
    {
        var chunksPerUpdate = 10;
        var count = 0;
        foreach (var id in chunkList)
        {
            if(count > chunksPerUpdate)
                break;

            if (activeChunks.ContainsKey((id.x,id.y,id.z)))
            {
                continue;
            }
            else
            {
                
                var localStartBounds = min_bounds + (id * chunkSize);
                var localEndBounds = localStartBounds + (Vector3.one * chunkSize);
                Vector3 center = (localStartBounds + localEndBounds) / 2;
                var chunk_go = GameObject.Instantiate(this.chunk_go, center, Quaternion.identity, planet_go.transform);
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
                count++;
            }
        }
            
    }

    void BuildClosestChunks()
    {
        var closestChunkIdx = RoundDownVector((player.position-min_bounds) / chunkSize);
        //GenerateChunks(closestChunkIdx);
        GenerateChunksV3(allChunks, closestChunkIdx);
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
                    if (point < 0)
                        continue;
                }
                Gizmos.color = point > 0 ? Color.red : Color.blue;
                Gizmos.DrawSphere(c, 50f);
            }
        }

        if (drawParticles && waterParticles != null)
        {
            foreach (var p in waterParticles)
            {
                var pos = GridToWorld(p.pos);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(pos, 5f);
                // UnityEditor.Handles.Label(pos - Vector3.one * 3,$"water: {p.water} \n" +
                // $"sediment: {p.sediment} \n" +
                // $"phi: {p.phi} \n" +
                // $"vel: {p.vel} \n"
                // );
                }
            }
        }

    Vector3 GridToWorld(Vector3 gridPos)
    {
        return (gridPos * totalSize.x/globalRes) + min_bounds; 
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

    // TODO: Needs Refactoring
    // public List<Vector3Int> GetRadialChunks()
    // {
    //     var max_radius = (planet.radius+planet.radialRange) / chunkSize;
    //     var min_radius = (planet.radius-planet.radialRange) / chunkSize;
    //     var r2_max = max_radius*max_radius;
    //     var r2_min =min_radius*min_radius;

    //     var center = WorldToGrid(planet.coordinates);
    //     var z_min = (int)(center.z - max_radius);
    //     var z_max = (int)(z_min + 2 * max_radius);
    //     var z_rows = z_max - z_min;
    //     var chunks = new List<Vector3Int>();

    //     for (int k = 0; k <= z_rows; k ++)
    //     {
    //         var row_z = z_min + k;
    //         float dzh = 0;

    //         if (row_z > center.z)
    //                 dzh = center.z - (row_z);
    //         else
    //             dzh = center.z - (row_z+1);
            
    //         var y_radius_max = Mathf.Sqrt(r2_max - (dzh * dzh));

    //         float y_radius_min = 0;
    //         if(dzh < min_radius)
    //             y_radius_min = Mathf.Sqrt(r2_min - (dzh * dzh));

    //         var y_r2_max = y_radius_max*y_radius_max;
    //         var y_r2_min = y_radius_min*y_radius_min;

    //         var y_min = (int)(center.y - y_radius_max);
    //         var y_max = (int)(y_min + 2 * y_radius_max);
    //         var rows = y_max - y_min;
            
    //         for (int i = 0; i <= rows; i++)
    //         {
    //             var row_y = y_min + i;
    //             float dh_max = 0;
    //             float dh_min = 0;

    //             if (row_y > center.y)
    //             {
    //                 dh_max = center.y - (row_y);
    //                 dh_min = Mathf.Min((row_y+1) - center.y, y_radius_max);
    //             }
    //             else
    //             {
    //                 dh_max = center.y - (row_y+1);
    //                 dh_min = Mathf.Min(center.y - (row_y), y_radius_max);
    //             }

    //             float dx_min = 0;
    //             if(dh_min < y_radius_min)
    //                 dx_min = Mathf.Sqrt(y_r2_min - (dh_min * dh_min));

    //             var dx_max = Mathf.Sqrt(y_r2_max - (dh_max * dh_max));


    //             //chunks.Add(RoundDownVector(new Vector3((int)(center.x-dx), row_y, row_z)));
    //             //chunks.Add(RoundDownVector(new Vector3((int)(center.x+dx), row_y, row_z)));
    //             for (int x = (int)(center.x-dx_max); x <= center.x-dx_min; x++)
    //             {
    //                 chunks.Add(RoundDownVector(new Vector3(x, row_y, row_z)));
    //             }
    //             for (int x = (int)(center.x+dx_min); x <= center.x+dx_max; x++)
    //             {
    //                 chunks.Add(RoundDownVector(new Vector3(x, row_y, row_z)));
    //             }
    //         }
    //     }

    //     return chunks;
    // }

    public Vector3 WorldToGrid(Vector3 dims)
    {
        return (dims - min_bounds) / chunkSize;
    }

    private bool CanRedraw()
    {
        var currentTime = Time.time;
        var timeSince = currentTime - lastRedrawTime;

        if (timeSince >= timeBetweenRedraw)
        {
            lastRedrawTime = currentTime;
            return true;
        }
        return false;
    }

    private List<Vector3Int> GetAllChunks()
    {
        var end = RoundDownVector(WorldToGrid(max_bounds));
        var chunks = new List<Vector3Int>();
        var center = WorldToGrid(planet_go.transform.position);

        var radius = planet.planet_params.radius;
        var radialRange = planet.planet_params.radial_range;

        var max_radius = (radius + radialRange) / chunkSize;
        var min_radius = (radius - radialRange) / chunkSize;
        bool onlyOne = max_bounds.z == chunkSize / 2;

        for (int i = 0; i < end.x; i++)
        {
            for (int j = 0; j < end.y; j++)
            {
                for (int k = 0; k < end.z; k++)
                {
                    var point = new Vector3Int(i, j, k);
                    var chunkRadius = point-center;
                    if(chunkRadius.magnitude > max_radius && !onlyOne)
                        continue;

                    if(chunkRadius.magnitude < min_radius && !onlyOne)
                        continue;

                    chunks.Add(point);
                }   
            }
        }

        return chunks;
    }
}
