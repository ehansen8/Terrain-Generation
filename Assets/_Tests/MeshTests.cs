using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MeshTests
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
