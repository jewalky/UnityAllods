using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace Spells
{
    [SpellProcId(Spell.Spells.Teleport)]
    public class SpellProcTeleport : SpellProc
    {
        int Timer = 0;

        public SpellProcTeleport(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            if (!(Spell.User is MapUnit))
                return false; // teleport not usable for structures... can't teleport your house!

            if (Timer == 0)
            {
                float cscale = (Spell.User.Width + Spell.User.Height) / 2f;
                Server.SpawnProjectileSimple(AllodsProjectile.Teleport, Spell.User, Spell.User.X + Spell.User.Width / 2f, Spell.User.Y + Spell.User.Height / 2f, 0.2f*cscale, 1f, cscale);
                Server.SpawnProjectileSimple(AllodsProjectile.Teleport, Spell.User, TargetX + 0.5f, TargetY + 0.5f, 0.2f*cscale, 1f, cscale);
            }

            Timer++;

            if (Timer >= 8)
            {
                int utpX = TargetX - Spell.User.Width / 2;
                int utpY = TargetY - Spell.User.Height / 2;
                MapUnit unit = Spell.User;
                if (!unit.Interaction.CheckWalkableForUnit(utpX, utpY, false))
                    return false;
                unit.SetPosition(utpX, utpY, true);
                return false;
            }

            return true;
        }
    }

    [SpellProcId(Spell.Spells.Haste)]
    public class SpellProcHaste : SpellProc
    {
        public SpellProcHaste(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            SpellEffects.Effect eff = new SpellEffects.Haste((int)(MapLogic.TICRATE * Spell.GetDuration()), Spell.GetSpeed());
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Shield)]
    public class SpellProcShield : SpellProc
    {
        public SpellProcShield(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            SpellEffects.Effect eff = new SpellEffects.Shield((int)(MapLogic.TICRATE * Spell.GetDuration()), Spell.GetAbsorbtion());
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Control_Spirit)]
    public class SpellProcControlSpirit : SpellProc
    {
        public SpellProcControlSpirit(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            if (TargetX < 0 || TargetX >= MapLogic.Instance.Width ||
                TargetY < 0 || TargetY >= MapLogic.Instance.Height) return false;

            // check if this cell is blocked by something
            MapNode node = MapLogic.Instance.Nodes[TargetX, TargetY];

            // find any corpse at X/Y
            for (int i = 0; i < node.Objects.Count; i++)
            {
                MapObject o = node.Objects[i];
                if (!(o is MapUnit))
                    continue;
                MapUnit u = (MapUnit)o;
                if (u.IsAlive || u.IsDying || u.IsFlying)
                    continue;
                if (u.Class.ID == 69 || // ghost
                   (u.Class.ID >= 89 && u.Class.ID <= 92) || // skeleton fighter
                   (u.Class.ID >= 93 && u.Class.ID <= 96) || // skeleton archer
                   (u.Class.ID >= 97 && u.Class.ID <= 98) || // skeleton mage
                   (u.Class.ID >= 82 && u.Class.ID <= 85) || // zombie fighter
                   (u.Class.ID >= 86 && u.Class.ID <= 88)) // zombie archer
                {
                    continue; // don't allow resurrecting undead
                }
                if (u.BoneFrame > 3)
                    continue;
                if (TargetX >= u.X && TargetX < u.X+u.Width &&
                    TargetY >= u.Y && TargetY < u.Y+u.Height)
                {
                    float raiseHpMult;
                    string raiseName;

                    switch (u.BoneFrame)
                    {
                        case 0:
                            raiseHpMult = 1f;
                            raiseName = "F_Zombie.1";
                            break;
                        case 1:
                            raiseHpMult = 0.5f;
                            raiseName = "F_Zombie.1";
                            break;
                        case 2:
                            raiseHpMult = 0.5f;
                            raiseName = "F_Skeleton.1";
                            break;
                        default:
                            raiseHpMult = 0.5f;
                            raiseName = "Ghost";
                            break;
                    }

                    MapUnit unit = new MapUnit(raiseName);
                    if (unit.Class == null)
                    {
                        Debug.LogFormat("Failed to spawn resurrected unit {0}", raiseName);
                        return false;
                    }

                    unit.Player = Spell.User.Player;
                    unit.Tag = MapLogic.Instance.GetFreeUnitTag();
                    unit.SetPosition(TargetX, TargetY, false);
                    unit.LinkToWorld();
                    if (!unit.Interaction.CheckWalkableForUnit(TargetX, TargetY, false))
                    {
                        // invalid position, don't add unit
                        // (but still delete bone)
                        u.BoneFrame = 4;
                        u.RenderViewVersion++;
                        if (NetworkManager.IsServer)
                            Server.NotifyUnitBoneFrame(u);
                        unit.Dispose();
                        return false;
                    }
                    unit.CoreStats.HealthMax = (int)(u.CoreStats.HealthMax * raiseHpMult);
                    unit.UpdateItems();
                    MapLogic.Instance.AddObject(unit, true);

                    u.BoneFrame = 4; // invisible corpse
                    u.RenderViewVersion++;
                    if (NetworkManager.IsServer)
                        Server.NotifyUnitBoneFrame(u);

                    return false;
                }
            }

            return false;
        }
    }

    [SpellProcId(Spell.Spells.Summon)]
    public class SpellProcSummon : SpellProc
    {
        public SpellProcSummon(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            // find unit to spawn
            string[] baseUnits = new string[] { "Squirrel", "Turtle", "Foot_Animated" };
            int level = Mathf.Max(1, Mathf.Min(4, (Spell.Skill / 4) + 1));
            string unitName = baseUnits[UnityEngine.Random.Range(0, 3)] + ((level > 1) ? string.Format(".{0}", level) : "");

            MapUnit unit = new MapUnit(unitName);
            if (unit.Class == null)
            {
                Debug.LogFormat("Failed to spawn summoned unit {0}", unitName);
                return false;
            }

            unit.Player = Spell.User.Player;
            unit.Tag = MapLogic.Instance.GetFreeUnitTag();
            if (!unit.RandomizePosition(Spell.User.X, Spell.User.Y, 2, false))
            {
                // invalid position, don't add unit
                unit.Dispose();
                return false;
            }

            if (!unit.IsLinked) unit.LinkToWorld();

            unit.SummonTimeMax = 30;
            unit.SummonTime = 0;

            MapLogic.Instance.AddObject(unit, true);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Heal)]
    public class SpellProcHeal : SpellProc
    {
        public SpellProcHeal(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            SpellEffects.Effect eff = new SpellEffects.Heal(MapLogic.TICRATE, Spell.GetDamage());
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Drain_Life)]
    public class SpellProcDrainLife : SpellProc
    {
        public SpellProcDrainLife(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            SpellEffects.Effect eff = new SpellEffects.Drain(MapLogic.TICRATE, Spell.GetDamage(), Spell.User);
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }
}