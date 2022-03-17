using System;
using UnityEngine;

public class MapObstacle : MapObject, IVulnerable
{
    public override MapObjectType GetObjectType() { return MapObjectType.Obstacle; }
    protected override Type GetGameObjectType() { return typeof(MapViewObstacle); }

    public ObstacleClass Class = null;
    public int CurrentFrame = 0;
    public int CurrentTime = 0;
    public bool IsDead = false;

    private bool WasDead = false;
    private bool NeedDeathSFX = false;

    public MapObstacle(int typeId)
    {
        Class = ObstacleClassLoader.GetObstacleClassById(typeId);
        if (Class == null)
            Debug.LogFormat("Invalid obstacle created (typeId={0})", typeId);
        else InitObstacle();
    }

    public MapObstacle(string name)
    {
        Class = ObstacleClassLoader.GetObstacleClassByName(name);
        if (Class == null)
            Debug.LogFormat("Invalid obstacle created (name={0})", name);
        else InitObstacle();
    }

    private void InitObstacle()
    {
        // ???
        Width = 1;
        Height = 1;
        RenderViewVersion++;
    }

    public override void CheckAllocateObject()
    {
        if (GetVisibility() >= 1)
            AllocateObject();
    }

    public override void Update()
    {
        if (Class == null)
            return;

        if (!WasDead && IsDead)
        {
            if (!NetworkManager.IsClient && NeedDeathSFX)
            {
                Server.SpawnProjectileSimple(AllodsProjectile.FireWall, null, X + 0.5f, Y + 0.5f, 0, 1);
            }
            WasDead = IsDead;
        }

        UpdateNetVisibility(); // :( ?

        // do not animate if visibility != 2, also do not render at all if visibility == 0
        if (Class.Frames.Length > 1 && GetVisibility() == 2)
        {
            CurrentTime++;
            if (CurrentTime > Class.Frames[CurrentFrame].Time)
            {
                CurrentFrame = ++CurrentFrame % Class.Frames.Length;
                CurrentTime = 0;
                RenderViewVersion++;
            }
        }
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        return MapNodeFlags.BlockedGround;
    }

    public bool SetDead(bool sfx)
    {
        // check if "alive"
        if (IsDead || Class.DeadObject < 0 || Class.DeadObject == Class.ID)
            return false;

        IsDead = true;
        NeedDeathSFX = sfx;
        WasDead = false;

        // set current class id to deadobject.
        ObstacleClass deadClass = ObstacleClassLoader.GetObstacleClassById(Class.DeadObject);
        if (deadClass == null)
            return false;

        Class = deadClass;
        CurrentFrame = 0;
        CurrentTime = 0;
        RenderViewVersion++;
        return true;
    }

    public int TakeDamage(DamageFlags flags, MapUnit source, int count)
    {
        if ((flags & DamageFlags.Fire) == 0)
            return 0;

        if (SetDead(true))
        {
            // inform clients.
            if (NetworkManager.IsServer)
                Server.NotifyStaticObjectDead(X, Y);

            return count;
        }

        return 0;
    }
}
