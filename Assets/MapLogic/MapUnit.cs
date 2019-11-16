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
    Dying,
    Bone
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
    Curse           = 0x0200,
    Healing         = 0x0400,
    Draining        = 0x0800
}

public class MapUnitAggro
{
    public MapUnit Target;
    public int LastDamage;
    public int LastSeen;
    public int CountDamage;
    public float Damage;
    public float Factor;

    public MapUnitAggro(MapUnit target)
    {
        Target = target;
        LastDamage = LastSeen = MapLogic.Instance.LevelTime;
        CountDamage = 0;
        Damage = 0;
        Factor = 1f;
    }

    public float GetAggro()
    {
        if (CountDamage == 0)
            return 0;
        float baseAggro = Damage / CountDamage;
        // rougly 10 seconds for 0.5 aggro factor here. defined by /5 at the end of timeFac
        int curTime = MapLogic.Instance.LevelTime;
        float timeFac = Mathf.Min(1f / ((curTime - LastDamage) / MapLogic.TICRATE / 5), 1f);
        float fac = Mathf.Max(Factor, 1f); // increasing factor. not used right now, later may be used for tank mechanics
        return baseAggro * fac * timeFac;
    }
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
            if (_Player != null)
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
    public int BoneFrame = 0;
    public int BoneTime = 0;
    // for visual state stuff
    public float FracX = 0;
    public float FracY = 0;
    // for damage calculation per tick
    private bool DamageLastVisible = false;
    private int DamageLast = 0;
    // for summoned units
    public int SummonTime = 0;
    public int SummonTimeMax = 0;
    // for respawn
    public int SpawnX = -1;
    public int SpawnY = -1;
    public int LastSpawnX = -1;
    public int LastSpawnY = -1;
    // for AI
    public int TargetX = -1;
    public int TargetY = -1;

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

    // AI stuff
    public MapUnit Target { get; set; }
    public readonly List<MapUnitAggro> Aggro = new List<MapUnitAggro>();
    private Group _Group = null;
    public Group Group
    {
        get
        {
            return _Group;
        }

        set
        {
            if (_Group != null)
            {
                if (_Group.Units.Contains(this))
                    _Group.Units.Remove(this);
            }
            _Group = value;
            if (_Group != null)
            {
                if (!_Group.Units.Contains(this))
                    _Group.Units.Add(this);
            }
        }
    }

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

        OnUpdateItems();
    }

    public override void Dispose()
    {
        Flags = 0; // remove all indicators
        if (_Player != null)
            _Player.Objects.Remove(this);
        base.Dispose();
    }

    // this is called when on-body items or effects are modified
    protected virtual void OnUpdateItems()
    {
        if (NetworkManager.IsClient)
            return;

        // 
        float origHealth = (float)Stats.Health / Stats.HealthMax;
        float origMana = (float)Stats.Mana / Stats.ManaMax;

        // set stats from effects
        Stats = new UnitStats(CoreStats);
        for (int i = 0; i < SpellEffects.Count; i++)
            SpellEffects[i].ProcessStats(Stats);

        Stats.Health = (int)(origHealth * Stats.HealthMax);
        Stats.Mana = (int)(origMana * Stats.ManaMax);

        CalculateVision();

        DoUpdateView = true;
        DoUpdateInfo = true;
    }

    public void UpdateItems()
    {
        UnitStats oldStats = Stats;
        OnUpdateItems();

        if (NetworkManager.IsServer)
        {
            UnitStats.ModifiedFlags statsChanged = oldStats.CompareStats(Stats);
            if (statsChanged != 0)
                Server.NotifyUnitPackedStats(this, statsChanged);
        }
    }

    public bool CanDetectUnit(MapUnit other)
    {
        if (!other.IsLinked)
            return false;
        if (other.Flags.HasFlag(UnitFlags.Invisible) && !other.Player.Diplomacy[Player.ID].HasFlag(DiplomacyFlags.Vision))
            return false;
        return true;
    }

    // 0=cant see, 1=can see but not in vision, 2=can fully see
    public int CanSeeUnit(MapUnit other)
    {
        if (!CanDetectUnit(other))
            return 0;
        // check x/y coords
        int offsX = other.X - X;
        int offsY = other.Y - Y;
        if (offsX < -20 || offsX > 20 || offsY < -20 || offsY > 20)
            return 1;
        offsX += 20;
        offsY += 20;
        if (Vision[offsX, offsY])
            return 2;
        return 1;
    }

    public void UpdateAggro()
    {
        // first off, slowly tick aggro factors down
        // and remove dead/unlinked units
        for (int i = 0; i < Aggro.Count; i++)
        {
            MapUnitAggro ag = Aggro[i];
            if (ag.Target == null || !ag.Target.IsAlive || !ag.Target.IsLinked)
            {
                // remove item
                Aggro.RemoveAt(i);
                i--;
                continue;
            }
            if (ag.Factor > 1f)
            {
                ag.Factor = Mathf.Max(1f, ag.Factor - 0.5f / MapLogic.TICRATE); // factor falls by 0.5 per second, cannot be under 1
            }
            int sight = CanSeeUnit(ag.Target);
            if (sight == 0)
            {
                // cannot target invisible units...
                Aggro.RemoveAt(i);
                i--;
                continue;
            }
            if (sight == 2)
            {
                // unit is in sight area
                ag.LastSeen = MapLogic.Instance.LevelTime;
            }
            // check if last seen a lot of time ago ( > 5 seconds for now)
            if (MapLogic.Instance.LevelTime - ag.LastSeen > MapLogic.TICRATE * 2)
            {
                Aggro.RemoveAt(i);
                i--;
                continue;
            }
        }
        // pick group target if we don't have our own, as lowest priority
        if (Aggro.Count <= 0 && Group != null)
        {
            if (Group.SharedTarget != null && Group.SharedTarget.IsAlive && Group.SharedTarget.IsLinked)
            {
                MapUnitAggro newAg = new MapUnitAggro(Group.SharedTarget);
                Aggro.Add(newAg);
            }
        }
        Aggro.Sort((MapUnitAggro a1, MapUnitAggro a2) =>
        {
            float f1 = a1.GetAggro();
            float f2 = a2.GetAggro();
            if (f1 > f2) return -1;
            if (f1 < f2) return 1;
            return 0;
        });
        // check current target
        Target = null;
        if (Aggro.Count > 0)
            Target = Aggro[0].Target;
    }

    public void UpdateAI()
    {
        if (!IsAlive || IsDying || !IsLinked)
            return;

        // if there is a target, chase and attack it
        if (Target != null)
        {
            // check if we are currently doing attack state
            if (!(States[States.Count-1] is AttackState))
                SetState(new AttackState(this, Target));
        }
        else
        {
            // check if we are at our respawn point
            if (X != LastSpawnX || Y != LastSpawnY)
            {
                if (!(States[States.Count - 1] is MoveState))
                    SetState(new MoveState(this, LastSpawnX, LastSpawnY));
            }
        }

        // rotate randomly
        if ((UnityEngine.Random.Range(0, 256) < 1) &&
            Actions.Count == 1) // unit is idle and 1/256 chance returns true
        {
            int angle = UnityEngine.Random.Range(0, 36) * 10;
            AddActions(new RotateAction(this, angle));
        }
    }

    public override void Update()
    {
        if (Class == null)
            return;

        // check summon timer
        if (SummonTimeMax > 0 && (MapLogic.Instance.LevelTime % MapLogic.TICRATE == 0))
        {
            SummonTime++;
            if (NetworkManager.IsServer)
                Server.NotifyUnitSummonTime(this);
            DoUpdateView = true;
            // todo update summon time to client
            if (SummonTime > SummonTimeMax)
            {
                Dispose();
                return;
            }
        }

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

        if (!NetworkManager.IsClient && Player.DoFullAI)
        {
            // process aggro list, pick new target if needed
            UpdateAggro();
            UpdateAI();
        }

        // process actions
        while (!Actions.Last().Process())
            Actions.RemoveAt(Actions.Count - 1);

        // process spell effects
        for (int i = 0; i < SpellEffects.Count; i++)
        {
            if (!SpellEffects[i].Process())
            {
                global::SpellEffects.Effect ef = SpellEffects[i];
                SpellEffects.RemoveAt(i);
                ef.OnDetach();
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
            BoneTime = 0;
            BoneFrame = 0;

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
            if (!NetworkManager.IsClient && !IsDying && IsAlive)
            {
                if (MapLogic.Instance.LevelTime % 40 == 0)
                {
                    if (Stats.Mana < Stats.ManaMax)
                    {
                        if (Stats.TrySetMana((int)(Stats.Mana + Mathf.Max(1, (float)Stats.ManaMax / 20 * Stats.ManaRegeneration / 100))) &&
                            NetworkManager.IsServer) Server.NotifyUnitStatsShort(this);
                        DoUpdateView = true;
                        DoUpdateInfo = true;
                    }
                    if (Stats.Health < Stats.HealthMax)
                    {
                        if (Stats.TrySetHealth((int)(Stats.Health + Mathf.Max(1, (float)Stats.HealthMax / 20 * Stats.HealthRegeneration / 100))) &&
                            NetworkManager.IsServer) Server.NotifyUnitStatsShort(this);
                        DoUpdateView = true;
                        DoUpdateInfo = true;
                    }
                }
            }
        }
        else if (!NetworkManager.IsClient)
        {
            if (!IsAlive && Actions.Count == 1)
            {
                // check if we have bones at all
                UnitClass dCls = Class;
                while (dCls.Dying != null && dCls.Dying != dCls)
                    dCls = dCls.Dying;
                bool haveBones = dCls.BonePhases >= 3;

                if (haveBones && BoneFrame < 4)
                {
                    BoneTime++;
                    if (BoneTime > MapLogic.TICRATE * 10)
                    {
                        BoneFrame++;
                        BoneTime = 0;
                        // notify clients of bone phase change
                        if (NetworkManager.IsServer)
                            Server.NotifyUnitBoneFrame(this);
                        DoUpdateView = true;
                    }
                }
                else if (BoneFrame != 4)
                {
                    BoneFrame = 4;
                    BoneTime = 0;
                    if (NetworkManager.IsServer)
                        Server.NotifyUnitBoneFrame(this);
                    DoUpdateView = true;
                }
            }
            else
            {
                BoneTime = 0;
                BoneFrame = 0;
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

    public override void CheckAllocateObject()
    {
        if (GetVisibility() == 2)
            AllocateObject();
    }

    public override int GetVisibility()
    {
        if (BoneFrame > 3)
            return 0;
        return base.GetVisibility();
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        return IsFlying ? MapNodeFlags.DynamicAir : MapNodeFlags.DynamicGround;
    }

    private int LastMoveTargetX = -1;
    private int LastMoveTargetY = -1;
    private int LastMoveTargetWidth = -1;
    private int LastMoveTargetHeight = -1;
    private float LastMoveDistance = 0;
    private IEnumerator<Vector2i> LastPath = null;
    private IEnumerator<Vector2i> Pathfind(int left, int top, int right, int bottom, float distance)
    {
        AstarPathfinder p = new AstarPathfinder();
        // for now generate astar path once
        bool logPathfind = false;

        if (logPathfind) Debug.LogFormat("Pathfind({2}): Started at {0}, {1}", X, Y, ID);
        bool doRestart;
        do
        {
            doRestart = false;
            // main check: static only
            List<Vector2i> nodes = p.FindPath(this, X, Y, left, top, right, bottom, distance, true);
            if (nodes == null)
            {
                while (true)
                    yield return null;
            }

            // top loop: move along static list
            for (int i = 1; i < nodes.Count; i++)
            {
                Vector2i node = nodes[i];

                if (logPathfind) Debug.LogFormat("Pathfind({2}): Continued at {0}, {1}", node.x, node.y, ID);

                // check if static node is not walkable
                if (!Interaction.CheckWalkableForUnit(node.x, node.y, false) || Interaction.CheckDangerous(node.x, node.y))
                {
                    i--;
                    node = nodes[i];
                    // end try 1
                    // try 2: find limited astar to next unblocked static cell
                    // find LAST unblocked node that is contained inside 16x16 radius.
                    // do same pathfinding as above then
                    int lastFreeNode = -1;
                    int lastNode = i + 1;
                    while (lastNode < nodes.Count && Math.Abs(nodes[lastNode].x - X) < 16 && Math.Abs(nodes[lastNode].y - Y) < 16)
                    {
                        if (Interaction.CheckWalkableForUnit(nodes[lastNode].x, nodes[lastNode].y, false))
                            lastFreeNode = lastNode;
                        lastNode++;
                    }

                    if (logPathfind) Debug.LogFormat("Pathfind({3}): Trying to Find Node after {0}, {1} = {2}", node.x, node.y, lastFreeNode, ID);

                    if (lastFreeNode < 0)
                        lastFreeNode = nodes.Count - 1; // try final node

                    if (logPathfind) Debug.LogFormat("Pathfind({4}): Choose Dynamic to Static at {0}, {1} (to {2}, {3})", node.x, node.y, nodes[lastFreeNode].x, nodes[lastFreeNode].y, ID);

                    // find path...
                    // if not found, then we are blocked. try finding again until unblocked
                    List<Vector2i> altNodes = (lastFreeNode == nodes.Count-1) ?
                        p.FindPath(this, X, Y, left, top, right, bottom, 0, false, 16)
                        :
                        p.FindPath(this, X, Y, nodes[lastFreeNode].x, nodes[lastFreeNode].y, nodes[lastFreeNode].x, nodes[lastFreeNode].y, 0, false, 16);
                    bool pathfindSuccess = false;
                    bool altDoRestart;
                    do
                    {
                        altDoRestart = false;

                        if (altNodes == null)
                        {
                            // find next node
                            int nextFreeNode = -1;
                            lastNode = lastFreeNode + 1;
                            while (lastNode < nodes.Count && Math.Abs(nodes[lastNode].x - X) < 16 && Math.Abs(nodes[lastNode].y - Y) < 16)
                            {
                                if (Interaction.CheckWalkableForUnit(nodes[lastNode].x, nodes[lastNode].y, false))
                                    nextFreeNode = lastNode;
                                lastNode++;
                            }

                            if (lastFreeNode < 0)
                                lastFreeNode = nodes.Count - 1;

                            altNodes = (lastFreeNode == nodes.Count-1) ?
                                p.FindPath(this, X, Y, left, top, right, bottom, 0, false, 16)
                                :
                                p.FindPath(this, X, Y, nodes[lastFreeNode].x, nodes[lastFreeNode].y, nodes[lastFreeNode].x, nodes[lastFreeNode].y, 0, false, 16);

                            if (altNodes == null && nextFreeNode >= 0)
                            {
                                // try with updated free node
                                altNodes = (nextFreeNode == nodes.Count-1) ?
                                    p.FindPath(this, X, Y, left, top, right, bottom, 0, false, 16)
                                    :
                                    p.FindPath(this, X, Y, nodes[nextFreeNode].x, nodes[nextFreeNode].y, nodes[nextFreeNode].x, nodes[nextFreeNode].y, 0, false, 16);
                                if (altNodes != null)
                                {
                                    // if found path with nextFreeNode, but not with lastFreeNode, continue path this way...
                                    lastFreeNode = nextFreeNode;
                                }
                            }

                            if (altNodes == null)
                            {
                                yield return null; // pause for a bit to avoid infinite recursion

                                if (X != node.x || Y != node.y) // if we lost dynamic path at some random point
                                {
                                    doRestart = true;
                                    break;
                                }

                                // otherwise restart
                                altDoRestart = true;
                            }
                        }

                        if (altNodes != null)
                        {
                            for (int j = 1; j < altNodes.Count; j++)
                            {
                                Vector2i altNode = altNodes[j];

                                if (logPathfind) Debug.LogFormat("Pathfind({2}): Dynamic to Static at {0}, {1}", altNode.x, altNode.y, ID);

                                // path is not walkable, update dynamic
                                if (!Interaction.CheckWalkableForUnit(altNode.x, altNode.y, true))
                                {
                                    if (logPathfind) Debug.LogFormat("Pathfind({2}): Dynamic to Static Obsolete at {0}, {1}", altNode.x, altNode.y, ID);
                                    doRestart = true;
                                    break;
                                }

                                yield return altNode;

                                // Next node mismatch, unit teleported or otherwise broke
                                if (X != altNode.x || Y != altNode.y)
                                {
                                    if (logPathfind) Debug.LogFormat("Pathfind({4}): Dynamic to Static Restarted ({0}!={1} or {2}!={3})", X, altNode.x, Y, altNode.y, ID);
                                    doRestart = true;
                                    break;
                                }
                            }
                        }

                        if (!altDoRestart && !doRestart)
                        {
                            // we walked up to lastFreeNode
                            i = lastFreeNode;
                            if (logPathfind) Debug.LogFormat("Pathfind({2}): Drop Back To Static at {0}, {1}", nodes[i].x, nodes[i].y, ID);
                            pathfindSuccess = true;
                        }

                        altNodes = null;
                                
                    }
                    while (altDoRestart && !doRestart);

                    if (pathfindSuccess)
                    {
                        // just do next 'i' node directly
                        continue;
                    }
                }

                // if something broke really seriously
                if (doRestart)
                    break;

                yield return node; // use this node as next

                // Next node mismatch, unit teleported or otherwise broke
                if (X != node.x || Y != node.y)
                {
                    if (logPathfind) Debug.LogFormat("Pathfind({4}): Restarted ({0}!={1} or {2}!={3})", X, node.x, Y, node.y, ID);
                    doRestart = true;
                    break;
                }
            }
        }
        while (doRestart);

        // return null path until changed
        while (true)
            yield return null;
    }

    /* WarBeginner */
    public Vector2i DecideNextMove(int targetX, int targetY, int targetWidth, int targetHeight, float distance = 0)
    {
        if (distance < 0)
            distance = 0;

        int left = targetX;
        int top = targetY;
        int right = targetX;
        int bottom = targetY;

        if (targetWidth > 0 || targetHeight > 0)
        {
            left -= 1;
            top -= 1;
            right += targetWidth;
            bottom += targetHeight;
        }

        // start or restart pathfinding.
        // or continue, if same path is being looked for
        if (!(LastPath != null &&
                targetX == LastMoveTargetX && targetY == LastMoveTargetY &&
                targetWidth == LastMoveTargetWidth && targetHeight == LastMoveTargetHeight &&
                distance == LastMoveDistance))
        {
            // generate new pathfinder enumerator
            LastPath = Pathfind(left, top, right, bottom, distance);
            LastMoveTargetX = targetX;
            LastMoveTargetY = targetY;
            LastMoveTargetWidth = targetWidth;
            LastMoveTargetHeight = targetHeight;
            LastMoveDistance = distance;
        }

        try
        {
            LastPath.MoveNext();
            Vector2i node = LastPath.Current;
            return node;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
    }
    //* end *//


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
        TargetX = X = x;
        TargetY = Y = y;
        if (IsAlive) LinkToWorld();
        CalculateVision();
        DoUpdateView = true;

        if (netupdate)
        {
            UpdateNetVisibility();
            if (NetworkManager.IsServer)
                Server.NotifyUnitTeleport(this);
        }

        if (SpawnX < 0 || SpawnY < 0)
        {
            SpawnX = LastSpawnX = x;
            SpawnY = LastSpawnY = y;
        }
    }

    // finds random position for the unit
    public Vector2i FindRandomPosition(int x, int y, int radius)
    {
        if (radius < 0) radius = 0;
        if (radius == 0)
        {
            if (!Interaction.CheckWalkableForUnit(x, y, false))
                return null;
        }

        int minX = Mathf.Max(x - radius - (Width - 1), 0);
        int maxX = Mathf.Min(x + radius, MapLogic.Instance.Width - 1);
        int minY = Mathf.Max(y - radius - (Height - 1), 0);
        int maxY = Mathf.Min(y + radius, MapLogic.Instance.Height - 1);

        List<Vector2i> coords = new List<Vector2i>();

        for (int tryY = minY; tryY <= maxY; tryY++)
        {
            for (int tryX = minX; tryX <= maxX; tryX++)
            {
                if (Interaction.CheckWalkableForUnit(tryX, tryY, false))
                    coords.Add(new Vector2i(tryX, tryY));
            }
        }

        // if no valid coords found, use center
        if (coords.Count == 0)
        {
            if (!Interaction.CheckWalkableForUnit(x, y, false))
                return null;
            return new Vector2i(x, y);
        }

        // 
        Vector2i coord = coords[UnityEngine.Random.Range(0, coords.Count)];
        return coord;
    }

    // sets random position for the unit. returns true if valid space was found
    public bool RandomizePosition(int x, int y, int radius, bool netupdate)
    {
        Vector2i coord = FindRandomPosition(x, y, radius);
        if (coord == null)
            return false;
        SetPosition(coord.x, coord.y, netupdate);
        return true;
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
        // hack: adding death action removes all other actions abruptly
        bool isDeath = false;

        int hadActions = Actions.Count;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] is DeathAction)
                isDeath = true;
            Actions.Add(states[i]);
        }
        
        if (isDeath)
        {
            Actions.RemoveRange(1, hadActions - 1);
        }

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
        DoUpdateView = true;
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
        {
            int putAtOffset = ItemsPack.Count;
            if (item.Parent == ItemsPack)
                putAtOffset = Math.Min(item.Index, putAtOffset);
            ItemsPack.PutItem(putAtOffset, ItemsBody.TakeItem(currentItem, 1));
        }

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

    public void ValidatePosition(bool netupdate)
    {
        if (!Interaction.CheckWalkableForUnit(X, Y, false))
        {
            for (int radius = 1; radius < 6; radius++)
            {
                for (int lx = -radius; lx <= radius; lx++)
                {
                    for (int ly = -radius; ly <= radius; ly++)
                    {
                        if ((lx == -radius || lx == radius) && (ly == -radius || ly == radius))
                        {
                            if (Interaction.CheckWalkableForUnit(X + lx, Y + ly, false))
                            {
                                SetPosition(X + lx, Y + ly, netupdate);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }

    public void Respawn(int x, int y)
    {
        // if cannot stand directly at these coordinates, try to find better ones
        X = x;
        Y = y;
        ValidatePosition(false);
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
        MapUnitAggro ag = null;
        // validate diplomacy: alliance will not attack even if attacked
        if (!NetworkManager.IsClient && !Player.Diplomacy[source.Player.ID].HasFlag(DiplomacyFlags.Ally))
        {
            if (Player.DoFullAI)
            {
                for (int i = 0; i < Aggro.Count; i++)
                {
                    if (Aggro[i].Target == source)
                    {
                        ag = Aggro[i];
                        break;
                    }
                }

                if (ag == null)
                {
                    ag = new MapUnitAggro(source);
                    Aggro.Add(ag);
                }
            }

            // additionally, if this player is neutral, it will switch to Enemy if attacked
            // and the attacking player will start being enemy as well
            if (!Player.Diplomacy[source.Player.ID].HasFlag(DiplomacyFlags.Enemy))
                Player.Diplomacy[source.Player.ID] |= DiplomacyFlags.Enemy;
            if (!source.Player.Diplomacy[Player.ID].HasFlag(DiplomacyFlags.Ally))
                source.Player.Diplomacy[Player.ID] |= DiplomacyFlags.Enemy;
        }

        if (ag != null)
            ag.LastDamage = MapLogic.Instance.LevelTime;

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

        // apply damage to aggro
        if (ag != null)
        {
            ag.CountDamage++;
            ag.Damage += damagecount;
        }

        if (Stats.TrySetHealth(Stats.Health - damagecount))
        {
            DoUpdateInfo = true;
            DoUpdateView = true;

            if (!NetworkManager.IsClient)
            {
                DamageLast += damagecount;
                if ((flags & DamageFlags.Astral) == 0)
                    DamageLastVisible = true;
                // give experience to damager. does not work like in ROM2
                if ((flags & DamageFlags.AllowExp) != 0 && Template != null && source != null && source is MapHuman)
                {
                    int expFactor = (int)((float)damagecount / Stats.HealthMax * Template.Experience * (1f+source.Stats.Mind/100f));
                    if (Stats.Health <= 0)
                        expFactor *= 2;
                    MapHuman srcHuman = (MapHuman)source;
                    if ((flags & DamageFlags.Fire) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Fire, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Fire) + expFactor, true);
                    if ((flags & DamageFlags.Water) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Water, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Water) + expFactor, true);
                    if ((flags & DamageFlags.Air) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Air, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Air) + expFactor, true);
                    if ((flags & DamageFlags.Earth) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Earth, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Earth) + expFactor, true);
                    if ((flags & DamageFlags.Astral) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Astral, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Astral) + expFactor, true);
                    if ((flags & DamageFlags.Blade) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Blade, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Blade) + expFactor, true);
                    if ((flags & DamageFlags.Axe) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Axe, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Axe) + expFactor, true);
                    if ((flags & DamageFlags.Bludgeon) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Bludgeon, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Bludgeon) + expFactor, true);
                    if ((flags & DamageFlags.Pike) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Pike, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Pike) + expFactor, true);
                    if ((flags & DamageFlags.Shooting) != 0)
                        srcHuman.SetSkillExperience(MapHuman.ExperienceSkill.Shooting, srcHuman.GetSkillExperience(MapHuman.ExperienceSkill.Shooting) + expFactor, true);
                }
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
    public Spell GetSpell(Spell.Spells spell, ushort itemId = 0)
    {
        if (itemId == 0)
        {
            foreach (Spell cspell in SpellBook)
            {
                if (cspell.SpellID == spell)
                    return cspell;
            }
        }

        // check scrolls, return highest skill
        // iterate items to find scrolls
        // actually, let's just allow any special (one-time) item that has castSpell
        Spell topSpell = null;
        foreach (Item item in ItemsPack)
        {
            if (!item.Class.IsScroll) // special/scrolls
                continue;
            if (itemId != 0 && item.Class.ItemID != itemId)
                continue;
            Spell sp = item.GetScrollEffect(this, spell);
            if (sp != null && (topSpell == null || topSpell.Skill < sp.Skill))
                topSpell = sp;
        }

        return topSpell;
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

    public virtual DamageFlags GetDamageType()
    {
        return DamageFlags.Raw;
    }
}