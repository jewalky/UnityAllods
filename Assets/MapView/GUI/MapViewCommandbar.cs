using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewCommandbar : MonoBehaviour, IUiEventProcessor
{
    private static Texture2D CommandBarL = null;
    private static Texture2D CommandBarR = null;
    private static Texture2D CommandBarPressed = null;
    private static Texture2D CommandBarEmpty = null;

    private GameObject CommandBarLObject;
    private GameObject CommandBarEmptyObject;

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        transform.localScale = new Vector3(1, 1, 0.01f);
        transform.localPosition = new Vector3(Screen.width - 176, 158, MainCamera.InterfaceZ + 0.99f); // on this layer all map UI is drawn

        if (CommandBarL == null) CommandBarL = Images.LoadImage("graphics/interface/commandbarl.bmp", 0, Images.ImageType.AllodsBMP);
        if (CommandBarR == null) CommandBarR = Images.LoadImage("graphics/interface/commandbarr.bmp", Images.ImageType.AllodsBMP);
        if (CommandBarPressed == null) CommandBarPressed = Images.LoadImage("graphics/interface/commanddnr.bmp", Images.ImageType.AllodsBMP);
        if (CommandBarEmpty == null) CommandBarEmpty = Images.LoadImage("graphics/interface/commandempr.bmp", Images.ImageType.AllodsBMP);

        Utils.MakeTexturedQuad(out CommandBarLObject, CommandBarL);
        Utils.MakeTexturedQuad(out CommandBarEmptyObject, CommandBarEmpty);
        CommandBarLObject.transform.parent = transform;
        CommandBarLObject.transform.localPosition = new Vector3(0, 0, 0);
        CommandBarEmptyObject.transform.parent = transform;
        CommandBarEmptyObject.transform.localPosition = new Vector3(CommandBarL.width, 0, 0);
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public bool ProcessEvent(Event e)
    {
        if (e.rawType == EventType.MouseDown ||
            e.rawType == EventType.MouseUp ||
            e.rawType == EventType.MouseMove)
        {
            Vector2 mPos = Utils.GetMousePosition();
            if (!new Rect(transform.position.x, transform.position.y, CommandBarL.width + CommandBarR.width, CommandBarR.height).Contains(mPos))
                return false;

            return true;
        }

        return false;
    }

    public void Update()
    {

    }
}