using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ThermalErosionParameters
{
    public float talus_angle = 20;
    public float min_sediment_loss = 0.05f;
    public float t_step_size = 0.5f;
}