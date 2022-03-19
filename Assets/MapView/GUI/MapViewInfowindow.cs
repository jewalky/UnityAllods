using System;
using UnityEngine;

public class MapViewInfowindow : MonoBehaviour, IUiEventProcessor, IUiItemDragger
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
                {
                    _Viewer.DisplayPic(false, null);
                    _Viewer.DisplayInfo(false, null);
                }
                _Viewer = value;
                if (_Viewer != null && HBackRObject != null && TBackRObject != null)
                {
                    _Viewer.DisplayPic((BHumanModeObject == null || HumanMode), HBackRObject.transform);
                    _Viewer.DisplayInfo((BHumanModeObject == null || !HumanMode), TBackRObject.transform);
                }
            }
        }
    }

    private static Texture2D HBackL;
    private static Texture2D HBackR;
    private static Texture2D TBackL;
    private static Texture2D TBackR;
    private static Texture2D ExtraL; // this is either extra1024 or extra800
    private static Texture2D ExtraR; //

    // settings
    public bool PackAvailable = true;
    public bool BookAvailable = true;
    public bool ForceSmall = false;
    private bool Small = false;

    // buttons
    private static Texture2D BTextMode;
    private static Texture2D BHumanMode;
    private static Texture2D BPackOpen;
    private static Texture2D BPackClosed;
    private static Texture2D BBookOpen;
    private static Texture2D BBookClosed;

    private GameObject HBackRObject;
    private GameObject HBackLObject;
    private GameObject TBackRObject;
    private GameObject TBackLObject;
    private GameObject ExtraRObject;
    private GameObject ExtraLObject;

    // buttons
    private GameObject BTextModeObject;
    private GameObject BHumanModeObject;
    private GameObject BPackOpenObject;
    private GameObject BPackClosedObject;
    private GameObject BBookOpenObject;
    private GameObject BBookClosedObject;

    // black quad for <=768 video modes
    private GameObject BlackQuad;

    private bool HumanMode = true;
    public bool IsHumanMode
    {
        get
        {
            return HumanMode;
        }

        set
        {
            HumanMode = value;

            if (BHumanModeObject != null)
            {
                HBackRObject.SetActive(HumanMode);
                HBackLObject.SetActive(HumanMode);
                TBackRObject.SetActive(!HumanMode);
                TBackLObject.SetActive(!HumanMode);
                BHumanModeObject.SetActive(!HumanMode);
                BTextModeObject.SetActive(HumanMode);

                if (_Viewer != null)
                {
                    _Viewer.DisplayPic(HumanMode, HBackRObject.transform);
                    _Viewer.DisplayInfo(!HumanMode, TBackRObject.transform);
                }
            }
        }
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        Small = ForceSmall || MainCamera.Height < 768;

        if (HBackL == null) HBackL = Images.LoadImage("graphics/interface/humanbackl.bmp", 0, Images.ImageType.AllodsBMP);
        if (HBackR == null) HBackR = Images.LoadImage("graphics/interface/humanbackr.bmp", Images.ImageType.AllodsBMP);
        if (TBackL == null) TBackL = Images.LoadImage("graphics/interface/textbackl.bmp", 0, Images.ImageType.AllodsBMP);
        if (TBackR == null) TBackR = Images.LoadImage("graphics/interface/textbackr.bmp", Images.ImageType.AllodsBMP);

        string extraBase = null;
        if (MainCamera.Height == 768) extraBase = "graphics/interface/extra1024";
        else if (MainCamera.Height >= 600 && MainCamera.Height < 768) extraBase = "graphics/interface/extra800";
        if (extraBase != null && ExtraL == null) ExtraL = Images.LoadImage(extraBase + "l.bmp", 0, Images.ImageType.AllodsBMP);
        if (extraBase != null && ExtraR == null) ExtraR = Images.LoadImage(extraBase + "r.bmp", Images.ImageType.AllodsBMP);

        if (BTextMode == null) BTextMode = Images.LoadImage("graphics/interface/textmode.bmp", 0, Images.ImageType.AllodsBMP);
        if (BHumanMode == null) BHumanMode = Images.LoadImage("graphics/interface/humanmode.bmp", 0, Images.ImageType.AllodsBMP);
        if (BPackOpen == null) BPackOpen = Images.LoadImage("graphics/interface/backpackop.bmp", 0, Images.ImageType.AllodsBMP);
        if (BPackClosed == null) BPackClosed = Images.LoadImage("graphics/interface/backpackcl.bmp", 0, Images.ImageType.AllodsBMP);
        if (BBookOpen == null) BBookOpen = Images.LoadImage("graphics/interface/bookopened.bmp", 0, Images.ImageType.AllodsBMP);
        if (BBookClosed == null) BBookClosed = Images.LoadImage("graphics/interface/bookclosed.bmp", 0, Images.ImageType.AllodsBMP);

        transform.localScale = new Vector3(1, 1, 1);

        Utils.MakeTexturedQuad(out HBackLObject, HBackL);
        Utils.MakeTexturedQuad(out HBackRObject, HBackR);

        HBackLObject.transform.parent = transform;
        HBackLObject.transform.localScale = new Vector3(1, 1, 1);
        HBackLObject.transform.localPosition = new Vector3(0, 0, 0);
        HBackRObject.transform.parent = transform;
        HBackRObject.transform.localScale = new Vector3(1, 1, 1);
        HBackRObject.transform.localPosition = new Vector3(HBackL.width, 0, 0);

        Utils.MakeTexturedQuad(out TBackLObject, TBackL);
        Utils.MakeTexturedQuad(out TBackRObject, TBackR);

        // check current resolution.
        // if we have >=768 height, put textback alongside humanback.
        // otherwise enable switcher button and switch with tab/click.
        float tbackY = 0;
        if (!Small)
        {
            if (MainCamera.Height == 768)
                tbackY = HBackR.height + ExtraR.height;
            else tbackY = MainCamera.Height - transform.localPosition.y - TBackR.height;
        }

        TBackLObject.transform.parent = transform;
        TBackLObject.transform.localScale = new Vector3(1, 1, 1);
        TBackLObject.transform.localPosition = new Vector3(0, tbackY, 0);
        TBackRObject.transform.parent = transform;
        TBackRObject.transform.localScale = new Vector3(1, 1, 1);
        TBackRObject.transform.localPosition = new Vector3(TBackL.width, tbackY, 0);

        // hide textback if we're switching
        if (Small && !HumanMode)
        {
            TBackRObject.SetActive(false);
            TBackLObject.SetActive(false);
        }

        // show extra
        if (ExtraL != null && ExtraR != null)
        {
            Utils.MakeTexturedQuad(out ExtraLObject, ExtraL);
            Utils.MakeTexturedQuad(out ExtraRObject, ExtraR);

            ExtraLObject.transform.parent = transform;
            ExtraLObject.transform.localScale = new Vector3(1, 1, 1);
            ExtraLObject.transform.localPosition = new Vector3(0, HBackR.height, 0);
            ExtraRObject.transform.parent = transform;
            ExtraRObject.transform.localScale = new Vector3(1, 1, 1);
            ExtraRObject.transform.localPosition = new Vector3(TBackL.width, HBackR.height, 0);
        }

        if (Small)
        {
            Utils.MakeTexturedQuad(out BTextModeObject, BTextMode);
            Utils.MakeTexturedQuad(out BHumanModeObject, BHumanMode);

            BTextModeObject.transform.parent = transform;
            BTextModeObject.transform.localScale = new Vector3(1, 1, 1);
            BTextModeObject.transform.localPosition = new Vector3(144, 4, -0.002f);
            BHumanModeObject.transform.parent = transform;
            BHumanModeObject.transform.localScale = new Vector3(1, 1, 1);
            BHumanModeObject.transform.localPosition = new Vector3(144, 4, -0.002f);

            if (HumanMode)
                BTextModeObject.SetActive(false);
            else BHumanModeObject.SetActive(false);
        }

        // display black quad.
        if (MainCamera.Height <= 768)
        {
            int quadheight = (int)(MainCamera.Height - transform.localPosition.y);
            Utils.MakeQuad(out BlackQuad, TBackR.width, quadheight, new Color(0, 0, 0, 1));
            BlackQuad.transform.parent = transform;
            BlackQuad.transform.localScale = new Vector3(1, 1, 1);
            BlackQuad.transform.localPosition = new Vector3(TBackL.width, 0, 0.002f);
        }

        // these buttons are present in any resolution
        // backpack open/closed button
        Utils.MakeTexturedQuad(out BPackOpenObject, BPackOpen);
        Utils.MakeTexturedQuad(out BPackClosedObject, BPackClosed);
        BPackOpenObject.transform.parent = BPackClosedObject.transform.parent = transform;
        BPackOpenObject.transform.localScale = BPackClosedObject.transform.localScale = new Vector3(1, 1, 1);
        BPackOpenObject.transform.localPosition = new Vector3(16, 208, -0.002f);
        BPackClosedObject.transform.localPosition = new Vector3(17, 201, -0.002f);
        BPackOpenObject.SetActive(false);
        // spellbook open/close button
        Utils.MakeTexturedQuad(out BBookOpenObject, BBookOpen);
        Utils.MakeTexturedQuad(out BBookClosedObject, BBookClosed);
        BBookOpenObject.transform.parent = BBookClosedObject.transform.parent = transform;
        BBookOpenObject.transform.localScale = BBookClosedObject.transform.localScale = new Vector3(1, 1, 1);
        BBookOpenObject.transform.localPosition = new Vector3(16, 0, -0.002f);
        BBookClosedObject.transform.localPosition = new Vector3(16, 4, -0.002f);
        BBookOpenObject.SetActive(false);

        if (_Viewer != null)
        {
            _Viewer.DisplayPic((BHumanModeObject == null || HumanMode), HBackRObject.transform);
            _Viewer.DisplayInfo((BHumanModeObject == null || !HumanMode), TBackRObject.transform);
        }
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
            Vector2 mPosLocal = new Vector2(mPos.x - transform.position.x, mPos.y - transform.position.y);
            // global check if mouse is inside any of child components
            if (!new Rect(0, 0, HBackR.width + HBackL.width, HBackR.height).Contains(mPosLocal) &&
                !new Rect(0, TBackRObject.transform.localPosition.y, TBackR.width + TBackL.width, TBackR.height).Contains(mPosLocal) &&
                (ExtraRObject == null ||
                 !new Rect(0, ExtraRObject.transform.localPosition.y, ExtraR.width + ExtraL.width, ExtraR.height).Contains(mPosLocal)) &&
                (BlackQuad == null ||
                 !new Rect(BlackQuad.transform.localPosition.x,
                           BlackQuad.transform.localPosition.y,
                           TBackR.width, MainCamera.Height - transform.localPosition.y).Contains(mPosLocal))) return false;

            MouseCursor.SetCursor(MouseCursor.CurDefault);
            // 
            if (e.rawType == EventType.MouseDown)
            {
                if (BHumanModeObject != null)
                {
                    if (new Rect(BHumanModeObject.transform.localPosition.x,
                                 BHumanModeObject.transform.localPosition.y,
                                 BHumanMode.width, BHumanMode.height).Contains(mPosLocal))
                    {
                        // switch to textmode
                        IsHumanMode = !IsHumanMode;
                    }
                }

                if (BPackOpenObject != null)
                {
                    if (new Rect(BPackOpenObject.transform.localPosition.x,
                                 BPackOpenObject.transform.localPosition.y,
                                 BPackOpen.width, BPackOpen.height).Contains(mPosLocal))
                    {
                        // switch to open inventory.
                        MapView.Instance.InventoryVisible = !MapView.Instance.InventoryVisible;
                    }
                }

                if (BBookOpenObject != null)
                {
                    if (new Rect(BBookOpenObject.transform.localPosition.x,
                                 BBookOpenObject.transform.localPosition.y,
                                 BBookOpen.width, BBookOpen.height).Contains(mPosLocal))
                    {
                        // switch to open spellbook
                        MapView.Instance.SpellbookVisible = !MapView.Instance.SpellbookVisible;
                    }
                }
            }

            // check if event was inside view pic
            if (new Rect(HBackL.width, 0, HBackR.width, HBackR.height).Contains(mPosLocal)) // right side of humanback
            {
                if (_Viewer != null)
                    _Viewer.ProcessEventPic(e, mPosLocal.x - HBackL.width, mPosLocal.y);
            }
            else if (new Rect(TBackL.width, TBackRObject.transform.localPosition.y, TBackR.width, TBackR.height).Contains(mPosLocal))
            {
                if (_Viewer != null)
                    _Viewer.ProcessEventInfo(e, mPosLocal.x - TBackL.width, mPosLocal.y - TBackRObject.transform.localPosition.y);
            }

            return true;
        }
        else if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Tab && BHumanModeObject != null)
            {
                IsHumanMode = !IsHumanMode;
                return true;
            }
        }

        return false;
    }

    public bool ProcessCustomEvent(CustomEvent ce)
    {
        return false;
    }

    private int ViewVersion = -1;

    public void Update()
    {
        if (Viewer != null && Viewer.GetObject().RenderInfoVersion != ViewVersion)
        {
            Viewer.DisplayPic((BHumanModeObject == null || HumanMode), HBackRObject.transform);
            Viewer.DisplayInfo((BHumanModeObject == null || !HumanMode), TBackRObject.transform);
            ViewVersion = Viewer.GetObject().RenderInfoVersion;
        }

        if (BPackOpenObject != null && BPackClosedObject != null && PackAvailable)
        {
            bool invopen = MapView.Instance.InventoryVisible;
            BPackOpenObject.SetActive(invopen);
            BPackClosedObject.SetActive(!invopen);
        }
        else if (!PackAvailable)
        {
            BPackOpenObject.SetActive(false);
            BPackClosedObject.SetActive(false);
        }

        if (BBookOpenObject != null && BBookClosedObject != null && BookAvailable)
        {
            bool spbopen = MapView.Instance.SpellbookVisible;
            BBookOpenObject.SetActive(spbopen);
            BBookClosedObject.SetActive(!spbopen);
        } else if (!BookAvailable)
        {
            BBookOpenObject.SetActive(false);
            BBookClosedObject.SetActive(false);
        }
    }

    public bool ProcessStartDrag(float x, float y)
    {
        Vector2 mPosLocal = new Vector2(x - transform.position.x, y - transform.position.y);
        if (!new Rect(HBackL.width, 0, HBackR.width, HBackR.height).Contains(mPosLocal))
            return false;

        if (Viewer != null)
            return Viewer.ProcessStartDrag(mPosLocal.x - HBackL.width, mPosLocal.y);
        return false;
    }

    public bool ProcessDrag(Item item, float x, float y)
    {
        Vector2 mPosLocal = new Vector2(x - transform.position.x, y - transform.position.y);
        if (!new Rect(HBackL.width, 0, HBackR.width, HBackR.height).Contains(mPosLocal))
            return false;

        if (Viewer != null)
            return Viewer.ProcessDrag(item, mPosLocal.x - HBackL.width, mPosLocal.y);
        return false;
    }

    public UiItemDragResult ProcessDrop(Item item, float x, float y)
    {
        Vector2 mPosLocal = new Vector2(x - transform.position.x, y - transform.position.y);
        if (!new Rect(HBackL.width, 0, HBackR.width, HBackR.height).Contains(mPosLocal))
            return UiItemDragResult.Failed;

        if (Viewer != null)
            return Viewer.ProcessDrop(item, mPosLocal.x - HBackL.width, mPosLocal.y);
        return UiItemDragResult.Failed;
    }

    public void ProcessEndDrag()
    {
        if (Viewer != null)
            Viewer.ProcessEndDrag();
    }

    public void ProcessFailDrag(Item item)
    {
        if (Viewer != null)
            Viewer.ProcessFailDrag(item);
    }
    
    public Item ProcessVerifyEndDrag()
    {
        if (Viewer != null)
            return Viewer.ProcessVerifyEndDrag();
        return null;
    }
}