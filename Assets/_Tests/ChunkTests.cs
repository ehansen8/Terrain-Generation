using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ChunkTests
{
    [Test]
    [TestCase(9)]
    public void ChunkShaderIsInitialized(int grid_res)
    {
        var gridBuffer = ShaderTestHelpers.MockGrid(grid_res);
        var chunkNoise = new NoiseParameters(1, 1, 2, 0.5f, Vector3.zero, 0, 100, 1);
        var chunkShader = new ChunkShader(gridBuffer, chunkNoise);

        Assert.AreEqual("ChunkShader", chunkShader.shader.name);
    }

    [Test]
    [TestCase(9)]       // res = 2^x
    public void ChunkIsCorrectSizeNoEnhance(int grid_res)
    {
        var expected_count = ShaderTestHelpers.CubeRes(grid_res);
        var gridBuffer = ShaderTestHelpers.MockGrid(grid_res);
        var chunkNoise = new NoiseParameters(1, 1, 2, 0.5f, Vector3.zero, 0, 100, 1);
        var chunkShader = new ChunkShader(gridBuffer, chunkNoise);

        var chunk = new Chunk();
        chunk.addNoise = false;
        chunk.grid_offset = new int[] { 0, 0, 0 };
        chunk.pos_offset = new float[] { 0, 0, 0 };
        chunk.increment = 1;
        chunk.initial_grid_res = grid_res;
        chunk.res_factor = 0;

        var chunkBuffer = chunkShader.GetChunk(chunk);

        Assert.AreEqual(expected_count, chunkBuffer.count);
    }
}
