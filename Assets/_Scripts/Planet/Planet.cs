using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Planet
{
    // Planet Parameters
    public PlanetParameters planet_params;
    public NoiseParameters noise_params;
    public MeshParameters mesh_params;
    public HydraulicErosionParameters hydraulic_params;
    public ThermalErosionParameters thermal_params;
    public Grid grid;

    public PlanetManager manager;

    public Planet(PlanetParameters planet_params)
    {
        this.planet_params = planet_params;
    }

    public Mesh GetChunkMesh(Chunk chunk, Transform parent)
    {
        return manager.GetMesh(chunk, parent);
    }

    public void ClearGrid()
    {
        grid.ReleaseBuffer();
        manager.ReleaseAllBuffers();
    }

    private void OnDestroy()
    {
        ClearGrid();
    }
}
