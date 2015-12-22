using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public MapView MapView;

    private static GameManager _Instance = null;
    public static GameManager Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<GameManager>();
            return _Instance;
        }
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
