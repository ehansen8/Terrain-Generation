using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class MeshParameters
{
    //Display Parameters
    public bool invert_normals;
    public bool flat_shade;
    public float iso;
    public bool interpolate;
    public bool enhance;
    public bool use32IndexFormat;
}