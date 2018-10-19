using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace Spells
{
    [SpellProcId(Spell.Spells.Lightning)]
    public class SpellProcLightning : SpellProc
    {
        public SpellProcLightning(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit)
        {
            SpawnProjectile(20);
        }

        // this should only be called for arrows (i.e. homing projectiles), with TargetUnit set.
        protected void SpawnProjectile(int damage)
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

            Server.SpawnProjectileLightning(Spell.User, cX, cY, 0,
                                            TargetUnit,
                                            0,
                                            (MapProjectile fproj) =>
                                            {
                                                DamageFlags spdf = SphereToDamageFlags(Spell);

                                                if (TargetUnit.TakeDamage(spdf, Spell.User, damage) > 0)
                                                {
                                                    TargetUnit.DoUpdateInfo = true;
                                                    TargetUnit.DoUpdateView = true;
                                                }
                                            });
        }
    }
}