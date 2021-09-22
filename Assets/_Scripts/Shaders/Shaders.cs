using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Base Class for all Compute Shaders. It is assumed that the shader contains all relevent kernels needed for its operation.
/// </summary>
public abstract class ShaderData
{
    public int global_res;
    public int global_grid_res;
    public Vector3 gridIncrements;
    public Vector3 startCoordinates;
    public ComputeShader shader;
    public int gridGroups;

    protected void InitializeBase(int global_res, int global_grid_res, Vector3 gridIncrements, Vector3 startCoordinates)
    {
        this.global_res = global_res;
        this.global_grid_res = global_grid_res;
        this.gridIncrements = gridIncrements;
        this.startCoordinates = startCoordinates;

        shader.SetInt(Shader.PropertyToID("res"), global_res);
        shader.SetInt(Shader.PropertyToID("gridRes"), global_grid_res);
        shader.SetFloats(Shader.PropertyToID("start"), new float[] { startCoordinates.x, startCoordinates.y, startCoordinates.z });
        shader.SetFloats(Shader.PropertyToID("increments"), new float[] { gridIncrements.x, gridIncrements.y, gridIncrements.z });
    }

    public void ReleaseBuffers()
    {
        var props = typeof(ErosionSimulation).GetProperties();
        foreach (var prop in props)
        {
            if (prop.GetType() == typeof(ComputeBuffer))
            {
                var m = prop.PropertyType.GetMethod("Release");
                m.Invoke(prop.GetValue(this), null);
            }
        }
    }
}

public class GridShader : ShaderData
{
    public int grid_kernel;
    public ComputeBuffer gridBuffer;
    public Planet planet;

    public GridShader(Planet planet, ComputeShader shader)
    {
        this.planet = planet;
        this.shader = shader;
    }

    public void Initialize(int global_res, int global_grid_res, Vector3 gridIncrements, Vector3 startCoordinates)
    {
        InitializeBase(global_res, global_grid_res, gridIncrements, startCoordinates);

        // Set Kernels
        grid_kernel = shader.FindKernel("SimplexGrid");

        // Initialize Grid Specific Parameters
        shader.SetFloat(Shader.PropertyToID("freq"), planet.frequency);
        shader.SetInt(Shader.PropertyToID("octaves"), planet.octaves);
        shader.SetFloat(Shader.PropertyToID("lacunarity"), planet.lacunarity);
        shader.SetFloat(Shader.PropertyToID("persistance"), planet.persistance);
        shader.SetFloats(Shader.PropertyToID("offset"), new float[] { planet.offset.x, planet.offset.y, planet.offset.z });
        shader.SetFloat(Shader.PropertyToID("planet_radius"), planet.radius);
        shader.SetFloat(Shader.PropertyToID("asymptote"), planet.asymptote);
        shader.SetFloat(Shader.PropertyToID("curvature"), planet.curvature);
        shader.SetFloat(Shader.PropertyToID("mod_offset"), planet.mod_offset);

        InitializeBuffers();
    }

    public ComputeBuffer GetGridBuffer()
    {
        DispatchShader();
        return gridBuffer;
    }

    private void InitializeBuffers()
    {
        int gridSize = (int) Mathf.Pow(global_grid_res, 3);
        gridBuffer = new ComputeBuffer(gridSize, sizeof(float));

        shader.SetBuffer(grid_kernel, "gridBuffer", gridBuffer);
    }

    private void DispatchShader()
    {
        // Dispatch Groups
        gridGroups = (global_grid_res + 7) / 8;

        shader.Dispatch(grid_kernel, gridGroups, gridGroups, gridGroups);
    }
}

/// <summary>
/// Multi-Pass shader to
/// A: Calculate chunk normals;
/// B: Calculate interpolated vertices + normals
/// C: Build Triangle from said vertices
/// D: Returns TriangleStruct[]
/// </summary>
public class MeshShader : ShaderData
{
    public ComputeBuffer gridBuffer;
    public int vertex_kernel;
    public int normal_kernel;
    public int tri_kernel;
    public float iso;
    public bool interpolate;

    public MeshShader(ComputeBuffer gridBuffer, ComputeShader shader, float iso, bool interpolate)
    {
        this.gridBuffer = gridBuffer;
        this.shader = shader;
        this.iso = iso;
        this.interpolate = interpolate;

    }

    public void Initialize(int global_res, int global_grid_res, Vector3 gridIncrements, Vector3 startCoordinates)
    {
        InitializeBase(global_res, global_grid_res, gridIncrements, startCoordinates);
        normal_kernel = shader.FindKernel("CalculateNormals");
        vertex_kernel = shader.FindKernel("CalculateVertices");
        tri_kernel = shader.FindKernel("CalculateTriangles");

        shader.SetFloat(Shader.PropertyToID("iso"), iso);
        shader.SetBool(Shader.PropertyToID("interpolate"), interpolate);

        InitializeConstantBuffers();
    }

    private void InitializeConstantBuffers()
    {
        shader.SetBuffer(tri_kernel, "gridBuffer", gridBuffer);
        shader.SetBuffer(normal_kernel, "gridBuffer", gridBuffer);
        shader.SetBuffer(vertex_kernel, "gridBuffer", gridBuffer);
    }

    /// <summary>
    /// Gets the list of triangles that make up the mesh for the passed in chunk
    /// </summary>
    /// <param name="chunk_res"></param>
    /// <param name="chunk_grid_res"></param>
    /// <param name="grid_offset"></param>
    /// <returns></returns>
    public (TriStruct[],Vertex[]) GetTriangles(Chunk chunk)
    {
        //Set up Instance Buffers
        var vert_len = (int)Mathf.Pow(chunk.grid_res, 3);
        var tri_len = (int)Mathf.Pow(chunk.res, 3) * 5;

        var vertexBuffer = new ComputeBuffer(vert_len, EdgeStruct.GetSize());
        var normalBuffer = new ComputeBuffer(vert_len, sizeof(float) * 3);
        var triBuffer = new ComputeBuffer(tri_len, TriStruct.GetSize(), ComputeBufferType.Append);
        var ordered_vertex_buffer = new ComputeBuffer(vert_len, Vertex.GetSize());
        var vCount = new ComputeBuffer(1, sizeof(int));


        InitializeInstanceVariables(chunk.grid_offset, chunk.res, normalBuffer, vertexBuffer, triBuffer, ordered_vertex_buffer, vCount);

        //Always reset this to zero before Dispatch()
        triBuffer.SetCounterValue(0);
        vCount.SetData(new int[1] { 0 });
        DispatchShader(chunk.res, chunk.grid_res);

        //Get Data
        var triangles = new TriStruct[GetTriangleCount(triBuffer)];
        var vertices = new Vertex[GetVertexCount(vCount)];

        ordered_vertex_buffer.GetData(vertices);
        triBuffer.GetData(triangles);

        //Release used buffers
        //DebugVerts(vertexBuffer);
        normalBuffer.Release();
        vertexBuffer.Release();
        triBuffer.Release();
        ordered_vertex_buffer.Release();
        vCount.Release();

        return (triangles, vertices);
    }

    private void InitializeInstanceVariables(int[] grid_offset, 
                                             int chunk_res, 
                                             ComputeBuffer normalBuffer,
                                             ComputeBuffer vertexBuffer, 
                                             ComputeBuffer triBuffer, 
                                             ComputeBuffer ordered_vertex_buffer,
                                             ComputeBuffer vCount)
    {
        //Set instance variables
        shader.SetInts(Shader.PropertyToID("gridPosOffset"), grid_offset);
        shader.SetInt(Shader.PropertyToID("chunk_res"), chunk_res);
        shader.SetInt(Shader.PropertyToID("chunk_grid_res"), chunk_res +1);

        // Set instance Buffers
        //Pass 1
        shader.SetBuffer(normal_kernel, "normalBuffer", normalBuffer);

        //Pass 2
        shader.SetBuffer(vertex_kernel, "normalBuffer", normalBuffer);
        shader.SetBuffer(vertex_kernel, "vertexBuffer", vertexBuffer);

        //Pass 3
        shader.SetBuffer(tri_kernel, "vertexBuffer", vertexBuffer);
        shader.SetBuffer(tri_kernel, "triBuffer", triBuffer);
        shader.SetBuffer(tri_kernel, "ordered_vertex_buffer", ordered_vertex_buffer);
        shader.SetBuffer(tri_kernel, "vCount", vCount);
    }

    private void DispatchShader(int chunk_res, int chunk_grid_res)
    {
        int vertex_groups = (chunk_grid_res+7) / 8;
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

    private void DebugNormals(ComputeBuffer normalBuffer)
    {
        var data = new Vector3[normalBuffer.count];
        normalBuffer.GetData(data);
    }

    private void DebugVerts(ComputeBuffer vertexBuffer)
    {
        var g = new Vector4[gridBuffer.count];
        gridBuffer.GetData(g);
        var data = new EdgeStruct[vertexBuffer.count];
        vertexBuffer.GetData(data);
    }
}

public class ErosionSimulation : ShaderData
{
    public int kernel;
    public Planet planet;
    public ComputeBuffer waterBuffer;
    public int num_particles;

    public ErosionSimulation(Planet planet, int sim_size, ComputeShader shader)
    {
        this.planet = planet;
        this.num_particles = sim_size;
        this.shader = shader;
    }

    public void Initialize(int global_res, int global_grid_res, Vector3 gridIncrements, Vector3 startCoordinates)
    {
        InitializeBase(global_res, global_grid_res, gridIncrements, startCoordinates);
        // Get Kernel
        kernel = shader.FindKernel("SimulateErosion");

        // HACK Pass these in somehow
        // Erosion Specific Parameters
        shader.SetFloat(Shader.PropertyToID("P_evaporation"), 0.01f);
        shader.SetFloat(Shader.PropertyToID("P_gravity"), 9.8f);
        shader.SetFloat(Shader.PropertyToID("P_capacity"), .01f);
        shader.SetFloat(Shader.PropertyToID("P_erosion"), .01f);
        shader.SetFloat(Shader.PropertyToID("P_deposition"), .3f);
        shader.SetFloat(Shader.PropertyToID("K_normal"), 0.05f);
        shader.SetFloat(Shader.PropertyToID("K_tangent"), 0.2f);
        shader.SetFloat(Shader.PropertyToID("K_min_deposition"), 0.001f);
        shader.SetFloat(Shader.PropertyToID("alpha_1"), 0.5f);
        shader.SetFloat(Shader.PropertyToID("alpha_2"), 0.9f);
        shader.SetFloat(Shader.PropertyToID("sigma"), 0.5f);
        shader.SetFloat(Shader.PropertyToID("K_fric"), 0.5f);
        shader.SetFloat(Shader.PropertyToID("K_air"), 0.121f);      //approx. value for terminal velocity of 9 m/s
        shader.SetFloat(Shader.PropertyToID("K_kill_speed"), 0.1f);

        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        waterBuffer = new ComputeBuffer(num_particles, Water.GetSize());

        // Set Buffers 
        shader.SetBuffer(kernel, "gridBuffer", planet.grid.gridBuffer);
        shader.SetBuffer(kernel, "waterBuffer", waterBuffer);
    }

    public void DispatchShader(float min_water)
    {

        shader.SetFloat(Shader.PropertyToID("P_min_water"), min_water);
        shader.Dispatch(kernel, num_particles / 1024, 1, 1);   // divide steps by / Numthreads
    }

    public void NewParticleBatch()
    {
        //Initialize water particles
        var particles = new Water[num_particles];
        for (int i = 0; i < num_particles; i++)
        {
            Water p = new Water();
            p.pos = Random.onUnitSphere * (planet.radius + planet.atmosphere / 4);
            p.pos = (p.pos - startCoordinates) / gridIncrements.x;   //map world space to grid space
            p.vel = (Vector3.one * global_res / 2) - p.pos;
            p.vel.Normalize();// direction of gravity
            p.water = 1;
            p.sediment = 0f;
            particles[i] = p;

        }
        waterBuffer.SetData(particles);

        DispatchShader(0.05f);
    }

}

