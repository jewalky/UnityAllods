using UnityEngine;
using System.Collections;
using System;

public class MapView : MonoBehaviour, IUiEventProcessor, IUiItemDragger
{
    private static MapView _Instance = null;
    public static MapView Instance
    {
        get
        {
            if (_Instance == null) _Instance = GameManager.Instance.MapView;
            return _Instance;
        }
    }
    
    void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    IEnumerator DoUpdateLight()
    {
        while (true)
        {
            //yield return new WaitUntil(() => (MapLogic.Instance.MapLightingNeedsUpdate));
            yield return new WaitForEndOfFrame();
            MapLogic.Instance.GetLightingTexture(); // update lighting texture
        }
    }

    IEnumerator DoUpdateFOW()
    {
        while (true)
        {
            //yield return new WaitUntil(() => (MapLogic.Instance.MapFOWNeedsUpdate));
            yield return new WaitForEndOfFrame();
            MapLogic.Instance.GetFOWTexture(); // update fog of war texture
        }
    }

    private MapViewMiniMap MiniMap;
    private MapViewCommandbar Commandbar;
    private MapViewInfowindow Infowindow;
    private MapViewChat Chat;
    private MapViewInventory Inventory;
    private MapViewSpellbook Spellbook;
    private bool SpellbookDecastOnHide;

    public Spell OneTimeCast; // this holds a spell casted from scroll

    public MapObject SelectedObject { get; private set; }
    public MapObject HoveredObject { get; private set; }

    // Use this for initialization
    void Start ()
    {
        UiManager.Instance.Subscribe(this);
        StartCoroutine(DoUpdateLight());
        StartCoroutine(DoUpdateFOW());
        //InitFromFile("scenario/20.alm");
        //InitFromFile("an_heaven_5_8.alm");
        //InitFromFile("kids3.alm");

        // create child objects
        MiniMap = Utils.CreateObjectWithScript<MapViewMiniMap>();
        MiniMap.transform.parent = UiManager.Instance.transform; // despite it being a part of minimap, it should be in UiManager since it doesn't move unlike the MapView
        MiniMap.gameObject.SetActive(false);
        Infowindow = Utils.CreateObjectWithScript<MapViewInfowindow>();
        Infowindow.transform.parent = UiManager.Instance.transform;
        Infowindow.gameObject.SetActive(false);
        Commandbar = Utils.CreateObjectWithScript<MapViewCommandbar>();
        Commandbar.transform.parent = UiManager.Instance.transform;
        Commandbar.gameObject.SetActive(false);
        Inventory = Utils.CreateObjectWithScript<MapViewInventory>();
        Inventory.transform.parent = UiManager.Instance.transform;
        Inventory.gameObject.SetActive(false);
        Spellbook = Utils.CreateObjectWithScript<MapViewSpellbook>();
        Spellbook.transform.parent = UiManager.Instance.transform;
        Spellbook.gameObject.SetActive(false);
        SpellbookDecastOnHide = true;
        OneTimeCast = null;

        Chat = Utils.CreateObjectWithScript<MapViewChat>();
        Chat.transform.parent = UiManager.Instance.transform;
    }

    public Rect UnpaddedVisibleRect { get; private set; }
    private Rect _VisibleRect = new Rect(0, 0, 0, 0);
    public Rect VisibleRect
    {
        get
        {
            return _VisibleRect;
        }
    }

    public void Load()
    {
        // show UI parts
        Chat.Show();
    }

    public void OnMapUnloaded()
    {
        Infowindow.Viewer = null;
        Commandbar.EnabledCommands = 0;
        UnloadMeshes();
        // delete all child objects that we have
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }

    public void UnloadMeshes()
    {

        GameObject[] dMeshChunks = MeshChunks;
        GameObject[] dFOWMeshChunks = FOWMeshChunks;
        GameObject[] dGridMeshChunks = GridMeshChunks;
        GameManager.Instance.CallDelegateOnNextFrame(() =>
        {
            for (int i = 0; i < dMeshChunks.Length; i++)
            {
                Utils.DestroyObjectAndMesh(dMeshChunks[i]);
                Utils.DestroyObjectAndMesh(dFOWMeshChunks[i]);
                Utils.DestroyObjectAndMesh(dGridMeshChunks[i]);
            }
            // hide UI parts
            Chat.Hide();
            return false;
        });

        MeshChunks = new GameObject[0];
        FOWMeshChunks = new GameObject[0];
        GridMeshChunks = new GameObject[0];
        MeshChunkRects = new Rect[0];
        MeshChunkMeshes = new Mesh[0];
    }

    // loading thread
    public Texture2D MapTiles = null;
    public Rect[] MapRects = null;
    public void InitFromFile(string filename)
    {
        if (MapTiles == null)
        {
            MapTiles = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            Texture2D[] tiles_tmp = new Texture2D[52];
            for (int i = 0; i < 52; i++)
            {
                int t_c = ((i & 0xF0) >> 4) + 1;
                int t_d = (i & 0x0F);
                string t_fn = string.Format("graphics/terrain/tile{0}-{1}.bmp", t_c, t_d.ToString().PadLeft(2, '0'));
                tiles_tmp[i] = Images.LoadImage(t_fn, Images.ImageType.AllodsBMP);
            }
            MapTiles.filterMode = FilterMode.Point;
            MapRects = MapTiles.PackTextures(tiles_tmp, 1);
            foreach (Texture2D tex in tiles_tmp)
                DestroyImmediate(tex);
        }

        MapLogic.Instance.InitFromFile(filename, false);
    }

    public void OnMapLoaded()
    {
        Debug.LogFormat("map = {0} ({1}x{2})", MapLogic.Instance.Title, MapLogic.Instance.Width - 16, MapLogic.Instance.Height - 16);

        InitMeshes();
        SetScroll(8, 8);
        MiniMap.UpdateTexture(true);

        // run generic load
        Load();

        this.transform.localScale = new Vector3(1, 1, 0.005f);
    }

    GameObject[] MeshChunks = new GameObject[0];
    Rect[] MeshChunkRects = new Rect[0];
    Mesh[] MeshChunkMeshes = new Mesh[0];
    GameObject[] FOWMeshChunks = new GameObject[0];
    GameObject[] GridMeshChunks = null;
    Material MeshMaterial = null;
    Material FOWMeshMaterial = null;
    Material GridMeshMaterial = null;

    void InitMeshes()
    {
        if (MeshMaterial == null)
        {
            MeshMaterial = new Material(MainCamera.TerrainShader);
            MeshMaterial.mainTexture = MapTiles;
        }

        if (FOWMeshMaterial == null)
            FOWMeshMaterial = new Material(MainCamera.TerrainFOWShader);

        if (GridMeshMaterial == null)
            GridMeshMaterial = new Material(MainCamera.MainShader);

        UnloadMeshes();
        if (!MapLogic.Instance.IsLoaded)
            return;

        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        int cntX = Mathf.CeilToInt((float)mw / 64);
        int cntY = Mathf.CeilToInt((float)mh / 64);
        MeshChunks = new GameObject[cntX * cntY];
        MeshChunkRects = new Rect[cntX * cntY];
        MeshChunkMeshes = new Mesh[cntX * cntY];
        FOWMeshChunks = new GameObject[cntX * cntY];
        GridMeshChunks = new GameObject[cntX * cntY];
        int mc = 0;
        for (int y = 0; y < cntY; y++)
        {
            for (int x = 0; x < cntX; x++)
            {
                GameObject go = Utils.CreateObject();
                go.name = "MapViewChunk";
                go.transform.parent = gameObject.transform;
                go.transform.localPosition = new Vector3(0, 0, 0);
                go.transform.localScale = new Vector3(1, 1, 1);
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.material = MeshMaterial;
                MeshFilter mf = go.AddComponent<MeshFilter>();
                int m_x = x * 64;
                int m_y = y * 64;
                int m_w = 64;
                int m_h = 64;
                if (m_x + m_w > MapLogic.Instance.Width)
                    m_w = MapLogic.Instance.Width - m_x;
                if (m_y + m_h > MapLogic.Instance.Height)
                    m_h = MapLogic.Instance.Height - m_y;
                mf.mesh = CreatePartialMesh(new Rect(m_x, m_y, m_w, m_h), false);
                MeshChunkRects[mc] = new Rect(m_x, m_y, m_w, m_h);
                MeshChunkMeshes[mc] = mf.mesh;
                MeshChunks[mc] = go;

                // also duplicate this object for fog of war drawing
                FOWMeshChunks[mc] = GameObject.Instantiate(go);
                FOWMeshChunks[mc].GetComponent<MeshRenderer>().material = FOWMeshMaterial;
                Mesh m2 = CreatePartialMesh(new Rect(m_x, m_y, m_w, m_h), true);
                // update m2 to have uv == uv2
                m2.uv = m2.uv2;
                FOWMeshChunks[mc].GetComponent<MeshFilter>().mesh = m2;
                FOWMeshChunks[mc].transform.parent = transform;
                FOWMeshChunks[mc].transform.localPosition = new Vector3(0, 0, -8192);
                FOWMeshChunks[mc].transform.localScale = new Vector3(1, 1, 1);
                
                GameObject ggo = Utils.CreateObject();
                ggo.name = "MapViewGridChunk";
                ggo.transform.parent = gameObject.transform;
                ggo.transform.localScale = new Vector3(1, 1, 1);
                ggo.transform.localPosition = new Vector3(0, 0, -1);
                MeshRenderer gmr = ggo.AddComponent<MeshRenderer>();
                gmr.material = GridMeshMaterial;
                MeshFilter gmf = ggo.AddComponent<MeshFilter>();
                gmf.mesh = CreatePartialGridMesh(new Rect(m_x, m_y, m_w, m_h));
                GridMeshChunks[mc] = ggo;

                mc++;
            }
        }

        GridMeshMaterial.color = new Color(1, 0, 0, 0.5f);
    }

    Mesh CreatePartialGridMesh(Rect rec)
    {
        int x = (int)rec.x;
        int y = (int)rec.y;
        int w = (int)rec.width;
        int h = (int)rec.height;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        Mesh gmesh = new Mesh();
        Vector3[] gqv = new Vector3[w * h * 3];
        int[] gt = new int[w * h * 4];
        int gpp = 0;
        MapNode[,] nodes = MapLogic.Instance.Nodes;
        for (int ly = y; ly < y + h; ly++)
        {
            for (int lx = x; lx < x + w; lx++)
            {
                short h1 = nodes[lx, ly].Height;
                short h2 = (lx + 1 < mw) ? nodes[lx + 1, ly].Height : (short)0;
                short h3 = (ly + 1 < mh) ? nodes[lx, ly + 1].Height : (short)0;
                //short h4 = (lx + 1 < mw && ly + 1 < mh) ? nodes[lx + 1, ly + 1].Height : (short)0;
                gqv[gpp++] = new Vector3(lx * 32, ly * 32 - h1, 0);
                gqv[gpp++] = new Vector3(lx * 32 + 33, ly * 32 - h2, 0);
                gqv[gpp++] = new Vector3(lx * 32, ly * 32 + 32 - h3 + 1f, 0);
            }
        }

        gpp = 0;
        for (int i = 0; i < gqv.Length; i += 3)
        {
            gt[gpp++] = i;
            gt[gpp++] = i + 1;
            gt[gpp++] = i;
            gt[gpp++] = i + 2;
        }

        gmesh.vertices = gqv;
        gmesh.SetIndices(gt, MeshTopology.Lines, 0);
        return gmesh;
    }

    void UpdateLighting(Texture2D lightTex)
    {
        MeshMaterial.SetTexture("_LightTex", lightTex);
    }

    void UpdateFOW(Texture2D fowTex)
    {
        FOWMeshMaterial.mainTexture = fowTex;
        FOWMeshMaterial.SetColor("_Color", new Color(0, 0, 0, 1));
    }

    Mesh CreatePartialMesh(Rect rec, bool inverted)
    {
        // generate mesh
        Mesh mesh = new Mesh();
        UpdatePartialMesh(mesh, rec, inverted);
        UpdatePartialTiles(mesh, rec, WaterAnimFrame);
        return mesh;
    }

    short GetHeightAt(int x, int y)
    {
        if (x >= 0 && x < MapLogic.Instance.Width &&
            y >= 0 && y < MapLogic.Instance.Height)
            return MapLogic.Instance.Nodes[x, y].Height;
        return 0;
    }

    void UpdatePartialMesh(Mesh mesh, Rect rec, bool inverted)
    {
        int x = (int)rec.x;
        int y = (int)rec.y;
        int w = (int)rec.width;
        int h = (int)rec.height;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        MapNode[,] nodes = MapLogic.Instance.Nodes;

        Vector3[] qv = new Vector3[4 * w * h];

        int pp = 0;
        for (int ly = y; ly < y + h; ly++)
        {
            for (int lx = x; lx < x + w; lx++)
            {
                short h1 = nodes[lx, ly].Height;
                short h2 = (lx + 1 < mw) ? nodes[lx+1, ly].Height : (short)0;
                short h3 = (ly + 1 < mh) ? nodes[lx, ly+1].Height : (short)0;
                short h4 = (lx + 1 < mw && ly + 1 < mh) ? nodes[lx+1, ly+1].Height : (short)0;
                qv[pp++] = new Vector3(lx * 32, ly * 32 - h1, 0);
                qv[pp++] = new Vector3(lx * 32 + 33, ly * 32 - h2, 0);
                qv[pp++] = new Vector3(lx * 32 + 33, ly * 32 + 32 - h4 + 1f, 0);
                qv[pp++] = new Vector3(lx * 32, ly * 32 + 32 - h3 + 1f, 0);
            }
        }

        mesh.vertices = qv;

        int[] qt = new int[6 * w * h];
        if (!inverted)
        {
            pp = 0;
            for (int i = 0; i < 4 * w * h; i += 4)
            {
                qt[pp] = i;
                qt[pp + 1] = i + 1;
                qt[pp + 2] = i + 3;
                qt[pp + 3] = i + 3;
                qt[pp + 4] = i + 1;
                qt[pp + 5] = i + 2;
                pp += 6;
            }
        }
        else
        {
            pp = qt.Length - 6;
            for (int i = 0; i < 4 * w * h; i += 4)
            {
                qt[pp] = i;
                qt[pp + 1] = i + 1;
                qt[pp + 2] = i + 3;
                qt[pp + 3] = i + 3;
                qt[pp + 4] = i + 1;
                qt[pp + 5] = i + 2;
                pp -= 6;
            }
        }

        mesh.triangles = qt;

        // also UV, but only uv2
        Vector2[] quv2 = new Vector2[4 * w * h];
        pp = 0;
        for (int ly = y; ly < y + h; ly++)
        {
            for (int lx = x; lx < x + w; lx++)
            {
                quv2[pp++] = new Vector2((float)lx / 256, (float)ly / 256);
                quv2[pp++] = new Vector2((float)(lx + 1) / 256, (float)ly / 256);
                quv2[pp++] = new Vector2((float)(lx + 1) / 256, (float)(ly + 1) / 256);
                quv2[pp++] = new Vector2((float)lx / 256, (float)(ly + 1) / 256);
            }
        }

        mesh.uv2 = quv2;
    }

    void UpdatePartialTiles(Mesh mesh, Rect rec, int waf)
    {
        waf %= 4;
        MapNode[,] nodes = MapLogic.Instance.Nodes;

        int x = (int)rec.x;
        int y = (int)rec.y;
        int w = (int)rec.width;
        int h = (int)rec.height;

        Vector2[] quv = new Vector2[4 * w * h];
        int ppt = 0;
        //float unit32x = 1f / MapTiles.width * 32;
        float unit32y = 1f / MapTiles.height * 32;
        //float unit32x1 = 1f / MapTiles.width;
        //float unit32y1 = 1f / MapTiles.height;
        for (int ly = y; ly < y + h; ly++)
        {
            for (int lx = x; lx < x + w; lx++)
            {
                MapNode node = nodes[lx, ly];

                ushort tile = node.Tile;
                int tilenum = (tile & 0xFF0) >> 4; // base rect
                int tilein = tile & 0x00F; // number of picture inside rect

                if (((node.Flags & MapNodeFlags.Visible) != 0) && tilenum >= 0x20 && tilenum <= 0x2F)
                {
                    tilenum -= 0x20;
                    int tilewi = tilenum / 4;
                    int tilew = tilenum % 4;
                    int waflocal = tilewi;
                    if (waf != tilewi)
                        waflocal = ++waflocal % 4;
                    tilenum = 0x20 + (4 * waflocal) + tilew;
                    node.Tile = (ushort)((tilenum << 4) | tilein);
                }

                Rect tileBaseRect = MapRects[tilenum];
                float tileY = tileBaseRect.yMin + unit32y * tilein;
                quv[ppt++] = new Vector2(tileBaseRect.xMin, tileY);
                quv[ppt++] = new Vector2(tileBaseRect.xMax, tileY);
                quv[ppt++] = new Vector2(tileBaseRect.xMax, tileY + unit32y);
                quv[ppt++] = new Vector2(tileBaseRect.xMin, tileY + unit32y);
            }
        }

        mesh.uv = quv;
    }

    private int _MouseCellX = -1;
    private int _MouseCellY = -1;

    public int MouseCellX { get { return _MouseCellX; } }
    public int MouseCellY { get { return _MouseCellY; } }

    private int _ScrollX = -1;
    private int _ScrollY = -1;

    public int ScrollX { get { return _ScrollX; } }
    public int ScrollY { get { return _ScrollY; } }

    public void OnObjectSelected(MapObject mobj)
    {
        if (mobj is IPlayerPawn)
        {
            Commandbar.InitDefault(SelectedObject);
            if (mobj.GetObjectType() == MapObjectType.Human &&
                ((MapHuman)mobj).IsHero)
            {
                Inventory.SetPack((MapHuman)mobj);
                ///
            }
            else Inventory.SetPack(null);
            if (mobj is MapUnit)
                Spellbook.SetSpells((MapUnit)mobj);
            else Spellbook.SetSpells(null);
        }
    }

    public void CenterOnObject(MapObject mobj)
    {
        CenterOnCell(mobj.X+mobj.Width/2, mobj.Y+mobj.Height/2);
        if (mobj is IPlayerPawn)
        {
            SelectedObject = mobj;
            OnObjectSelected(mobj);
        }
    }

    public void CenterOnCell(int x, int y)
    {
        int screenWB = (int)((float)Screen.width / 32 - 5); // 5 map cells are the right panels. these are always there.
        int screenHB = (int)((float)Screen.height / 32);
        SetScroll(x - screenWB / 2, y - screenHB / 2);
    }

    public void SetScroll(int x, int y)
    {
        const int minX = 8;
        const int minY = 8;
        int screenWB = (int)((float)Screen.width / 32 - 5); // 5 map cells are the right panels. these are always there.
        int screenHB = (int)((float)Screen.height / 32);
        int maxX = MapLogic.Instance.Width - screenWB - 8;
        int maxY = MapLogic.Instance.Height - screenHB - 10;

        if (x < minX) x = minX;
        if (y < minY) y = minY;
        if (x > maxX) x = maxX;
        if (y > maxY) y = maxY;

        if (_ScrollX != x || _ScrollY != y)
        {
            _ScrollX = x;
            _ScrollY = y;

            _VisibleRect = new Rect(_ScrollX, _ScrollY, screenWB, screenHB);
            UnpaddedVisibleRect = new Rect(_VisibleRect);
            _VisibleRect.xMin -= 4;
            _VisibleRect.yMin -= 4;
            _VisibleRect.xMax += 4;
            _VisibleRect.yMax += 4;
            if (_VisibleRect.xMin < 0)
                _VisibleRect.xMin = 0;
            if (_VisibleRect.yMin < 0)
                _VisibleRect.yMin = 0;
            if (_VisibleRect.xMax > MapLogic.Instance.Width)
                _VisibleRect.xMax = MapLogic.Instance.Width;
            if (_VisibleRect.yMax > MapLogic.Instance.Height)
                _VisibleRect.yMax = MapLogic.Instance.Height;

            float sx = _ScrollX;
            float sy = _ScrollY;
            transform.position = new Vector3((-sx * 32), (-sy * 32), 0);

            MapLogic.Instance.CalculateDynLighting(); // this is needed due to the fact that we calculate dynlights based on viewrect
        }
    }

    public bool GridEnabled = false;

    int ScrollDeltaX = 0;
    int ScrollDeltaY = 0;
    int WaterAnimFrame = 0;
    float scrollTimer = 0;

    private bool _InventoryVisible;
    public bool InventoryVisible
    {
        get
        {
            return (_InventoryVisible && MapLogic.Instance.IsLoaded);
        }

        set
        {
            _InventoryVisible = value;
        }
    }

    private bool _SpellbookVisible;
    public bool SpellbookVisible
    {
        get
        {
            return (_SpellbookVisible && MapLogic.Instance.IsLoaded);
        }

        set
        {
            _SpellbookVisible = value;
        }
    }

    void Update()
    {
        MiniMap.gameObject.SetActive(MapLogic.Instance.IsLoaded);
        Commandbar.gameObject.SetActive(MapLogic.Instance.IsLoaded);
        Infowindow.gameObject.SetActive(MapLogic.Instance.IsLoaded);
        Inventory.gameObject.SetActive(InventoryVisible);
        Spellbook.gameObject.SetActive(SpellbookVisible);

        if (!MapLogic.Instance.IsLoaded)
            return;

        if (GridMeshMaterial == null || FOWMeshMaterial == null || MeshMaterial == null)
            return;

        if (GridEnabled) GridMeshMaterial.color = new Color(1, 0, 0, 0.5f);
        else GridMeshMaterial.color = new Color(0, 0, 0, 0);

        // update lighting.
        Texture2D lightTex = MapLogic.Instance.CheckLightingTexture();
        if (lightTex != null)
            UpdateLighting(lightTex);
        Texture2D fowTex = MapLogic.Instance.CheckFOWTexture();
        if (fowTex != null)
            UpdateFOW(fowTex);

        if (HoveredObject != null && (HoveredObject.GetObjectType() == MapObjectType.Monster ||
                                      HoveredObject.GetObjectType() == MapObjectType.Human) && (!((MapUnit)HoveredObject).IsAlive || !HoveredObject.IsLinked)) HoveredObject = null;
        if (SelectedObject != null && (SelectedObject.GetObjectType() == MapObjectType.Monster ||
                                       SelectedObject.GetObjectType() == MapObjectType.Human) && (!((MapUnit)SelectedObject).IsAlive || !SelectedObject.IsLinked)) SelectedObject = null;

        UpdateLogic();

        int waterAnimFrameNew = (MapLogic.Instance.LevelTime % 20) / 5;
        if (WaterAnimFrame != waterAnimFrameNew)
        {
            WaterAnimFrame = waterAnimFrameNew;
            UpdateTiles(WaterAnimFrame);
        }

        // update scrolling
        scrollTimer += Time.unscaledDeltaTime;
        if (scrollTimer > 0.01)
        {
            SetScroll(ScrollX + ScrollDeltaX, ScrollY + ScrollDeltaY);
            scrollTimer = 0;
        }
    }

    public bool ProcessEvent(Event e)
    {
        if (!MapLogic.Instance.IsLoaded)
        {
            if (MapLogic.Instance.IsLoading)
                MouseCursor.SetCursor(MouseCursor.CurWait);
            return false;
        }

        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.LeftArrow:
                    if (ScrollDeltaX == 0)
                        ScrollDeltaX = -1;
                    return true;
                case KeyCode.RightArrow:
                    if (ScrollDeltaX == 0)
                        ScrollDeltaX = 1;
                    return true;
                case KeyCode.UpArrow:
                    if (ScrollDeltaY == 0)
                        ScrollDeltaY = -1;
                    return true;
                case KeyCode.DownArrow:
                    if (ScrollDeltaY == 0)
                        ScrollDeltaY = 1;
                    return true;
                case KeyCode.Plus:
                case KeyCode.Equals: // + = =
                case KeyCode.KeypadPlus:
                    {
                        int oldSpeed = MapLogic.Instance.Speed;
                        if (!NetworkManager.IsClient)
                            MapLogic.Instance.Speed++;
                        if (oldSpeed != MapLogic.Instance.Speed)
                            MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, Locale.Main[108 + MapLogic.Instance.Speed]);
                    }
                    return true;
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    {
                        int oldSpeed = MapLogic.Instance.Speed;
                        if (!NetworkManager.IsClient)
                            MapLogic.Instance.Speed--;
                        if (oldSpeed != MapLogic.Instance.Speed)
                            MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, Locale.Main[108 + MapLogic.Instance.Speed]);
                    }
                    return true;
                case KeyCode.F3:
                    //if (NetworkManager.IsClient)
                    {
                        DiplomacyWindow wnd = Utils.CreateObjectWithScript<DiplomacyWindow>();
                        //Debug.LogFormat("created a window!");
                    }
                    return true;
                case KeyCode.Escape:
                    {
                        // todo: create main menu
                        ExampleWindow wnd = Utils.CreateObjectWithScript<ExampleWindow>();
                    }
                    return true;
                case KeyCode.Space:
                    // check if local unit is dead.
                    // for dead local unit, space sends respawn.
                    // for alive local unit, space toggles both spellbook and inventory.
                    if (MapLogic.Instance.ConsolePlayer != null &&
                        !MapLogic.Instance.ConsolePlayer.Avatar.IsAlive)
                    {
                        Client.SendRespawn();
                    }
                    else
                    {
                        bool cstate = InventoryVisible && SpellbookVisible;
                        InventoryVisible = !cstate;
                        SpellbookVisible = !cstate;
                        Spellbook.Update();
                    }
                    return true;
                case KeyCode.BackQuote:
                    InventoryVisible = !InventoryVisible;
                    Spellbook.Update();
                    return true;
                case KeyCode.Q:
                    SpellbookVisible = !SpellbookVisible;
                    Spellbook.Update();
                    return true;
            }
        }
        else if (e.type == EventType.KeyUp)
        {
            switch (e.keyCode)
            {
                case KeyCode.LeftArrow:
                    if (ScrollDeltaX == -1)
                        ScrollDeltaX = 0;
                    return true;
                case KeyCode.RightArrow:
                    if (ScrollDeltaX == 1)
                        ScrollDeltaX = 0;
                    return true;
                case KeyCode.UpArrow:
                    if (ScrollDeltaY == -1)
                        ScrollDeltaY = 0;
                    return true;
                case KeyCode.DownArrow:
                    if (ScrollDeltaY == 1)
                        ScrollDeltaY = 0;
                    return true;
            }
        }
        else if (e.rawType == EventType.MouseMove)
        {
            UpdateInput();
            return true;
        }
        else if (e.rawType == EventType.MouseDown && e.button == 0)
        {
            // select unit if not selected yet
            if (HoveredObject != null &&
                (SelectedObject == null || (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Move && GetCastSpell() == Spell.Spells.NoneSpell &&
                                            !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) || (Commandbar.CurrentCommand == 0)))
            {
                SelectedObject = HoveredObject;
                OnObjectSelected(SelectedObject);
            }
            else if (SelectedObject != null && SelectedObject is IPlayerPawn && ((IPlayerPawn)SelectedObject).GetPlayer() == MapLogic.Instance.ConsolePlayer)
            {
                // todo: handle commands here
                // try to walk.
                if (SelectedObject.GetObjectType() == MapObjectType.Monster ||
                    SelectedObject.GetObjectType() == MapObjectType.Human)
                {
                    MapUnit unit = (MapUnit)SelectedObject;
                    Spell.Spells castSpId = GetCastSpell();
                    if (castSpId != Spell.Spells.NoneSpell && (MapLogic.Instance.Nodes[MouseCellX, MouseCellY].Flags & MapNodeFlags.Visible) != 0)
                    {
                        Spell castSp = GetOneTimeCast();
                        if (castSp == null)
                            castSp = unit.GetSpell(castSpId);
                        if (HoveredObject != null && (HoveredObject.GetObjectType() == MapObjectType.Monster ||
                                                      HoveredObject.GetObjectType() == MapObjectType.Human))
                            Client.SendCastToUnit(unit, castSp, (MapUnit)HoveredObject, MouseCellX, MouseCellY);
                        else Client.SendCastToArea(unit, castSp, MouseCellX, MouseCellY);
                        if (castSp == OneTimeCast || Commandbar.CurrentCommandActual == MapViewCommandbar.Commands.Cast)
                        {
                            SpellbookVisible = false;
                            if (Commandbar.CurrentCommandActual == MapViewCommandbar.Commands.Cast)
                                Commandbar.InitDefault(SelectedObject);
                        }
                        OneTimeCast = null;
                    }
                    else if (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Move)
                    {
                        Client.SendMoveUnit(unit, MouseCellX, MouseCellY);
                    }
                    else if (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Attack && 
                             HoveredObject != null && 
                             (HoveredObject.GetObjectType() == MapObjectType.Monster ||
                              HoveredObject.GetObjectType() == MapObjectType.Human))
                    {
                        Client.SendAttackUnit(unit, (MapUnit)HoveredObject);
                    }
                    else if (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Pickup &&
                             MapLogic.Instance.GetSackAt(MouseCellX, MouseCellY) != null)
                    {
                        Client.SendPickupUnit(unit, MouseCellX, MouseCellY); // issue sack pickup command by current unit at target coordinates
                    }
                }
            }

            return true;
        }
        else if (e.rawType == EventType.MouseDown && e.button == 1)
        {
            // logic: if currently casting a scroll, cancel cast from item
            if (GetOneTimeCast() != null)
            {
                OneTimeCast = null;
                return true;
            }

            // logic: if spellbook is open and some spell is selected, set spell to NoneSpell
            // player clicked somewhere not on the book (presumably over the map)
            if (SpellbookVisible)
            {
                if (Spellbook.ActiveSpell != Spell.Spells.NoneSpell)
                {
                    Spellbook.ActiveSpell = Spell.Spells.NoneSpell;
                    return true;
                }
            }

            SelectedObject = null; // deselect
            Commandbar.EnabledCommands = 0;
            return true;
        }

        return false;
    }

    public Spell GetOneTimeCast()
    {
        if (OneTimeCast == null || OneTimeCast.User == null ||
            OneTimeCast.User != SelectedObject ||
            OneTimeCast.Template == null)
        {
            OneTimeCast = null;
            return null;
        }

        return OneTimeCast;
    }

    public Spell.Spells GetCastSpell()
    {
        Spell.Spells splId = Spell.Spells.NoneSpell;
        Templates.TplSpell spl = null;
        Spell itemSp = GetOneTimeCast();

        if (itemSp != null)
        {
            splId = itemSp.SpellID;
            spl = itemSp.Template;
        }
        else if (SpellbookVisible && Spellbook.Unit == SelectedObject && Spellbook.ActiveSpell != Spell.Spells.NoneSpell)
        {
            splId = Spellbook.ActiveSpell;
            spl = TemplateLoader.GetSpellById((int)splId - 1);
        }

        if (spl != null &&
            (spl.IsAreaSpell ||
            (!spl.IsAreaSpell && HoveredObject != null && (HoveredObject.GetObjectType() == MapObjectType.Human || HoveredObject.GetObjectType() == MapObjectType.Monster))))
        {
            return splId;
        }

        return Spell.Spells.NoneSpell;
    }

    void UpdateInput()
    {
        if (!MapLogic.Instance.IsLoaded)
            return;

        // we'll just put this here
        Spell itemSp = GetOneTimeCast();
        if (itemSp != null && Spellbook != null)
            Spellbook.ActiveSpell = itemSp.SpellID;

        // update mouse x/y
        int oldMouseCellX = MouseCellX;
        int oldMouseCellY = MouseCellY;
        Vector3 mPos = Utils.GetMousePosition();
        mPos.x += ScrollX * 32;
        mPos.y += ScrollY * 32;
        float cXFrac = (mPos.x / 32) - Mathf.Floor(mPos.x / 32);
        _MouseCellX = (int)(mPos.x / 32);
        _MouseCellY = 0;
        for (int y = (int)_VisibleRect.yMin; y <= _VisibleRect.yMax; y++)
        {
            float h1 = y * 32 - GetHeightAt(_MouseCellX, y);
            float h2 = y * 32 - GetHeightAt(_MouseCellX + 1, y);
            float h = h1 * (1f - cXFrac) + h2 * cXFrac;
            if (mPos.y < h)
            {
                _MouseCellY = y - 1;
                break;
            }
        }
        _MouseCellX = Mathf.Clamp(_MouseCellX, 0, MapLogic.Instance.Width-1);
        _MouseCellY = Mathf.Clamp(_MouseCellY, 0, MapLogic.Instance.Height-1);
        //Debug.Log(string.Format("mouse = {0} {1} (from {2} {3})", _MouseCellX, _MouseCellY, mPos.x, mPos.y));

        // temporary!
        if (MapLogic.Instance.ConsolePlayer == null) // mostly if we're server
        {
            if (oldMouseCellX != MouseCellX ||
                oldMouseCellY != MouseCellY)
            {
                MapLogic.Instance.SetTestingVisibility(MouseCellX, MouseCellY, 5);
            }
        }

        // PERMANENT (x2)
        // check currently hovered object
        MapNode[,] nodes = MapLogic.Instance.Nodes;
        MapObject o = null;
        float oZ = 0;
        for (int y = (int)_VisibleRect.yMin; y <= _VisibleRect.yMax; y++)
        {
            for (int x = (int)_VisibleRect.xMin; x <= _VisibleRect.xMax; x++)
            {
                foreach (MapObject mobj in nodes[x, y].Objects)
                {
                    if (mobj.GameObject == null || mobj.GameScript == null)
                        continue;

                    //
                    if (!(mobj.GameScript is IMapViewSelectable) || !((IMapViewSelectable)mobj.GameScript).IsSelected((int)mPos.x, (int)mPos.y))
                        continue;

                    if (mobj.GameObject.transform.position.z < oZ)
                    {
                        o = mobj;
                        oZ = mobj.GameObject.transform.position.z;
                    }
                }
            }
        }

        MouseCursor.SetCursor(MouseCursor.CurDefault);
        bool hoveringDarkness = (nodes[MouseCellX, MouseCellY].Flags & MapNodeFlags.Visible) == 0;

        if (o != null && (o.GameScript is IMapViewSelfie))
            Infowindow.Viewer = (IMapViewSelfie)o.GameScript;
        else if (SelectedObject != null && (SelectedObject.GameScript is IMapViewSelfie))
            Infowindow.Viewer = (IMapViewSelfie)SelectedObject.GameScript;
        else Infowindow.Viewer = null;
        HoveredObject = o;

        if (GetCastSpell() != Spell.Spells.NoneSpell && !hoveringDarkness)
        {
            MouseCursor.SetCursor(MouseCursor.CurCast);
            return;
        }

        if (!hoveringDarkness && HoveredObject != null && (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Move || Commandbar.CurrentCommand == 0))
        {
            // hovered usable buildings have different cursor picture.
            if (HoveredObject.GetObjectType() == MapObjectType.Structure &&
                ((MapStructure)HoveredObject).Class.Usable) MouseCursor.SetCursor(MouseCursor.CurSelectStructure);
            else MouseCursor.SetCursor(MouseCursor.CurSelect);
        }
        else if (SelectedObject != null && SelectedObject is IPlayerPawn)
        {
            Player sp = ((IPlayerPawn)SelectedObject).GetPlayer();
            if (sp == MapLogic.Instance.ConsolePlayer)
            {
                if (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Move)
                {
                    MouseCursor.SetCursor(MouseCursor.CurMove);
                }
                else if (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Attack)
                {
                    MouseCursor.SetCursor(MouseCursor.CurAttack);
                }
                else if (Commandbar.CurrentCommand == MapViewCommandbar.Commands.MoveAttack)
                {
                    MouseCursor.SetCursor(MouseCursor.CurMoveAttack);
                }
                else if (Commandbar.CurrentCommand == MapViewCommandbar.Commands.Pickup)
                {
                    MouseCursor.SetCursor(MouseCursor.CurPickup);
                }
            }
        }
    }

    float lastLogicUpdateTime = 0;
    float lastLogTime = 0;
    float lastUpTime = 0;
    void UpdateLogic()
    {
        for (int i = 0; i < MapLogic.Instance.Objects.Count; i++)
        {
            MapObject mobj = MapLogic.Instance.Objects[i];
            mobj.CheckAllocateObject();
            if (mobj.GameScript != null && mobj.GameScript is IObjectManualUpdate)
                ((IObjectManualUpdate)mobj.GameScript).OnUpdate();
        }

        lastLogTime += Time.deltaTime;
        lastLogicUpdateTime += Time.deltaTime;
        if (lastLogicUpdateTime >= 1)
        {
            while (lastLogicUpdateTime >= 1)
            {
                float time1 = Time.realtimeSinceStartup;
                MapLogic.Instance.Update();
                lastUpTime += Time.realtimeSinceStartup - time1;
                if (lastLogTime >= 1)
                {
                    //Debug.Log(string.Format("update = {0}s/1s", lastUpTime));
                    lastUpTime = 0;
                    lastLogTime = 0;
                }
                lastLogicUpdateTime -= 1;
            }
        }
    }

    void UpdateTiles(int waf)
    {
        for (int i = 0; i < MeshChunks.Length; i++)
            UpdatePartialTiles(MeshChunkMeshes[i], MeshChunkRects[i], waf);
    }

    // create gameobject based off this instance for logic
    public GameObject CreateObject(Type t, MapObject obj)
    {
        GameObject o = Utils.CreateObject();
        MapViewObject viewscript = (MapViewObject)o.AddComponent(t);
        viewscript.SetLogicObject(obj);
        o.transform.parent = transform;
        return o;
    }

    // spawn damage count
    public void SpawnDamageNumbers(MapUnit unit, int damage, bool crit)
    {
        float usz = 1;// (int)(1f + (float)(unit.Width + unit.Height) / 4);
        int offsX = (int)(-4 * usz);
        int offsY = (int)(-16 * usz);

        SpawnDamageNumbers(unit.X + unit.FracX, unit.Y + unit.FracY, 0, damage, false, offsX, offsY, unit.Player);
    }

    private void SpawnDamageNumbers(float x, float y, float z, int damage, bool crit, int offsX, int offsY, Player p)
    {
        int fromX = Mathf.FloorToInt(x);
        int fromY = Mathf.FloorToInt(y);
        int toX = Mathf.CeilToInt(x);
        int toY = Mathf.CeilToInt(y);

        // check if any cell around this number is visible
        bool anyvisible = false;
        for (int cy = fromY; cy <= toY; cy++)
        {
            if (anyvisible) break;
            for (int cx = fromX; cx <= toX; cx++)
            {
                if (cx < 8 || cy < 8 ||
                    cx >= MapLogic.Instance.Width - 8 ||
                    cy >= MapLogic.Instance.Height - 8) continue;
                if ((MapLogic.Instance.Nodes[cx, cy].Flags & MapNodeFlags.Visible) == 0)
                    continue;
                anyvisible = true;
                break;
            }
        }

        if (!anyvisible)
            return;

        MapViewNumbers mvn = MapViewNumbers.Create(x, y, z, damage, crit, offsX, offsY, p);
        mvn.transform.parent = transform;
    }

    public Vector2 MapToScreenCoords(float x, float y, int w, int h)
    {
        float height = MapLogic.Instance.GetHeightAt(x, y, w, h);
        Vector2 ov = new Vector2(Mathf.Round(x * 32), Mathf.Round(y * 32 - height));
        return ov;
    }

    public bool ProcessStartDrag(float x, float y)
    {
        return false; // drag from map is not supported.
    }

    public bool ProcessDrag(Item item, float x, float y)
    {
        return MapLogic.Instance.IsLoaded &&
            SelectedObject != null &&
            SelectedObject is IPlayerPawn &&
            ((IPlayerPawn)SelectedObject).GetPlayer() == MapLogic.Instance.ConsolePlayer &&
            SelectedObject.GetObjectType() == MapObjectType.Human &&
            ((MapHuman)SelectedObject).IsHero;
    }

    public bool ProcessDrop(Item item, float x, float y)
    {
        //
        //throw new NotImplementedException();
        // disregard x/y, as we got MouseCellX/MouseCellY.
        //Debug.LogFormat("dropping {0} of {1}", item.Count, item.ToString());
        MapHuman human = (MapHuman)SelectedObject;
        Client.DropItem(human, item, MouseCellX, MouseCellY);
        return true;
    }

    public void ProcessEndDrag()
    {
        // this is the function that's called after item FROM this has succeeded dragging.
    }

    public void ProcessFailDrag()
    {
        // same as above except with error.
    }

    public Item ProcessVerifyEndDrag()
    {
        // same as above, used in dragging.
        return null;
    }
}
