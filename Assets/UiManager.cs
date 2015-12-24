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
        Processors.Clear();
        GotProcessors = false;
    }

    void EnumerateChildObjects(Transform tr)
    {
        int start = Processors.Count;
        for (int i = 0; i < tr.childCount; i++)
        {
            GameObject co = tr.GetChild(i).gameObject;
            if (!co.activeInHierarchy) continue;
            MonoBehaviour[] mb_list = co.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour mb in mb_list)
            {
                if (mb.enabled && mb is IUiEventProcessor)
                    Processors.Add(mb);
            }
        }

        int added_count = Processors.Count - start;
        for (int i = start; i < start + added_count; i++)
            EnumerateChildObjects(Processors[i].gameObject.transform);
    }

    void EnumerateObjects()
    {
        if (GotProcessors) return;
        // traverse the tree
        GotProcessors = true;
        //float time1 = Time.realtimeSinceStartup;
        EnumerateChildObjects(SceneRoot.Instance.transform);
        //Debug.Log(string.Format("objects = {0}, run in {1}s, {2} in transform root", Processors.Count, Time.realtimeSinceStartup - time1, transform.root.childCount));
    }

    void OnGUI()
    {
        // send event.current to every object that has subscribed. if some object processes an event, don't send it further.
        EnumerateObjects();
        // reverse iteration
        for (int i = Processors.Count - 1; i >= 0; i--)
        {
            if (((IUiEventProcessor)Processors[i]).ProcessEvent(Event.current))
                break;
        }
    }
}