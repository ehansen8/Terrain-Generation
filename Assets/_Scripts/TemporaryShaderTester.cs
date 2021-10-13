using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class TemporaryShaderTester
    {
        public ThermalErosionSimulation InitShaderHelper()
        {
            ThermalErosionParameters simParams = new ThermalErosionParameters();
            PlanetParameters planetParams = new PlanetParameters();
            Grid grid = new Grid(MockGrid(5),5);

            var shader = Utilities.FindShader("3dSimplexNoise");
            var sim = new ThermalErosionSimulation(simParams,planetParams,grid);

            return sim;
        }

        int GetGridIndex(Vector3Int pos, int res)
        {
            return pos.x + (pos.y * res) + (pos.z * res * res);
        }

        /// <summary>
        /// Creates a grid where every point is 1 except the exterior layer which is -1
        /// </summary>
        /// <param name="grid_res"></param>
        /// <returns></returns>
        public ComputeBuffer MockGrid(int grid_res)
        {
            var size = (int)Mathf.Pow(grid_res, 3);
            var gBuffer = new ComputeBuffer(size, sizeof(float));

            //Create data
            var data = new float[size];
            for (int i = 0; i < grid_res; i++)
            {
                for (int j = 0; j < grid_res; j++)
                {
                    for (int k = 0; k < grid_res; k++)
                    {
                        var GI = GetGridIndex(new Vector3Int(i, j, k), grid_res);
                        var phi = 1;
                        
                        if((i == 0 || i == grid_res-1) ||
                           (j == 0 || j == grid_res-1)||
                           (k == 0 || k == grid_res-1))
                        {
                            phi = -1;
                        }

                        data[GI] = phi;
                    }
                }
            }

            gBuffer.SetData(data);
            return gBuffer;
        }

        public void OutputValuesAreInRange()
        {

        }
        public void UnderTalusAngleNoLoss()
        {

        }
        public void AboveTalusAngleLoss()
        {

        }

        public void ViewGravityAndNormal()
        {
        }
    }
}
