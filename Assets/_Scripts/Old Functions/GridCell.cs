using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GridCell
{
    public int cubeIndex = 0;
    public Vector4[] cube;
    public (Vector3 vert, int world_index)[] vertList;
    public float isoLevel;
    public VertexHash hash;
    public int vertCount;
    public List<Vector3> orderedVertList;
    public List<int> orderedTriList;
    public bool interpolate;



    /*
     * Cube indices look like so
     *        4---------5
     *     -          - |
     *  -          -    |
     * 7---------6      |
     * |      0  |      1
     * |         |    -       
     * |         | -
     * 3---------2
     */
    public GridCell(Vector4[] gridVals, float isoLevel, VertexHash hash, int count, bool interpolate)
    {
        orderedVertList = new List<Vector3>();
        orderedTriList = new List<int>();
        vertCount = count;
        this.hash = hash;
        this.interpolate = interpolate;
        // grid should only be length 8
        if (gridVals.Length != 8) throw new System.ArgumentOutOfRangeException("gridVals", "Size of grid must be exactly 8");
        cube = gridVals;
        this.isoLevel = isoLevel;
        for (int i = 0; i < 8; i++)
        {
            if (cube[i].w < isoLevel) cubeIndex |= (int)Mathf.Pow(2,i);
        }

        CalcVertList();
        OrderTriangles();
    }

    void CalcVertList()
    {
        int[] edgeTable = Triangulation.edgeTable;
        vertList = new (Vector3 vert, int world_index)[12];
        if (edgeTable[cubeIndex] == 0)
            return;

        /* Find the vertices where the surface intersects the cube */
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 1))
        {
            vertList[0] = VertexInterp(isoLevel, cube[0], cube[1]);
        }
            
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 2))
            vertList[1] = 
                VertexInterp(isoLevel, cube[1], cube[2]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 4))
            vertList[2] =
               VertexInterp(isoLevel, cube[2], cube[3]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 8))
            vertList[3] =
               VertexInterp(isoLevel, cube[3], cube[0]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 16))
            vertList[4] =
               VertexInterp(isoLevel, cube[4], cube[5]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 32))
            vertList[5] =
               VertexInterp(isoLevel, cube[5], cube[6]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 64))
            vertList[6] =
               VertexInterp(isoLevel, cube[6], cube[7]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 128))
            vertList[7] =
               VertexInterp(isoLevel, cube[7], cube[4]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 256))
            vertList[8] =
               VertexInterp(isoLevel, cube[0], cube[4]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 512))
            vertList[9] =
               VertexInterp(isoLevel, cube[1], cube[5]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 1024))
            vertList[10] =
               VertexInterp(isoLevel, cube[2], cube[6]);
        if (System.Convert.ToBoolean(edgeTable[cubeIndex] & 2048))
            vertList[11] =
               VertexInterp(isoLevel, cube[3], cube[7]);
    }
    
    (Vector3,int) VertexInterp(float isoLevel, Vector4 p1, Vector4 p2)
    {

        // if its not an existing vertex, calculate the vertex and make its worldIndex the vertCount++
        // else return the tuple with its value and worldIndex
        (Vector3, int) localVert;
        if (!hash.verts.ContainsKey((p1, p2)))
        {
            Vector3 p = new Vector3();
            if(interpolate)
            {
                //if (Mathf.Abs(isoLevel - p1.w) < 0.00001)
                //    p = p1;
                //else if (Mathf.Abs(isoLevel - p2.w) < 0.00001)
                //    p = p2;
                //else if (Mathf.Abs(p1.w - p2.w) < 0.00001)
                //    p = p1;
                //else
                {
                    float mu = (isoLevel - p1.w) / (p2.w - p1.w);
                    p.x = p1.x + mu * (p2.x - p1.x);
                    p.y = p1.y + mu * (p2.y - p1.y);
                    p.z = p1.z + mu * (p2.z - p1.z);
                }
            }

            // just go halfway
            else
                p = Vector3.Lerp(p1, p2, 0.5f);


            localVert = (p, vertCount++);
            hash.verts.Add((p1, p2), localVert);
            hash.verts.Add((p2, p1), localVert);
            orderedVertList.Add(p);
        }
        else
            localVert = hash.verts[(p1, p2)];

        return localVert;
    }

    void OrderTriangles()
    {
        for (int i = 0; i < 16; i++)
        {
            
            var localIdx = Triangulation.triTable[cubeIndex, i];
            if (localIdx == -1) return;

            var vertex = vertList[localIdx];
            orderedTriList.Add(vertex.world_index);
        }
    }
}

public struct VertexHash
{
    public Dictionary<(Vector3 p1, Vector3 p2), (Vector3 vertex, int idx)> verts;
}
