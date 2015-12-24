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

    private bool GotProcessors = false;
    private List<MonoBehaviour> Processors = new List<MonoBehaviour>();

    void Update()
    {
        GotProcessors = false;
    }

    public void Subscribe(IUiEventProcessor mb)
    {
        if (!Processors.Contains((MonoBehaviour)mb))
            Processors.Add((MonoBehaviour)mb);
    }

    public void Unsubscribe(IUiEventProcessor mb)
    {
        Processors.Remove((MonoBehaviour)mb);
    }

    void EnumerateObjects()
    {
        if (GotProcessors) return;
        Processors.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
        GotProcessors = true;
    }

    void OnGUI()
    {
        // send event.current to every object that has subscribed. if some object processes an event, don't send it further.
        EnumerateObjects();
        // reverse iteration
        bool EventIsGlobal = (Event.current.type == EventType.KeyUp ||
                              Event.current.type == EventType.MouseUp);
        for (int i = Processors.Count - 1; i >= 0; i--)
        {
            if (((IUiEventProcessor)Processors[i]).ProcessEvent(Event.current) && !EventIsGlobal)
                break;
        }
    }
}