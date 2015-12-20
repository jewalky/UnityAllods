using UnityEngine;
using System.Collections;

public class MapViewScript : MonoBehaviour {

    // Use this for initialization
	void Start ()
    {
        InitFromFile("scenario/20.alm");
        //InitFromFile("./an_heaven_5_8.alm");
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
        }

        MapLogic.Instance.InitFromFile(filename);
        Debug.Log(string.Format("map = {0} ({1}x{2})", MapLogic.Instance.Title, MapLogic.Instance.Width - 16, MapLogic.Instance.Height - 16));

        MapNode[] nodes = MapLogic.Instance.Nodes;

        int w = MapLogic.Instance.Width;
        int h = MapLogic.Instance.Height;

        // generate mesh
        Mesh mesh = new Mesh();
        Vector3[] qv = new Vector3[4 * w * h];
        Color[] qc = new Color[4 * w * h];
        Vector2[] qtex = new Vector2[4 * w * h];
        int pp = 0;
        int ppc = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                short h1 = nodes[y * w + x].Height;
                short h2 = (x + 1 < w) ? nodes[y * w + x + 1].Height : (short)0;
                short h3 = (y + 1 < h) ? nodes[(y + 1) * w + x].Height : (short)0;
                short h4 = (x + 1 < w && y + 1 < h) ? nodes[(y + 1) * w + x + 1].Height : (short)0;
                qv[pp++] = new Vector3(x * 32, y * 32 - h1, 0);
                qv[pp++] = new Vector3(x * 32 + 33, y * 32 - h2, 0);
                qv[pp++] = new Vector3(x * 32 + 33, y * 32 + 32 - h4 + 1f, 0);
                qv[pp++] = new Vector3(x * 32, y * 32 + 32 - h3 + 1f, 0);
                qc[ppc++] = new Color(1, 1, 1, 1);
                qc[ppc++] = new Color(1, 1, 1, 1);
                qc[ppc++] = new Color(1, 1, 1, 1);
                qc[ppc++] = new Color(1, 1, 1, 1);
            }
        }

        int[] qt = new int[6 * w * h];
        pp = 0;
        for (int i = 0; i < qv.Length; i += 4)
        {
            qt[pp] = i;
            qt[pp + 1] = i + 1;
            qt[pp + 2] = i + 3;
            qt[pp + 3] = i + 3;
            qt[pp + 4] = i + 1;
            qt[pp + 5] = i + 2;
            pp += 6;
        }

        mesh.vertices = qv;
        mesh.triangles = qt;
        mesh.colors = qc;
        mesh.uv = qtex;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;
        UpdateTiles();
        //mr.material = new Material(Shader.Find("Custom/MainShader"));
        mr.material.mainTexture = MapTiles;
        this.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        SetScroll(8, 8);
    }

    void UpdateTiles(int WaterAnimFrame = 0)
    {
        WaterAnimFrame %= 4;
        MapNode[] nodes = MapLogic.Instance.Nodes;

        int w = MapLogic.Instance.Width;
        int h = MapLogic.Instance.Height;

        Vector2[] qtex = new Vector2[4 * w * h];
        int ppt = 0;
        float unit32x = 1f / MapTiles.width * 32;
        float unit32y = 1f / MapTiles.height * 32;
        float unit32x1 = 1f / MapTiles.width;
        float unit32y1 = 1f / MapTiles.height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                ushort tile = nodes[y * w + x].Tile;
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
                qtex[ppt++] = new Vector2(tileBaseRect.xMin, tileBaseRect.yMin + unit32y * tilein);
                qtex[ppt++] = new Vector2(tileBaseRect.xMax, tileBaseRect.yMin + unit32y * tilein);
                qtex[ppt++] = new Vector2(tileBaseRect.xMax, tileBaseRect.yMin + unit32y * tilein + unit32y);
                qtex[ppt++] = new Vector2(tileBaseRect.xMin, tileBaseRect.yMin + unit32y * tilein + unit32y);
            }
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;
        mesh.uv = qtex;
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
        int minX = 8;
        int minY = 8;
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
    void Update () {
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
}
