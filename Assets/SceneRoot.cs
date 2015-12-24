using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class SceneRoot : MonoBehaviour
{
    private static SceneRoot _Instance = null;
    public static SceneRoot Instance
    {
        get
        {
            if (_Instance == null) _Instance = GameObject.FindObjectOfType<SceneRoot>();
            return _Instance;
        }
    }
}