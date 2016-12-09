using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace Spells
{
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

    [SpellProcId(Spell.Spells.Fire_Ball)]
    public class SpellProcFireBall : SpellProc
    {
        public SpellProcFireBall(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            int ballDamage = 16;

            // here SpawnProjectile is basically duplicated. except some functions that aren't needed.
            // following offsets are based on unit's width, height and center
            float tX, tY;
            tX = TargetX + 0.5f;
            tY = TargetY + 0.5f;

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

            Server.SpawnProjectileDirectional(AllodsProjectile.FireBall, Spell.User,    cX, cY, 0,
                                                                                        tX, tY, 0,
                                                                                        10,
                                                                                        (MapProjectile fproj) =>
                                                                                        {
                                                                                            //Debug.LogFormat("spell projectile hit!");
                                                                                            // done, make damage
                                                                                            DamageFlags spdf = SphereToDamageFlags(Spell);

                                                                                            // spawn explosion. serverside, since projectile hit callback is serverside as well.
                                                                                            Server.SpawnProjectileSimple(AllodsProjectile.Explosion, null, fproj.ProjectileX, fproj.ProjectileY, fproj.ProjectileZ);
                                                                                            
                                                                                            // apply damage over 3x3 cells around the fireball
                                                                                            for (int y = fproj.Y-1; y <= fproj.Y+1; y++)
                                                                                            {
                                                                                                for (int x = fproj.X-1; x <= fproj.X+1; x++)
                                                                                                {
                                                                                                    if (x < 0 || y < 0 || x >= MapLogic.Instance.Width || y >= MapLogic.Instance.Height)
                                                                                                        continue;

                                                                                                    int dmg = (int)((2f-(new Vector2(x-fproj.X, y-fproj.Y).magnitude)) * ballDamage);
                                                                                                    // damage in the center is approximately 1.4 or 1.6
                                                                                                    MapNode node = MapLogic.Instance.Nodes[x, y];
                                                                                                    for (int i = 0; i < node.Objects.Count; i++)
                                                                                                    {
                                                                                                        MapObject mo = node.Objects[i];
                                                                                                        if (!(mo is IVulnerable))
                                                                                                            continue;
                                                                                                        IVulnerable mov = (IVulnerable)mo;
                                                                                                        mov.TakeDamage(spdf, Spell.User, dmg);
                                                                                                        mo.DoUpdateInfo = true;
                                                                                                        mo.DoUpdateView = true;
                                                                                                        //Debug.LogFormat("{0} <- {1}", mo, dmg);
                                                                                                    }
                                                                                                }
                                                                                            }

                                                                                            fproj.Dispose();
                                                                                            MapLogic.Instance.Objects.Remove(fproj);
                                                                                        });

            return false;
        }
    }
}