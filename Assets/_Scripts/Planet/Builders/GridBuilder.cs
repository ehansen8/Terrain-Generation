using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBuilder
{
    public GridShader gridShader;
    public PlanetParameters planetParameters;
    public NoiseParameters noiseParameters;

    public GridBuilder(PlanetParameters planetParameters, NoiseParameters noiseParameters)
    {
        this.planetParameters = planetParameters;
        this.noiseParameters = noiseParameters;
        gridShader = new GridShader(planetParameters, noiseParameters);
    }

    public Grid Build()
    {
        return new Grid(gridShader.GetGridBuffer(), planetParameters.global_grid_res);
    }
}
