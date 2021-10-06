using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ErosionParameters
{
    public int num_particles = 1024*12;
    public float P_step_size = 1;
    public float P_starting_radius = 1500;
    public float P_init_water = 1;
    public float P_init_sediment = 0f;
    public float P_min_water = 0.05f;
    public float P_evaporation = 0.01f;
    public float P_gravity = 10f;
    public float P_capacity = 0.01f;
    public float P_erosion = 0.01f;
    public float P_deposition = 0.3f;
    public float K_normal = 0.05f;
    public float K_tangent = 0.2f;
    public float K_min_deposition = 0.001f;
    public float alpha_1 = 0.5f;
    public float alpha_2 = 0.9f;
    public float sigma = 0.5f;
    public float K_fric = 0.5f;
    public float K_air = 0.121f;      //approx. value for terminal velocity of 9 m/s
}
