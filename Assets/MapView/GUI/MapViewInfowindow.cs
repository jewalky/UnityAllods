using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewInfowindow : MonoBehaviour, IUiEventProcessor
{
    private IMapViewSelfie _Viewer = null;
    public IMapViewSelfie Viewer
    {
        get
        {
            return _Viewer;
        }

        set
        {
            if (_Viewer != value)
            {
                if (_Viewer != null)
                    _Viewer.DisplayPic(false, null);
                _Viewer = value;
                if (_Viewer != null)
                    _Viewer.DisplayPic(true, transform);
            }
        }
    }

    private static Texture2D HBackL;
    private static Texture2D HBackR;

    private GameObject HBackRObject;
    private MeshRenderer HBackRRenderer;
    private GameObject HBackLObject;
    private MeshRenderer HBackLRenderer;

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        if (HBackL == null) HBackL = Images.LoadImage("graphics/interface/humanbackl.bmp", 0, Images.ImageType.AllodsBMP);
        if (HBackR == null) HBackR = Images.LoadImage("graphics/interface/humanbackr.bmp", Images.ImageType.AllodsBMP);
        transform.localScale = new Vector3(1, 1, 0.01f);
        transform.localPosition = new Vector3(Screen.width - 176, 214, MainCamera.InterfaceZ + 0.99f); // on this layer all map UI is drawn

        HBackLObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        HBackLRenderer = HBackLObject.GetComponent<MeshRenderer>();
        HBackLObject.transform.parent = transform;
        HBackLObject.transform.localScale = new Vector3(HBackL.width, HBackL.height, 1);
        HBackLObject.transform.localPosition = new Vector3(HBackL.width / 2, HBackL.height / 2, 0);
        HBackLRenderer.material = new Material(MainCamera.MainShader);
        HBackLRenderer.material.mainTexture = HBackL;

        HBackRObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        HBackRRenderer = HBackRObject.GetComponent<MeshRenderer>();
        HBackRObject.transform.parent = transform;
        HBackRObject.transform.localScale = new Vector3(HBackR.width, HBackR.height, 1);
        HBackRObject.transform.localPosition = new Vector3(HBackL.width + HBackR.width / 2, HBackR.height / 2, 0);
        HBackRRenderer.material = new Material(MainCamera.MainShader);
        HBackRRenderer.material.mainTexture = HBackR;
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public bool ProcessEvent(Event e)
    {
        return false;
    }

    public void Update()
    {
        
    }
}