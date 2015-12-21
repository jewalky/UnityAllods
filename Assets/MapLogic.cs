using UnityEngine;

class MapNode
{
    public short Height = 0;
    public byte Light = 255; //?
    public ushort Tile = 0;
    public ushort Flags = 0;
}

class MapLogic
{
    private static MapLogic _Instance = null;
    public static MapLogic Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new MapLogic();
            return _Instance;
        }
    }

    private MapLogic() { } // disallow instantiation

    private AllodsMap MapStructure = null;
    private MapNode[] _Nodes = null;
    
    public MapNode[] Nodes
    {
        get
        {
            return _Nodes;
        }
    }

    public int Width
    {
        get
        {
            if (MapStructure != null)
                return (int)MapStructure.Data.Width;
            return 0;
        }
    }

    public int Height
    {
        get
        {
            if (MapStructure != null)
                return (int)MapStructure.Data.Height;
            return 0;
        }
    }

    public string Title
    {
        get
        {
            if (MapStructure != null)
                return MapStructure.Data.Name;
            return "";
        }
    }

    private TerrainLighting MapLighting = null;
    private bool MapLightingNeedsUpdate = false;
    private Texture2D MapLightingTex = null;
    private Color MapLightingColor = new Color(1, 1, 1, 1);
    private bool MapLightingUpdated = false;

    public void InitFromFile(string filename)
    {
        MapStructure = AllodsMap.LoadFrom(filename);
        if (MapStructure == null)
        {
            Core.Abort("Couldn't load \"{0}\"", filename);
            return;
        }

        _Nodes = new MapNode[Width * Height];
        for (int i = 0; i < Width * Height; i++)
        {
            Nodes[i] = new MapNode();
            Nodes[i].Tile = (ushort)(MapStructure.Tiles[i] & 0x3FF);
            Nodes[i].Height = MapStructure.Heights[i];
            Nodes[i].Flags = (ushort)(MapStructure.Tiles[i] & 0xFC00);
            Nodes[i].Light = 255;
        }

        MapLighting = new TerrainLighting(Width, Height);
        CalculateLighting(180, 1, 1, 1);

        Speed = 5;
    }

    public void CalculateLighting(float SolarAngle, float r, float g, float b)
    {
        MapLighting.Calculate(MapStructure.Heights, (float)SolarAngle);
        for (int i = 0; i < Width * Height; i++)
            Nodes[i].Light = MapLighting.Result[i];
        MapLightingNeedsUpdate = true;
        GetLightingTexture(r, g, b);
    }

    public Texture2D CheckLightingTexture()
    {
        if (MapLightingUpdated)
            return MapLightingTex;
        return null;
    }

    public Texture2D GetLightingTexture(float r, float g, float b)
    {
        if (MapLightingColor.r != r || MapLightingColor.g != g || MapLightingColor.b != b)
        {
            MapLightingColor = new Color(r, g, b, 1);
            MapLightingNeedsUpdate = true;
        }

        if (MapLightingTex == null)
        {
            MapLightingTex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            MapLightingTex.filterMode = FilterMode.Bilinear;
            MapLightingNeedsUpdate = true;
        }

        if (MapLightingNeedsUpdate)
        {
            Color[] colors = new Color[256 * 256];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float lvw = (float)Nodes[y * Width + x].Light / 255;
                    colors[y * 256 + x] = new Color(r * lvw, g * lvw, b * lvw, 1);
                }
            }
            MapLightingTex.SetPixels(colors);
            MapLightingTex.Apply(false);
            MapLightingNeedsUpdate = false;
            MapLightingUpdated = true;
        }

        return MapLightingTex;
    }

    private int _LevelTime = 0;
    public int LevelTime
    {
        get
        {
            return _LevelTime;
        }
    }

    private int _Speed = 1;
    public int Speed
    {
        set
        {
            if (value < 0) value = 0;
            if (value > 8) value = 8;
            _Speed = value;
            float scale = 5 * (_Speed+1);
            Time.timeScale = scale;
        }

        get
        {
            return _Speed;
        }
    }

    int _testangle = 0;
    int _slast = 0;
    public void Update()
    {
        _slast++;
        if (_slast > 20)
        {
            _testangle = (_testangle + 15) % 360;
            CalculateLighting(_testangle, 1, 1, 1);
            _slast = 0;
        }

        _LevelTime++;
    }
}
