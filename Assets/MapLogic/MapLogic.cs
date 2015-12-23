using System.Collections.Generic;
using UnityEngine;

public enum MapNodeFlags
{
    BlockedGround = 0x0001,
    BlockedAir = 0x0002,
    Discovered = 0x0004, // the cell was visible
    Visible = 0x0008, // the cell is now visible (always Visible+Discovered means open cell, only Discovered means fog of war, neither means cell is black)
    Unblocked = 0x0010 // walkable water / rocks
}

public class MapNode
{
    public short Height = 0;
    public byte Light = 255; //?
    public ushort Tile = 0;
    public MapNodeFlags Flags = 0;
    public List<MapLogicObject> Objects = new List<MapLogicObject>();
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
    private MapNode[,] _Nodes = null;
    private List<MapLogicObject> _Objects = new List<MapLogicObject>();
    private int _TopObjectID = 0;
    
    public bool IsLoaded
    {
        get
        {
            return (MapStructure != null);
        }
    }

    public MapNode[,] Nodes
    {
        get
        {
            return _Nodes;
        }
    }

    public List<MapLogicObject> Objects
    {
        get
        {
            return _Objects;
        }
    }

    public int TopObjectID
    {
        get
        {
            return _TopObjectID++;
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

    public void CalculateLighting(float SolarAngle)
    {
        MapLighting.Calculate(MapStructure.Heights, (float)SolarAngle);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Nodes[x, y].Light = MapLighting.Result[y * Width + x];
        MapLightingNeedsUpdate = true;
        GetLightingTexture();
    }

    public Texture2D CheckLightingTexture()
    {
        Texture2D tex = GetLightingTexture();
        if (MapLightingUpdated)
        {
            MapLightingUpdated = false;
            return tex;
        }
        return null;
    }

    public Texture2D GetLightingTexture()
    {
        if (MapLightingTex == null)
        {
            MapLightingTex = new Texture2D(256, 256, TextureFormat.Alpha8, false);
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
                    float lvw = (float)Nodes[x, y].Light / 255;
                    colors[y * 256 + x] = new Color(1, 1, 1, lvw);
                }
            }
            MapLightingTex.SetPixels(colors);
            MapLightingTex.Apply(false);
            MapLightingNeedsUpdate = false;
            MapLightingUpdated = true;
        }

        return MapLightingTex;
    }

    private bool MapFOWNeedsUpdate = false;
    private Texture2D MapFOWTex = null;
    private bool MapFOWUpdated = false;

    public Texture2D CheckFOWTexture()
    {
        Texture2D tex = GetFOWTexture();
        if (MapFOWUpdated)
        {
            MapFOWUpdated = false;
            return tex;
        }
        return null;
    }

    public Texture2D GetFOWTexture()
    {
        if (MapFOWTex == null)
        {
            MapFOWTex = new Texture2D(256, 256, TextureFormat.Alpha8, false);
            MapFOWTex.filterMode = FilterMode.Bilinear;
            MapFOWNeedsUpdate = true;
        }

        if (MapFOWNeedsUpdate)
        {
            Color[] colors = new Color[256 * 256];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    MapNodeFlags flags = Nodes[x, y].Flags;
                    float alpha = 1;
                    if ((flags & MapNodeFlags.Discovered) != 0) alpha -= 0.5f;
                    if ((flags & MapNodeFlags.Visible) != 0) alpha -= 0.5f;
                    colors[y * 256 + x] = new Color(1, 1, 1, alpha);
                }
            }
            MapFOWTex.SetPixels(colors);
            MapFOWTex.Apply(false);
            MapFOWNeedsUpdate = false;
            MapFOWUpdated = true;
        }

        return MapFOWTex;
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

    public void Update()
    {
        _LevelTime++;
        foreach (MapLogicObject mo in _Objects)
            mo.Update();
    }

    private void InitGeneric()
    {
        ObstacleClassLoader.InitClasses();
        MapLightingNeedsUpdate = true;
        MapFOWNeedsUpdate = true;
    }

    public void InitFromFile(string filename)
    {
        InitGeneric();

        MapStructure = AllodsMap.LoadFrom(filename);
        if (MapStructure == null)
        {
            Core.Abort("Couldn't load \"{0}\"", filename);
            return;
        }

        _Nodes = new MapNode[Width, Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _Nodes[x, y] = new MapNode();
                _Nodes[x, y].Tile = (ushort)(MapStructure.Tiles[y * Width + x] & 0x3FF);
                _Nodes[x, y].Height = MapStructure.Heights[y * Width + x];
                //Nodes[i].Flags = (ushort)(MapStructure.Tiles[y * Width + x] & 0xFC00);
                //Nodes[i].Flags = MapNodeFlags.Discovered;
                _Nodes[x, y].Flags = 0;
                _Nodes[x, y].Light = 255;
            }
        }

        MapLighting = new TerrainLighting(Width, Height);
        CalculateLighting(180);

        Speed = 5;
        _TopObjectID = 0;

        /*
        MapLogicObject mo = new MapLogicObstacle(0);
        mo.X = 16;
        mo.Y = 16;
        mo.Width = 1;
        mo.Height = 1;
        mo.LinkToWorld();*/
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int typeId = MapStructure.Objects[y * Width + x];
                if (typeId <= 0) continue;
                typeId -= 1;
                MapLogicObstacle mob = new MapLogicObstacle(typeId);
                mob.X = x;
                mob.Y = y;
                mob.Width = 1;
                mob.Height = 1;
                mob.LinkToWorld();
            }
        }
    }

    public void SetTestingVisibility(int x, int y, float range)
    {
        int ri = (int)range;
        // first, delete existing visibility
        for (int ly = 0; ly < Height; ly++)
        {
            for (int lx = 0; lx < Width; lx++)
            {
                Nodes[lx, ly].Flags &= ~MapNodeFlags.Visible;
            }
        }

        for (int ly = y - ri; ly <= y + ri; ly++)
        {
            for (int lx = x - ri; lx <= x + ri; lx++)
            {
                if (lx < 0 || lx >= Width ||
                    ly < 0 || ly >= Height) continue;

                Nodes[lx, ly].Flags |= MapNodeFlags.Visible|MapNodeFlags.Discovered;
            }
        }

        MapFOWNeedsUpdate = true;
    }
}
