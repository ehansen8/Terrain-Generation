using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetManager
{
    private GridBuilder gridBuilder;
    private MeshBuilder meshBuilder;
    private SimulationController sim;
    public Planet planet;

    

    public PlanetManager(Planet planet)
    {
        this.planet = planet;
    }

    public Grid GetGrid()
    {
        if(gridBuilder == null)
            gridBuilder = new GridBuilder(planet.planet_params, planet.noise_params);

        return gridBuilder.Build();
    }

    public Mesh GetMesh(Chunk chunk, Transform parent)
    {
        if(meshBuilder == null)
            meshBuilder = new MeshBuilder(planet.mesh_params, planet.noise_params, planet.grid.gridBuffer);

        return meshBuilder.Build(chunk, parent);
    }

    public void RunThermalSimulation()
    {
        if(sim == null)
            sim = new SimulationController(planet.thermal_params, planet.hydraulic_params, planet.planet_params, planet.grid);

        sim.RunThermalSim();
    }

    public Water[] GetHydraulicParticles()
    {
        return sim.GetHydraulicParticles();
    }

    public void RunHydraulicSimulation()
    {
        if(sim == null)
            sim = new SimulationController(planet.thermal_params, planet.hydraulic_params, planet.planet_params, planet.grid);

        sim.RunThermalSim();
    }

    /// <summary>
    /// Runs HydraulicSim followed by ThermalSim
    /// </summary>
    public void RunCombinedSimulation()
    {
        if(sim == null)
            sim = new SimulationController(planet.thermal_params, planet.hydraulic_params, planet.planet_params, planet.grid);

        sim.RunHydraulicSim();
        sim.RunThermalSim();
    }

    public void ReleaseAllBuffers()
    {
        sim.ReleaseBuffers();
    }
}
