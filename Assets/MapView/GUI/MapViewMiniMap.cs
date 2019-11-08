using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

public class MapViewMiniMap : MonoBehaviour, IUiEventProcessor
{
    private Texture2D MapTexture;
    private Color[] MapTextureColors;
    private bool MapTextureNeedsUpdate = false;
    private int MapWidth = 0;
    private int MapHeight = 0;

    private bool IsDragging = false;

    private static Color[] MiniMapData;

    private static Texture2D CrystalL;
    private static Texture2D CrystalR;

    private GameObject CrystalRObject;
    private MeshRenderer CrystalRRenderer;
    private GameObject CrystalLObject;
    private MeshRenderer CrystalLRenderer;
    private GameObject TexObject;
    private MeshRenderer TexRenderer;

    private GameObject BoundsObject;
    private MeshRenderer BoundsRenderer;

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    private void PushTexture()
    {
        MapTexture.Resize(MapWidth, MapHeight);
        MapTexture.SetPixels(MapTextureColors);
        MapTexture.Apply(false);
        TexRenderer.material.mainTexture = MapTexture;
        MapTextureNeedsUpdate = false;
        TexRenderer.enabled = true;
    }

    IEnumerator DoUpdateTexture()
    {
        while (true)
        {
            yield return new WaitUntil(() => (MapTextureNeedsUpdate));
            PushTexture();
        }
    }

    public void OnEnable()
    {
        StartCoroutine(DoUpdateTexture());
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        if (CrystalL == null) CrystalL = Images.LoadImage("graphics/interface/crystall.bmp", 0, Images.ImageType.AllodsBMP);
        if (CrystalR == null) CrystalR = Images.LoadImage("graphics/interface/crystalr.bmp", Images.ImageType.AllodsBMP);
        transform.localScale = new Vector3(1, 1, 0.01f);
        transform.localPosition = new Vector3(Screen.width - 176, 0, MainCamera.InterfaceZ + 0.99f); // on this layer all map UI is drawn

        CrystalLObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        CrystalLRenderer = CrystalLObject.GetComponent<MeshRenderer>();
        CrystalLObject.transform.parent = transform;
        CrystalLObject.transform.localScale = new Vector3(CrystalL.width, CrystalL.height, 1);
        CrystalLObject.transform.localPosition = new Vector3(CrystalL.width / 2, CrystalL.height / 2, 0);
        CrystalLRenderer.material = new Material(MainCamera.MainShader);
        CrystalLRenderer.material.mainTexture = CrystalL;

        CrystalRObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        CrystalRRenderer = CrystalRObject.GetComponent<MeshRenderer>();
        CrystalRObject.transform.parent = transform;
        CrystalRObject.transform.localScale = new Vector3(CrystalR.width, CrystalR.height, 1);
        CrystalRObject.transform.localPosition = new Vector3(CrystalL.width + CrystalR.width / 2, CrystalR.height / 2, 0);
        CrystalRRenderer.material = new Material(MainCamera.MainShader);
        CrystalRRenderer.material.mainTexture = CrystalR;

        TexObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        TexRenderer = TexObject.GetComponent<MeshRenderer>();
        TexObject.transform.parent = transform;
        TexObject.transform.localPosition = new Vector3(CrystalL.width + CrystalR.width / 2, CrystalR.height / 2, 0);
        TexRenderer.material = new Material(MainCamera.MainShader);
        TexRenderer.material.mainTexture = MapTexture;
        TexRenderer.enabled = false;

        if (MiniMapData == null)
        {
            Texture2D mmData = Images.LoadImage("graphics/interface/minimapdata.bmp", Images.ImageType.AllodsBMP);
            MiniMapData = mmData.GetPixels();
            DestroyImmediate(mmData);
        }

        // update bounds object
        Utils.MeshBuilder mb = new Utils.MeshBuilder();

        mb.CurrentPosition = new Vector2(-0.00390625f, 0);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();
        mb.CurrentPosition = new Vector2(1, 0);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();

        mb.CurrentPosition = new Vector2(1, 0);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();
        mb.CurrentPosition = new Vector2(1, 1);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();

        mb.CurrentPosition = new Vector2(1, 1);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();
        mb.CurrentPosition = new Vector2(0, 1);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();

        mb.CurrentPosition = new Vector2(0, 1);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();
        mb.CurrentPosition = new Vector2(0, -0.00390625f);
        mb.CurrentColor = new Color(1, 1, 1, 1);
        mb.NextVertex();

        Mesh boundsMesh = mb.ToMesh(MeshTopology.Lines);
        BoundsObject = new GameObject();
        BoundsObject.name = "ScreenBounds";
        BoundsObject.transform.parent = transform;
        BoundsObject.transform.localPosition = new Vector3(0, 0, -2f);
        BoundsObject.transform.localScale = new Vector2(0, 0);
        MeshFilter boundsFilter = BoundsObject.AddComponent<MeshFilter>();
        boundsFilter.mesh = boundsMesh;
        BoundsRenderer = BoundsObject.AddComponent<MeshRenderer>();
        BoundsRenderer.material = new Material(MainCamera.MainShader);
        BoundsRenderer.enabled = false;
    }

    float miniMapTimer = 0;
    public void Update()
    {
        if (!MapLogic.Instance.IsLoaded)
            return;

        float timeReload = Mathf.Sqrt(MapWidth * MapHeight) / 480;
        miniMapTimer += Time.unscaledDeltaTime;
        if (miniMapTimer > timeReload)
        {
            UpdateTexture(false);
            miniMapTimer = 0;
        }

        UpdateBounds();
    }

    public void UpdateBounds()
    {
        //
        int mapLeft = 8;
        int mapTop = 8;
        float sW = MapLogic.Instance.Width - 16;
        float sH = MapLogic.Instance.Height - 16;
        float aspect = sW / sH;
        float w = 128;
        float h = 128;
        // handle uneven map sizes
        if (sW > sH)
            h /= aspect;
        else if (sH > sW)
            w *= aspect;
        float cX = 90 - w / 2;
        float cY = 83 - h / 2;
        Rect vrec = MapView.Instance.UnpaddedVisibleRect;
        vrec.x -= mapLeft;
        vrec.y -= mapTop;
        BoundsObject.transform.localPosition = new Vector3(Mathf.Floor(cX + vrec.xMin / sW * w), Mathf.Floor(cY + vrec.yMin / sH * h), -2f);
        BoundsObject.transform.localScale = new Vector2(vrec.width / sW * w, vrec.height / sH * h);
        BoundsRenderer.enabled = true;
    }

    public void UpdateTexture(bool inplace)
    {
        if (TexObject == null)
            return;

        if (!MapLogic.Instance.IsLoaded && MapTexture != null)
        {
            Destroy(MapTexture);
            MapTexture = null;
            TexRenderer.material.mainTexture = null;
            MapWidth = 0;
            MapHeight = 0;
            return;
        }

        int newW = MapLogic.Instance.Width - 16;
        int newH = MapLogic.Instance.Height - 16;
        if (newW <= 0 || newH <= 0)
            return;

        if (newW != MapWidth || newH != MapHeight)
        {
            float aspect = (float)newW / newH;
            float w = 128;
            float h = 128;
            // handle uneven map sizes
            if (newW > newH)
                h /= aspect;
            else if (newH > newW)
                w *= aspect;
            TexObject.transform.localScale = new Vector3(w, h, 1);
            TexObject.transform.localPosition = new Vector3(90, 83, -1);
            if (MapTexture == null)
            {
                MapTexture = new Texture2D(newW, newH, TextureFormat.ARGB32, false);
                MapTexture.filterMode = FilterMode.Point;
                MapTextureColors = new Color[newW * newH];
            }
            MapWidth = newW;
            MapHeight = newH;
            Array.Resize<Color>(ref MapTextureColors, newW * newH);
        }

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                MapNode node = MapLogic.Instance.Nodes[x + 8, y + 8];
                if ((node.Flags & MapNodeFlags.Discovered) == 0)
                {
                    MapTextureColors[y * MapWidth + x] = new Color(0, 0, 0, 0);
                    continue;
                }

                // minimapdata is always 52x14
                int tiley = node.Tile & 0xF;
                int tilex = (node.Tile & 0xFF0) >> 4;
                Color c = MiniMapData[tiley * 52 + tilex];
                // put buildings on it?
                // put vision on it

                float light = (float)node.Light / 255;
                c.r *= light;
                c.g *= light;
                c.b *= light;

                if ((node.Flags & MapNodeFlags.Visible) == 0)
                    c.a = 0.5f; // rom2 didn't do this, but this is because they had CPU renderer!

                foreach (MapObject mobj in node.Objects)
                {
                    if (!(mobj is IPlayerPawn))
                        continue;
                    if (((node.Flags & MapNodeFlags.Visible) == 0) && (mobj.GetObjectType() != MapObjectType.Structure))
                        continue; // don't show invisible monsters on minimap
                    Player player = ((IPlayerPawn)mobj).GetPlayer();
                    Color pColor = (player != null) ? (Color)Player.AllColors[player.Color] : new Color(1, 1, 1, 1);

                    pColor.a = c.a;
                    c = pColor;
                }

                MapTextureColors[y * MapWidth + x] = c;
            }
        }

        if (!inplace)
            MapTextureNeedsUpdate = true;
        else PushTexture();
    }

    public bool ProcessEvent(Event e)
    {
        if (e.rawType == EventType.MouseUp)
            IsDragging = false;

        if (e.rawType == EventType.MouseDown ||
            e.rawType == EventType.MouseUp ||
            e.rawType == EventType.MouseMove)
        {
            Vector2 mPos = Utils.GetMousePosition();
            if (mPos.x > transform.position.x &&
                mPos.y > transform.position.y &&
                mPos.x < transform.position.x + 176 &&
                mPos.y < transform.position.y + 158)
            {
                mPos -= new Vector2(transform.position.x, transform.position.y);
                // check cursor specifically
                int mapLeft = 8;
                int mapTop = 8;
                float sW = MapLogic.Instance.Width - 16;
                float sH = MapLogic.Instance.Height - 16;
                float aspect = sW / sH;
                float w = 128;
                float h = 128;
                // handle uneven map sizes
                if (sW > sH)
                    h /= aspect;
                else if (sH > sW)
                    w *= aspect;
                float cX = 90 - w / 2;
                float cY = 83 - h / 2;
                float cXCur = 90 - 64;
                float cYCur = 83 - 64;
                Rect vrec = MapView.Instance.UnpaddedVisibleRect;
                vrec.x -= mapLeft;
                vrec.y -= mapTop;

                MouseCursor.SetCursor(MouseCursor.CurDefault);
                if (mPos.x >= cXCur && mPos.x < cXCur+128 &&
                    mPos.y >= cYCur && mPos.y < cYCur+128)
                {
                    MouseCursor.SetCursor(MouseCursor.CurSmallDefault);
                    if (e.rawType == EventType.MouseDown)
                        IsDragging = true;

                    if (IsDragging)
                    {
                        // find map coordinates from cursor
                        float offsX = (mPos.x - cX) / w * sW + 8 - vrec.width / 2;
                        float offsY = (mPos.y - cY) / h * sH + 8 - vrec.height / 2;
                        MapView.Instance.SetScroll((int)offsX, (int)offsY);
                    }
                }

                return true;
            }
        }
        return false;
    }
}