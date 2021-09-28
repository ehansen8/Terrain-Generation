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
    public int chunk_kernel;
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
    }

    /// Gets the list of triangles that make up the mesh for the passed in chunk
    public (TriStruct[],Vertex[]) GetTriangles(Chunk chunk, ComputeBuffer chunkBuffer)
    {
        //Set up Instance Buffers
        var vert_len = (int)Mathf.Pow(chunk.final_grid_res, 3);
        var tri_len = (int)Mathf.Pow(chunk.final_res, 3) * 5;

        var vertexBuffer = new ComputeBuffer(vert_len, EdgeStruct.GetSize());
        var normalBuffer = new ComputeBuffer(vert_len, sizeof(float) * 3);
        var triBuffer = new ComputeBuffer(tri_len, TriStruct.GetSize(), ComputeBufferType.Append);
        var ordered_vertex_buffer = new ComputeBuffer(vert_len, Vertex.GetSize());
        var vCount = new ComputeBuffer(1, sizeof(int));


        this.gridIncrements = chunk.increments;
        InitializeInstanceVariables(chunk.pos_offset, chunk.final_grid_res, normalBuffer, vertexBuffer, triBuffer, ordered_vertex_buffer, vCount, chunkBuffer);

        //Always reset this to zero before Dispatch()
        triBuffer.SetCounterValue(0);
        vCount.SetData(new int[1] { 0 });
        DispatchShader(chunk.final_res);

        //Get Data
        var triangles = new TriStruct[GetTriangleCount(triBuffer)];
        var vertices = new Vertex[GetVertexCount(vCount)];

        ordered_vertex_buffer.GetData(vertices);
        triBuffer.GetData(triangles);

        //Release used buffers
        //DebugVerts(vertexBuffer);
        //DebugChunk(chunkBuffer);
        normalBuffer.Release();
        vertexBuffer.Release();
        triBuffer.Release();
        ordered_vertex_buffer.Release();
        vCount.Release();
        chunkBuffer.Release();

        return (triangles, vertices);
    }

    private void InitializeInstanceVariables(float[] pos_offset, 
                                             int chunk_grid_res,
                                             ComputeBuffer normalBuffer,
                                             ComputeBuffer vertexBuffer, 
                                             ComputeBuffer triBuffer, 
                                             ComputeBuffer ordered_vertex_buffer,
                                             ComputeBuffer vCount,
                                             ComputeBuffer chunkBuffer)
    {

        //Set instance variables
        shader.SetFloats(Shader.PropertyToID("gridPosOffset"), pos_offset);
        shader.SetInt(Shader.PropertyToID("chunk_grid_res"), chunk_grid_res);

        //Override this one
        shader.SetFloats(Shader.PropertyToID("increments"), new float[] { gridIncrements.x, gridIncrements.y, gridIncrements.z });

        // Set instance Buffers
        //Pass 2
        shader.SetBuffer(normal_kernel, "normalBuffer", normalBuffer);
        shader.SetBuffer(normal_kernel, "chunkBuffer", chunkBuffer);

        //Pass 2
        shader.SetBuffer(vertex_kernel, "normalBuffer", normalBuffer);
        shader.SetBuffer(vertex_kernel, "vertexBuffer", vertexBuffer);
        shader.SetBuffer(vertex_kernel, "chunkBuffer", chunkBuffer);

        //Pass 3
        shader.SetBuffer(tri_kernel, "vertexBuffer", vertexBuffer);
        shader.SetBuffer(tri_kernel, "triBuffer", triBuffer);
        shader.SetBuffer(tri_kernel, "ordered_vertex_buffer", ordered_vertex_buffer);
        shader.SetBuffer(tri_kernel, "vCount", vCount);
        shader.SetBuffer(tri_kernel, "chunkBuffer", chunkBuffer);
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

    private void DebugNormals(ComputeBuffer normalBuffer)
    {
        var data = new Vector3[normalBuffer.count];
        normalBuffer.GetData(data);
    }
    private void DebugChunk(ComputeBuffer buffer)
    {
        var data = new float[buffer.count];
        var grid = new float[gridBuffer.count];
        gridBuffer.GetData(grid);
        buffer.GetData(data);
        var combo = new List<Dictionary<string,object>>();
        for (int i = 0; i < data.Length; i++)
        {

            var w = data[i];
            var z = i / (17 * 17);
            var temp_i = i - z*(17*17);
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

// Given some chunk parameters -> size, resolution, etc... produce a chunk grid for the mesh builder to operate on
// Main functions are to reduce the passed data to the size required for the mesh.
// Will be able to increase resolution from the grid data, and add additional noise / smoothing.
public class ChunkShader : ShaderData
{
    public int convert_kernel;
    public int resolution_kernel;
    public int noise_kernel;
    public ComputeBuffer gridBuffer;
    public Planet planet;

    public ChunkShader(Planet planet, ComputeShader shader)
    {
        this.planet = planet;
        this.gridBuffer = planet.grid.gridBuffer;
        this.shader = shader;
    }

    public void Initialize(int global_res, 
                           int global_grid_res, 
                           Vector3 gridIncrements, 
                           Vector3 startCoordinates)
    {
        InitializeBase(global_res, global_grid_res, gridIncrements, startCoordinates);

        // Set Kernels
        convert_kernel = shader.FindKernel("ConvertGridToChunk");
        resolution_kernel = shader.FindKernel("IncreaseResolution");
        noise_kernel = shader.FindKernel("AddNoise");

        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        shader.SetBuffer(convert_kernel, "gridBuffer", gridBuffer);
    }

    public ComputeBuffer GetChunk(Chunk chunk)
    {
        var chunk_len = (int)Mathf.Pow(chunk.initial_grid_res, 3);
        InitializeInstanceVariables(chunk);
        var chunkBuffer = new ComputeBuffer(chunk_len, sizeof(float));

        // We now have chunk
        ConvertToChunk(chunkBuffer, chunk.initial_grid_res);
        chunkBuffer = IncreaseResolution(chunkBuffer, chunk);
        if(chunk.addNoise)
            chunkBuffer = AddNoise(chunkBuffer, chunk.final_grid_res);

        //DebugChunk(chunkBuffer, chunk);

        return chunkBuffer;
    }

    private void InitializeInstanceVariables(Chunk chunk)
    {

        //Set instance variables
        shader.SetInts(Shader.PropertyToID("grid_offset"), chunk.grid_offset);
        shader.SetFloats(Shader.PropertyToID("pos_offset"), chunk.pos_offset);
        shader.SetFloats(Shader.PropertyToID("increments"), new float[] {chunk.increments.x,
                                                                         chunk.increments.y,
                                                                         chunk.increments.z });

    }
    private void ConvertToChunk(ComputeBuffer chunkBuffer, int res)
    {
        int groups = (res / 8) + 1;

        shader.SetInt("target_grid_res", res);
        shader.SetBuffer(convert_kernel, "newChunkBuffer", chunkBuffer);
        shader.Dispatch(convert_kernel, groups, groups, groups);
    }

    private ComputeBuffer IncreaseResolution(ComputeBuffer oldChunkbuffer, Chunk chunk)
    {
        var initial_res = chunk.initial_res;
        // res_factor is how many times the resolution is doubled
        for(int i = 0; i < chunk.res_factor; i++)
        {
            var target_grid_res = (initial_res * 2)+1;
            var newChunkBuffer = CreateNewBuffer(target_grid_res);
            SetResolutionDispatchVariables(initial_res, target_grid_res, oldChunkbuffer, newChunkBuffer);

            var groups = (initial_res / 8) + 1;
            shader.Dispatch(resolution_kernel, groups, groups, groups);
            oldChunkbuffer.Release();
            oldChunkbuffer = newChunkBuffer;
            initial_res *= 2;
        }

        return oldChunkbuffer;
    }

    private ComputeBuffer CreateNewBuffer(int target_grid_res)
    {
        int len = (int)Mathf.Pow(target_grid_res, 3);
        return new ComputeBuffer(len, sizeof(float));
    }

    private void SetResolutionDispatchVariables(int initial_res, int target_grid_res, ComputeBuffer o, ComputeBuffer n)
    {
        shader.SetInt(Shader.PropertyToID("initial_res"), initial_res);
        shader.SetInt(Shader.PropertyToID("initial_grid_res"), initial_res+1);
        shader.SetInt(Shader.PropertyToID("target_grid_res"), target_grid_res);
        shader.SetBuffer(resolution_kernel, "oldChunkBuffer", o);
        shader.SetBuffer(resolution_kernel, "newChunkBuffer", n);
    }

    private ComputeBuffer AddNoise(ComputeBuffer chunkBuffer, int target_grid_res)
    {
        shader.SetInt("target_grid_res", target_grid_res);
        shader.SetBuffer(noise_kernel, "newChunkBuffer", chunkBuffer);
        SetNoiseVariables(planet.parameters);

        var groups = (target_grid_res / 8)+1;
        shader.Dispatch(noise_kernel, groups, groups, groups);

        return chunkBuffer;
    }

    private void SetNoiseVariables(PlanetParameters p)
    {
        // Initialize Grid Specific Parameters
        shader.SetFloat(Shader.PropertyToID("freq"), p.frequency*2);
        shader.SetInt(Shader.PropertyToID("octaves"), p.octaves);
        shader.SetFloat(Shader.PropertyToID("lacunarity"), p.lacunarity);
        shader.SetFloat(Shader.PropertyToID("persistance"), p.persistance);
        shader.SetFloats(Shader.PropertyToID("offset"), new float[] { p.offset.x, p.offset.y, p.offset.z });
        shader.SetFloat(Shader.PropertyToID("planet_radius"), p.radius);
        shader.SetFloat(Shader.PropertyToID("asymptote"), p.asymptote);
        shader.SetFloat(Shader.PropertyToID("curvature"), p.curvature);
        shader.SetFloat(Shader.PropertyToID("mod_offset"), p.mod_offset);
        shader.SetFloat(Shader.PropertyToID("tempMult"), p.initial_amplitude);
        shader.SetFloat(Shader.PropertyToID("clampRange"), p.clampRange);
    }

    private void DebugChunk(ComputeBuffer buffer, Chunk chunk)
    {
        var data = new float[buffer.count];
        var grid = new float[gridBuffer.count];
        gridBuffer.GetData(grid);
        buffer.GetData(data);
        var combo = new List<Dictionary<string, object>>();
        for (int i = 0; i < data.Length; i++)
        {

            var w = data[i];
            var z = i / (chunk.final_grid_res * chunk.final_grid_res);
            var temp_i = i - z * (chunk.final_grid_res * chunk.final_grid_res);
            var y = temp_i / chunk.final_grid_res;
            var x = temp_i - y * chunk.final_grid_res;

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
}



