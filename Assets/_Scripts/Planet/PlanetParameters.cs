using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetParameters : MonoBehaviour
{
    // Planet Parameters
    public float radius;
    [Range(0.0001f, 2)]
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
    [Range(0,1)]
    public float initial_amplitude;
    [Range(0,1)]
    public float clampRange;
}
