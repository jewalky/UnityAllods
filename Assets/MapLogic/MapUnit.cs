using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IUnitAction
{
    bool Process();
}

public interface IUnitState
{
    bool Process();
}

public enum UnitVisualState
{
    Idle,
    Rotating,
    Moving,
    Attacking,
    Dying
}

public class MapUnit : MapObject, IPlayerPawn, IVulnerable, IDisposable
{
    public override MapObjectType GetObjectType() { return MapObjectType.Monster; }
    protected override Type GetGameObjectType() { return typeof(MapViewUnit); }

    public UnitClass Class = null;
    public Templates.TplMonster Template = null; // 
    public UnitStats Stats;
    private Player _Player;

    public Player Player
    {
        get
        {
            return _Player;
        }

        set
        {
            if (_Player != null)
                _Player.Objects.Remove(this);
            _Player = value;
            _Player.Objects.Add(this);
        }
    }

    public Player GetPlayer() { return _Player; }
    public int Tag = 0;
    private int _Angle = 0;
    public int Angle
    {
        get
        {
            return _Angle;
        }

        set
        {
            _Angle = value;
            while (_Angle < 0)
                _Angle += 360;
            while (_Angle >= 360)
                _Angle -= 360;
        }
    }

    public bool IsAlive = false;
    public bool IsDying = false;
    public List<IUnitAction> Actions = new List<IUnitAction>();
    public List<IUnitState> States = new List<IUnitState>();
    public UnitVisualState VState = UnitVisualState.Idle;
    public bool AllowIdle = false;
    public int IdleFrame = 0;
    public int IdleTime = 0;
    public int MoveFrame = 0;
    public int MoveTime = 0;
    public int AttackFrame = 0;
    public int AttackTime = 0;
    public int DeathFrame = 0;
    public int DeathTime = 0;
    // for visual state stuff
    public float FracX = 0;
    public float FracY = 0;

    public enum BodySlot
    {
        Special = 0,
        Weapon = 1,
        Shield = 2,
        // slot 3 unused
        Ring = 4,
        Amulet = 5,
        Hat = 6,
        MailRobe = 7,
        CuirassCloak = 8,
        Bracers = 9,
        Gloves = 10,
        // slot 11 unused
        Boots = 12,
        // slots 13, 14 and 15 are unused
    }

    public Item[] ItemsBody = new Item[15];
    public ItemPack ItemsPack = new ItemPack();

    //
    public readonly bool[,] Vision = new bool[41, 41];
    public readonly ScanrangeCalc VisionCalc = new ScanrangeCalc();
    public readonly UnitInteraction Interaction = null;

    public MapUnit(int serverId)
    {
        Interaction = new UnitInteraction(this);
        Template = TemplateLoader.GetMonsterById(serverId);
        if (Template == null)
            Debug.LogFormat("Invalid unit created (serverId={0})", serverId);
        else InitUnit();
    }

    public MapUnit(string name)
    {
        Interaction = new UnitInteraction(this);
        Template = TemplateLoader.GetMonsterByName(name);
        if (Template == null)
            Debug.LogFormat("Invalid unit created (name={0})", name);
        else InitUnit();
    }

    private void InitUnit()
    {
        Class = UnitClassLoader.GetUnitClassById(Template.TypeID);
        if (Class == null)
        {
            Debug.LogFormat("Invalid unit created (class not found, serverId={0}, typeId={1})", Template.ServerID, Template.TypeID);
            Template = null;
            return;
        }

        IsAlive = true;
        IsDying = false;

        Stats = new UnitStats();
        Width = Template.TokenSize;
        Height = Template.TokenSize;

        Stats.Health = Stats.HealthMax = Math.Max(Template.HealthMax, 0);
        Stats.Mana = Stats.ManaMax = Math.Max(Template.ManaMax, 0); // they sometimes put -1 as mana counter for fighters

        // BRMS
        Stats.Body = (short)Template.Body;
        Stats.Reaction = (short)Template.Reaction;
        Stats.Mind = (short)Template.Mind;
        Stats.Spirit = (short)Template.Spirit;

        // physical damage and resists
        int templateMin = Template.PhysicalMin;
        int templateMax = Template.PhysicalMax - templateMin;
        if (Template.IsIgnoringArmor && ((templateMin & 0x80) != 0))
        {
            templateMin = (templateMin & 0x7F) * 15;
            templateMax *= 15;
        }
        if (templateMax < 0)
        {
            templateMin = Template.PhysicalMax;
            templateMax = (Template.PhysicalMin - Template.PhysicalMax) * 64;
        }
        Stats.DamageMin = (short)templateMin;
        Stats.DamageMax = (short)(templateMax + templateMin);
        Stats.ToHit = (short)Template.ToHit;
        Stats.Absorbtion = (short)Template.Absorbtion;
        Stats.Defence = (short)Template.Defense;

        // magical resists
        Stats.ProtectionFire = (byte)Template.ProtectionFire;
        Stats.ProtectionWater = (byte)Template.ProtectionWater;
        Stats.ProtectionAir = (byte)Template.ProtectionAir;
        Stats.ProtectionEarth = (byte)Template.ProtectionEarth;
        Stats.ProtectionAstral = (byte)Template.ProtectionAstral;

        // physical resists (custom)
        Stats.ProtectionBlade = (byte)Template.ProtectionBlade;
        Stats.ProtectionAxe = (byte)Template.ProtectionAxe;
        Stats.ProtectionBludgeon = (byte)Template.ProtectionBludgeon;
        Stats.ProtectionPike = (byte)Template.ProtectionPike;
        Stats.ProtectionShooting = (byte)Template.ProtectionShooting;

        // speed and scanrange
        Stats.RotationSpeed = (byte)Template.RotationSpeed;
        if (Stats.RotationSpeed < 1)
            Stats.RotationSpeed = 1;
        Stats.Speed = (byte)Template.Speed;
        if (Stats.Speed < 1)
            Stats.Speed = 1;
        Stats.ScanRange = Template.ScanRange;

        // initial items
        if (Template.EquipItem1.Length > 0)
        {
            Item item1 = new Item(Template.EquipItem1);
            if (item1.IsValid)
                PutItemToBody((BodySlot)item1.Class.Option.Slot, item1);
        }

        if (Template.EquipItem2.Length > 0)
        {
            Item item2 = new Item(Template.EquipItem2);
            if (item2.IsValid)
                PutItemToBody((BodySlot)item2.Class.Option.Slot, item2);
        }

        Actions.Clear();
        States.Clear();

        Actions.Add(new IdleAction(this));
        States.Add(new IdleState(this));

        DoUpdateView = true;
        VState = UnitVisualState.Idle;
        CalculateVision();
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_Player != null)
            _Player.Objects.Remove(this);
    }

    public override void Update()
    {
        if (Class == null)
            return;

        UpdateNetVisibility();

        while (!Actions.Last().Process())
            Actions.RemoveAt(Actions.Count - 1);

        // check DEATH
        if (Stats.Health <= 0 && IsAlive && !IsDying)
        {
            if (!NetworkManager.IsClient)
                AddActions(new DeathAction(this));
            IsDying = true;
        }
        else if (Stats.Health > 0 && (IsDying || !IsAlive))
        {
            if (!IsAlive)
                LinkToWorld();
            IsDying = false;
            IsAlive = true;
        }

        if (!NetworkManager.IsClient && IsAlive && IsDying)
        {
            if (MapLogic.Instance.LevelTime % 40 == 0)
            {
                if (Stats.TrySetHealth(Stats.Health - 1))
                {
                    Server.NotifyDamageUnit(this, 1, false);
                    DoUpdateView = true;
                    DoUpdateInfo = true;
                }
            }
        }

        if (IsAlive)
        {
            if (Stats.Health <= -10)
            {
                IsAlive = false;
                IsDying = false;
                DoUpdateView = true;
                UnlinkFromWorld();
                if (Player == MapLogic.Instance.ConsolePlayer &&
                    Player != null && Player.Avatar == this)
                {
                    //
                    MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, Locale.Patch[68]);
                    MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, Locale.Patch[69]); // your character died. press space to continue
                }
            }
        }
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        if (Template == null)
            return 0;
        return (Template.IsFlying) ? MapNodeFlags.DynamicAir : MapNodeFlags.DynamicGround;
    }

    private ShortestPathGraphSearch<Vector2i, Vector2i> AstarSearcher = null;
    private UnitAstarHelper AstarSearcherH = null;
    public List<Vector2i> DecideNextMove(int targetX, int targetY, bool staticOnly)
    {
        // if targetX,targetY is blocked, refuse to pathfind.
        if (!Interaction.CheckWalkableForUnit(targetX, targetY, staticOnly))
            return null;

        // init astar searcher
        if (AstarSearcherH == null)
            AstarSearcherH = new UnitAstarHelper(this);
        if (AstarSearcher == null)
            AstarSearcher = new ShortestPathGraphSearch<Vector2i, Vector2i>(AstarSearcherH);
        AstarSearcherH.StaticLookup = staticOnly;

        try
        {
            List<Vector2i> nodes = AstarSearcher.GetShortestPath(new Vector2i(X, Y), new Vector2i(targetX, targetY));
            if (nodes == null)
                return null;
            nodes.Add(new Vector2i(targetX, targetY));
            return nodes;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public int FaceCell(int x, int y)
    {
        // from current x/y
        float deltaY = y - Y;
        float deltaX = x - X;
        int sang = (int)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI) - 90;
        while (sang > 360)
            sang -= 360;
        while (sang < 0)
            sang += 360;
        return sang;
    }

    public void SetPosition(int x, int y)
    {
        UnlinkFromWorld();
        X = x;
        Y = y;
        if (IsAlive) LinkToWorld();
        CalculateVision();
        DoUpdateView = true;
    }

    public void CalculateVision()
    {
        // we have vision from multiple points (based on size)
        for (int ly = 0; ly < 41; ly++)
            for (int lx = 0; lx < 41; lx++)
                Vision[lx, ly] = false;
        for (int ly = 0; ly < Height; ly++)
        {
            for (int lx = 0; lx < Width; lx++)
            {
                VisionCalc.CalculateVision(X+lx, Y+ly, Stats.ScanRange);
                for (int lly = 0; lly < 41; lly++)
                {
                    for (int llx = 0; llx < 41; llx++)
                    {
                        if (VisionCalc.pTablesVision[llx, lly] > 0 &&
                            lx + llx < 41 && ly + lly < 41)
                                Vision[lx + llx, ly + lly] = true;
                    }
                }
            }
        }
    }

    public void AddActions(params IUnitAction[] states)
    {
        for (int i = 0; i < states.Length; i++)
            Actions.Add(states[i]);
        if (NetworkManager.IsServer)
            Server.NotifyAddUnitActions(this, states);
    }

    public void SetState(IUnitState state)
    {
        States.RemoveRange(1, States.Count - 1);
        States.Add(state);
    }

    public Item GetItemFromBody(BodySlot slot)
    {
        return ItemsBody[(int)slot];
    }

    public void PutItemToBody(BodySlot slot, Item item)
    {
        if (ItemsBody[(int)slot] != null)
            ItemsPack.PutItem(ItemsPack.Count, ItemsBody[(int)slot]); // put current item to pack
        ItemsBody[(int)slot] = item;
    }

    public void Respawn(int x, int y)
    {
        X = x;
        Y = y;
        Stats.Health = Stats.HealthMax;
        IsAlive = true;
        IsDying = false;
        VState = UnitVisualState.Idle;
        LinkToWorld();
        if (NetworkManager.IsServer)
            Server.NotifyRespawn(this);
        DoUpdateView = true;
    }

    public int TakeDamage(DamageFlags flags, MapUnit source, int damagecount)
    {
        if (damagecount <= 0)
            return 0;

        bool sourceIgnoresArmor = source != null && source.Template.IsIgnoringArmor;
        if ((flags & DamageFlags.PhysicalDamage) != 0 && !sourceIgnoresArmor)
        {
            int ownChance = (int)(1.25f * Stats.Defence);
            int hisChance = (source != null) ? 5+source.Stats.ToHit : ownChance;

            if (ownChance > hisChance)
            {
                if (UnityEngine.Random.Range(0, ownChance) > hisChance)
                    return 0;
            }

            damagecount -= Stats.Absorbtion;
            if (damagecount <= 0)
                return 0;
        }

        // magic resists
        if ((flags & DamageFlags.Fire) != 0)
            damagecount -= damagecount * Stats.ProtectionFire / 100;
        if ((flags & DamageFlags.Water) != 0)
            damagecount -= damagecount * Stats.ProtectionWater / 100;
        if ((flags & DamageFlags.Air) != 0)
            damagecount -= damagecount * Stats.ProtectionAir / 100;
        if ((flags & DamageFlags.Earth) != 0)
            damagecount -= damagecount * Stats.ProtectionEarth / 100;
        if ((flags & DamageFlags.Astral) != 0)
            damagecount -= damagecount * Stats.ProtectionAstral / 100;

        // physical resists in monsters
        if ((flags & DamageFlags.Blade) != 0)
            damagecount -= damagecount * Stats.ProtectionBlade / 100;
        if ((flags & DamageFlags.Axe) != 0)
            damagecount -= damagecount * Stats.ProtectionAxe / 100;
        if ((flags & DamageFlags.Bludgeon) != 0)
            damagecount -= damagecount * Stats.ProtectionBludgeon / 100;
        if ((flags & DamageFlags.Pike) != 0)
            damagecount -= damagecount * Stats.ProtectionPike / 100;
        if ((flags & DamageFlags.Shooting) != 0)
            damagecount -= damagecount * Stats.ProtectionShooting / 100;

        if (Stats.TrySetHealth(Stats.Health - damagecount))
        {
            if (NetworkManager.IsServer)
                Server.NotifyDamageUnit(this, damagecount, (flags & DamageFlags.Astral) == 0);
            return damagecount;
        }

        return 0;
    }
}