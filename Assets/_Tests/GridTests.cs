using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GridTests
{
    [Test]
    [TestCase(8)]       // res < 2^x
    [TestCase(9)]       // res = 2^x
    [TestCase(10)]      // res > 2^x
    public void GridIsCorrectSize(int grid_res)
    {
        var grid_count = (int) Mathf.Pow(grid_res, 3);
        var planetParameters = new PlanetParameters(10, 10, 10, grid_res-1, grid_res);
        var noiseParameters = new NoiseParameters(1, 1, 2, 0.5f, Vector3.zero, 0, 100, 1);
        var gridShader = new GridShader(planetParameters, noiseParameters);

        var gridBuffer = gridShader.GetGridBuffer();
        Assert.AreEqual(gridBuffer.count, grid_count);
    }

    [Test()]
    [TestCase(9)]       // res = 2^x
    public void GridIsNonZero(int grid_res)
    {
        var grid_count = (int) Mathf.Pow(grid_res, 3);
        var planetParameters = new PlanetParameters(10, 10, 10, grid_res-1, grid_res);
        var noiseParameters = new NoiseParameters(1, 1, 2, 0.5f, Vector3.zero, 0, 100, 1);
        var gridShader = new GridShader(planetParameters, noiseParameters);

        var gridBuffer = gridShader.GetGridBuffer();
        var data = new float[grid_count];
        gridBuffer.GetData(data);
        foreach (var d in data)
        {
            if (d != 0)
            {
                Assert.Pass("Grid had a non-zero point");
            }
        }
        Assert.Fail("Grid had all zero points");
    }
}


public static class ShaderTestHelpers
{
    public static int GetGridIndex(Vector3Int pos, int res)
    {
        return pos.x + (pos.y * res) + (pos.z * res * res);
    }
    
    /// <summary>
    /// Creates a grid with a center of 1's and edges of -1's
    /// </summary>
    /// <param name="grid_res"></param>
    /// <returns> gridBuffer </returns>
    public static ComputeBuffer MockGrid(int grid_res)
    {
        var size = (int)Mathf.Pow(grid_res, 3);
        var gBuffer = new ComputeBuffer(size, sizeof(float));

        //Create data
        var data = new float[size];
        for (int i = 0; i < grid_res; i++)
        {
            for (int j = 0; j < grid_res; j++)
            {
                for (int k = 0; k < grid_res; k++)
                {
                    var GI = GetGridIndex(new Vector3Int(i, j, k), grid_res);
                    var phi = 1;
                    
                    if((i == 0 || i == grid_res-1) ||
                        (j == 0 || j == grid_res-1)||
                        (k == 0 || k == grid_res-1))
                    {
                        phi = -1;
                    }

                    data[GI] = phi;
                }
            }
        }

        gBuffer.SetData(data);
        return gBuffer;
    }

    public static int CubeRes(int grid_res)
    {
        return (int)Mathf.Pow(grid_res, 3);
    }
}