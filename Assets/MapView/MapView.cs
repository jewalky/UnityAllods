using UnityEngine;
using System.Collections;

public class MapView : MonoBehaviour
{
    public static MapView Instance
    {
        get
        {
            return GameObject.FindObjectOfType<MapView>();
        }
    }

    // Use this for initialization
	void Start ()
    {
        //InitFromFile("scenario/20.alm");
        InitFromFile("./an_heaven_5_8.alm");
    }

    public Texture2D MapTiles = null;
    public Rect[] MapRects = null;
    void InitFromFile(string filename)
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
            for (int i = 0; i < tiles_tmp.Length; i++)
                GameObject.DestroyImmediate(tiles_tmp[i]);
        }

        MapLogic.Instance.InitFromFile(filename);
        Debug.Log(string.Format("map = {0} ({1}x{2})", MapLogic.Instance.Title, MapLogic.Instance.Width - 16, MapLogic.Instance.Height - 16));

        MapNode[] nodes = MapLogic.Instance.Nodes;

        this.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        InitMeshes();
        SetScroll(8, 8);
    }

    GameObject[] MeshChunks = new GameObject[0];
    Rect[] MeshChunkRects = new Rect[0];
    Mesh[] MeshChunkMeshes = new Mesh[0];

    void InitMeshes()
    {
        for (int i = 0; i < MeshChunks.Length; i++)
            Destroy(MeshChunks[i]);
        int cntX = Mathf.CeilToInt((float)MapLogic.Instance.Width / 64);
        int cntY = Mathf.CeilToInt((float)MapLogic.Instance.Height / 64);
        MeshChunks = new GameObject[cntX * cntY];
        MeshChunkRects = new Rect[cntX * cntY];
        MeshChunkMeshes = new Mesh[cntX * cntY];
        int mc = 0;
        for (int y = 0; y < cntY; y++)
        {
            for (int x = 0; x < cntX; x++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "MapViewChunk";
                go.transform.parent = gameObject.transform;
                go.transform.localScale = new Vector3(1, 1, 1);
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                MeshFilter mf = go.GetComponent<MeshFilter>();
                mr.material = new Material(Shader.Find("Custom/TerrainShader"));
                mr.material.mainTexture = MapTiles;
                int m_x = x * 64;
                int m_y = y * 64;
                int m_w = 64;
                int m_h = 64;
                if (m_x + m_w > MapLogic.Instance.Width)
                    m_w = MapLogic.Instance.Width - m_x;
                if (m_y + m_h > MapLogic.Instance.Height)
                    m_h = MapLogic.Instance.Height - m_y;
                mf.mesh = CreatePartialMesh(new Rect(m_x, m_y, m_w, m_h));
                MeshChunkRects[mc] = new Rect(m_x, m_y, m_w, m_h);
                MeshChunkMeshes[mc] = mf.mesh;
                MeshChunks[mc] = go;
                mc++;
            }
        }
    }

    void UpdateLighting(Texture2D lightTex)
    {
        //                mr.material.SetTexture("_LightTex", MapLogic.Instance.GetLightingTexture(1, 1, 1));
        for (int i = 0; i < MeshChunks.Length; i++)
        {
            MeshRenderer mr = MeshChunks[i].GetComponent<MeshRenderer>();
            mr.material.SetTexture("_LightTex", lightTex);
        }
    }

    void UpdateTiles(int WaterAnimFrame)
    {
        for (int i = 0; i < MeshChunks.Length; i++)
            UpdatePartialTiles(MeshChunkMeshes[i], MeshChunkRects[i], WaterAnimFrame);
    }

    Mesh CreatePartialMesh(Rect rec)
    {
        int x = (int)rec.x;
        int y = (int)rec.y;
        int w = (int)rec.width;
        int h = (int)rec.height;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;

        // generate mesh
        Mesh mesh = new Mesh();
        UpdatePartialMesh(mesh, rec);
        UpdatePartialTiles(mesh, rec, waterAnimFrame);
        return mesh;
    }

    void UpdatePartialMesh(Mesh mesh, Rect rec)
    {
        int x = (int)rec.x;
        int y = (int)rec.y;
        int w = (int)rec.width;
        int h = (int)rec.height;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        MapNode[] nodes = MapLogic.Instance.Nodes;

        Vector3[] qv = new Vector3[4 * w * h];
        Color[] qc = new Color[4 * w * h];

        int pp = 0;
        int ppc = 0;
        for (int ly = y; ly < y + h; ly++)
        {
            for (int lx = x; lx < x + w; lx++)
            {
                short h1 = nodes[ly * mw + lx].Height;
                short h2 = (lx + 1 < mw) ? nodes[ly * mw + lx + 1].Height : (short)0;
                short h3 = (ly + 1 < mh) ? nodes[(ly + 1) * mw + lx].Height : (short)0;
                short h4 = (lx + 1 < mw && ly + 1 < mh) ? nodes[(ly + 1) * mw + lx + 1].Height : (short)0;
                qv[pp++] = new Vector3(lx * 32, ly * 32 - h1, 0);
                qv[pp++] = new Vector3(lx * 32 + 33, ly * 32 - h2, 0);
                qv[pp++] = new Vector3(lx * 32 + 33, ly * 32 + 32 - h4 + 1f, 0);
                qv[pp++] = new Vector3(lx * 32, ly * 32 + 32 - h3 + 1f, 0);
                qc[ppc++] = new Color(1, 1, 1, 1);
                qc[ppc++] = new Color(1, 1, 1, 1);
                qc[ppc++] = new Color(1, 1, 1, 1);
                qc[ppc++] = new Color(1, 1, 1, 1);
            }
        }

        mesh.vertices = qv;
        mesh.colors = qc;

        int[] qt = new int[6 * w * h];
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

    void UpdatePartialTiles(Mesh mesh, Rect rec, int WaterAnimFrame = 0)
    {
        WaterAnimFrame %= 4;
        MapNode[] nodes = MapLogic.Instance.Nodes;

        int x = (int)rec.x;
        int y = (int)rec.y;
        int w = (int)rec.width;
        int h = (int)rec.height;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;

        Vector2[] quv = new Vector2[4 * w * h];
        int ppt = 0;
        float unit32x = 1f / MapTiles.width * 32;
        float unit32y = 1f / MapTiles.height * 32;
        float unit32x1 = 1f / MapTiles.width;
        float unit32y1 = 1f / MapTiles.height;
        for (int ly = y; ly < y + h; ly++)
        {
            for (int lx = x; lx < x + w; lx++)
            {
                ushort tile = nodes[ly * mw + lx].Tile;
                int tilenum = (tile & 0xFF0) >> 4; // base rect
                int tilein = tile & 0x00F; // number of picture inside rect

                if (tilenum >= 0x20 && tilenum <= 0x2F)
                {
                    tilenum -= 0x20;
                    int tilewi = tilenum / 4;
                    int tilew = tilenum % 4;
                    int waflocal = (tilewi + WaterAnimFrame) % 4;
                    tilenum = 0x20 + (4 * waflocal) + tilew;
                }

                Rect tileBaseRect = MapRects[tilenum];
                quv[ppt++] = new Vector2(tileBaseRect.xMin, tileBaseRect.yMin + unit32y * tilein);
                quv[ppt++] = new Vector2(tileBaseRect.xMax, tileBaseRect.yMin + unit32y * tilein);
                quv[ppt++] = new Vector2(tileBaseRect.xMax, tileBaseRect.yMin + unit32y * tilein + unit32y);
                quv[ppt++] = new Vector2(tileBaseRect.xMin, tileBaseRect.yMin + unit32y * tilein + unit32y);
            }
        }

        mesh.uv = quv;
    }

    private int _ScrollX = 8;
    private int _ScrollY = 8;

    public int ScrollX
    {
        get
        {
            return _ScrollX;
        }
    }

    public int ScrollY
    {
        get
        {
            return _ScrollY;
        }
    }

    void SetScroll(int x, int y)
    {
        SetScroll(new Vector3(0, 0, 0), x, y);
    }

    void SetScroll(Vector3 baseOffset, int x, int y)
    {
        int minX = 10;
        int minY = 10;
        int screenWB = (int)((float)Screen.width / 32);
        int screenHB = (int)((float)Screen.height / 32);
        int maxX = MapLogic.Instance.Width - screenWB - 8;
        int maxY = MapLogic.Instance.Height - screenHB - 8;

        if (x < minX) x = minX;
        if (y < minY) y = minY;
        if (x > maxX) x = maxX;
        if (y > maxY) y = maxY;

        _ScrollX = x;
        _ScrollY = y;

        this.transform.position = new Vector3((-x * 32) / 100, (-y * 32) / 100, 0);
    }

    // Update is called once per frame
    int waterAnimFrame = 0;
    void Update ()
    {
        // update lighting.
        Texture2D lightTex = MapLogic.Instance.CheckLightingTexture();
        if (lightTex != null)
            UpdateLighting(lightTex);

        UpdateInput();
        UpdateLogic();

        int waterAnimFrameNew = (MapLogic.Instance.LevelTime % 20) / 5;
        if (waterAnimFrame != waterAnimFrameNew)
        {
            waterAnimFrame = waterAnimFrameNew;
            UpdateTiles(waterAnimFrame);
        }
    }

    float lastScrollTime = 0;
    float lastSpeedTime = 0;
    void UpdateInput()
    {
        if (Time.unscaledTime - lastScrollTime > 0.01)
        {
            int deltaX = 0;
            int deltaY = 0;
            float dX = Input.GetAxisRaw("Horizontal");
            float dY = Input.GetAxisRaw("Vertical");
            if (dX < 0) deltaX = -1;
            else if (dX > 0) deltaX = 1;
            if (dY < 0) deltaY = 1;
            else if (dY > 0) deltaY = -1;
            if (deltaX != 0 || deltaY != 0)
            {
                //Debug.Log(string.Format("{0} {1}", dX, dY));
                lastScrollTime = Time.unscaledTime;
                SetScroll(ScrollX + deltaX, ScrollY + deltaY);
            }
        }

        if (Time.unscaledTime - lastSpeedTime > 0.250)
        {
            float dSpeed = Input.GetAxisRaw("Speed");
            if (dSpeed != 0)
            {
                if (dSpeed > 0) MapLogic.Instance.Speed++;
                if (dSpeed < 0) MapLogic.Instance.Speed--;
                lastSpeedTime = Time.unscaledTime;
                Debug.Log(string.Format("Speed = {0}", MapLogic.Instance.Speed));
            }
        }
    }

    float lastLogicUpdateTime = 0;
    void UpdateLogic()
    {
        if (lastLogicUpdateTime == 0)
            lastLogicUpdateTime = Time.time;
        float delta = Time.time - lastLogicUpdateTime;
        if (delta > 1)
        {
            while (delta >= 1)
            {
                MapLogic.Instance.Update();
                delta -= 1;
            }

            lastLogicUpdateTime = Time.time - delta;
        }
    }

    // create gameobject based off this instance for logic
    public GameObject CreateObject<T>(MapLogicObject obj)
    {
        GameObject o = new GameObject();
        MapViewObject viewscript = (MapViewObject)o.AddComponent(typeof(T));
        viewscript.SetLogicObject(obj);
        o.transform.parent = transform;
        return o;
    }
}
