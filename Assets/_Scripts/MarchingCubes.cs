using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MarchingCubes
{
    // Planet parameters
    public Planet planet;
    public int globalRes;
    public int globalGridRes;

    // MC parameters
    public int chunkRes;
    public int chunkGridRes;
    public float isoLevel;
    public bool refreshMesh;
    public bool invertNormals;
    public bool interpolateVertices;
    public bool use32IndexFormat;
    public Gradient colorGradient;
    

    // Noise parameters
    //public float frequency;
    //public int octaves;
    //public float lacunarity;
    //public float persistance;
    //public float offset;
    //public float planet_radius;

    

    // Grid & Density function parameters
    public ComputeShader densityShader;
    public ComputeShader marchShader;
    public ComputeBuffer waterBuffer;
    public int erosionKernel;

    

    public MarchingCubes(int chunkRes, float isoLevel, bool invertNormals, bool interpolateVertices,bool use32IndexFormat,  Gradient colorGradient)
    {
        this.chunkRes = chunkRes;
        this.chunkGridRes = chunkRes + 1;
        this.isoLevel = isoLevel;
        this.invertNormals = invertNormals;
        this.interpolateVertices = interpolateVertices;
        this.use32IndexFormat = use32IndexFormat;
        this.colorGradient = colorGradient;
    }

    public void SetShaders(ComputeShader densityShader, ComputeShader marchShader)
    {
        this.densityShader = densityShader;
        this.marchShader = marchShader;
    }
    

    public void InitializeErosion(Vector3 startBounds, Vector3 endBounds)
    {
        Vector3 increments = (endBounds - startBounds) / (globalRes);
        
        // Simulate Erosion
        erosionKernel = densityShader.FindKernel("SimulateErosion");

        // Initialize variables
        densityShader.SetInt(Shader.PropertyToID("res"), globalGridRes);
        densityShader.SetFloats(Shader.PropertyToID("start"), new float[] { startBounds.x, startBounds.y, startBounds.z });
        densityShader.SetFloats(Shader.PropertyToID("increments"), new float[] { increments.x, increments.y, increments.z });

        // 
        densityShader.SetFloat(Shader.PropertyToID("P_evaporation"), 0.04f);
        densityShader.SetFloat(Shader.PropertyToID("P_gravity"), 1f);
        densityShader.SetFloat(Shader.PropertyToID("P_capacity"), .1f);
        densityShader.SetFloat(Shader.PropertyToID("P_erosion"), .5f);
        densityShader.SetFloat(Shader.PropertyToID("K_normal"), 0.1f);
        densityShader.SetFloat(Shader.PropertyToID("K_tangent"), 0.1f);
        densityShader.SetFloat(Shader.PropertyToID("K_min_deposition"), 0.0002f);
        densityShader.SetFloat(Shader.PropertyToID("alpha_1"), 0.6f);
        densityShader.SetFloat(Shader.PropertyToID("alpha_2"), 0.8f);
        densityShader.SetFloat(Shader.PropertyToID("sigma"), 0.1f);
        //float P_radius;

        //Initialize Buffers
        int numParticles = 10;
        var particles = new Water[numParticles];
        for (int i = 0; i < numParticles; i++)
        {
            Water p = new Water();
            p.pos = Random.onUnitSphere * planet.radius * 1.5f;
            p.pos = (p.pos - startBounds) / increments.x;   //map world space to grid space
            p.vel = (Vector3.one * globalRes / 2) - p.pos;
            p.vel.Normalize();// direction of gravity
            p.water = 10;
            p.sediment = 0f;
            particles[i] = p;

        }
        waterBuffer = new ComputeBuffer(numParticles, Water.GetSize());
        waterBuffer.SetData(particles);
        
        // Set Buffers 
        densityShader.SetBuffer(erosionKernel, "grid", planet.grid.gridBuffer);
        densityShader.SetBuffer(erosionKernel, "waterBuffer", waterBuffer);
    }

    public Water[] TakeErosionStep()
    {
        densityShader.Dispatch(erosionKernel, 1, 1, 1);
        var particles = new Water[waterBuffer.count];
        waterBuffer.GetData(particles);
        return particles;
    }
}
