using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid
{
    public ComputeBuffer gridBuffer;
    public int global_grid_res;

    public Grid(ComputeBuffer gridBuffer, int global_grid_res)
    {
        this.gridBuffer = gridBuffer;
        this.global_grid_res = global_grid_res;
    }

    public Vector4[] GetGridArray()
    {
        var grid = new Vector4[gridBuffer.count];
        gridBuffer.GetData(grid);

        return grid;
    }
}
