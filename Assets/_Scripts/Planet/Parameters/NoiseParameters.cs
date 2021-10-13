using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseParameters
{
    public float frequency;
    [Range(1,8)]
    public int octaves;
    [Range(1, 3)]
    public float lacunarity;
    [Range(0, 1)]
    public float persistance;
    public Vector3 offset;
    public float maskStartRadius;
    public float maskEndRadius;
    public float maskStrength;
}