using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicErosionSimulation
{
    public ComputeShader shader;
    public int hydraulic_kernel;
    public ComputeBuffer waterBuffer;
    public Grid grid;
    public HydraulicErosionParameters simParams;
    public PlanetParameters planetParams;

    public HydraulicErosionSimulation(HydraulicErosionParameters simParams, Grid grid)
    {
        this.simParams = simParams;
        this.grid = grid;

        shader = Utilities.FindShader("3dSimplexNoise");
        hydraulic_kernel = shader.FindKernel("SimulateHydraulicErosion");

        SetHydraulicVariables();
        SetHydraulicBuffers();
    }

    private void SetHydraulicVariables()
    {
        shader.SetFloat(Shader.PropertyToID("P_min_water"), simParams.P_min_water);
        shader.SetFloat(Shader.PropertyToID("P_evaporation"), simParams.P_evaporation);
        shader.SetFloat(Shader.PropertyToID("P_gravity"), simParams.P_gravity);
        shader.SetFloat(Shader.PropertyToID("P_capacity"), simParams.P_capacity);
        shader.SetFloat(Shader.PropertyToID("P_erosion"), simParams.P_erosion);
        shader.SetFloat(Shader.PropertyToID("P_deposition"), simParams.P_deposition);
        shader.SetFloat(Shader.PropertyToID("K_normal"), simParams.K_normal);
        shader.SetFloat(Shader.PropertyToID("K_tangent"), simParams.K_tangent);
        shader.SetFloat(Shader.PropertyToID("K_min_deposition"), simParams.K_min_deposition);
        shader.SetFloat(Shader.PropertyToID("alpha_1"), simParams.alpha_1);
        shader.SetFloat(Shader.PropertyToID("alpha_2"), simParams.alpha_2);
        shader.SetFloat(Shader.PropertyToID("sigma"), simParams.sigma);
        shader.SetFloat(Shader.PropertyToID("K_fric"), simParams.K_fric);
        shader.SetFloat(Shader.PropertyToID("K_air"), simParams.K_air);      //approx. value for terminal velocity of 9 m/s
        shader.SetFloat(Shader.PropertyToID("P_step_size"), simParams.P_step_size);      //approx. value for terminal velocity of 9 m/s
    }

    private void SetHydraulicBuffers()
    {
        waterBuffer = new ComputeBuffer(simParams.num_particles, Water.GetSize());

        // Set Buffers 
        shader.SetBuffer(hydraulic_kernel, "gridBuffer", grid.gridBuffer);
        shader.SetBuffer(hydraulic_kernel, "waterBuffer", waterBuffer);
    }

    public void DispatchShader()
    {
        shader.Dispatch(hydraulic_kernel, simParams.num_particles / 1024, 1, 1);   // divide steps by / Numthreads
    }

    public void Run()
    {
        //Initialize water particles
        var particles = new Water[simParams.num_particles];
        for (int i = 0; i < particles.Length; i++)
        {
            Water p = new Water();
            p.pos = Random.onUnitSphere * simParams.P_starting_radius;
            p.pos = (p.pos - planetParams.start_coordinates) / planetParams.increment;   //map world space to grid space
            p.vel = (Vector3.one * planetParams.global_res / 2) - p.pos;
            p.vel.Normalize();// direction of gravity
            p.water = simParams.P_init_water;
            p.sediment = simParams.P_init_sediment;
            particles[i] = p;

        }
        waterBuffer.SetData(particles);

        DispatchShader();
    }

    public Water[] GetParticles()
    {
        var data = new Water[simParams.num_particles];
        waterBuffer.GetData(data);

        return data;
    }

    public void ReleaseBuffers()
    {
        waterBuffer.Release();
    }
}





