using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _Instance = null;
    public static GameManager Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<GameManager>();
            return _Instance;
        }
    }

    // since this is a part of global state
    public MapView MapView;
    public AllodsUI.GameConsole GameConsole;

    void Start()
    {
        GameConsole = Utils.CreateObjectWithScript<AllodsUI.GameConsole>();
        GameConsole.transform.parent = UiManager.Instance.transform;
    }

    void Update()
    {

    }
}
