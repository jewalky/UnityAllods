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

    private static Color[] MiniMapData;

    private static Texture2D CrystalL;
    private static Texture2D CrystalR;

    private GameObject CrystalRObject;
    private MeshRenderer CrystalRRenderer;
    private GameObject CrystalLObject;
    private MeshRenderer CrystalLRenderer;
    private GameObject TexObject;
    private MeshRenderer TexRenderer;

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

    public void Start()
    {
        UiManager.Instance.Subscribe(this);
        StartCoroutine(DoUpdateTexture());

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
    }

    public void UpdateTexture(bool inplace)
    {
        if (!MapLogic.Instance.IsLoaded && MapTexture != null)
        {
            Destroy(MapTexture);
            MapTexture = null;
            TexRenderer.material.mainTexture = null;
            MapWidth = 0;
            MapHeight = 0;
            return;
        }

        int newW = MapLogic.Instance.Width-16;
        int newH = MapLogic.Instance.Height-16;
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
        if (e.type == EventType.MouseDown ||
            e.type == EventType.MouseUp ||
            e.type == EventType.MouseMove)
        {
            Vector2 mPos = Utils.GetMousePosition();
            if (mPos.x > transform.localPosition.x &&
                mPos.y > transform.localPosition.y &&
                mPos.x < transform.localPosition.x + 176 &&
                mPos.y < transform.localPosition.y + 158) return true;
        }
        return false;
    }
}