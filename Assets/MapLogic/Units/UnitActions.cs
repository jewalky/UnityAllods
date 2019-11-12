using System;
using System.Linq;
using ProtoBuf;
using UnityEngine;
using System.Collections.Generic;

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

        if (Unit.Actions.Count > actionsBefore) // new actions were added
        {
            while (Unit.Actions.Count > 1 && !Unit.Actions.Last().Process())
                Unit.Actions.RemoveAt(Unit.Actions.Count - 1);
            return true;
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
        a = a + ((a > 180) ? -360 : (a < -180) ? 360 : 0);
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
        if (!NetworkManager.IsClient && !Unit.Interaction.CheckWalkableForUnit(TargetX, TargetY, false))
            return false; // stop this state. possibly try to pathfind again. otherwise idle.

        Unit.TargetX = TargetX;
        Unit.TargetY = TargetY;

        //Debug.LogFormat("walk state: moving to {0},{1} ({2})", TargetX, TargetY, Frac);
        if (Frac >= 1)
        {
            Frac = 1;
            Unit.SetPosition(TargetX, TargetY, false);
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
                FracAdd *= Unit.Interaction.GetNodeSpeedFactor(TargetX, TargetY, false); // do we need to do this every time?
                MoveSpeed = (float)Unit.Stats.Speed / 20; // move animation play speed.
            }

            Frac = Mathf.Clamp01(Frac);
            Unit.FracX = Frac * (TargetX - Unit.X);
            Unit.FracY = Frac * (TargetY - Unit.Y);
            Frac += FracAdd;
            Unit.DoUpdateView = true;
            Unit.VState = UnitVisualState.Moving;
            if (Unit.Class.MovePhases > 1)
            {
                if (MoveSpeed * Unit.MoveTime >= Unit.Class.MoveFrames[Unit.MoveFrame].Time)
                {
                    Unit.MoveFrame = ++Unit.MoveFrame % Unit.Class.MovePhases;
                    Unit.MoveTime = 0;
                    Unit.DoUpdateView = true;
                }

                Unit.MoveTime++;
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
    public Spell Spell;
    private bool DamageDone;
    [ProtoMember(1)]
    public int Timer;
    [ProtoMember(2)]
    public DamageFlags DamageFlags;
    [ProtoMember(3)]
    public int Damage;
    [ProtoMember(4)]
    public float Speed;
    [ProtoMember(5)]
    public int TargetX;
    [ProtoMember(6)]
    public int TargetY;
    [ProtoMember(7)]
    public int SpellID;

    public AttackAction()
    {
        Unit = null;
        TargetUnit = null;
        Spell = null;
        DamageDone = false;
    }

    public AttackAction(MapUnit unit, MapUnit targetUnit, DamageFlags damageFlags, int damage)
    {
        Unit = unit;
        TargetUnit = targetUnit;
        DamageFlags = damageFlags;
        Damage = damage;
        Timer = 0;
        Spell = null;
        SpellID = -1;
        TargetX = TargetY = -1;
    }

    public AttackAction(MapUnit unit, MapUnit targetUnit, Spell spell, int targetX, int targetY)
    {
        Unit = unit;
        TargetUnit = targetUnit;
        Spell = spell;
        if (Spell != null)
            SpellID = (int)Spell.SpellID;
        else SpellID = -1;
        TargetX = targetX;
        TargetY = targetY;
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

            // disable invisibility on attack, but only if target is not self
            if (TargetUnit != Unit)
            {
                List<SpellEffects.Invisibility> invis = Unit.GetSpellEffects<SpellEffects.Invisibility>();
                foreach (SpellEffects.Invisibility inv in invis)
                    Unit.RemoveSpellEffect(inv);
            }
        }

        if (Unit.Class.AttackPhases > 1)
        {
            if (Speed * Unit.AttackTime >= Unit.Class.AttackFrames[Unit.AttackFrame].Time)
            {
                //Unit.AttackFrame = ++Unit.AttackFrame % Unit.Class.AttackPhases;
                Unit.AttackFrame++;
                if (Unit.AttackFrame >= Unit.Class.AttackPhases)
                    Unit.AttackFrame = Unit.Class.AttackPhases - 1;
                Unit.AttackTime = -1;
                Unit.DoUpdateView = true;
            }

            Unit.AttackTime++;
        }
        else
        {
            Unit.AttackFrame = 0;
            Unit.AttackTime = 0;
        }

        if (Speed * Timer >= Unit.Charge && !NetworkManager.IsClient && !DamageDone)
        {
            // do damage here!
            // check if we need to fire a projectile (range)
            AllodsProjectile proj = AllodsProjectile.None; // default :D

            // check for castspell in weapon.
            // check for weapon.
            List<Spell> castspells = new List<Spell>();
            bool procoverride = false;
            if (Unit.GetObjectType() == MapObjectType.Human)
            {
                Item weapon = ((MapHuman)Unit).GetItemFromBody(MapUnit.BodySlot.Weapon);
                // if weapon is a staff, then castspell is called immediately.
                // otherwise castspell is only applied when the projectile hits.
                if (weapon != null && weapon.IsValid)
                {
                    foreach (ItemEffect eff in weapon.Effects)
                    {
                        if (eff.Type1 == ItemEffect.Effects.CastSpell)
                        {
                            Spell sp = new Spell(eff.Value1);
                            sp.Skill = eff.Value2;
                            sp.User = Unit;
                            sp.Item = weapon;
                            castspells.Add(sp);
                        }
                    }

                    if (weapon.Class.Option.Name == "Staff" || weapon.Class.Option.Name == "Shaman Staff")
                        procoverride = true;
                }
            }

            if (Unit.Interaction.GetAttackRange() > 1)
            {
                // send this projectile to clients too
                // make projectile specified in the unit class.
                proj = (AllodsProjectile)Unit.Class.Projectile;
            }

            if (!procoverride && Spell == null)
            {
                if (proj != AllodsProjectile.None)
                {
                    // following offsets are based on unit's width, height and center
                    float cX = Unit.X + Unit.Width * 0.5f + Unit.FracX;
                    float cY = Unit.Y + Unit.Height * 0.5f + Unit.FracY;

                    float tX = TargetUnit.X + TargetUnit.Width * 0.5f + TargetUnit.FracX;
                    float tY = TargetUnit.Y + TargetUnit.Height * 0.5f + TargetUnit.FracY;

                    Vector2 dir = new Vector2(tX - cX, tY - cY).normalized * ((Unit.Width + Unit.Height) / 2) / 1.5f;
                    cX += dir.x;
                    cY += dir.y;

                    Server.SpawnProjectileDirectional(proj, Unit, cX, cY, 0,
                                                                  tX, tY, 0,
                                                                  10,
                                                                  (MapProjectile fproj) =>
                                                                  {
                                                                      if (fproj.ProjectileX >= TargetUnit.X + TargetUnit.FracX &&
                                                                          fproj.ProjectileY >= TargetUnit.Y + TargetUnit.FracY &&
                                                                          fproj.ProjectileX < TargetUnit.X + TargetUnit.FracX + TargetUnit.Width &&
                                                                          fproj.ProjectileY < TargetUnit.Y + TargetUnit.FracY + TargetUnit.Height)
                                                                      {
                                                                          //Debug.LogFormat("projectile hit!");
                                                                          // done, make damage
                                                                          TargetUnit.TakeDamage(DamageFlags, Unit, Damage);

                                                                          // and cast spell
                                                                          foreach (Spell spell in castspells)
                                                                          {
                                                                              Spells.SpellProc sp = spell.Cast(TargetUnit.X + TargetUnit.Width / 2, TargetUnit.Y + TargetUnit.Height / 2, TargetUnit);
                                                                              if (sp != null) Unit.AddSpellProcessors(sp);
                                                                          }
                                                                      }

                                                                      fproj.Dispose();
                                                                  });
                }
                else
                {
                    TargetUnit.TakeDamage(DamageFlags, Unit, Damage);
                }
            }
            else
            {
                // cast spells directly
                // just do something atm.
                // todo: check spells that can be only casted on self (distance 0).
                if (Spell != null)
                {
                    Spells.SpellProc sp;
                    if (TargetUnit != null)
                        sp = Spell.Cast(TargetUnit.X + TargetUnit.Width / 2, TargetUnit.Y + TargetUnit.Height / 2, TargetUnit);
                    else sp = Spell.Cast(TargetX, TargetY, null);
                    if (sp != null) Unit.AddSpellProcessors(sp);
                    castspells.Clear();
                }
            }

            foreach (Spell spell in castspells)
            {
                Spells.SpellProc sp = spell.Cast(TargetUnit.X + TargetUnit.Width / 2, TargetUnit.Y + TargetUnit.Height / 2, TargetUnit);
                if (sp != null) Unit.AddSpellProcessors(sp);
            }

            DamageDone = true;
        }

        if (Speed * Timer >= Unit.Charge + Unit.Relax)
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
