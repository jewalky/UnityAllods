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

//* WarBeginner *//
	private MapWizard _Wizard = null;
	public MapWizard Wizard
	{
		get
		{
			if (_Wizard == null)
				_Wizard = new MapWizard();
			return _Wizard;
		}
	}
//* end *//

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
        Groups = new List<Group>();
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
    public List<Group> Groups { get; private set; }

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
            }
        }

        for (int y = (int)vRec.yMin; y < vRec.yMax; y++)
        {
            for (int x = (int)vRec.xMin; x < vRec.xMax; x++)
            {
                foreach (MapObject mobj in Nodes[x, y].Objects)
                {
                    if (mobj is IDynlight)
                    {
                        int lval = ((IDynlight)mobj).GetLightValue();
                        if (lval > 256)
                        {
                            float lightDistF = (float)lval / 256;
                            int lightDist = Mathf.CeilToInt(lightDistF);
                            for (int ly = y-lightDist; ly <= y+lightDist; ly++)
                            {
                                if (ly < (int)vRec.yMin || ly >= (int)vRec.yMax)
                                   continue;
                                for (int lx = x-lightDist; lx <= x+lightDist; lx++)
                                {
                                    if (lx < (int)vRec.xMin || lx >= (int)vRec.xMax)
                                        continue;
                                    float dst = Mathf.Min(new Vector2(lx - x, ly - y).magnitude, lightDist);
                                    int lightExtend = (int)(256f * (lightDistF - dst) / lightDistF);
                                    if (lightExtend < 0)
                                        lightExtend = 0;
                                    Nodes[lx, ly].DynLight += lightExtend;
                                }
                            }
                        }
                        else
                        {
                            Nodes[x, y].DynLight += lval;
                        }
                    }
                }
            }
        }
        MapLightingNeedsUpdate = true;
    }

    public float GetHeightAt(int x, int y)
    {
        if (x >= 0 && x < MapLogic.Instance.Width &&
            y >= 0 && y < MapLogic.Instance.Height)
            return MapLogic.Instance.Nodes[x, y].Height;
        return 0;
    }

    public float GetHeightAt(float x, float y, int w, int h)
    {
        float height = 0;
        int count = 0;
        for (int ly = (int)y; ly < (int)y + h; ly++)
        {
            for (int lx = (int)x; lx < (int)x + w; lx++)
            {
                int baseX = lx;
                int baseY = ly;
                float fracX = x - baseX; // fractional part
                float fracY = y - baseY; // fractional part

                float h1 = GetHeightAt(baseX, baseY);
                float h2 = GetHeightAt(baseX + 1, baseY);
                float h3 = GetHeightAt(baseX, baseY + 1);
                float h4 = GetHeightAt(baseX + 1, baseY + 1);

                float l1 = h1 * (1.0f - fracX) + h2 * fracX;
                float l2 = h3 * (1.0f - fracX) + h4 * fracX;
                height += (l1 * (1.0f - fracY) + l2 * fracY);
                count++;
            }
        }
        return height/count;
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

    public const int TICRATE = 25; // this is the amount of ticks in a second with speed=4
    private int _Speed = 1;
    public int Speed
    {
        set
        {
            if (value < 0) value = 0;
            if (value > 8) value = 8;

            _Speed = value;
            float scale = 5 * (_Speed + 1);
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

        // process group AI
        for (int i = 0; i < Groups.Count; i++)
        {
            Group g = Groups[i];
            g.Update();
        }

        // process object AI
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
        Groups.Clear();
        ConsolePlayer = null;
        FileName = null;
        MapStructure = null;
        GameManager.Instance.MapView.Unload();
//* WarBeginner *//
	Wizard.Unload();
//* end *//
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

        // load groups
        if (!NetworkManager.IsClient && mapStructure.Groups != null)
        {
            foreach (AllodsMap.AlmGroup almgroup in mapStructure.Groups)
            {
                Group grp = FindNewGroup((int)almgroup.GroupID);
                grp.RepopDelay = (int)almgroup.RepopTime * MapLogic.TICRATE;
                grp.Flags = 0;
                if (almgroup.GroupFlag.HasFlag(AllodsMap.AlmGroup.AlmGroupFlags.RandomPositions))
                    grp.Flags |= GroupFlags.RandomPositions;
                if (almgroup.GroupFlag.HasFlag(AllodsMap.AlmGroup.AlmGroupFlags.QuestKill))
                    grp.Flags |= GroupFlags.QuestKill;
                if (almgroup.GroupFlag.HasFlag(AllodsMap.AlmGroup.AlmGroupFlags.QuestIntercept))
                    grp.Flags |= GroupFlags.QuestIntercept;
            }
        }

        // load units
        if (!NetworkManager.IsClient && mapStructure.Units != null)
        {
            int c = 0;
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (AllodsMap.AlmUnit almunit in mapStructure.Units)
            {
                if ((almunit.Flags & 0x10) != 0)
                {
                    MapHuman human = new MapHuman(almunit.ServerID);
                    human.X = human.TargetX = human.SpawnX = human.LastSpawnX = (int)almunit.X;
                    human.Y = human.TargetY = human.SpawnY = human.LastSpawnY = (int)almunit.Y;
                    human.Tag = almunit.ID;
                    human.Player = GetPlayerByID(almunit.Player - 1);
                    if (almunit.HealthMax >= 0)
                    {
                        human.CoreStats.HealthMax = almunit.HealthMax;
                        human.UpdateItems();
                    }
                    if (almunit.Health >= 0)
                        human.Stats.TrySetHealth(almunit.Health);
                    human.CalculateVision();
                    human.Group = FindNewGroup(almunit.Group);

                    human.LinkToWorld();
                    Objects.Add(human);
                }
                else
                {
                    MapUnit unit = new MapUnit(almunit.ServerID);
                    unit.X = unit.TargetX = unit.SpawnX = unit.LastSpawnX = (int)almunit.X;
                    unit.Y = unit.TargetY = unit.SpawnY = unit.LastSpawnY = (int)almunit.Y;
                    unit.Tag = almunit.ID;
                    unit.Player = GetPlayerByID(almunit.Player - 1);
                    if (almunit.HealthMax >= 0)
                    {
                        unit.CoreStats.HealthMax = almunit.HealthMax;
                        unit.UpdateItems();
                    }
                    if (almunit.Health >= 0)
                        unit.Stats.TrySetHealth(almunit.Health);
                    unit.CalculateVision();
                    unit.Group = FindNewGroup(almunit.Group);

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
                ConsolePlayer.Name = Config.cl_nickname.Length == 0 ? "Self" : Config.cl_nickname;
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

        if (!NetworkManager.IsClient)
        {
            // testing
            /*
            ItemPack testpack = new ItemPack();
            testpack.PutItem(0, new Item("Very Rare Crystal Ring"));
            PutSackAt(16, 16, testpack, false);
            */

            /*
            MapProjectile proj = new MapProjectile(15);
            proj.SetPosition(16, 16, 0);
            Objects.Add(proj);
            */
        }

//* WarBeginner *//
	Wizard.LoadMap(this);
//* end *//

    }

    private Group FindNewGroup(int groupId)
    {
        for (int i = 0; i < Groups.Count; i++)
        {
            Group g = Groups[i];
            if (g.ID == groupId)
                return g;
        }

        Group newGroup = new Group();
        newGroup.RepopDelay = 120 * MapLogic.TICRATE; // default
        newGroup.ID = groupId;
        Groups.Add(newGroup);
        return newGroup;
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
                        if (ly < 8 || ly >= Height-8) continue;
                        for (int lx = xOrigin; lx < xOrigin + 41; lx++)
                        {
                            if (lx < 8 || lx >= Width-8) continue;
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
            unit = new MapHuman(Config.sv_avatar, true);
        if (unit.Class == null)
            return null;
        unit.Player = player;
        unit.Tag = GetFreeUnitTag(); // this is also used as network ID.
        unit.SetPosition(16, 16, false);
        Objects.Add(unit);

        // add items for testing
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Meteoric Amulet {skillfire=100,skillwater=100,skillair=100,skillearth=100,skillastral=100,manamax=16000}")); // for testing mage
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Ring {body=3,scanrange=1,spirit=1}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Amulet {body=3,scanrange=1,spirit=1}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Dragon Leather Large Shield {body=3,protectionearth=20,damagebonus=20}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Plate Helm {body=3,scanrange=2}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Plate Cuirass {body=3}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Plate Bracers {body=3}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Scale Gauntlets {body=3}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Plate Boots {body=3}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Crystal Pike {tohit=500,damagemin=10,damagemax=20}"));
        unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Very Rare Meteoric Crossbow {damagemax=500}"));
        for (int i = 0; i < 50; i++)
            unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("SuperScroll Teleport"));
        for (int i = 0; i < 250; i++)
            unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item("Scroll Teleport"));


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

    public MapStructure GetStructureByTag(int tag)
    {
        foreach (MapObject mobj in Objects)
        {
            if (mobj.GetObjectType() != MapObjectType.Structure) continue;
            MapStructure struc = (MapStructure)mobj;
            if (struc.Tag == tag)
                return struc;
        }

        return null;
    }

    // sack related
    public MapSack GetSackByTag(int tag)
    {
        foreach (MapObject mobj in Objects)
        {
            if (mobj.GetObjectType() != MapObjectType.Sack) continue;
            MapSack sack = (MapSack)mobj;
            if (sack.Tag == tag)
                return sack;
        }

        return null;
    }

    // get sack at x,y
    public MapSack GetSackAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return null;

        foreach (MapObject mobj in Nodes[x, y].Objects)
        {
            if (mobj.GetObjectType() != MapObjectType.Sack) continue;
            MapSack sack = (MapSack)mobj;
            return sack;
        }

        return null;
    }

    public void RemoveSackAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;

        MapNode node = Nodes[x, y];
        for (int i = 0; i < node.Objects.Count; i++)
        {
            MapObject mobj = node.Objects[i];
            if (mobj.GetObjectType() != MapObjectType.Sack) continue;
            mobj.Dispose();
            Objects.Remove(mobj); // remove from the list
            i--;
        }

        if (NetworkManager.IsServer)
            Server.NotifyNoSack(x, y);
    }

    // put sack at x,y
    private static int TopSackTag = 1;
    public MapSack PutSackAt(int x, int y, ItemPack pack, bool alwayscreate)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return null;

        MapSack existingsack = null;
        if (alwayscreate) RemoveSackAt(x, y);
        else existingsack = GetSackAt(x, y);

        if (existingsack == null)
        {
            existingsack = new MapSack(pack);
            existingsack.X = x;
            existingsack.Y = y;
            existingsack.Tag = TopSackTag++;
            existingsack.LinkToWorld();
            Objects.Add(existingsack);
            existingsack.UpdateNetVisibility();
        }
        else
        {
            for (int i = 0; i < pack.Count; i++)
                existingsack.Pack.PutItem(existingsack.Pack.Count, new Item(pack[i], pack[i].Count));
            existingsack.Pack.Money += pack.Money;
            existingsack.DoUpdateView = true;

            if (NetworkManager.IsServer)
                Server.NotifySack(x, y, existingsack.Pack.Price);
        }

        return existingsack;
    }
}
