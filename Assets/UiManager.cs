using System.Collections.Generic;
using System.Linq;
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

    // each Window occupies certain Z position in the interface layer. Interface layer is a 1..10 field, and a window occupies approximately 0.05 in this field.
    // shadow is 0.00, background is 0.01, element shadows are 0.02, elements are 0.03, element overlays are 0.04.
    // as such, there's a maximum of 200 windows at once.
    public float TopZ = MainCamera.InterfaceZ;
    private List<MonoBehaviour> Windows = new List<MonoBehaviour>();

    private void UpdateTopZ()
    {
        TopZ = MainCamera.InterfaceZ;
        foreach (MonoBehaviour wnd in Windows)
        {
            if (wnd.transform.position.z - 0.05f < TopZ)
                TopZ = wnd.transform.position.z - 0.05f;
        }
    }

    public float RegisterWindow(MonoBehaviour wnd)
    {
        Windows.Add(wnd);
        UpdateTopZ();
        return TopZ;
    }

    public void UnregisterWindow(MonoBehaviour wnd)
    {
        Windows.Remove(wnd);
        UpdateTopZ();
    }

    public void ClearWindows()
    {
        foreach (MonoBehaviour wnd in Windows)
            Destroy(wnd);
        Windows.Clear();
    }

    void Start()
    {

    }

    private bool GotProcessors = false;
    private List<MonoBehaviour> Processors = new List<MonoBehaviour>();
    private List<bool> ProcessorsEnabled = new List<bool>();


    //private float lastMouseX = 0;
    //private float lastMouseY = 0;
    void Update()
    {
        GotProcessors = false;
        EnumerateObjects();
        // get all events.
        Event e = new Event();
        while (Event.PopEvent(e))
        {
            // pressing PrintScreen or Alt+S results in screenshot unconditionally.
            if (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Print ||
                 e.keyCode == KeyCode.SysReq ||
                (e.keyCode == KeyCode.S && e.alt)))
            {
                MainCamera.Instance.TakeScreenshot();
                return;
            }

            // reverse iteration
            bool EventIsGlobal = (e.type == EventType.KeyUp ||
                                  e.rawType == EventType.MouseUp);
            for (int i = Processors.Count - 1; i >= 0; i--)
            {
                // check if processor's renderer is enabled. implicitly don't give any events to invisible objects.
                if (!ProcessorsEnabled[i]) continue;
                if (((IUiEventProcessor)Processors[i]).ProcessEvent(e) && !EventIsGlobal)
                    break;
            }
        }

        // also fake mouse event for each processor
        //Vector2 mPos = Utils.GetMousePosition();
        /*if (mPos.x != lastMouseX ||
            mPos.y != lastMouseY)*/
        {
            Event ef = new Event();
            ef.type = EventType.MouseMove;
            for (int i = Processors.Count - 1; i >= 0; i--)
            {
                // check if processor's renderer is enabled. implicitly don't give any events to invisible objects.
                if (!ProcessorsEnabled[i]) continue;
                if (((IUiEventProcessor)Processors[i]).ProcessEvent(ef))
                    break;
            }

            //lastMouseX = mPos.x;
            //lastMouseY = mPos.y;
        }
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
            bool isEnabled = renderers.Any(render => render.enabled);

            ProcessorsEnabled.Add(isEnabled);
            if (isEnabled)
                enc++;
        }

        GotProcessors = true;
    }
}