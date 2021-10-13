using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController
{
    public ThermalErosionSimulation thermalSim;
    public HydraulicErosionSimulation hydraulicSim;
    
    public SimulationController(ThermalErosionParameters thermalParams, 
                                HydraulicErosionParameters hydraulicParams, 
                                PlanetParameters planetParams, 
                                Grid grid)
    {
        thermalSim = new ThermalErosionSimulation(thermalParams, planetParams, grid);
        hydraulicSim = new HydraulicErosionSimulation(hydraulicParams, grid);
    }

    public void RunHydraulicSim()
    {
        hydraulicSim.Run();
    }

    public void RunThermalSim()
    {
        thermalSim.Run();
    }

    public Water[] GetHydraulicParticles()
    {
        return hydraulicSim.GetParticles();
    }

    public void ReleaseBuffers()
    {
        hydraulicSim.ReleaseBuffers();
    }
}
