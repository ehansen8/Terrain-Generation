using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Planet: MonoBehaviour
{
    // Planet Parameters
    public float radius;
    public float atmosphere;
    public Vector3 coordinates;
    public float sea_level_radius;
    public float snowcap_radius;

    // Noise parameters
    public float frequency;
    public int octaves;
    public float lacunarity;
    public float persistance;
    public Vector3 offset;
    public float asymptote;
    public float curvature;
    
    //Display Parameters
    public int global_res;
    public int global_grid_res;
    public Gradient gradient;
    public bool invert_normals;
    public bool flat_shade;

    public Vector3 bounds;
    public Vector3 start_coordinates;
    public Vector3 increments;
    public float iso;
    public bool interpolate;

    // Grid Data
    public Grid grid;

    public PlanetManager manager;

    public ComputeShader simplex;
    public ComputeShader mc;

    public void Awake()
    {
        atmosphere = radius / 2f;
        bounds = Vector3.one * (radius + atmosphere) * 2;
        start_coordinates = coordinates - bounds / 2;
    }
    public void ConfigurePlanet(int global_res, int global_grid_res, float iso, bool interpolate)
    {
        this.global_res = global_res;
        this.global_grid_res = global_grid_res;
        this.iso = iso;
        this.interpolate = interpolate;

        increments = bounds / global_res;
        manager = new PlanetManager(this);
    }

    public Mesh GetChunkMesh(Chunk chunk, Transform parent)
    {
        return manager.meshBuilder.GetMesh(chunk, parent);
    }

    public void ClearGrid()
    {
        grid.gridBuffer.Release();
        manager.sim.erosionSim.waterBuffer.Release();
    }

    public void OnDestroy()
    {
        ClearGrid();
    }
}
