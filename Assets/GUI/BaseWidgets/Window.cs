using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Window : MonoBehaviour, IUiEventProcessor
{
    private static Images.AllodsSprite wnd_LM = null;
    private static Material wnd_LMMat = null;

    private Mesh BgMesh;
    private GameObject BgObject;

    public int Width = 0;
    public int Height = 0;

    public void Start()
    {
        if (wnd_LM == null)
        {
            wnd_LM = Images.Load256("graphics/interface/lm.256");
            wnd_LMMat = new Material(MainCamera.MainShaderPaletted);
            wnd_LMMat.mainTexture = wnd_LM.Atlas;
            wnd_LMMat.SetTexture("_Palette", wnd_LM.OwnPalette);
        }

        transform.parent = UiManager.Instance.transform;
        transform.localScale = new Vector3(1, 1, 1);
        transform.position = new Vector3(0, 0, UiManager.Instance.RegisterWindow(this));
        UiManager.Instance.Subscribe(this);

        // init base mesh
        BgObject = Utils.CreateObject();
        BgObject.transform.parent = transform;
        BgObject.transform.localPosition = new Vector3(0, 0, 0);
        BgObject.transform.localScale = new Vector3(1, 1, 1);
        MeshRenderer bgRenderer = BgObject.AddComponent<MeshRenderer>();
        MeshFilter bgFilter = BgObject.AddComponent<MeshFilter>();
        BgMesh = new Mesh();
        bgFilter.mesh = BgMesh;
        bgRenderer.material = wnd_LMMat;

        // origin = Screen.width / 2 - Width * 96 / 2
        //          Screen.height / 2 - Height * 64 / 2
        // approximate vertex count: 4 * 4 (corners) + 8 * width (borders horizontal) + 8 * height (borders vertical) + 4 * width * height
        //int vcnt = 4 * 4 + 8 * Width + 8 * Height + 4 * Width * Height;
        Width = 3;
        Height = 3;
        int vcnt = (8 * Height + 8 * Width + 4 * Width * Height + 4 * 4) // main part
                    + (4 * Width + 4 * Height + 4 * 3)
                    + 4; // shadow border
        Color fullColor = new Color(1, 1, 1, 1);
        Color shadowColor = new Color(0, 0, 0, 0.5f);
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

        int shadowOffs = 8;

        // main shadow
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, 0, 0, Screen.width, Screen.height, wnd_LM.AtlasRects[0], shadowColor);

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

        BgMesh.vertices = qv;
        BgMesh.uv = quv;
        BgMesh.colors = qc;
        BgMesh.SetIndices(qt, MeshTopology.Quads, 0);
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
            }
        }
        else if (e.rawType == EventType.MouseMove)
        {
            MouseCursor.SetCursor(MouseCursor.CurDefault);
        }

        return true;
    }
}