using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public delegate void MapProjectileCallback(MapProjectile projectile);
public interface IMapProjectileLogic
{
    void SetProjectile(MapProjectile proj);
    bool Update();
}

// MapProjectileLogicHoming = a projectile that is homing on it's target
// MapProjectileLogicDirectional = a projectile that is just flying to specified coordinates

public class MapProjectileLogicHoming : IMapProjectileLogic
{
    MapProjectile Projectile;
    MapUnit Target;
    float Speed;

    public MapProjectileLogicHoming(MapUnit target, float speed)
    {
        Target = target;
        Speed = speed;
    }

    public void SetProjectile(MapProjectile proj)
    {
        if (Projectile != null && Target != null)
            Target.TargetedBy.Remove(Projectile);
        Projectile = proj;
        if (Target != null)
            Target.TargetedBy.Add(proj);
    }

    public virtual bool Update()
    {
        // calculate target direction!
        // check if target is gone
        if (!Target.IsAlive || !MapLogic.Instance.Objects.Contains(Target))
            Target = null;

        if (Target != null)
        {
            // special magic for lightning
            Projectile.LightLevel = 256;
            Vector2 targetCenter = new Vector2(Target.X + (float)Target.Width / 2 + Target.FracX, Target.Y + (float)Target.Height / 2 + Target.FracY);
            Projectile.Angle = MapObject.FaceVector(targetCenter.x - Projectile.ProjectileX, targetCenter.y - Projectile.ProjectileY);
            Vector2 dir = new Vector2(targetCenter.x - Projectile.ProjectileX, targetCenter.y - Projectile.ProjectileY);
            dir.Normalize();
            //dir /= 5;
            dir *= Speed / 20;
            Vector2 newPos = new Vector2(Projectile.ProjectileX + dir.x, Projectile.ProjectileY + dir.y);
            if (Projectile.ProjectileX != targetCenter.x ||
                Projectile.ProjectileY != targetCenter.y)
            {
                if (Math.Sign(targetCenter.x - newPos.x) != Math.Sign(targetCenter.x - Projectile.ProjectileX))
                    newPos.x = targetCenter.x;
                if (Math.Sign(targetCenter.y - newPos.y) != Math.Sign(targetCenter.y - Projectile.ProjectileY))
                    newPos.y = targetCenter.y;
                Projectile.SetPosition(newPos.x, newPos.y, Projectile.ProjectileZ);
                return true;
            }
            else
            {
                // flight done! call the callback and delete the projectile.
                return false;
            }
        }

        return false;
    }
}

public class MapProjectileLogicDirectional : IMapProjectileLogic
{
    MapProjectile Projectile;
    float TargetX;
    float TargetY;
    float TargetZ;
    float Speed;

    public MapProjectileLogicDirectional(float x, float y, float z, float speed)
    {
        TargetX = x;
        TargetY = y;
        TargetZ = z;
        Speed = speed;
    }

    public void SetProjectile(MapProjectile proj)
    {
        Projectile = proj;
    }

    public bool Update()
    {
        Projectile.LightLevel = 256;
        Vector3 targetCenter = new Vector3(TargetX, TargetY, TargetZ);
        Projectile.Angle = MapObject.FaceVector(targetCenter.x - Projectile.ProjectileX, targetCenter.y - Projectile.ProjectileY);
        Vector3 dir = new Vector3(targetCenter.x - Projectile.ProjectileX, targetCenter.y - Projectile.ProjectileY, targetCenter.z - Projectile.ProjectileZ);
        dir.Normalize();
        dir *= Speed / 20;
        Vector3 newPos = new Vector3(Projectile.ProjectileX + dir.x, Projectile.ProjectileY + dir.y, Projectile.ProjectileZ + dir.z);
        if (Projectile.ProjectileX != targetCenter.x ||
            Projectile.ProjectileY != targetCenter.y ||
            Projectile.ProjectileZ != targetCenter.z)
        {
            bool done = false;
            if ((new Vector3(Projectile.ProjectileX, Projectile.ProjectileY, Projectile.ProjectileZ) - targetCenter).magnitude <= 0.01f ||
                Math.Sign(targetCenter.x - newPos.x) != Math.Sign(targetCenter.x - Projectile.ProjectileX) ||
                Math.Sign(targetCenter.y - newPos.y) != Math.Sign(targetCenter.y - Projectile.ProjectileY) ||
                Math.Sign(targetCenter.z - newPos.z) != Math.Sign(targetCenter.z - Projectile.ProjectileZ))
            {
                newPos.x = targetCenter.x;
                newPos.y = targetCenter.y;
                newPos.z = targetCenter.z;
                done = true;
            }

            Projectile.SetPosition(newPos.x, newPos.y, newPos.z);
            return !done;
        }
        else
        {
            // flight done! call the callback and delete the projectile.
            return false;
        }
    }
}

// MapProjectileLogicSimple = a projectile that is used for sfx, not as actual projectile. this plays an animation.
public class MapProjectileLogicSimple : IMapProjectileLogic
{
    MapProjectile Projectile = null;
    float AnimationSpeed;
    int Timer = 0;

    public MapProjectileLogicSimple(float animspeed = 0.5f)
    {
        AnimationSpeed = animspeed;
    }

    public void SetProjectile(MapProjectile proj)
    {
        Projectile = proj;
    }

    public bool Update()
    {
        if (AnimationSpeed == 0)
            return true;

        int frame = (int)(Timer * AnimationSpeed);
        if (frame < 0) frame = 0; // shouldn't happen though
        if (frame >= Projectile.Class.Phases)
            return false;
        Projectile.CurrentFrame = frame;
        Projectile.CurrentTics = 0;
        Projectile.DoUpdateView = true;

        // this particular part is a hack
        if (Projectile.Class.ID == (int)AllodsProjectile.Explosion)
        {
            // radians aren't 0..1 which is not exactly easy to work with, but well
            // todo: google, rewrite properly
            float frad = Mathf.Max(0, (Mathf.Sin((float)frame * (Mathf.PI * 2) / Projectile.Class.Phases) + 0.5f));
            Projectile.LightLevel = (int)(frad * 512);
        }

        Timer++;
        return true;
    }
}

// MapProjectileLogicLightning = same as homing but special
public class MapProjectileLogicLightning : IMapProjectileLogic
{
    MapProjectile Projectile;
    MapUnit Target;
    List<MapProjectile> SubProjectiles;

    int TargetAngle;
    Vector2 TargetCenter;
    Vector2 TargetDir;
    float TargetDst;

    float AnimTime;

    int Color;

    private static Color[] Colors =
    new Color[] {
        new Color(1, 1, 1, 1),
        new Color(1, 0, 0, 1),
        new Color(1, 0.5f, 0, 1), // lol 7 colors
        new Color(1, 1, 0, 1),
        new Color(0, 1, 0, 1),
        new Color(0, 1, 1, 1),
        new Color(0.5f, 0, 1, 1), // lol 7 colors (x2)
        new Color(1, 0, 1, 1),
    };

    public MapProjectileLogicLightning(MapUnit target, int color)
    {
        Target = target;
        SubProjectiles = new List<MapProjectile>();
        AnimTime = 0;
        if (Color < 0) color = 0;
        if (Color > 7) color = 7;
        Color = color;
    }

    public void SetProjectile(MapProjectile proj)
    {
        if (Projectile != null && Target != null)
            Target.TargetedBy.Remove(Projectile);
        Projectile = proj;
        if (Target != null)
            Target.TargetedBy.Add(proj);
    }

    private float Sin10(float v)
    {
        return Mathf.Sin(2 * Mathf.PI * v);
    }

    public virtual bool Update()
    {
        // calculate target direction!
        // check if target is gone
        if (Target != null && (!Target.IsAlive || !MapLogic.Instance.Objects.Contains(Target)))
            Target = null;

        Projectile.Alpha = 0;

        const float density = 0.15f;

        if (Target != null && SubProjectiles.Count <= 0)
        {
            // special magic for lightning
            Projectile.LightLevel = 256;
            TargetCenter = new Vector2(Target.X + (float)Target.Width / 2 + Target.FracX, Target.Y + (float)Target.Height / 2 + Target.FracY);
            TargetAngle = Projectile.Angle = MapObject.FaceVector(TargetCenter.x - Projectile.ProjectileX, TargetCenter.y - Projectile.ProjectileY);
            TargetDir = new Vector2(TargetCenter.x - Projectile.ProjectileX, TargetCenter.y - Projectile.ProjectileY);
            TargetDir.Normalize();
            // spawn projectiles along the way
            TargetDst = new Vector2(TargetCenter.x - Projectile.ProjectileX, TargetCenter.y - Projectile.ProjectileY).magnitude;
            for (float i = 0; i < TargetDst; i += density)
            {
                MapProjectile visProj = new MapProjectile(AllodsProjectile.Lightning);
                visProj.Color = Colors[Color];
                visProj.LightLevel = 0;
                visProj.SetPosition(Projectile.ProjectileX + TargetDir.x * i, Projectile.ProjectileY + TargetDir.y * i, 0); // for now
                MapLogic.Instance.Objects.Add(visProj);
                SubProjectiles.Add(visProj);
            }

            Projectile.CallCallback();
        }

        AnimTime += 0.08f;

        float hUDiff = MapLogic.Instance.GetHeightAt(TargetCenter.x, TargetCenter.y, 1, 1) / 32f;
        float htOrig = MapLogic.Instance.GetHeightAt(Projectile.ProjectileX, Projectile.ProjectileY, 1, 1) / 32f;
        float angleToDst = Mathf.Atan2(TargetDir.y, TargetDir.x);
        float sinScale = TargetDst / 8 * Mathf.Sin((float)MapLogic.Instance.LevelTime / 2 * TargetDst);
        Vector2 offs = new Vector2(0, 1);
        float sinX = Mathf.Cos(angleToDst) * offs.x - Mathf.Sin(angleToDst) * offs.y;
        float sinY = Mathf.Cos(angleToDst) * offs.y + Mathf.Sin(angleToDst) * offs.x;

        for (int i = 0; i < SubProjectiles.Count; i++)
        {
            float idst = density * i;
            // get angle from first to second. calculate sin wave
            float baseX = Projectile.ProjectileX + TargetDir.x * idst;
            float baseY = Projectile.ProjectileY + TargetDir.y * idst;
            
            float sscale = (Sin10(idst / TargetDst) * sinScale);
            MapProjectile sub = SubProjectiles[i];
            sub.Color = Colors[Color];
            sub.Alpha = 1f - AnimTime;
            sub.LightLevel = 0;
            if (i % 12 == 0)
                sub.LightLevel = (int)(512 * (1f - AnimTime));
            float htDiff = MapLogic.Instance.GetHeightAt(baseX + sinX*sscale, baseY + sinY*sscale, 1, 1)/32f;
            float lval = (float)i / SubProjectiles.Count;
            float htLerp = hUDiff * lval + htOrig * (1f - lval);

            sub.SetPosition(baseX + sinX*sscale, baseY + sinY*sscale, -htDiff+htLerp);
        }

        if (AnimTime > 1)
        {
            foreach (MapProjectile sub in SubProjectiles)
            {
                sub.Dispose();
                MapLogic.Instance.Objects.Remove(sub);
            }

            Projectile.Dispose();
            MapLogic.Instance.Objects.Remove(Projectile);
            return false;
        }

        return true;
    }
}

// MapProjectileEOT = regular projectile, but animates for specified amount of time and calls callback every N ticks
public class MapProjectileLogicEOT : IMapProjectileLogic
{
    MapProjectile Projectile = null;
    int StartFrames;
    int EndFrames;
    int Timer = 0;
    int Frame = 0;

    int Duration;
    int Frequency;
    int TimerAtStart = -1;

    float RSeed = 0;

    public MapProjectileLogicEOT(int duration, int frequency, int startframes = 0, int endframes = 0)
    {
        RSeed = UnityEngine.Random.Range(0f, 1f);
        Duration = duration;
        StartFrames = startframes;
        EndFrames = endframes;
        Frequency = frequency;
    }

    public void SetProjectile(MapProjectile proj)
    {
        Projectile = proj;
    }

    public bool Update()
    {
        if (TimerAtStart == -1)
            TimerAtStart = MapLogic.Instance.LevelTime;

        int timeOffset = MapLogic.Instance.LevelTime - TimerAtStart;
        if (timeOffset % Frequency == 0)
            Projectile.CallCallback(timeOffset >= Duration);

        if (timeOffset >= Duration) // die
            return false;

        float ll = 0;

        // first, check start and end
        // roughly use 2 ticks per frame
        if (timeOffset > Duration-EndFrames*2)
        {
            int endframe = (timeOffset - (Duration - EndFrames * 2)) / 2;
            Frame = Projectile.Class.Phases - EndFrames + endframe;
            ll = 1f - (float)endframe / EndFrames;
        }
        else if (timeOffset < StartFrames*2)
        {
            Frame = timeOffset / 2;
            ll = (float)Frame / StartFrames;
        }
        else
        {
            Frame = (timeOffset / 2 % (Projectile.Class.Phases - (StartFrames + EndFrames))) + StartFrames;
            ll = 0.5f + Mathf.Sin((float)timeOffset/4+RSeed*2)/4;
        }

        if (Projectile.Class.ID == (int)AllodsProjectile.FireWall && timeOffset % 2 == 0)
            RSeed = UnityEngine.Random.Range(0f, 1f);

        Projectile.CurrentFrame = Frame;
        Projectile.CurrentTics = 0;
        Projectile.DoUpdateView = true;
        if (Projectile.Class.ID != (int)AllodsProjectile.FireWall)
            ll = -1;
        Projectile.LightLevel = (int)(256+ll*256f);

        if (Projectile.Class.ID == (int)AllodsProjectile.PoisonCloud)
        {
            if (timeOffset < 16)
                Projectile.Alpha = (float)timeOffset / 16;
            else if (timeOffset >= 16 && timeOffset < Duration - 16)
                Projectile.Alpha = (1f - (float)(timeOffset - 16) / (Duration - 32)) * 0.5f + 0.5f;
            else if (timeOffset > Duration - 16)
                Projectile.Alpha = 0.5f - ((float)timeOffset - (Duration - 16)) / 16 * 0.5f;
        }

        Timer++;
        return true;
    }
}

// human readable enum of projectile IDs for use with spells and such
public enum AllodsProjectile
{
    None = 0,
    BowArrow = 1,
    XBowArrow = 2,
    OrcArrow = 3,
    GoblinArrow = 4,
    Catapult1 = 5,
    Catapult2 = 6,
    FireArrow = 10,
    FireBall = 12,
    Explosion = 13,
    IceMissile = 18,
    PoisonCloud = 21,
    FireWall = 15,
    AcidSpray = 27,
    Lightning = 28,
    Healing = 56,
    Bless = 48,
    Shield = 62,
    ProtectionFire = 16,
    ProtectionWater = 24,
    ProtectionAir = 34,
    ProtectionEarth = 46,
    EarthWall = 43,
    PoisonSign = 20,
    Curse = 64,
    Drain = 60,
    Blizzard = 23,
    Catapult3 = 7,
    Steam = 8,
    Teleport = 54,
    ChainLightning = 30,
    DiamondDust = 40
}

public class MapProjectile : MapObject, IDynlight
{
    public override MapObjectType GetObjectType() { return MapObjectType.Effect; }
    protected override Type GetGameObjectType() { return typeof(MapViewProjectile); }

    public ProjectileClass Class;

    public float FracX;
    public float FracY;

    public float ProjectileX
    {
        get
        {
            return X + FracX;
        }
    }

    public float ProjectileY
    {
        get
        {
            return Y + FracY;
        }
    }

    private float _ProjectileZ = 0;
    public float ProjectileZ
    {
        get
        {
            return _ProjectileZ;
        }
    }

    public void SetPosition(float x, float y, float z)
    {
        bool bDoCalcLight = (LightLevel > 0 && ((int)x != X || (int)y != Y));

        UnlinkFromWorld();
        X = (int)x;
        Y = (int)y;
        FracX = x - X;
        FracY = y - Y;
        _ProjectileZ = z;
        LinkToWorld();
        DoUpdateView = true;

        if (bDoCalcLight)
            MapLogic.Instance.CalculateDynLighting();
    }

    private int _LightLevel;
    public int LightLevel
    {
        get
        {
            return _LightLevel;
        }

        set
        {
            if (_LightLevel != value)
            {
                _LightLevel = value;
                MapLogic.Instance.CalculateDynLighting();
            }
        }
    }

    public int GetLightValue() { return LightLevel; }
    public MapUnit Target = null;

    private IMapProjectileLogic Logic = null;
    private MapProjectileCallback Callback = null;

    public IPlayerPawn Source { get; private set; }

    // for appearance purposes
    public int CurrentFrame = 0;
    public int CurrentTics = 0;

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

    private float _Alpha;
    public float Alpha
    {
        get
        {
            return _Alpha;
        }

        set
        {
            _Alpha = value;
            DoUpdateView = true;
        }
    }

    private Color _Color;
    public Color Color
    {
        get
        {
            return _Color;
        }

        set
        {
            _Color = value;
            DoUpdateView = true;
        }
    }

    private int _ZOffset;
    public int ZOffset
    {
        get
        {
            return _ZOffset;
        }

        set
        {
            _ZOffset = value;
            DoUpdateView = true;
        }
    }

    public MapProjectile(AllodsProjectile proj, IPlayerPawn source = null, IMapProjectileLogic logic = null, MapProjectileCallback cb = null)
    {
        InitProjectile((int)proj, source, logic, cb);
    }

    public MapProjectile(int id, IPlayerPawn source = null, IMapProjectileLogic logic = null, MapProjectileCallback cb = null)
    {
        InitProjectile(id, source, logic, cb);
    }

    private void InitProjectile(int id, IPlayerPawn source, IMapProjectileLogic logic, MapProjectileCallback cb)
    {
        Class = ProjectileClassLoader.GetProjectileClassById(id);
        if (Class == null)
        {
            Debug.LogFormat("Invalid projectile created (id={0})", id);
            return;
        }

        Source = source;
        Logic = logic;
        if (Logic != null) Logic.SetProjectile(this);
        Callback = cb;

        Width = 1;
        Height = 1;
        Alpha = 1f;
        Color = new Color(1, 1, 1, 1);
        ZOffset = 128;
        DoUpdateView = true;
    }

    public override void Dispose()
    {
        LightLevel = 0;
        for (int i = 0; i < MapLogic.Instance.Objects.Count; i++)
        {
            MapObject mobj = MapLogic.Instance.Objects[i];
            if (mobj.GetObjectType() != MapObjectType.Monster &&
                mobj.GetObjectType() != MapObjectType.Human) continue;
            MapUnit unit = (MapUnit)mobj;
            unit.TargetedBy.Remove(this);
        }
        base.Dispose();
    }

    public void CallCallback(bool nullify = true)
    {
        if (Callback != null)
            Callback(this);
        if (nullify)
            Callback = null;
    }

    public override void Update()
    {
        if (Logic != null)
        {
            if (!Logic.Update())
            {
                CallCallback(true);
                Dispose();
                return;
            }
        }
    }
}