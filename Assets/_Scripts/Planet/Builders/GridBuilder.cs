using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBuilder
{
    public GridShader gridShader;
    public Planet planet;

    public GridBuilder(Planet planet)
    {
        this.planet = planet;
        gridShader = new GridShader(planet, planet.simplex);
        gridShader.Initialize(planet.global_res,
                                    planet.global_grid_res,
                                    planet.increments,
                                    planet.start_coordinates);
    }

    public Grid BuildGrid()
    {
        return new Grid(gridShader.GetGridBuffer(), planet.global_grid_res);
    }
}
