using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Planet: MonoBehaviour
{
    // Planet Parameters
    public float radius;
    public float radialRange;
    public float atmosphere;
    public Vector3 coordinates;
    public float sea_level_radius;
    public float snowcap_radius;

    // Noise parameters
    [HideInInspector]
    public float frequency;

    public float size_frequency_ratio;
    [Range(1,8)]
    public int octaves;
    [Range(1, 3)]
    public float lacunarity;
    [Range(0, 1)]
    public float persistance;
    public Vector3 offset;
    [Range(0.1f, 5)]
    public float asymptote;
    [Range(0.01f, 0.5f)]
    public float curvature;
    [Range(0, 3)]
    public float mod_offset;
    public float modifier_strength;

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
    public bool enhance;

    // Grid Data
    public Grid grid;

    public PlanetManager manager;

    public ComputeShader simplex;
    public ComputeShader mc;
    public ComputeShader chunk_shader;
    public PlanetParameters parameters;
    public PlanetParameters carve_params;

    public void Awake()
    {
        frequency = Mathf.Pow(radius,3) / Mathf.Pow(size_frequency_ratio,3);
        atmosphere = radius / 2f;
        bounds = Vector3.one * (radius + atmosphere) * 2;
        start_coordinates = coordinates - bounds / 2;
    }

    private void Update()
    {
        frequency = Mathf.Pow(radius,3) / Mathf.Pow(size_frequency_ratio,3);
        //asymptote = curvature +2f;
        //mod_offset = curvature + 0.9f;
    }
    public void ConfigurePlanet(int global_res, int global_grid_res, float iso, bool interpolate)
    {
        this.global_res = global_res;
        this.global_grid_res = global_grid_res;
        this.iso = iso;
        this.interpolate = interpolate;

        increments = bounds / global_res;
        manager = new PlanetManager(this);
        var colls = this.GetComponentsInParent<SphereCollider>();
        colls[0].radius = this.radius + this.radialRange;
        colls[1].radius = this.radius - this.radialRange;
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
