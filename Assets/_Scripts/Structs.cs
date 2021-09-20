using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TriStruct
{
    public int v1;
    public int v2;
    public int v3;

    public static int GetSize()
    {
        return sizeof(int) * 3;
    }
}

public struct Vertex
{
    public Vector3 position;
    public Vector3 normal;
    public int idx;

    public static int GetSize()
    {
        return (sizeof(float) * 3 * 2) + sizeof(int) ;
    }
};

public struct EdgeStruct
{
    public Vertex x;
    public Vertex y;
    public Vertex z;

    public static int GetSize()
    {
        return Vertex.GetSize() * 3;
    }
}

public struct Material
{
    public Vector3 pos;
    public Vector3 dir;
    public float mass;
    public float vel;

    public static int GetSize()
    {
        return sizeof(float) * (3 + 3 + 1 + 1);
    }
}

public struct Water
{
    public Vector3 pos;
    public Vector3 vel;     // normalize for movment updates
    public float water;    // mass of water that the particle contains
    public float sediment;
    //public Vector3 normal;
    //public Vector3 tangent;
    //public float phi;
    //public int collided;
    //public float c;
    //public float delta_s;

    public static int GetSize()
    {
        return sizeof(float) * (3 + 3 + 1 + 1);
    }
}
