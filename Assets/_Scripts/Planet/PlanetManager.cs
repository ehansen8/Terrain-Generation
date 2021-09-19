using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetManager
{
    private GridBuilder gridBuilder;
    public MeshBuilder meshBuilder;
    public SimulationController sim;
    public Planet planet;

    

    public PlanetManager(Planet planet)
    {
        this.planet = planet;

        gridBuilder = new GridBuilder(planet);
        planet.grid = gridBuilder.BuildGrid();

        meshBuilder = new MeshBuilder(planet, true, planet.invert_normals, planet.flat_shade);
        sim = new SimulationController(planet);
    }
}
