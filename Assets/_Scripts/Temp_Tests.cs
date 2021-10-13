using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;
public class Temp_Tests : MonoBehaviour
{
    TemporaryShaderTester t;
    public bool run = false;
    void Start()
    {
        t = new TemporaryShaderTester();
    }

    void Update()
    {
        if(run)
        {
            run = false;
            t.ViewGravityAndNormal();
        }
    }
}
