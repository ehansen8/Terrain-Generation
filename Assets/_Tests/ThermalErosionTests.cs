using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ThermalTests
{

    public ErosionSimulation InitShaderHelper()
    {
        var planet = new Planet();
        var shader = (ComputeShader)Resources.Load("3dSimplexNoise.compute");
        var sim = new ErosionSimulation(planet, 1, shader);

        return sim;
    }
    [Test]
    public void InitializeShader()
    {
        var planet = new Planet();
        var shader = (ComputeShader)Resources.Load("3dSimplexNoise.compute");
        var sim = new ErosionSimulation(planet, 1, shader);

        // Make sure all params are set
    }

    [Test]
    [TestCase(9,2)]
    [TestCase(10,2)]
    [TestCase(17,3)]
    [TestCase(8,1)]
    public void DispatchGroupsAreCorrect(int grid_res, int correct)
    {
        var sim = InitShaderHelper();

        var groups = sim.GetThermalGroups(grid_res);
        Assert.AreEqual(groups, correct);
    }

    [Test]
    public void OutputValuesAreInRange()
    {

    }

    [Test]
    public void UnderTalusAngleNoLoss()
    {

    }

    [Test]
    public void AboveTalusAngleLoss()
    {

    }

    [Test]
    public void OnlyLowerNeighborsFill()
    {

    }



    
}