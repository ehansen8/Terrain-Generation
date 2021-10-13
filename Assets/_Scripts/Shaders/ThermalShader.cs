using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThermalErosionSimulation
{
public ComputeShader shader;
public int thermal_kernel;
public ComputeBuffer testBuffer;
public Grid grid;
public ThermalErosionParameters simParams;
public PlanetParameters planetParams;

    public ThermalErosionSimulation(ThermalErosionParameters simParams, PlanetParameters planetParams, Grid grid)
    {
        this.simParams = simParams;
        this.grid = grid;
        this.planetParams = planetParams;

        shader = Utilities.FindShader("3dSimplexNoise");
        thermal_kernel = shader.FindKernel("SimulateThermalErosion");

        SetThermalVariables();
        SetThermalBuffers();
    }

    private void SetThermalVariables()
    {
        shader.SetFloat(Shader.PropertyToID("talus_angle"), simParams.talus_angle);
        shader.SetFloat(Shader.PropertyToID("min_sediment_loss"), simParams.min_sediment_loss);
        shader.SetFloat(Shader.PropertyToID("t_step_size"), simParams.t_step_size);
    }
    
    private void SetThermalBuffers()
    {
        // Needs to be set after kernel is assigned
        testBuffer = new ComputeBuffer(grid.gridBuffer.count, Test.GetSize());
        shader.SetBuffer(thermal_kernel, "testBuffer", testBuffer);
        shader.SetBuffer(thermal_kernel, "gridBuffer", grid.gridBuffer);
    }

    public void Run()
    {
        DispatchThermalShader();
    }

    private int GetThermalGroups(int grid_res)
    {
        return (grid_res + 7) / 8;
    }

    private void DispatchThermalShader()
    {
        var gData_before = grid.GetGridArray();
        var g = GetThermalGroups(planetParams.global_grid_res);
        shader.Dispatch(thermal_kernel, g, g, g);
        DebugThermal(testBuffer, gData_before);

    }

    public void ReleaseBuffers()
    {
        testBuffer.Release();
    }

    public struct Test
    {
        public Vector3Int Tid;
        public int GI;
        public float phi;
        public Vector3 n;
        public Vector3 g;
        public float a;
        public float delta_s;
        public Vector3Int dir;

        public static int GetSize()
        {
            return sizeof(float) * (1 + 3 + 3 + 1 + 1) + sizeof(int) * (3 + 1 + 3);
        }
    }
    public void DebugThermal(ComputeBuffer testBuffer, float[] gData_before)
    {
        var data = new Test[testBuffer.count];
        var talus_angle = simParams.talus_angle;
        testBuffer.GetData(data);
        var gData_after = grid.GetGridArray();

        var finalists = new List<Test>();
        var phiFail = 0;
        var aFail = 0;
        var deltaFail = 0;
        foreach (var d in data)
        {
            if (d.phi <= 0)
            {
                phiFail++;
                continue;
            }

            if (((d.a <= 90) || (d.a >= 180 - talus_angle)))
            {
                aFail++;
                continue;
            }

            if (d.delta_s == 0)
            {
                deltaFail++;
                continue;
            }

            finalists.Add(d);
        }

        float added = 0;
        float subbed = 0;
        for (int i = 0; i < gData_before.Length; i++)
        {
            var diff = gData_after[i] - gData_before[i];
            if (diff > 0)
                added += diff;
            else
                subbed += diff;
        }
    }



}


