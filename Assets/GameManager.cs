using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public delegate void LoadCoroutine();

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
    public GameConsole GameConsole;

    void Start()
    {
        GameConsole = Utils.CreateObjectWithScript<GameConsole>();
        GameConsole.transform.parent = UiManager.Instance.transform;
    }

    private IEnumerator DelegateCoroutine(LoadCoroutine del)
    {
        yield return new WaitForEndOfFrame();
        del();
    }

    public void CallDelegateOnNextFrame(LoadCoroutine del)
    {
        StartCoroutine(DelegateCoroutine(del));
    }

    void Update()
    {

    }
}
