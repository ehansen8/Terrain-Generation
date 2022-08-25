using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class PlanetBehavior: MonoBehaviour
{
    [HideInInspector]   
    public Planet planet;

    public PlanetParameters planet_params = null;
    public NoiseParameters noise_params = null;
    public MeshParameters mesh_params = null;
    public HydraulicErosionParameters hydraulic_params = null;
    public ThermalErosionParameters thermal_params = null;


    public Planet CreatePlanet()
    {
        var builder = new PlanetBuilder(planet_params);
        planet = builder.AddNoiseParams(noise_params)
                        .AddGrid()
                        .AddMeshParams(mesh_params)
                        .AddHydraulicParams(hydraulic_params)
                        .AddThermalParams(thermal_params)
                        .Build();

        return planet;
    }
}
