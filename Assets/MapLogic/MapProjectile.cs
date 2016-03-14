using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

    public MapProjectile(int id)
    {
        Class = ProjectileClassLoader.GetProjectileClassById(id);
        if (Class == null)
        {
            Debug.LogFormat("Invalid projectile created (id={0})", id);
            return;
        }

        Width = 1;
        Height = 1;
        DoUpdateView = true;
    }

    public override void Update()
    {
        // calculate target direction!
        if (Target != null)
        {
            Vector2 targetCenter = new Vector2(Target.X + (float)Target.Width / 2 + Target.FracX, Target.Y + (float)Target.Height / 2 + Target.FracY);
            Vector2 dir = new Vector2(targetCenter.x - ProjectileX, targetCenter.y - ProjectileY);
            dir.Normalize();
            dir /= 5;
            Vector2 newPos = new Vector2(ProjectileX + dir.x, ProjectileY + dir.y);
            if (Math.Sign(targetCenter.x - newPos.x) != Math.Sign(targetCenter.x - ProjectileX))
                newPos.x = targetCenter.x;
            if (Math.Sign(targetCenter.y - newPos.y) != Math.Sign(targetCenter.y - ProjectileY))
                newPos.y = targetCenter.y;
            SetPosition(newPos.x, newPos.y, ProjectileZ);
        }

        //Debug.LogFormat("update at {0},{1}", ProjectileX, ProjectileY);
        //UpdateNetVisibility();
    }
}