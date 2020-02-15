using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FullscreenView : Widget, IUiEventProcessor
{
    private GameObject BgObject;

    public GameObject WorkingArea { get; private set; }

    public delegate void ReturnHandler();
    public ReturnHandler OnReturn = null;

    public virtual void OnAwake()
    {

    }

    public void Awake()
    {
        UiManager.Instance.Subscribe(this);
        OnAwake();
    }

    public virtual void OnStart()
    {

    }

    public void Start()
    {
        BgObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        BgObject.transform.parent = transform;
        BgObject.transform.localPosition = new Vector3(MainCamera.Width / 2, MainCamera.Height / 2, 0.025f);
        BgObject.transform.localScale = new Vector3(MainCamera.Width, -MainCamera.Height, 1);
        MeshRenderer bgRenderer = BgObject.GetComponent<MeshRenderer>();
        bgRenderer.material = new Material(MainCamera.MainShader);
        bgRenderer.material.color = new Color(0, 0, 0, 1);

        // init transform
        transform.parent = UiManager.Instance.transform;
        transform.localScale = new Vector3(1, 1, 1);
        transform.position = new Vector3(0, 0, UiManager.Instance.RegisterWindow(this));

        OnStart();
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
        UiManager.Instance.UnregisterWindow(this);
    }

    public virtual bool ProcessEvent(Event e)
    {
        if (e.rawType == EventType.MouseMove)
            MouseCursor.SetCursor(MouseCursor.CurDefault);
        return true;
    }

    public virtual bool ProcessCustomEvent(CustomEvent ce)
    {
        return false;
    }
}