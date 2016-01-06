using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProtoBuf;

public class IdleAction : IUnitAction
{
    private MapUnit Unit;

    public IdleAction(MapUnit unit)
    {
        Unit = unit;
    }

    public virtual bool Process()
    {
        int actionsBefore = Unit.Actions.Count;

        if (!NetworkManager.IsClient)
        {
            while (Unit.States.Count > 0 && !Unit.States.Last().Process())
                Unit.States.RemoveAt(Unit.States.Count - 1);
        }

        if (Unit.Actions.Count != actionsBefore)
            return true;

        if (Unit.VState != UnitVisualState.Idle && (!NetworkManager.IsClient || Unit.AllowIdle))
        {
            Unit.VState = UnitVisualState.Idle; // set state to idle
            Unit.DoUpdateView = true;

            if (NetworkManager.IsServer)
                Server.NotifyIdleUnit(Unit, Unit.X, Unit.Y, Unit.Angle);
            Unit.AllowIdle = false;
        }


        // possibly animate
        if (Unit.Class.IdlePhases > 1)
        {
            Unit.IdleTime++;
            if (Unit.IdleTime >= Unit.Class.IdleFrames[Unit.IdleFrame].Time)
            {
                Unit.IdleFrame = ++Unit.IdleFrame % Unit.Class.IdlePhases;
                Unit.IdleTime = 0;
                Unit.DoUpdateView = true;
            }
        }
        else
        {
            Unit.IdleFrame = 0;
            Unit.IdleTime = 0;
        }

        return true; // idle state is always present
    }
}

[ProtoContract]
public class RotateAction : IUnitAction
{
    public MapUnit Unit;
    [ProtoMember(1)]
    public int TargetAngle;

    public RotateAction()
    {
        Unit = null;
    }

    public RotateAction(MapUnit unit, int targetAngle)
    {
        Unit = unit;
        TargetAngle = targetAngle;
    }

    // returns smallest difference, either positive or negative.
    private int CheckAngle(int a1, int a2)
    {
        int a = a2 - a1;
        a = a += (a > 180) ? -360 : (a < -180) ? 360 : 0;
        return a;
    }

    public virtual bool Process()
    {
        // 
        //Debug.Log(string.Format("walk state: angle changed {0}->{1}", Unit.Angle, TargetAngle));

        // if we wrapped around, set angle and exit
        if (Unit.Angle != TargetAngle) // sometimes it happens that angle is already set
        {
            int ToRotate = CheckAngle(Unit.Angle, TargetAngle);
            if (ToRotate > 0)
                ToRotate = Math.Min(ToRotate, Unit.Stats.RotationSpeed);
            else ToRotate = Math.Max(ToRotate, -Unit.Stats.RotationSpeed);
            Unit.Angle += ToRotate;
            Unit.DoUpdateView = true;
            Unit.VState = UnitVisualState.Rotating; // set state to rotating
        }

        return (Unit.Angle != TargetAngle);
    }
}

[ProtoContract]
public class MoveAction : IUnitAction
{
    public MapUnit Unit;
    [ProtoMember(1)]
    public int TargetX;
    [ProtoMember(2)]
    public int TargetY;
    [ProtoMember(3)]
    public float Frac;
    [ProtoMember(4)]
    public float FracAdd;
    [ProtoMember(5)]
    public float MoveSpeed;

    public MoveAction()
    {
        Unit = null;
    }

    public MoveAction(MapUnit unit, int x, int y)
    {
        Unit = unit;
        TargetX = x;
        TargetY = y;
        Frac = 0;
    }

    public virtual bool Process()
    {
        // check if it's possible to walk there (again). NOT on client.
        if (!NetworkManager.IsClient && !Unit.CheckWalkableForUnit(TargetX, TargetY, false))
            return false; // stop this state. possibly try to pathfind again. otherwise idle.

        //Debug.LogFormat("walk state: moving to {0},{1} ({2})", TargetX, TargetY, Frac);
        if (Frac >= 1)
        {
            Unit.SetPosition(TargetX, TargetY);
            Unit.FracX = 0;
            Unit.FracY = 0;
            Unit.DoUpdateView = true;
            return false;
        }
        else
        {
            if (Frac == 0)
            {
                Unit.LinkToWorld(TargetX, TargetY); // link to target coordinates. don't unlink from previous yet.
                FracAdd = (float)Unit.Stats.Speed / 400; // otherwise can be written as speed / 20 / 20.
                MoveSpeed = (float)Unit.Stats.Speed / 20; // move animation play speed.
            }
            Frac += FracAdd;
            Unit.FracX = Frac * (TargetX - Unit.X);
            Unit.FracY = Frac * (TargetY - Unit.Y);
            Unit.DoUpdateView = true;
            Unit.VState = UnitVisualState.Moving;
            if (Unit.Class.MovePhases > 1)
            {
                Unit.MoveTime++;
                if (MoveSpeed * Unit.MoveTime >= Unit.Class.MoveFrames[Unit.MoveFrame].Time)
                {
                    Unit.MoveFrame = ++Unit.MoveFrame % Unit.Class.MovePhases;
                    Unit.MoveTime = 0;
                    Unit.DoUpdateView = true;
                }
            }
            else
            {
                Unit.MoveFrame = 0;
                Unit.MoveTime = 0;
            }
            return true;
        }
    }
}

[ProtoContract]
public class AttackAction : IUnitAction
{
    public MapUnit Unit;
    public MapUnit TargetUnit;
    private bool DamageDone;
    [ProtoMember(1)]
    public int Timer;
    [ProtoMember(2)]
    public DamageFlags DamageFlags;
    [ProtoMember(3)]
    public int Damage;
    [ProtoMember(4)]
    public float Speed;

    public AttackAction()
    {
        Unit = null;
        TargetUnit = null;
    }

    public AttackAction(MapUnit unit, MapUnit targetUnit, DamageFlags damageFlags, int damage)
    {
        Unit = unit;
        TargetUnit = targetUnit;
        DamageFlags = damageFlags;
        Damage = damage;
        Timer = 0;
    }

    public bool Process()
    {
        if (Timer == 0)
        {
            Unit.VState = UnitVisualState.Attacking;
            Unit.AttackFrame = 0;
            Unit.AttackTime = 0;
            Unit.DoUpdateView = true;
            Speed = 0.5f;
        }

        if (Unit.Class.AttackPhases > 1)
        {
            Unit.AttackTime++;
            if (Speed * Unit.AttackTime >= Unit.Class.AttackFrames[Unit.AttackFrame].Time)
            {
                Unit.DoUpdateView = true;
                Unit.AttackFrame = ++Unit.AttackFrame % Unit.Class.AttackPhases;
                Unit.AttackTime = 0;
                Unit.DoUpdateView = true;
            }
        }
        else
        {
            Unit.AttackFrame = 0;
            Unit.AttackTime = 0;
        }

        if (Speed * Timer >= Unit.Template.Charge && !NetworkManager.IsClient && !DamageDone)
        {
            // do damage here!
            //
            if (TargetUnit.TakeDamage(DamageFlags, Unit, Damage) > 0)
            {
                TargetUnit.DoUpdateInfo = true;
                TargetUnit.DoUpdateView = true;
            }
            DamageDone = true;
        }

        if (Speed * Timer >= Unit.Template.Charge + Unit.Template.Relax)
            return false; // end of attack

        Timer++;
        return true;
    }
}

[ProtoContract]
public class DeathAction : IUnitAction
{
    public MapUnit Unit;
    [ProtoMember(1)]
    public int Timer;

    public DeathAction()
    {
        Unit = null;
    }

    public DeathAction(MapUnit unit)
    {
        Unit = unit;
        Timer = 0;
    }

    public bool Process()
    {
        if (Timer == 0)
        {
            Unit.VState = UnitVisualState.Dying;
            Unit.DeathFrame = 0;
            Unit.DeathTime = 0;
            Unit.DoUpdateView = true;
        }

        Timer++;

        UnitClass dCls = Unit.Class;
        while (dCls.Dying != null && dCls.Dying != dCls)
            dCls = dCls.Dying;

        if (++Unit.DeathTime >= 2)
        {
            Unit.DeathFrame++;
            Unit.DeathTime = 0;
            Unit.DoUpdateView = true;
        }

        if (Unit.DeathFrame >= dCls.DyingPhases - 1)
            return false;

        return true;
    }
}