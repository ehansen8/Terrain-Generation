using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController
{
    public ErosionSimulation erosionSim;
    public Planet planet;
    public int sim_size;
    public SimulationController(Planet planet)
    {
        this.sim_size = 1024 * 12;
        this.planet = planet;

        erosionSim = new ErosionSimulation(planet, sim_size, planet.simplex);
        erosionSim.Initialize(planet.global_res,
                                planet.global_grid_res,
                                planet.increments,
                                planet.start_coordinates);


    }

    public void RunSimulation(int steps)
    {
        erosionSim.NewParticleBatch();
        //var data = new Water[sim_size];
        //erosionSim.waterBuffer.GetData(data);

        //return data;
    }
}
