using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridShader
{
    private ComputeShader shader;
    public int grid_kernel;
    public ComputeBuffer gridBuffer;
    public int grid_res;

    public GridShader(PlanetParameters planet, NoiseParameters noise)
    {
        this.shader = Utilities.FindShader("3dSimplexNoise");
        this.grid_kernel = shader.FindKernel("SimplexGrid");
        this.grid_res = planet.global_grid_res;
        SetPlanetVariables(planet);
        SetNoiseVariables(noise);
    }

    private void SetPlanetVariables(PlanetParameters planet)
    {
        shader.SetInt(Shader.PropertyToID("res"), planet.global_grid_res-1);
        shader.SetInt(Shader.PropertyToID("gridRes"), planet.global_grid_res);
        shader.SetVector(Shader.PropertyToID("start"), planet.start_coordinates);
        shader.SetFloat(Shader.PropertyToID("increment"), planet.increment);
    }

    private void SetNoiseVariables(NoiseParameters noise)
    {
        shader.SetFloat(Shader.PropertyToID("freq"), noise.frequency);
        shader.SetInt(Shader.PropertyToID("octaves"), noise.octaves);
        shader.SetFloat(Shader.PropertyToID("lacunarity"), noise.lacunarity);
        shader.SetFloat(Shader.PropertyToID("persistance"), noise.persistance);
        shader.SetVector(Shader.PropertyToID("offset"), noise.offset);
        shader.SetFloat(Shader.PropertyToID("maskStartRadius"), noise.maskStartRadius);
        shader.SetFloat(Shader.PropertyToID("maskEndRadius"), noise.maskEndRadius);
        shader.SetFloat(Shader.PropertyToID("maskStrength"), noise.maskStrength);
    }

    public ComputeBuffer GetGridBuffer()
    {
        SetBuffers();
        DispatchShader();
        return gridBuffer;
    }

    private void SetBuffers()
    {
        int gridSize = (int)Mathf.Pow(grid_res, 3);
        gridBuffer = new ComputeBuffer(gridSize, sizeof(float));

        shader.SetBuffer(grid_kernel, "gridBuffer", gridBuffer);
    }

    private void DispatchShader()
    {
        // Dispatch Groups
        var gridGroups = (grid_res + 7) / 8;

        shader.Dispatch(grid_kernel, gridGroups, gridGroups, gridGroups);
    }
}
