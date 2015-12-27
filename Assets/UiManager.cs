using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IUiEventProcessor
{
    bool ProcessEvent(Event e);
}

public interface IUiEventProcessorBackground { }

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
    private List<bool> ProcessorsEnabled = new List<bool>();

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

        ProcessorsEnabled.Clear();
        int enc = 0;
        foreach (MonoBehaviour mb in Processors)
        {
            if (mb is IUiEventProcessorBackground)
            {
                ProcessorsEnabled.Add(true);
                enc++;
                continue;
            }

            // check if object has any visible parts
            List<Renderer> renderers = mb.gameObject.GetComponentsInChildren<Renderer>().Concat(mb.gameObject.GetComponents<Renderer>()).ToList();
            bool isEnabled = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled)
                {
                    isEnabled = true;
                    break;
                }
            }

            ProcessorsEnabled.Add(isEnabled);
            if (isEnabled)
                enc++;
        }

        GotProcessors = true;
    }

    void OnGUI()
    {
        // pressing PrintScreen or Alt+S results in screenshot unconditionally.
        if (Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Print ||
             Event.current.keyCode == KeyCode.SysReq ||
            (Event.current.keyCode == KeyCode.S && Event.current.alt)))
        {
            MainCamera.Instance.TakeScreenshot();
            return;
        }

        // send event.current to every object that has subscribed. if some object processes an event, don't send it further.
        EnumerateObjects();
        // reverse iteration
        bool EventIsGlobal = (Event.current.type == EventType.KeyUp ||
                              Event.current.type == EventType.MouseUp);
        for (int i = Processors.Count - 1; i >= 0; i--)
        {
            // check if processor's renderer is enabled. implicitly don't give any events to invisible objects.
            if (!ProcessorsEnabled[i]) continue;
            if (((IUiEventProcessor)Processors[i]).ProcessEvent(Event.current) && !EventIsGlobal)
                break;
        }
    }
}