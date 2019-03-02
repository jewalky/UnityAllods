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
            if (NetworkManager.IsClient)
                return false;

            SpawnProjectile(AllodsProjectile.FireArrow, 10, Spell.GetDamage());
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Fire_Ball)]
    public class SpellProcFireBall : SpellProc
    {
        public SpellProcFireBall(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            int ballDamage = Spell.GetDamage();

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
                                                                                            if (Spell.Item == null)
                                                                                                spdf |= DamageFlags.AllowExp;

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
                                                                                                        {
                                                                                                            // aoe fire effect: remove cloud effects if any
                                                                                                            if (!(mo is MapProjectile))
                                                                                                                continue;

                                                                                                            MapProjectile mp = (MapProjectile)mo;
                                                                                                            if (mp.Class == null || mp.Class.ID != (int)AllodsProjectile.PoisonCloud)
                                                                                                                continue;

                                                                                                            // don't remove if on edge of fire wall
                                                                                                            if (new Vector2(mp.ProjectileX - fproj.ProjectileX, mp.ProjectileY - fproj.ProjectileY).magnitude > 1.5f)
                                                                                                                continue;

                                                                                                            mp.Dispose();
                                                                                                            i--;
                                                                                                            continue;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            IVulnerable mov = (IVulnerable)mo;
                                                                                                            mov.TakeDamage(spdf, Spell.User, dmg);
                                                                                                        }
                                                                                                    }
                                                                                                }
                                                                                            }

                                                                                            fproj.Dispose();
                                                                                        });

            return false;
        }
    }

    [SpellProcId(Spell.Spells.Wall_of_Fire)]
    public class SpellProcFireWall : SpellProc
    {
        public SpellProcFireWall(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            // we need to spawn several EoT projectiles at the destination
            // 5x2

            float sourceX = Spell.User.X + Spell.User.FracX + Spell.User.Width / 2f;
            float sourceY = Spell.User.Y + Spell.User.FracY + Spell.User.Height / 2f;
            Vector2 direction = new Vector2(TargetX + 0.5f - sourceX, TargetY + 0.5f - sourceY);
            float angle = Mathf.Atan2(direction.y, direction.x) - (Mathf.PI/180)*90;

            for (int x = -2; x <= 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    // rotate x and y
                    float fx = x;
                    float fy = y;
                    float rx = Mathf.Cos(angle) * x - Mathf.Sin(angle) * y;
                    float ry = Mathf.Cos(angle) * y + Mathf.Sin(angle) * x;
                    Server.SpawnProjectileEOT(AllodsProjectile.FireWall, Spell.User, TargetX+rx+0.5f, TargetY+ry+0.5f, 0, (int)(MapLogic.TICRATE * Spell.GetDuration()), 10, 4, 4, 16, proj =>
                    {
                        DamageFlags spdf = SphereToDamageFlags(Spell);
                        if (Spell.Item == null)
                            spdf |= DamageFlags.AllowExp;
                        // get projectile cells
                        int axFrom = Mathf.Max(0, Mathf.FloorToInt(proj.ProjectileX));
                        int axTo = Mathf.Min(MapLogic.Instance.Width-1, Mathf.CeilToInt(proj.ProjectileX));
                        int ayFrom = Mathf.Max(0, Mathf.FloorToInt(proj.ProjectileY));
                        int ayTo = Mathf.Min(MapLogic.Instance.Height - 1, Mathf.CeilToInt(proj.ProjectileY));
                        for (int py = ayFrom; py <= ayTo; py++)
                        {
                            for (int px = axFrom; px <= axTo; px++)
                            {
                                // check how much projectile is on this cell
                                float pdst = 1f - Mathf.Min(1f, new Vector2(px + 0.5f - proj.ProjectileX, py + 0.5f - proj.ProjectileY).magnitude);
                                // 0..1 effect power
                                MapNode node = MapLogic.Instance.Nodes[px, py];
                                for (int i = 0; i < node.Objects.Count; i++)
                                {
                                    MapObject mo = node.Objects[i];
                                    if (!(mo is IVulnerable))
                                    {
                                        // aoe fire effect: remove cloud effects if any
                                        if (!(mo is MapProjectile))
                                            continue;

                                        MapProjectile mp = (MapProjectile)mo;
                                        if (mp.Class == null || mp.Class.ID != (int)AllodsProjectile.PoisonCloud)
                                            continue;

                                        // don't remove if on edge of fire wall
                                        if (new Vector2(mp.ProjectileX - proj.ProjectileX, mp.ProjectileY - proj.ProjectileY).magnitude > 0.8f)
                                            continue;

                                        mp.Dispose();
                                        i--;
                                        continue;
                                    }
                                    else
                                    {
                                        IVulnerable mov = (IVulnerable)mo;
                                        int dmg = (int)(Spell.GetDamage() * pdst);
                                        mov.TakeDamage(spdf, Spell.User, dmg);
                                    }
                                }
                            }
                        }
                    });
                }
            }

            return false;
        }
    }
}