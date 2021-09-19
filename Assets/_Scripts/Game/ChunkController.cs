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
    public Gradient colorGradient;
    public Material meshMaterial;
    public ComputeShader densityShader;
    public ComputeShader marchShader;

    public GameObject planet_go;
    Planet planet;
    public Transform player;


    MarchingCubes MC;
    Dictionary<(int,int,int),Transform> activeChunks;
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
        //grid = planet.grid.GetGridArray();
    }

    void GenerateChunks(Vector3Int c)
    {
        var view = 0;
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
                                        new int[] { i * chunkRes, j * chunkRes, k * chunkRes }
                                        ,chunkRes, chunkRes+1);

                        chunk_go.GetComponent<MeshFilter>().mesh = planet.GetChunkMesh(chunk, chunk_go);
                        activeChunks.Add((i, j, k), chunk_go);
                    }
                    
                }
            }
        }
    }

    void BuildClosestChunks()
    {
        var closestChunkIdx = RoundDownVector((player.position-min_bounds) / chunkSize);
        GenerateChunks(closestChunkIdx);
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
                        var v = mesh.vertices[i];
                        var n = v + mesh.normals[i];
                        Gizmos.DrawLine(v, n);
                    }
                }
            }
            if (drawGrid)
            {
                var grid = planet.grid.GetGridArray();
                foreach (var point in grid)
                {
                    if (onlyDrawInside)
                    {
                        if (point.w < isoLevel)
                            continue;
                    }
                    Gizmos.color = point.w > isoLevel ? Color.red : Color.blue;
                    Gizmos.DrawSphere(point, 0.5f);
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
}
