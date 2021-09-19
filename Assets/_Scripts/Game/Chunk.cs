using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public int res;
    public int grid_res;
    public int[] grid_offset;
    public Vector3 center;

    public void InitChunk(Vector3 start_bounds,
                            Vector3 end_bounds,
                            int[] grid_offset,
                            int res,
                            int grid_res)
    { 
        this.grid_offset = grid_offset;
        this.center = (start_bounds+end_bounds) / 2;
        this.res = res;
        this.grid_res = grid_res;
    }
}
