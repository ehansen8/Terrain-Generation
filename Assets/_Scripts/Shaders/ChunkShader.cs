using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Given some chunk parameters -> size, resolution, etc... produce a chunk grid for the mesh builder to operate on
// Main functions are to reduce the passed data to the size required for the mesh.
// Will be able to increase resolution from the grid data, and add additional noise / smoothing.
public class ChunkShader
{
    public ComputeShader shader;
    public int convert_kernel;
    public int resolution_kernel;
    public int noise_kernel;
    public ComputeBuffer gridBuffer;

    public ChunkShader(ComputeBuffer gridBuffer, NoiseParameters chunkNoise)
    {
        this.gridBuffer = gridBuffer;

        shader = Utilities.FindShader("ChunkShader");

        convert_kernel = shader.FindKernel("ConvertGridToChunk");
        resolution_kernel = shader.FindKernel("IncreaseResolution");
        noise_kernel = shader.FindKernel("AddNoise");

        SetNoiseVariables(chunkNoise);
    }

    /// <summary>
    /// Creates and returns a chunk that is 1 vertex larger in all directions.
    /// So final_grid_res +2
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns>oversize Chunk</returns>
    public ComputeBuffer GetChunk(Chunk chunk)
    {
        SetConvertVariables(chunk);
        var chunkBuffer = SetConvertBuffers(chunk);

        // We now have oversized chunkBuffer -> still needs assignment
        ConvertToChunk(chunkBuffer, chunk);

        // Now have chunkBuffer of size final_grid_res+2
        chunkBuffer = IncreaseResolution(chunkBuffer, chunk);
        if (chunk.addNoise)
            chunkBuffer = AddNoise(chunkBuffer, chunk);

        //DebugChunk(chunkBuffer, chunk);

        return chunkBuffer;
    }

    private void SetConvertVariables(Chunk chunk)
    {
        //Set instance variables
        shader.SetInts(Shader.PropertyToID("grid_offset"), chunk.grid_offset);
        shader.SetFloats(Shader.PropertyToID("pos_offset"), chunk.pos_offset);
        shader.SetFloat(Shader.PropertyToID("increment"), chunk.increment);
        shader.SetInt("target_grid_res", chunk.initial_grid_res);
    }

    private ComputeBuffer SetConvertBuffers(Chunk chunk)
    {
        var chunk_len = (int)Mathf.Pow(chunk.initial_grid_res + 2, 3);
        var chunkBuffer = new ComputeBuffer(chunk_len, sizeof(float));
        
        shader.SetBuffer(convert_kernel, "gridBuffer", gridBuffer);
        shader.SetBuffer(convert_kernel, "newChunkBuffer", chunkBuffer);

        return chunkBuffer;
    }
    
    private void ConvertToChunk(ComputeBuffer chunkBuffer, Chunk chunk)
    {
        int groups = (chunk.initial_grid_res + 7 ) / 8;

        shader.Dispatch(convert_kernel, groups, groups, groups);
    }

    private ComputeBuffer IncreaseResolution(ComputeBuffer oldChunkBuffer, Chunk chunk)
    {

        var initial_res = chunk.initial_res;

        // res_factor is how many times the resolution is doubled
        for (int i = 0; i < chunk.res_factor; i++)
        {
            var target_grid_res = (initial_res * 2) + 1;
            var newChunkBuffer = CreateNewBuffer(target_grid_res + 2);
            SetResolutionVariables(initial_res + 2, target_grid_res + 2, oldChunkBuffer, newChunkBuffer);
            var groups = (initial_res + 7) / 8;
            shader.Dispatch(resolution_kernel, groups, groups, groups);

            oldChunkBuffer.Release();
            oldChunkBuffer = newChunkBuffer;
            initial_res *= 2;
        }

        return oldChunkBuffer;
    }

    private ComputeBuffer CreateNewBuffer(int target_grid_res)
    {
        int len = (int)Mathf.Pow(target_grid_res, 3);
        return new ComputeBuffer(len, sizeof(float));
    }

    private void SetResolutionVariables(int initial_res, int target_grid_res, ComputeBuffer o, ComputeBuffer n)
    {
        shader.SetInt(Shader.PropertyToID("initial_res"), initial_res);
        shader.SetInt(Shader.PropertyToID("initial_grid_res"), initial_res + 1);
        shader.SetInt(Shader.PropertyToID("target_grid_res"), target_grid_res);
        shader.SetBuffer(resolution_kernel, "oldChunkBuffer", o);
        shader.SetBuffer(resolution_kernel, "newChunkBuffer", n);
    }

    private ComputeBuffer AddNoise(ComputeBuffer chunkBuffer, Chunk chunk)
    {
        var target_grid_res = chunk.final_grid_res + 2;
        shader.SetInt("target_grid_res", target_grid_res);
        shader.SetBuffer(noise_kernel, "newChunkBuffer", chunkBuffer);

        var groups = (target_grid_res / 8) + 1;
        shader.Dispatch(noise_kernel, groups, groups, groups);

        return chunkBuffer;
    }

    private void SetNoiseVariables(NoiseParameters p)
    {
        // Initialize Noise Parameters
        shader.SetFloat(Shader.PropertyToID("freq"), p.frequency * 2);
        shader.SetInt(Shader.PropertyToID("octaves"), p.octaves);
        shader.SetFloat(Shader.PropertyToID("lacunarity"), p.lacunarity);
        shader.SetFloat(Shader.PropertyToID("persistance"), p.persistance);
        shader.SetVector(Shader.PropertyToID("offset"), p.offset);
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





