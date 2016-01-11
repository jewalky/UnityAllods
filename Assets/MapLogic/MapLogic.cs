using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

[Flags]
public enum MapNodeFlags
{
    BlockedGround = 0x0001,
    BlockedAir = 0x0002,
    Discovered = 0x0004, // the cell was visible
    Visible = 0x0008, // the cell is now visible (always Visible+Discovered means open cell, only Discovered means fog of war, neither means cell is black)
    Unblocked = 0x0010, // walkable water / rocks
    DynamicGround = 0x0020, // temporarily blocked ground (unit)
    DynamicAir = 0x0040, // temporarily blocked air (unit)
}

public class MapNode
{
    public short Height = 0;
    public byte Light = 255;
    public int DynLight = 0;
    public ushort Tile = 0;
    public MapNodeFlags Flags = 0;
    public List<MapObject> Objects = new List<MapObject>();
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

    private MapLogic()
    {
        Objects = new List<MapObject>();
        Players = new List<Player>();
    }

    private AllodsMap MapStructure = null;
    public string FileName { get; private set; }
    public string FileMD5 { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public MapNode[,] Nodes { get; private set; }
    public List<MapObject> Objects { get; private set; }
    private int _TopObjectID = 0;
    public List<Player> Players { get; private set; }
    public const int MaxPlayers = 64;
    public Player ConsolePlayer { get; set; } // the player that we're directly controlling.

    public bool IsLoaded
    {
        get
        {
            return (MapStructure != null);
        }
    }

    public int TopObjectID
    {
        get
        {
            return _TopObjectID++;
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
    public bool MapLightingNeedsUpdate { get; private set; }
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
    }

    public void CalculateDynLighting()
    {
        Rect vRec = MapView.Instance.VisibleRect;
        for (int y = (int)vRec.yMin; y < vRec.yMax; y++)
        {
            for (int x = (int)vRec.xMin; x < vRec.xMax; x++)
            {
                Nodes[x, y].DynLight = 0;
                foreach(MapObject mobj in Nodes[x, y].Objects)
                {
                    if (mobj is IDynlight)
                        Nodes[x, y].DynLight += ((IDynlight)mobj).GetLightValue();
                }
            }
        }
        MapLightingNeedsUpdate = true;
    }

    public Texture2D CheckLightingTexture()
    {
        if (MapLightingUpdated || MapLightingTex == null)
            return GetLightingTexture();
        return null;
    }

    public Texture2D GetLightingTexture()
    {
        if (GameManager.Instance.IsHeadless)
            return null;

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
                    float lvw = (float)(Math.Min(255, Nodes[x, y].Light + Nodes[x, y].DynLight)) / 255; // combine dynlights here. Light is base terrain light.
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

    public bool MapFOWNeedsUpdate { get; private set; }
    private Texture2D MapFOWTex = null;
    private bool MapFOWUpdated = false;

    public Texture2D CheckFOWTexture()
    {
        if (MapFOWUpdated || MapFOWTex == null)
            return GetFOWTexture();
        return null;
    }

    public Texture2D GetFOWTexture()
    {
        if (GameManager.Instance.IsHeadless)
            return null;

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

            if (NetworkManager.IsServer)
                Server.NotifySpeedChanged(value);
        }

        get
        {
            return _Speed;
        }
    }

    public void Update()
    {
        _LevelTime++;

        for (int i = 0; i < Objects.Count; i++)
        {
            MapObject mobj = Objects[i];
            mobj.Update();
        }

        // update local scanrange every few tics (every 5?)
        if (ConsolePlayer != null && (_LevelTime % 5 == 0))
            UpdateVisibility();
    }

    public void Unload()
    {
        foreach (MapObject mo in Objects)
            mo.DisposeNoUnlink();
        Objects.Clear();
        Players.Clear();
        ConsolePlayer = null;
        FileName = null;
        MapStructure = null;
        GameManager.Instance.MapView.Unload();
    }

    private void InitGeneric()
    {
        MapLightingNeedsUpdate = true;
        MapFOWNeedsUpdate = true;
    }

    public void InitFromFile(string filename)
    {
        Unload();
        InitGeneric();

        AllodsMap mapStructure = AllodsMap.LoadFrom(filename);
        if (mapStructure == null)
        {
            //Core.Abort("Couldn't load \"{0}\"", filename);
            GameConsole.Instance.WriteLine("Couldn't load \"{0}\"", filename);
            Unload();
            return;
        }

        Width = (int)mapStructure.Data.Width;
        Height = (int)mapStructure.Data.Height;

        Nodes = new MapNode[Width, Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Nodes[x, y] = new MapNode();
                Nodes[x, y].Tile = (ushort)(mapStructure.Tiles[y * Width + x] & 0x3FF);
                Nodes[x, y].Height = mapStructure.Heights[y * Width + x];
                Nodes[x, y].Flags = 0;
                Nodes[x, y].Light = 255;
            }
        }

        // load players
        foreach (AllodsMap.AlmPlayer almplayer in mapStructure.Players)
        {
            Player player = new Player(almplayer);
            Players.Add(player);
            //Debug.Log(string.Format("player ID={2} {0} (flags {1})", player.Name, player.Flags, player.ID));
        }

        GameManager.Instance.CallDelegateOnNextFrame(() =>
        {
            Speed = 5;
            return false;
        });

        _TopObjectID = 0;

        // load obstacles
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int typeId = mapStructure.Objects[y * Width + x];
                if (typeId <= 0) continue;
                typeId -= 1;
                MapObstacle mob = new MapObstacle(typeId);
                mob.X = x;
                mob.Y = y;
                mob.LinkToWorld();
                Objects.Add(mob);
            }
        }

        // load structures
        if (mapStructure.Structures != null)
        {
            foreach (AllodsMap.AlmStructure almstruc in mapStructure.Structures)
            {
                MapStructure struc;
                struc = new MapStructure(almstruc.TypeID);
                struc.X = (int)almstruc.X;
                struc.Y = (int)almstruc.Y;
                struc.Health = almstruc.Health;
                struc.Tag = almstruc.ID;
                struc.Player = GetPlayerByID(almstruc.Player - 1);
                if (almstruc.IsBridge)
                {
                    struc.Width = almstruc.Width;
                    struc.Height = almstruc.Height;
                    // also this crutch is apparently done by ROM2
                    if (struc.Width < 2) struc.Width = 2;
                    if (struc.Height < 2) struc.Height = 2;
                    struc.IsBridge = true;
                }

                struc.LinkToWorld();
                Objects.Add(struc);
            }
        }

        // load units
        if (!NetworkManager.IsClient && mapStructure.Units != null)
        {
            foreach (AllodsMap.AlmUnit almunit in mapStructure.Units)
            {
                if ((almunit.Flags & 0x10) != 0)
                {
                    MapHuman human = new MapHuman(almunit.ServerID);
                    human.X = (int)almunit.X;
                    human.Y = (int)almunit.Y;
                    human.Tag = almunit.ID;
                    human.Player = GetPlayerByID(almunit.Player - 1);
                    if (almunit.HealthMax >= 0)
                        human.Stats.HealthMax = almunit.HealthMax;
                    if (almunit.Health >= 0)
                        human.Stats.TrySetHealth(almunit.Health);

                    human.LinkToWorld();
                    Objects.Add(human);
                }
                else
                {
                    MapUnit unit = new MapUnit(almunit.ServerID);
                    unit.X = (int)almunit.X;
                    unit.Y = (int)almunit.Y;
                    unit.Tag = almunit.ID;
                    unit.Player = GetPlayerByID(almunit.Player - 1);
                    if (almunit.HealthMax >= 0)
                        unit.Stats.HealthMax = almunit.HealthMax;
                    if (almunit.Health >= 0)
                        unit.Stats.TrySetHealth(almunit.Health);

                    unit.LinkToWorld();
                    Objects.Add(unit);
                }
            }
        }

        // only if loaded
        MapStructure = mapStructure;
        FileName = filename;
        FileMD5 = ResourceManager.CalcMD5(FileName);
        MapLighting = new TerrainLighting(Width, Height);
        CalculateLighting(180);

        // postprocessing
        // if we are playing in singleplayer, then console player is Self.
        if (!NetworkManager.IsClient && !NetworkManager.IsServer) 
        {
            Player Self = GetPlayerByName("Self");
            if (Self == null) GameConsole.Instance.WriteLine("Error: couldn't set ConsolePlayer: Self not found!");
            else ConsolePlayer = Self;
            if (ConsolePlayer != null)
            {
                ConsolePlayer.Flags = 0;
                ConsolePlayer.Diplomacy[ConsolePlayer.ID] = DiplomacyFlags.Ally | DiplomacyFlags.Vision;
                GameManager.Instance.CallDelegateOnNextFrame(() =>
                {
                    ConsolePlayer.Avatar = CreateAvatar(ConsolePlayer);
                    // center view on avatar.
                    MapView.Instance.CenterOnObject(ConsolePlayer.Avatar);
                    return false;
                });
            }
        }
    }

    public int GetFreePlayerID(bool ai)
    {
        int startingFrom = 0;
        if (!ai) startingFrom = 16;
        // tos 
        for (; startingFrom < MaxPlayers; startingFrom++)
        {
            bool used = false;
            foreach (Player player in Players)
            {
                if (player.ID == startingFrom)
                {
                    used = true;
                    break;
                }
            }

            if (!used) return startingFrom;
        }

        return -1;
    }

    public Player GetPlayerByID(int id)
    {
        foreach (Player player in Players)
        {
            if (player.ID == id)
                return player;
        }

        return null;
    }

    public Player GetPlayerByName(string name)
    {
        name = name.ToLower();
        foreach (Player player in Players)
        {
            if (player.Name.ToLower() == name)
                return player;
        }

        return null;
    }

    public void UpdateVisibility()
    {
        if (ConsolePlayer == null)
            return;

        for (int ly = 0; ly < Height; ly++)
            for (int lx = 0; lx < Width; lx++)
                Nodes[lx, ly].Flags &= ~MapNodeFlags.Visible;

        foreach (Player player in Players)
        {
            if ((player.Diplomacy[ConsolePlayer.ID] & DiplomacyFlags.Vision) != 0)
            {
                for (int i = 0; i < player.Objects.Count; i++)
                {
                    MapObject mobj = player.Objects[i];
                    // this mobj should add to the vision.
                    if (mobj.GetObjectType() != MapObjectType.Monster &&
                        mobj.GetObjectType() != MapObjectType.Human) continue;
                    MapUnit unit = (MapUnit)mobj;
                    if (!unit.IsAlive)
                        continue;
                    int xOrigin = unit.X - 20;
                    int yOrigin = unit.Y - 20;
                    for (int ly = yOrigin; ly < yOrigin + 41; ly++)
                    {
                        if (ly < 8 || ly > Height) continue;
                        for (int lx = xOrigin; lx < xOrigin + 41; lx++)
                        {
                            if (lx < 8 || lx > Width) continue;
                            if (unit.Vision[lx - xOrigin, ly - yOrigin])
                                Nodes[lx, ly].Flags |= MapNodeFlags.Visible | MapNodeFlags.Discovered;
                        }
                    }
                }
            }
        }

        MapFOWNeedsUpdate = true;
    }

    // this is still used on the server.
    public void SetTestingVisibility(int x, int y, float range)
    {
        int ri = (int)range;
        // first, delete existing visibility
        for (int ly = 0; ly < Height; ly++)
            for (int lx = 0; lx < Width; lx++)
                Nodes[lx, ly].Flags &= ~MapNodeFlags.Visible;

        for (int ly = y - ri; ly <= y + ri; ly++)
        {
            for (int lx = x - ri; lx <= x + ri; lx++)
            {
                if (lx < 0 || lx >= Width ||
                    ly < 0 || ly >= Height) continue;

                Nodes[lx, ly].Flags |= MapNodeFlags.Visible | MapNodeFlags.Discovered;
            }
        }

        MapFOWNeedsUpdate = true;
    }

    public Player GetNetPlayer(ServerClient client)
    {
        return Players.FirstOrDefault(player => player.NetClient == client);
    }


    public void AddNetPlayer(Player p, bool silent)
    {
        Players.Add(p);

        if (NetworkManager.IsServer)
            Server.NotifyPlayerJoined(p);
        else if (NetworkManager.IsClient)
        {
            if (!silent)
            {
                // player ... has joined the game
                MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, string.Format("{0} {1} {2}", Locale.Main[204], p.Name, Locale.Main[205]));
            }
        }
    }

    public void DelNetPlayer(Player p, bool silent, bool kicked) // this will remove player and all related objects
    {
        if ((p.Flags & PlayerFlags.NetClient) == 0)
            return; // can't remove non-net players with this function
        if (NetworkManager.IsServer)
            Server.NotifyPlayerLeft(p, kicked);
        else if (NetworkManager.IsClient)
        {
            if (kicked && !silent)
            {
                // player ... was kicked from the game
                MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, string.Format("{0} {1} {2}", Locale.Main[78], p.Name, Locale.Main[79]));
            }
        }

        for (int i = 0; i < Objects.Count; i++)
        {
            MapObject mobj = Objects[i];
            if (mobj is IPlayerPawn && ((IPlayerPawn)mobj).GetPlayer() == p)
            {
                mobj.Dispose(); // delete object and associated GameObject, and also call UnlinkFromWorld
                Objects.Remove(mobj); // remove from the list
                i--;
            }

            // also if this object is NOT an object of this player, we might need to recalc net visibility.
            mobj.SetVisibleForNetPlayer(p, false);
        }

        Players.Remove(p); // remove the player himself.
    }

    public int GetNetPlayerCount()
    {
        int count = 0;
        foreach (Player player in Players)
        {
            if ((player.Flags & PlayerFlags.NetClient) != 0)
                count++;
        }

        return count;
    }

    // create main unit for player.
    public MapUnit CreateAvatar(Player player)
    {
        MapUnit unit = new MapUnit(Config.sv_avatar);
        if (unit.Class == null)
            unit = new MapHuman(Config.sv_avatar);
        if (unit.Class == null)
            return null;
        unit.Player = player;
        unit.Tag = GetFreeUnitTag(); // this is also used as network ID.
        unit.SetPosition(16, 16);
        Objects.Add(unit);
        return unit;
    }

    public int GetFreeUnitTag()
    {
        int topTag = 0;
        foreach (MapObject mobj in Objects)
        {
            if (mobj.GetObjectType() != MapObjectType.Monster &&
                mobj.GetObjectType() != MapObjectType.Human) continue;
            MapUnit unit = (MapUnit)mobj;
            if (unit.Tag > topTag) topTag = unit.Tag;
        }

        return topTag + 1;
    }

    public MapUnit GetUnitByTag(int tag)
    {
        foreach (MapObject mobj in Objects)
        {
            if (mobj.GetObjectType() != MapObjectType.Monster &&
                mobj.GetObjectType() != MapObjectType.Human) continue;
            MapUnit unit = (MapUnit)mobj;
            if (unit.Tag == tag)
                return unit;
        }

        return null;
        }
}
