using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Utils
{
    public static Vector3 Vec3InvertY(Vector3 _in)
    {
       return new Vector3(_in.x / 100, ((float)Screen.height - _in.y) / 100, _in.z / 100);
       //return new Vector3(_in.x, ((float)Screen.height - _in.y), _in.z);
       //return _in;
    }
}

