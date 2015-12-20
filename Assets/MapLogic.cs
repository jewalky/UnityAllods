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

        Speed = 5;
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
    }
}
