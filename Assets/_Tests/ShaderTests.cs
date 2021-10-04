using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ShaderTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void ShaderTestsSimplePasses()
    {
        Planet planet = new Planet();
        planet.ConfigurePlanet(8, 9, 0, true);
        ComputeShader shader = (ComputeShader)Resources.Load("ChunkShader");
        ChunkShader cs = new ChunkShader(planet, shader);
    }

    public Planet GetTestPlanet()
    {
        Planet planet = new Planet();
        planet.radius = 1000;
        planet.radialRange = 0;
        planet.atmosphere = 100;
        planet.octaves = 1;
        planet.lacunarity = 2;
        planet.persistance = 0.5f;
        planet.asymptote = 2;
        planet.curvature = 0.5f;
        planet.mod_offset = 0;
        planet.modifier_strength = 1;
        planet.invert_normals = false;
        planet.flat_shade = false;
        // planet.bounds
        // planet.start_coordinates
        // planet.increments
        // planet.iso
        // planet.interpolate
        // planet.enhance
        // planet.grid
        // planet.manager
        // planet.simplex
        // planet.mc
        // planet.chunk_shader
        // planet.parameters

        return planet;
    }
}
