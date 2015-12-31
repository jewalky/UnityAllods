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
                List<Vector2i> path = Unit.DecideNextMove(Unit.WalkX, Unit.WalkY, true);
                if (path == null)
                {
                    //Debug.LogFormat("idle state: path to {0},{1} not found", Unit.WalkX, Unit.WalkY);
                    Unit.WalkX = -1;
                    Unit.WalkY = -1;
                    return true;
                }

                int sbd = 32;
                if (sbd > path.Count) sbd = path.Count;
                for (int i = 0; i < sbd; i++)
                {
                    if (!Unit.CheckWalkableForUnit(path[i].x, path[i].y, false))
                    {
                        // one of nodes in statically found path (up to 32 nodes ahead) is non-walkable.
                        // here we try to build another path around it instead.
                        // if it's not found, we continue to walk along the old path.
                        List<Vector2i> path2 = Unit.DecideNextMove(Unit.WalkX, Unit.WalkY, false);
                        if (path2 != null)
                            path = path2;
                        break;
                    }
                }

                // if NEXT node is not walkable, we drop into idle state.
                if (Unit.CheckWalkableForUnit(path[0].x, path[0].y, false))
                {
                    // next path node found
                    // notify clients
                    Unit.AddActions(new MoveAction(Unit, path[0].x, path[0].y), new RotateAction(Unit, Unit.FaceCell(path[0].x, path[0].y)));
                    return true;
                }
            }
        }

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
            Frac += 0.05f;
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