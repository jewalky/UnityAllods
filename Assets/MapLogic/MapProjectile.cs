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

    public bool Update()
    {
        // calculate target direction!
        // check if target is gone
        if (!Target.IsAlive || !MapLogic.Instance.Objects.Contains(Target))
            Target = null;

        if (Target != null)
        {
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
            if (Math.Sign(targetCenter.x - newPos.x) != Math.Sign(targetCenter.x - Projectile.ProjectileX))
                newPos.x = targetCenter.x;
            if (Math.Sign(targetCenter.y - newPos.y) != Math.Sign(targetCenter.y - Projectile.ProjectileY))
                newPos.y = targetCenter.y;
            if (Math.Sign(targetCenter.z - newPos.z) != Math.Sign(targetCenter.z - Projectile.ProjectileZ))
                newPos.z = targetCenter.z;
            Projectile.SetPosition(newPos.x, newPos.y, newPos.z);
            return true;
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
        int frame = (int)(Timer * AnimationSpeed);
        if (frame < 0) frame = 0; // shouldn't happen though
        if (frame >= Projectile.Class.Phases)
            return false;
        Projectile.CurrentFrame = frame;
        Projectile.CurrentTics = 0;
        Projectile.DoUpdateView = true;
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
        DoUpdateView = true;
    }

    public override void Dispose()
    {
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

    public override void Update()
    {
        if (Logic != null)
        {
            if (!Logic.Update())
            {
                if (Callback != null)
                    Callback(this);
                else
                {
                    Dispose();
                    return;
                }
                Logic = null; // logic done
            }
        }
    }
}