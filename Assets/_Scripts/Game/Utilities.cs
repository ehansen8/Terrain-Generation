using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    public static ComputeShader FindShader(string name)
    {
        var loadList = Resources.LoadAll("", typeof(ComputeShader));
        var shaderList = (ComputeShader[])Resources.FindObjectsOfTypeAll(typeof(ComputeShader));
        ComputeShader shader = null;
        foreach (var s in shaderList)
        {
            if(s.name == name)
            {
                shader = s;
                break;
            }
        }
    return shader;
    }
}
