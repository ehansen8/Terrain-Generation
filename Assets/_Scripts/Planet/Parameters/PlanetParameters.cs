using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetParameters
{
    // Planet Parameters
    public float radius;
    public float atmosphere;
    public float radial_range;
    public int global_res;
    public int global_grid_res;
    [HideInInspector]
    public Vector3 start_coordinates { get { return -Vector3.one * radius; } }
    public float increment { get { return bounds.x / global_res; } }
    public Vector3 bounds { get { return Vector3.one * 2 * (radius + atmosphere); } }

    public PlanetParameters(float radius, float atmosphere, float radial_range, int global_res, int global_grid_res)
    {
        this.radius = radius;
        this.atmosphere = atmosphere;
        this.radial_range = radial_range;
        this.global_res = global_res;
        this.global_grid_res = global_grid_res;
    }
}
