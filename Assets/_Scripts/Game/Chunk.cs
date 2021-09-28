using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public int initial_res;
    public int initial_grid_res;
    public int final_res;
    public int final_grid_res;
    public Vector3 increments;
    public int res_factor;
    public int[] grid_offset;
    public float[] pos_offset;
    public Vector3 center;
    public bool addNoise;


    public void InitChunk(Vector3 start_bounds,
                            Vector3 end_bounds,
                            int[] grid_offset,
                            int res,
                            int grid_res,
                            int res_factor,
                            bool addNoise)
    { 
        this.grid_offset = grid_offset;
        this.pos_offset = new float[] { start_bounds.x, start_bounds.y, start_bounds.z };
        this.center = (start_bounds+end_bounds) / 2;
        this.initial_res = res;
        this.initial_grid_res = grid_res;
        this.res_factor = res_factor;
        this.final_res = initial_res * (int)Mathf.Pow(2, res_factor);
        this.final_grid_res = final_res + 1;
        this.addNoise = addNoise;

        var size = end_bounds - start_bounds;

        var coll = this.GetComponentInParent<BoxCollider>();
        coll.size = size;

        this.increments = size / final_res;

        

    }
}
