using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProtoBuf;

public class IdleState : IUnitState
{
    private MapUnit Unit;

    public IdleState(MapUnit unit)
    {
        Unit = unit;
    }

    public virtual bool Process()
    {
        if (!NetworkManager.IsClient)
        {
            if (Unit.WalkX > 0 && Unit.WalkY > 0)
            {
                //Debug.LogFormat("idle state: walk to {0},{1}", Unit.WalkX, Unit.WalkY);
                if (Unit.WalkX == Unit.X && Unit.WalkY == Unit.Y)
                {
                    Unit.WalkX = -1;
                    Unit.WalkY = -1;
                    return true;
                }

                // try to pathfind
                Vector2i path = Unit.DecideNextMove(Unit.WalkX, Unit.WalkY);
                if (path == null)
                {
                    //Debug.LogFormat("idle state: path to {0},{1} not found", Unit.WalkX, Unit.WalkY);
                    Unit.WalkX = -1;
                    Unit.WalkY = -1;
                    return true;
                }

                // next path node found
                // notify clients
                Unit.AddStates(new MoveState(Unit, path.x, path.y), new RotateState(Unit, Unit.FaceCell(path.x, path.y)));
                return true;
            }

            if (Unit.VState != UnitVisualState.Idle)
            {
                Unit.VState = UnitVisualState.Idle; // set state to idle
                Unit.DoUpdateView = true;

                if (NetworkManager.IsServer)
                    Server.NotifyIdleUnit(Unit, Unit.X, Unit.Y, Unit.Angle);
            }
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
public class RotateState : IUnitState
{
    public MapUnit Unit;
    [ProtoMember(1)]
    public int TargetAngle;

    public RotateState()
    {
        Unit = null;
    }

    public RotateState(MapUnit unit, int targetAngle)
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
public class MoveState : IUnitState
{
    public MapUnit Unit;
    [ProtoMember(1)]
    public int TargetX;
    [ProtoMember(2)]
    public int TargetY;
    [ProtoMember(3)]
    public float Frac;

    public MoveState()
    {
        Unit = null;
    }

    public MoveState(MapUnit unit, int x, int y)
    {
        Unit = unit;
        TargetX = x;
        TargetY = y;
        Frac = 0;
    }

    public virtual bool Process()
    {
        // check if it's possible to walk there (again). NOT on client.
        if (!NetworkManager.IsClient && !Unit.CheckWalkableForUnit(TargetX, TargetY))
            return false; // stop this state. possibly try to pathfind again. otherwise idle.

        //Debug.LogFormat("walk state: moving to {0},{1} ({2})", TargetX, TargetY, Frac);
        if (Frac >= 1)
        {
            Unit.UnlinkFromWorld(); // unlink at old
            Unit.X = TargetX;
            Unit.Y = TargetY;
            Unit.FracX = 0;
            Unit.FracY = 0;
            Unit.LinkToWorld(); // relink at current. this is required as sometimes coordinates overlap.
            Unit.DoUpdateView = true;
            return false;
        }
        else
        {
            if (Frac == 0)
                Unit.LinkToWorld(TargetX, TargetY); // link to target coordinates. don't unlink from previous yet.
            Frac += 0.05f;
            Unit.FracX = Frac * (TargetX - Unit.X);
            Unit.FracY = Frac * (TargetY - Unit.Y);
            Unit.DoUpdateView = true;
            Unit.VState = UnitVisualState.Moving;
            if (Unit.Class.MovePhases > 1)
            {
                Unit.MoveTime++;
                if (Unit.MoveTime >= Unit.Class.MoveFrames[Unit.MoveFrame].Time)
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