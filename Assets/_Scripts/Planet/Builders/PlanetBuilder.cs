using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetBuilder
{
    private Planet _planet;
    public PlanetBuilder(PlanetParameters planet_params)
    {
        _planet = new Planet(planet_params);
        _planet.manager = new PlanetManager(_planet);
    }
    public PlanetBuilder AddNoiseParams(NoiseParameters parameters)
    {
        _planet.noise_params = parameters;
        return this;
    }

    public PlanetBuilder AddGrid(Grid grid = null)
    {
        if(grid == null)
        {
            grid = _planet.manager.GetGrid();
        }
        _planet.grid = grid;
        return this;
    }

    public PlanetBuilder AddHydraulicParams(HydraulicErosionParameters parameters)
    {
        _planet.hydraulic_params = parameters;
        return this;
    }

    public PlanetBuilder AddThermalParams(ThermalErosionParameters parameters)
    {
        _planet.thermal_params = parameters;
        return this;
    }

    public PlanetBuilder AddMeshParams(MeshParameters parameters)
    {
        _planet.mesh_params = parameters;
        return this;
    }

    public Planet Build()
    {
        return _planet;
    }
}