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
                {
                    _Viewer.DisplayPic(false, null);
                    _Viewer.DisplayInfo(false, null);
                }
                _Viewer = value;
                if (_Viewer != null)
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

    // buttons
    private static Texture2D BTextMode;
    private static Texture2D BHumanMode;

    private GameObject HBackRObject;
    private GameObject HBackLObject;
    private GameObject TBackRObject;
    private GameObject TBackLObject;
    private GameObject ExtraRObject;
    private GameObject ExtraLObject;

    // buttons
    private GameObject BTextModeObject;
    private GameObject BHumanModeObject;

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

        if (HBackL == null) HBackL = Images.LoadImage("graphics/interface/humanbackl.bmp", 0, Images.ImageType.AllodsBMP);
        if (HBackR == null) HBackR = Images.LoadImage("graphics/interface/humanbackr.bmp", Images.ImageType.AllodsBMP);
        if (TBackL == null) TBackL = Images.LoadImage("graphics/interface/textbackl.bmp", 0, Images.ImageType.AllodsBMP);
        if (TBackR == null) TBackR = Images.LoadImage("graphics/interface/textbackr.bmp", Images.ImageType.AllodsBMP);

        string extraBase = null;
        if (Screen.height == 768) extraBase = "graphics/interface/extra1024";
        else if (Screen.height >= 600 && Screen.height < 768) extraBase = "graphics/interface/extra800";
        if (extraBase != null && ExtraL == null) ExtraL = Images.LoadImage(extraBase + "l.bmp", 0, Images.ImageType.AllodsBMP);
        if (extraBase != null && ExtraR == null) ExtraR = Images.LoadImage(extraBase + "r.bmp", Images.ImageType.AllodsBMP);

        if (BTextMode == null) BTextMode = Images.LoadImage("graphics/interface/textmode.bmp", 0, Images.ImageType.AllodsBMP);
        if (BHumanMode == null) BHumanMode = Images.LoadImage("graphics/interface/humanmode.bmp", 0, Images.ImageType.AllodsBMP);

        transform.localScale = new Vector3(1, 1, 0.01f);
        transform.localPosition = new Vector3(Screen.width - 176, 238, MainCamera.InterfaceZ + 0.99f); // on this layer all map UI is drawn

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
        if (Screen.height >= 768)
        {
            if (Screen.height == 768)
                tbackY = HBackR.height + ExtraR.height;
            else tbackY = Screen.height - transform.localPosition.y - TBackR.height;
        }

        TBackLObject.transform.parent = transform;
        TBackLObject.transform.localScale = new Vector3(1, 1, 1);
        TBackLObject.transform.localPosition = new Vector3(0, tbackY, 0);
        TBackRObject.transform.parent = transform;
        TBackRObject.transform.localScale = new Vector3(1, 1, 1);
        TBackRObject.transform.localPosition = new Vector3(TBackL.width, tbackY, 0);

        // hide textback if we're switching
        if (Screen.height < 768 && !HumanMode)
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

        if (Screen.height < 768)
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
        if (Screen.height <= 768)
        {
            int quadheight = (int)(Screen.height - transform.localPosition.y);
            Utils.MakeQuad(out BlackQuad, TBackR.width, quadheight, new Color(0, 0, 0, 1));
            BlackQuad.transform.parent = transform;
            BlackQuad.transform.localScale = new Vector3(1, 1, 1);
            BlackQuad.transform.localPosition = new Vector3(TBackL.width, 0, 0.002f);
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
                           TBackR.width, Screen.height - transform.localPosition.y).Contains(mPosLocal))) return false;

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

    public void Update()
    {
        if (Viewer != null && Viewer.GetObject().DoUpdateInfo)
        {
            Viewer.DisplayPic((BHumanModeObject == null || HumanMode), HBackRObject.transform);
            Viewer.DisplayInfo((BHumanModeObject == null || !HumanMode), TBackRObject.transform);
            Viewer.GetObject().DoUpdateInfo = false;
        }
    }
}