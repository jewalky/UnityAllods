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
    private bool _IsHeadlessChecked = false;
    private bool _IsHeadless = false;
    public bool IsHeadless
    {
        get
        {
            if (!_IsHeadlessChecked)
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Contains("-nographics") || args.Contains("-batchmode"))
                    _IsHeadless = true;
                _IsHeadlessChecked = true;
            }

            return _IsHeadless;
        }
    }

    public MapView MapView;
    public GameConsole GameConsole;

    void Start()
    {
        if (!IsHeadless)
        {
            GameConsole = Utils.CreateObjectWithScript<GameConsole>();
            GameConsole.transform.parent = UiManager.Instance.transform;
        }
        else
        {
            MapLogic.Instance.InitFromFile("kids3.alm");
            NetworkManager.Instance.InitServer(8000);
        }
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
