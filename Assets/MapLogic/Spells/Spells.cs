using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

public class SpellProcId : Attribute
{
    public int SpellID { get; private set; }
    public SpellProcId(byte id)
    {
        SpellID = id;
    }
    public SpellProcId(Spell.Spells id)
    {
        SpellID = (int)id;
    }
}

namespace Spells
{
    public class SpellProc
    {
        private static List<Type> ProcTypes = null;
        private static Type FindProcTypeFromSpellId(string ns, int sid)
        {
            if (ProcTypes == null)
            {
                ProcTypes = new List<Type>();
                Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (Type type in types)
                {
                    SpellProcId[] npi = (SpellProcId[])type.GetCustomAttributes(typeof(SpellProcId), false);
                    if (npi.Length <= 0)
                        continue;
                    ProcTypes.Add(type);
                }
            }

            foreach (Type type in ProcTypes)
            {
                if (type.Namespace != ns)
                    continue;
                SpellProcId[] npi = (SpellProcId[])type.GetCustomAttributes(typeof(SpellProcId), false);
                if (npi[0].SpellID == sid)
                    return type;
            }

            return null;
        }

        public static Type FindProcTypeFromSpell(Spell spell)
        {
            return FindProcTypeFromSpellId("Spells", spell.SpellID);
        }

        protected readonly int TargetX;
        protected readonly int TargetY;
        protected readonly Spell Spell;
        protected readonly MapUnit TargetUnit;

        public SpellProc(Spell spell, int tgX, int tgY, MapUnit tgUnit)
        {
            Spell = spell;
            TargetX = tgX;
            TargetY = tgY;
            TargetUnit = tgUnit;
        }

        public virtual bool Process()
        {
            return false;
        }
    }

    // this is the base class used for
    //  - Fire_Arrow
    //  - Diamond_Dust
    //  - Ice_Missile
    //  - Fire_Ball (?)
    public class SpellProcProjectileGeneric : SpellProc
    {
        public SpellProcProjectileGeneric(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        protected void SpawnProjectile(AllodsProjectile type, int speed, int damage)
        {
            // following offsets are based on unit's width, height and center
            float tX, tY;
            if (TargetUnit != null)
            {
                tX = TargetUnit.X + TargetUnit.Width * 0.5f + TargetUnit.FracX;
                tY = TargetUnit.Y + TargetUnit.Height * 0.5f + TargetUnit.FracY;
            }
            else
            {
                tX = TargetX + 0.5f;
                tY = TargetY + 0.5f;
            }

            float cX, cY;
            if (Spell.User != null)
            {
                cX = Spell.User.X + Spell.User.Width * 0.5f + Spell.User.FracX;
                cY = Spell.User.Y + Spell.User.Height * 0.5f + Spell.User.FracY;
                Vector2 dir = new Vector2(tX - cX, tY - cY).normalized * ((Spell.User.Width + Spell.User.Height) / 2) / 1.5f;
                cX += dir.x;
                cY += dir.y;
            }
            else
            {
                cX = tX;
                cY = tY;
            }

            Server.SpawnProjectileDirectional(type, Spell.User, cX, cY, 0,
                                                                tX, tY, 0,
                                                                10,
                                                                (MapProjectile fproj) =>
                                                                {
                                                                    if (fproj.ProjectileX >= TargetUnit.X + TargetUnit.FracX &&
                                                                        fproj.ProjectileY >= TargetUnit.Y + TargetUnit.FracY &&
                                                                        fproj.ProjectileX < TargetUnit.X + TargetUnit.FracX + TargetUnit.Width &&
                                                                        fproj.ProjectileY < TargetUnit.Y + TargetUnit.FracY + TargetUnit.Height)
                                                                    {
                                                                        //Debug.LogFormat("spell projectile hit!");
                                                                        // done, make damage
                                                                        DamageFlags spdf = 0;
                                                                        // set damage flags depending on spell sphere
                                                                        switch (Spell.Template.Sphere)
                                                                        {
                                                                            case 1:
                                                                                spdf |= DamageFlags.Fire;
                                                                                break;
                                                                            case 2:
                                                                                spdf |= DamageFlags.Water;
                                                                                break;
                                                                            case 3:
                                                                                spdf |= DamageFlags.Air;
                                                                                break;
                                                                            case 4:
                                                                                spdf |= DamageFlags.Earth;
                                                                                break;
                                                                            case 5:
                                                                                spdf |= DamageFlags.Astral;
                                                                                break;
                                                                        }

                                                                        if (TargetUnit.TakeDamage(spdf, Spell.User, damage) > 0)
                                                                        {
                                                                            TargetUnit.DoUpdateInfo = true;
                                                                            TargetUnit.DoUpdateView = true;
                                                                        }
                                                                    }

                                                                    fproj.Dispose();
                                                                    MapLogic.Instance.Objects.Remove(fproj);
                                                                });
        }
    }

    [SpellProcId(Spell.Spells.Fire_Arrow)]
    public class SpellProcFireArrow : SpellProcProjectileGeneric
    {
        public SpellProcFireArrow(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            SpawnProjectile(AllodsProjectile.FireArrow, 10, 20);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Ice_Arrow)]
    public class SpellProcIceMissile : SpellProcProjectileGeneric
    {
        public SpellProcIceMissile(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            SpawnProjectile(AllodsProjectile.IceMissile, 10, 20);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Diamond_Dust)]
    public class SpellProcDiamondDust : SpellProcProjectileGeneric
    {
        public SpellProcDiamondDust(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            SpawnProjectile(AllodsProjectile.DiamondDust, 10, 20);
            return false;
        }
    }
}