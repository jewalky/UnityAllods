using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Window : Widget, IUiEventProcessor
{
    private static Images.AllodsSprite wnd_LM = null;
    private static Material wnd_LMMat = null;

    private Mesh wMesh;
    private RenderTexture BgTexture;
    private GameObject BgObject;

    public GameObject WorkingArea { get; private set; }

    public delegate void ReturnHandler();
    public ReturnHandler OnReturn = null;

    public virtual void OnAwake()
    {

    }

    public void Awake()
    {
        OnAwake();
    }

    public virtual void OnStart()
    {

    }

    public void Start()
    {
        if (wnd_LM == null)
        {
            wnd_LM = Images.Load256("graphics/interface/lm.256");
            wnd_LMMat = new Material(MainCamera.MainShaderPaletted);
            wnd_LMMat.mainTexture = wnd_LM.Atlas;
            wnd_LMMat.SetTexture("_Palette", wnd_LM.OwnPalette);
        }

        // init base mesh
        // make screenshot of background.
        // this should be done before everything, otherwise the previous window can't be screenshotted
        BgTexture = new RenderTexture(Screen.width, Screen.height, 24);
        Camera mc = MainCamera.Instance.GetComponent<Camera>();
        mc.targetTexture = BgTexture;
        bool cvis = MouseCursor.Instance.Visible;
        MouseCursor.Instance.Visible = false;
        mc.Render();
        MouseCursor.Instance.Visible = cvis;
        mc.targetTexture = null;
        BgObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        BgObject.transform.parent = transform;
        BgObject.transform.localPosition = new Vector3(Screen.width/2, Screen.height/2, 0.025f);
        BgObject.transform.localScale = new Vector3(Screen.width, -Screen.height, 1);
        MeshRenderer bgRenderer = BgObject.GetComponent<MeshRenderer>();
        bgRenderer.material = new Material(MainCamera.WindowBgShader);
        bgRenderer.material.mainTexture = BgTexture;
        bgRenderer.material.color = new Color(0.25f, 0.25f, 0.25f, 1);

        // init transform
        transform.parent = UiManager.Instance.transform;
        transform.localScale = new Vector3(1, 1, 1);
        transform.position = new Vector3(0, 0, UiManager.Instance.RegisterWindow(this));
        UiManager.Instance.Subscribe(this);

        MeshRenderer wRenderer = gameObject.AddComponent<MeshRenderer>();
        MeshFilter wFilter = gameObject.AddComponent<MeshFilter>();
        Mesh wMesh = new Mesh();
        wFilter.mesh = wMesh;
        wRenderer.material = wnd_LMMat;

        int vcnt = (8 * Height + 8 * Width + 4 * Width * Height + 4 * 4) // main part
                    + (4 * Width + 4 * Height + 4 * 3)
                    + 4; // shadow border
        Color fullColor = new Color(1, 1, 1, 1);
        Color shadowColor = new Color(0, 0, 0, 0.25f);
        Vector3[] qv = new Vector3[vcnt];
        Vector2[] quv = new Vector2[vcnt];
        Color[] qc = new Color[vcnt];
        int[] qt = new int[vcnt];
        for (int i = 0; i < qt.Length; i++)
            qt[i] = i;
        int pp = 0;
        int ppt = 0;
        int ppc = 0;

        int oScreenX = Screen.width / 2 - Width * 96 / 2;
        int oScreenY = Screen.height / 2 - Height * 64 / 2;

        WorkingArea = Utils.CreateObject();
        WorkingArea.transform.parent = transform;
        //WorkingArea.transform.localPosition = new Vector3(oScreenX, oScreenY, -0.025f);
        WorkingArea.transform.localPosition = new Vector3(oScreenX, oScreenY, -1.025f);

        int shadowOffs = 8;

        // main shadow
        //Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, 0, 0, Screen.width, Screen.height, wnd_LM.AtlasRects[0], shadowColor);
        

        // add shadow corners
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, shadowOffs + oScreenX + Width * 96, shadowOffs + oScreenY - 48, 48, 48, wnd_LM.AtlasRects[3], shadowColor);
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, shadowOffs + oScreenX + Width * 96, shadowOffs + oScreenY + Height * 64, 48, 48, wnd_LM.AtlasRects[8], shadowColor);
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, shadowOffs + oScreenX - 48, shadowOffs + oScreenY + Height * 64, 48, 48, wnd_LM.AtlasRects[6], shadowColor);

        for (int y = 0; y < Height; y++)
        {
            // left = 4
            // right = 5
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, shadowOffs + oScreenX + Width * 96, shadowOffs + oScreenY + y * 64, 48, 64, wnd_LM.AtlasRects[5], shadowColor);

            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX - 48, oScreenY + y * 64, 48, 64, wnd_LM.AtlasRects[4], fullColor);
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX + Width * 96, oScreenY + y * 64, 48, 64, wnd_LM.AtlasRects[5], fullColor);

            for (int x = 0; x < Width; x++)
            {
                if (y == 0) // do only once.
                {
                    Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, shadowOffs + oScreenX + x * 96, shadowOffs + oScreenY + Height * 64, 96, 48, wnd_LM.AtlasRects[7], shadowColor);

                    Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX + x * 96, oScreenY - 48, 96, 48, wnd_LM.AtlasRects[2], fullColor);
                    Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX + x * 96, oScreenY + Height * 64, 96, 48, wnd_LM.AtlasRects[7], fullColor);
                }

                Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX + x * 96, oScreenY + y * 64, 96, 64, wnd_LM.AtlasRects[0], fullColor);
            }
        }

        // add corners
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX - 48, oScreenY - 48, 48, 48, wnd_LM.AtlasRects[1], fullColor);
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX + Width * 96, oScreenY - 48, 48, 48, wnd_LM.AtlasRects[3], fullColor);
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX + Width * 96, oScreenY + Height * 64, 48, 48, wnd_LM.AtlasRects[8], fullColor);
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, oScreenX - 48, oScreenY + Height * 64, 48, 48, wnd_LM.AtlasRects[6], fullColor);

        wMesh.vertices = qv;
        wMesh.uv = quv;
        wMesh.colors = qc;
        wMesh.SetIndices(qt, MeshTopology.Quads, 0);

        OnStart();
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
        UiManager.Instance.UnregisterWindow(this);
    }

    public bool ProcessEvent(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    Destroy(gameObject);
                    break;
                case KeyCode.Tab:
                    // advance child focus
                    AdvanceWidgetFocus(WorkingArea.transform, !e.shift);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (OnReturn != null) OnReturn();
                    break;
            }
        }
        else if (e.rawType == EventType.MouseMove)
        {
            MouseCursor.SetCursor(MouseCursor.CurDefault);
        }

        return true;
    }

    public static void AdvanceWidgetFocus(Transform p, bool forward)
    {
        int cFocused = forward ? -1 : p.childCount;

        for (int i = 0; i < p.childCount; i++)
        {
            Widget cwid = p.GetChild(i).GetComponent<Widget>();
            if (cwid == null)
                continue;

            if (cwid.IsFocused)
            {
                cFocused = i;
                break;
            }
        }

        if (forward)
        {
            for (int i = cFocused+1; i < cFocused+p.childCount; i++)
            {
                Widget cwid = p.GetChild(i%p.childCount).GetComponent<Widget>();
                if (cwid == null)
                    continue;
                if (!(cwid is IFocusableWidget))
                    continue;

                cwid.IsFocused = true;
                break;
            }
        }
        else
        {
            for (int i = cFocused-1; i >= cFocused-p.childCount; i--)
            {
                int iact = i;
                while (iact < 0)
                    iact += p.childCount;
                iact %= p.childCount;

                Widget cwid = p.GetChild(iact).GetComponent<Widget>();
                if (cwid == null)
                    continue;
                if (!(cwid is IFocusableWidget))
                    continue;

                cwid.IsFocused = true;
                break;
            }
        }
    }
}