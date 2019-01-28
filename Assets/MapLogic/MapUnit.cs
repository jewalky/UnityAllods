using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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


[Flags] // This is the various effects unit can have
public enum UnitFlags
{
    None            = 0x0000,
    Invisible       = 0x0001,
    Poisoned        = 0x0002,
    ProtectionFire  = 0x0004,
    ProtectionWater = 0x0008,
    ProtectionAir   = 0x0010,
    ProtectionEarth = 0x0020,
    ProtectionAstral= 0x0040,
    Shield          = 0x0080,
    Bless           = 0x0100,
    Curse           = 0x0200
}

public class MapUnit : MapObject, IPlayerPawn, IVulnerable, IDisposable
{
    public override MapObjectType GetObjectType() { return MapObjectType.Monster; }
    protected override Type GetGameObjectType() { return typeof(MapViewUnit); }

    public UnitClass Class = null;
    private Templates.TplMonster Template = null; // 
    public UnitStats Stats;
    public UnitStats CoreStats;
    public UnitStats ItemStats;
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
            DoUpdateView = true;
        }
    }

    public bool IsAlive = true;
    public bool IsDying = false;
    public List<IUnitAction> Actions = new List<IUnitAction>();
    public List<IUnitState> States = new List<IUnitState>();
    public List<Spells.SpellProc> SpellProcessors = new List<Spells.SpellProc>();
    public List<SpellEffects.Effect> SpellEffects = new List<SpellEffects.Effect>();
    public List<SpellEffects.EffectIndicator> SpellIndicators = new List<SpellEffects.EffectIndicator>();
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
    // for damage calculation per tick
    private bool DamageLastVisible = false;
    private int DamageLast = 0;

    private UnitFlags _Flags = UnitFlags.None;
    public UnitFlags Flags
    {
        get
        {
            return _Flags;
        }

        set
        {
            if (_Flags != value)
            {
                _Flags = value;
                DoUpdateView = true;
                CheckIndicators();
                Server.NotifyUnitFlags(this);
            }
        }
    }

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
        TopSlot = 16
    }

    public ItemPack ItemsBody;
    public ItemPack ItemsPack;

    //
    public readonly bool[,] Vision = new bool[41, 41];
    public readonly ScanrangeCalc VisionCalc = new ScanrangeCalc();
    public readonly UnitInteraction Interaction = null;

    public readonly List<MapProjectile> TargetedBy = new List<MapProjectile>();

    public readonly List<Spell> SpellBook = new List<Spell>();

    public MapUnit()
    {
        Interaction = new UnitInteraction(this);
    }

    public MapUnit(int serverId) : this()
    {
        Template = TemplateLoader.GetMonsterById(serverId);
        if (Template == null)
            Debug.LogFormat("Invalid unit created (serverId={0})", serverId);
        else InitUnit();
    }

    public MapUnit(string name) : this()
    {
        Template = TemplateLoader.GetMonsterByName(name);
        if (Template == null)
            Debug.LogFormat("Invalid unit created (name={0})", name);
        else InitUnit();
    }

    protected void InitBaseUnit()
    {
        IsAlive = true;
        IsDying = false;
        Stats = new UnitStats();
        CoreStats = Stats;
        ItemStats = new UnitStats();
        Actions.Clear();
        States.Clear();
        Actions.Add(new IdleAction(this));
        States.Add(new IdleState(this));
        VState = UnitVisualState.Idle;
        DoUpdateView = true;
        ItemsBody = new ItemPack(false, this);
        ItemsPack = new ItemPack(false, this);
    }

    private void InitUnit()
    {
        InitBaseUnit();

        Class = UnitClassLoader.GetUnitClassById(Template.TypeID);
        if (Class == null)
        {
            Debug.LogFormat("Invalid unit created (class not found, serverId={0}, typeId={1})", Template.ServerID, Template.TypeID);
            Template = null;
            return;
        }

        Width = Template.TokenSize;
        Height = Width;

        CoreStats.Health = CoreStats.HealthMax = Math.Max(Template.HealthMax, 0);
        CoreStats.Mana = CoreStats.ManaMax = Math.Max(Template.ManaMax, 0); // they sometimes put -1 as mana counter for fighters

        CoreStats.HealthRegeneration = (short)Template.HealthRegeneration;
        CoreStats.ManaRegeneration = (short)Template.ManaRegeneration;

        // BRMS
        CoreStats.Body = (short)Template.Body;
        CoreStats.Reaction = (short)Template.Reaction;
        CoreStats.Mind = (short)Template.Mind;
        CoreStats.Spirit = (short)Template.Spirit;

        // physical damage and resists
        int templateMin = Template.PhysicalMin;
        int templateMax = Template.PhysicalMax - templateMin;
        if (IsIgnoringArmor && ((templateMin & 0x80) != 0))
        {
            templateMin = (templateMin & 0x7F) * 15;
            templateMax *= 15;
        }
        if (templateMax < 0)
        {
            templateMin = Template.PhysicalMax;
            templateMax = (Template.PhysicalMin - Template.PhysicalMax) * 64;
        }

        CoreStats.DamageMin = (short)templateMin;
        CoreStats.DamageMax = (short)(templateMax + templateMin);
        CoreStats.ToHit = (short)Template.ToHit;
        CoreStats.Absorbtion = (short)Template.Absorbtion;
        CoreStats.Defence = (short)Template.Defense;

        // magical resists
        CoreStats.ProtectionFire = (byte)Template.ProtectionFire;
        CoreStats.ProtectionWater = (byte)Template.ProtectionWater;
        CoreStats.ProtectionAir = (byte)Template.ProtectionAir;
        CoreStats.ProtectionEarth = (byte)Template.ProtectionEarth;
        CoreStats.ProtectionAstral = (byte)Template.ProtectionAstral;

        // physical resists (custom)
        CoreStats.ProtectionBlade = (byte)Template.ProtectionBlade;
        CoreStats.ProtectionAxe = (byte)Template.ProtectionAxe;
        CoreStats.ProtectionBludgeon = (byte)Template.ProtectionBludgeon;
        CoreStats.ProtectionPike = (byte)Template.ProtectionPike;
        CoreStats.ProtectionShooting = (byte)Template.ProtectionShooting;

        // speed and scanrange
        CoreStats.RotationSpeed = (byte)Template.RotationSpeed;
        if (CoreStats.RotationSpeed < 1)
            CoreStats.RotationSpeed = 1;
        CoreStats.Speed = (byte)Template.Speed;
        if (CoreStats.Speed < 1)
            CoreStats.Speed = 1;
        CoreStats.ScanRange = Template.ScanRange;

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

        // spellbook
        for (int i = 0; i < 32; i++)
        {
            uint sp = 1u << i;
            if (Template.ManaMax > 0 && (Template.KnownSpells & sp) != 0)
            {
                Spell cspell = new Spell(i, this);
                SpellBook.Add(cspell);
            }
        }

        CalculateVision();
        UpdateItems();
    }

    public override void Dispose()
    {
        Flags = 0; // remove all indicators
        if (_Player != null)
            _Player.Objects.Remove(this);
        base.Dispose();
    }

    // this is called when on-body items or effects are modified
    public virtual void UpdateItems()
    {
        // set stats from effects
        Stats = new UnitStats(CoreStats);
        for (int i = 0; i < SpellEffects.Count; i++)
            SpellEffects[i].ProcessStats(Stats);

        DoUpdateView = true;
        DoUpdateInfo = true;
    }

    public override void Update()
    {
        if (Class == null)
            return;

        UpdateNetVisibility();

        // process spell processors
        for (int i = 0; i < SpellProcessors.Count; i++)
        {
            if (!SpellProcessors[i].Process())
            {
                SpellProcessors.RemoveAt(i);
                i--;
            }
        }

        // process actions
        while (!Actions.Last().Process())
            Actions.RemoveAt(Actions.Count - 1);

        // process spell effects
        for (int i = 0; i < SpellEffects.Count; i++)
        {
            if (!SpellEffects[i].Process())
            {
                SpellEffects[i].OnDetach();
                SpellEffects.RemoveAt(i);
                i--;
            }
        }

        // process spell effect indicators
        for (int i = 0; i < SpellIndicators.Count; i++)
            SpellIndicators[i].Process();

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
                for (int i = 0; i < SpellEffects.Count; i++)
                    SpellEffects[i].OnDetach();
                SpellEffects.Clear();
                if (Player == MapLogic.Instance.ConsolePlayer &&
                    Player != null && Player.Avatar == this)
                {
                    //
                    MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, Locale.Patch[68]);
                    MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, Locale.Patch[69]); // your character died. press space to continue
                }
            }

            // health and mana regeneration
            // 1% per second * mana regeneration?
            if (!NetworkManager.IsClient)
            {
                if (MapLogic.Instance.LevelTime % 40 == 0)
                {
                    if (Stats.Mana < Stats.ManaMax)
                    {
                        if (Stats.TrySetMana((int)(Stats.Mana + (float)Stats.ManaMax / 100 * (float)Stats.ManaRegeneration / 100)) &&
                            NetworkManager.IsServer) Server.NotifyUnitStatsShort(this);
                        DoUpdateView = true;
                        DoUpdateInfo = true;
                    }
                    if (Stats.Health < Stats.HealthMax)
                    {
                        if (Stats.TrySetHealth((int)(Stats.Health + (float)Stats.HealthMax / 100 * Stats.HealthRegeneration / 100)) &&
                            NetworkManager.IsServer) Server.NotifyUnitStatsShort(this);
                        DoUpdateView = true;
                        DoUpdateInfo = true;
                    }
                }
            }
        }

        if (!NetworkManager.IsClient && DamageLast > 0)
        {
            if (NetworkManager.IsServer)
                Server.NotifyDamageUnit(this, DamageLast, DamageLastVisible);
            MapView.Instance.SpawnDamageNumbers(this, DamageLast, false);
            DamageLast = 0;
            DamageLastVisible = false;
        }
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        return IsFlying ? MapNodeFlags.DynamicAir : MapNodeFlags.DynamicGround;
    }

    private int AstarNodesWalked = 0;
    private int AstarLastX = -1;
    private int AstarLastY = -1;
    private List<Vector2i> AstarLastSolution;
    private ShortestPathGraphSearch<Vector2i, Vector2i> AstarSearcher = null;
    private UnitAstarHelper AstarSearcherH = null;
    public List<Vector2i> DecideNextMove(int targetX, int targetY, bool staticOnly, float distance = 1)
    {
        if (AstarNodesWalked < 16 &&
            AstarLastX == targetX && AstarLastY == targetY && AstarLastSolution != null &&
            AstarNodesWalked+1 < AstarLastSolution.Count &&
            AstarLastSolution.Count > 0 && Interaction.CheckWalkableForUnit(AstarLastSolution[AstarNodesWalked+1].x, AstarLastSolution[AstarNodesWalked+1].y, staticOnly))
        {
            if (AstarLastSolution[AstarNodesWalked].x == X &&
                AstarLastSolution[AstarNodesWalked].y == Y)
            {
                AstarNodesWalked++;
                List<Vector2i> npath = AstarLastSolution.Skip(AstarNodesWalked).ToList();
                return npath;
            }
        }

        AstarNodesWalked = 16;

        if (distance < 1)
            distance = 1;

        // if targetX,targetY is blocked, refuse to pathfind.
        if (!Interaction.CheckWalkableForUnit(targetX, targetY, staticOnly) && distance < 2)
            return null;

        // init astar searcher
        if (AstarSearcherH == null)
            AstarSearcherH = new UnitAstarHelper(this);
        if (AstarSearcher == null)
            AstarSearcher = new ShortestPathGraphSearch<Vector2i, Vector2i>(AstarSearcherH);
        AstarSearcherH.StaticLookup = staticOnly;
        AstarSearcherH.Distance = distance;

        try
        {
            List<Vector2i> nodes = AstarSearcher.GetShortestPath(new Vector2i(X, Y), new Vector2i(targetX, targetY));
            if (nodes == null)
                return null;
            nodes.Add(new Vector2i(targetX, targetY));
            AstarNodesWalked = 0;
            AstarLastX = targetX;
            AstarLastY = targetY;
            AstarLastSolution = nodes;
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
        return FaceVector(x - X, y - Y);
    }

    public int FaceCellPrecise(int x, int y)
    {
        float rx = x + 0.5f;
        float ry = y + 0.5f;
        return FaceVector(rx - (X + (float)Width / 2 + FracX), ry - (Y + (float)Height / 2 + FracY));
    }

    public void SetPosition(int x, int y, bool netupdate)
    {
        UnlinkFromWorld();
        X = x;
        Y = y;
        if (IsAlive) LinkToWorld();
        CalculateVision();
        DoUpdateView = true;

        if (netupdate)
        {
            UpdateNetVisibility();
            if (NetworkManager.IsServer)
                Server.NotifyUnitTeleport(this);
        }
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

    public void AddSpellEffects(params SpellEffects.Effect[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            SpellEffects.Effect ef = effects[i];
            if (!ef.OnAttach(this))
                continue;
            ef.SetUnit(this);
            SpellEffects.Add(ef);
        }
    }

    public List<T> GetSpellEffects<T>() where T : SpellEffects.Effect
    {
        List<T> ov = new List<T>();
        for (int i = 0; i < SpellEffects.Count; i++)
        {
            if (SpellEffects[i] is T)
                ov.Add((T)SpellEffects[i]);
        }

        return ov;
    }

    public void RemoveSpellEffect(SpellEffects.Effect ef)
    {
        if (SpellEffects.Remove(ef))
            ef.OnDetach();
    }

    public void AddSpellProcessors(params Spells.SpellProc[] processors)
    {
        for (int i = 0; i < processors.Length; i++)
            SpellProcessors.Add(processors[i]);
    }

    public void SetState(IUnitState state)
    {
        States.RemoveRange(1, States.Count - 1);
        States.Add(state);
    }

    public Item TakeItemFromBody(BodySlot slot)
    {
        Item item = ItemsBody.TakeItem(ItemsBody.FindItemBySlot(slot), 1);
        UpdateItems();
        DoUpdateInfo = true;
        return item;
    }

    public Item GetItemFromBody(BodySlot slot)
    {
        return ItemsBody.FindItemBySlot(slot);
    }

    public virtual bool IsItemUsable(Item item)
    {
        if (!item.IsValid)
            return false;
        return true;
    }

    public void PutItemToBody(BodySlot slot, Item item)
    {
        // unequip existing item on specified slot
        // if item count is too large
        if (item.Count > 1)
        {
            // put excessive items back to pack
            Item newitem = new Item(item, 1);
            item.Count--;
            ItemsPack.PutItem(ItemsPack.Count, item);
            item = newitem;
        }

        Item currentItem = GetItemFromBody(slot);
        if (currentItem != null)
            ItemsPack.PutItem(ItemsPack.Count, ItemsBody.TakeItem(currentItem, 1));

        if (item.Class.Option.TwoHanded == 2)
        {
            // unequip shield for 2-handed weapon
            Item shield = GetItemFromBody(BodySlot.Shield);
            if (shield != null)
                ItemsPack.PutItem(ItemsPack.Count, ItemsBody.TakeItem(shield, 1));
        }

        ItemsBody.PutItem(ItemsBody.Count, item);
        UpdateItems();
        DoUpdateInfo = true;
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

        bool sourceIgnoresArmor = source != null && source.IsIgnoringArmor;
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
            DoUpdateInfo = true;
            DoUpdateView = true;

            if (!NetworkManager.IsClient)
            {
                DamageLast += damagecount;
                if ((flags & DamageFlags.Astral) == 0)
                    DamageLastVisible = true;
            }
            return damagecount;
        }

        return 0;
    }

    // template-related stuff
    public virtual int Charge { get { return Template.Charge; } }
    public virtual int Relax { get { return Template.Relax; } }

    public virtual bool IsIgnoringArmor { get { return Template.IsIgnoringArmor; } }

    public virtual bool IsFlying { get { return Template.IsFlying; } }
    public virtual bool IsHovering { get { return Template.IsHovering; } }
    public virtual bool IsWalking { get { return Template.IsWalking; } }

    public virtual int ServerID { get { return Template.ServerID; } }
    public virtual int TypeID { get { return Class.ID; } }
    public virtual int Face { get { return Template.Face; } }

    public virtual string TemplateName { get { return Template.Name; } }

    // 
    public Spell GetSpell(Spell.Spells spell)
    {
        foreach (Spell cspell in SpellBook)
        {
            if (cspell.SpellID == spell)
                return cspell;
        }

        return null;
    }

    //
    public override int GetVisibilityInFOW()
    {
        // don't draw, don't allow to target invisible units
        if ((Flags & UnitFlags.Invisible) != 0)
        {
            if (MapLogic.Instance.ConsolePlayer != null && (Player.Diplomacy[MapLogic.Instance.ConsolePlayer.ID] & DiplomacyFlags.Vision) == 0)
                return 0;
        }

        return base.GetVisibilityInFOW();
    }

    //
    private void CheckIndicators()
    {
        // get all indicators for current flag set
        List<Type> currentindicators = new List<Type>();
        foreach (SpellEffects.EffectIndicator indicator in SpellIndicators)
            currentindicators.Add(indicator.GetType());

        List<Type> indicators = global::SpellEffects.EffectIndicator.FindIndicatorFromFlags(Flags);
        foreach (Type itype in indicators)
        {
            // add all indicators that don't exist yet
            if (!currentindicators.Contains(itype))
            {
                ConstructorInfo ci = itype.GetConstructor(new Type[] { typeof(MapUnit) });
                SpellEffects.EffectIndicator indicator = (SpellEffects.EffectIndicator)ci.Invoke(new object[] { this });
                indicator.OnEnable();
                SpellIndicators.Add(indicator);
            }
        }

        // remove all indicators that shouldn't exist
        for (int i = 0; i < SpellIndicators.Count; i++)
        {
            SpellEffects.EffectIndicator indicator = SpellIndicators[i];
            if (!indicators.Contains(indicator.GetType()))
            {
                indicator.OnDisable();
                SpellIndicators.RemoveAt(i);
                i--;
            }
        }
    }
}