﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Threading;
using System.IO;

public class GameManager : MonoBehaviour
{
    public delegate bool LoadCoroutine();

    private static GameManager _Instance = null;
    public static GameManager Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<GameManager>();
            return _Instance;
        }
    }

    private static bool CheckServerConfig()
    {
        if (ResourceManager.FileExists("server.cfg"))
        {
            StringFile sf = new StringFile("server.cfg");
            // execute all commands from there
            foreach (string cmd in sf.Strings)
            {
                Debug.Log(cmd);
                GameConsole.Instance.ExecuteCommand(cmd);
            }
            return true;
        }

        return false;
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

    void Awake()
    {
        // system Unity configuration
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Application.isBatchMode ? 10 : 60;

        //
        pMainThreadId = Thread.CurrentThread.ManagedThreadId;
        _Instance = this; // force set. through this field, other threads will access mapview.
        Locale.InitLocale(); // load locale strings, like main.txt, patch.txt, etc
    }

    void Start()
    {
        GameConsole = Utils.CreateObjectWithScript<GameConsole>();
        GameConsole.transform.parent = UiManager.Instance.transform;
    }

    void OnDestroy()
    {

    }

    private IEnumerator DelegateCoroutine(LoadCoroutine del)
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (!del())
                break;
        }
    }

    private int pMainThreadId = -1;
    private List<LoadCoroutine> pDelegates = new List<LoadCoroutine>();

    public void CallDelegateOnNextFrame(LoadCoroutine del)
    {
        if (pMainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            bool dFirst = del();
            if (dFirst)
                StartCoroutine(DelegateCoroutine(del));
            return;
        }

        lock (pDelegates)
            pDelegates.Add(del);
    }

    private Thread ClassLoadThread = null;
    private bool ClassLoadThreadDone = false;
    private void ClassLoadThreadProc()
    {
        try
        {
            TemplateLoader.LoadTemplates();
            ObstacleClassLoader.InitClasses();
            StructureClassLoader.InitClasses();
            UnitClassLoader.InitClasses();
            ItemClassLoader.InitClasses();
            ProjectileClassLoader.InitClasses();
            ClassLoadThreadDone = true;
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat("Exception while loading classes.\n{0}", e.ToString());
            ClassLoadThreadDone = true;
        }
    }

    void Update()
    {
        // initiate resource load.
        if (!ClassLoadThreadDone)
        {
            GameConsole.ConsoleEnabled = false;
            MapView.gameObject.SetActive(false);
            MouseCursor.SetCursor(MouseCursor.CurWait);

            if (ClassLoadThread == null)
            {
                ClassLoadThread = new Thread(new ThreadStart(ClassLoadThreadProc));
                ClassLoadThread.Start();
            }
        }
        else if (ClassLoadThreadDone && ClassLoadThread != null)
        {
            GameConsole.ConsoleEnabled = true;
            MapView.gameObject.SetActive(true);
            ClassLoadThread = null;

            MouseCursor.SetCursor(MouseCursor.CurDefault);
            Config.Load();
            CheckServerConfig();
        }

        lock (pDelegates)
        {
            for (int i = 0; i < pDelegates.Count; i++)
                StartCoroutine(DelegateCoroutine(pDelegates[i]));
            pDelegates.Clear();
        }
    }
}
