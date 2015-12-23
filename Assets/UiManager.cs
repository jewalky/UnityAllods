using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IUiEventProcessor
{
    bool ProcessEvent(Event e);
}

public class UiManager : MonoBehaviour
{
    private static UiManager _Instance = null;
    public static UiManager Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<UiManager>();
            return _Instance;
        }
    }

    void Start()
    {

    }

    void Update()
    {

    }

    void OnGUI()
    {
        // send event.current to every object that has subscribed. if some object processes an event, don't send it further.

    }
}