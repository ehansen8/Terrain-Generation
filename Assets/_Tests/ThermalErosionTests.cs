using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ThermalTests
{

    [Test]
    public void InitializeShader()
    {
    }

    [Test]
    [TestCase(9,2)]
    [TestCase(10,2)]
    [TestCase(17,3)]
    [TestCase(8,1)]
    public void DispatchGroupsAreCorrect(int grid_res, int correct)
    {
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