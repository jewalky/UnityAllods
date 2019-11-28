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
        public SpellProcLightning(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        protected void SpawnProjectile(MapUnit target, int damage, int color, float dst)
        {
            if (TargetUnit == null)
                return;

            // following offsets are based on unit's width, height and center
            float tX, tY;
            if (target != null)
            {
                tX = target.X + target.Width * 0.5f + target.FracX;
                tY = target.Y + target.Height * 0.5f + target.FracY;
            }
            else return;

            float cX, cY;
            if (Spell.User != null)
            {
                cX = Spell.User.X + Spell.User.Width * 0.5f + Spell.User.FracX;
                cY = Spell.User.Y + Spell.User.Height * 0.5f + Spell.User.FracY;
                Vector2 dir = new Vector2(tX - cX, tY - cY).normalized * ((Spell.User.Width + Spell.User.Height) / 2) / 1.5f;
                cX += dir.x * dst;
                cY += dir.y * dst;
            }
            else
            {
                cX = tX;
                cY = tY;
            }

            Server.SpawnProjectileLightning(Spell.User, cX, cY, 0,
                                            target,
                                            color,
                                            (MapProjectile fproj) =>
                                            {
                                                DamageFlags spdf = SphereToDamageFlags(Spell);
                                                if (Spell.Item == null)
                                                    spdf |= DamageFlags.AllowExp;
                                                target.TakeDamage(spdf, Spell.User, damage);
                                            });
        }

        public override bool Process()
        {
            SpawnProjectile(TargetUnit, Spell.GetDamage(), 0, 1);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Prismatic_Spray)]
    public class SpellProcPrismaticSpray : SpellProcLightning
    {
        public SpellProcPrismaticSpray(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            // look for 3-7 enemies nearby
            int maxTargets = Spell.GetIndirectPower();
            List<MapUnit> targets = new List<MapUnit>();
            int range = Mathf.CeilToInt(Spell.GetDistance());
            int fromX = Spell.User.X - range;
            int fromY = Spell.User.Y - range;
            int toX = Spell.User.X + Spell.User.Width + range;
            int toY = Spell.User.Y + Spell.User.Height + range;
            for (int y = fromY; y <= toY; y++)
            {
                if (y < 0 || y >= MapLogic.Instance.Height)
                    continue;
                for (int x = fromX; x <= toX; x++)
                {
                    if (x < 0 || x >= MapLogic.Instance.Width)
                        continue;

                    if (targets.Count >= maxTargets - 1)
                        break;

                    MapNode node = MapLogic.Instance.Nodes[x, y];
                    foreach (MapObject objnode in node.Objects)
                    {
                        if (objnode is MapUnit)
                        {
                            MapUnit unit = (MapUnit)objnode;
                            if ((Spell.User.Player.Diplomacy[unit.Player.ID] & DiplomacyFlags.Enemy) != 0) // in war with this unit, then add to list
                            {
                                if (!targets.Contains(unit))
                                    targets.Add(unit);
                            }
                        }
                    }
                }
            }

            // randomize units
            System.Random rng = new System.Random();
            targets = targets.Where(a => a != TargetUnit).OrderBy(a => rng.Next()).Take(maxTargets-1).ToList();
            targets.Insert(0, TargetUnit);

            for (int i = 0; i < targets.Count; i++)
            {
                SpawnProjectile(targets[i], Spell.GetDamage(), i + 1, 1f / targets.Count);
            }

            return false;
        }
    }

    [SpellProcId(Spell.Spells.Invisibility)]
    public class SpellProcInvisiblity : SpellProc
    {
        public SpellProcInvisiblity(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            SpellEffects.Effect eff = new SpellEffects.Invisibility(40*15); // 15 seconds invisible
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Light)]
    public class SpellProcLight : SpellProc
    {
        public SpellProcLight(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            // 11x11, but remove corners
            for (int y = TargetY - 5; y <= TargetY + 5; y++)
            {
                for (int x = TargetX - 5; x <= TargetX + 5; x++)
                {
                    // check for corner tiles :)
                    if (new Vector2(x - TargetX, y - TargetY).magnitude > 5.5f)
                        continue;
                    if (x < 0 || y < 0 ||
                        x >= MapLogic.Instance.Width ||
                        y >= MapLogic.Instance.Height) continue;

                    // remove darkness effects
                    MapNode node = MapLogic.Instance.Nodes[x, y];
                    for (int i = 0; i < node.Objects.Count; i++)
                    {
                        MapObject mo = node.Objects[i];
                        if (mo is MapProjectile &&
                            ((((MapProjectile)mo).ClassID == AllodsProjectile.SpecDarkness) || (((MapProjectile)mo).ClassID == AllodsProjectile.SpecLight)))
                        {
                            mo.Dispose();
                            i--;
                            continue;
                        }
                    }

                    float power = Spell.GetScanRange();
                    int lightlevel = (int)(64 + (power * 32));

                    Server.SpawnProjectileEOT(AllodsProjectile.SpecLight, Spell.User, x + 0.5f, y + 0.5f, 0, (int)(MapLogic.TICRATE * Spell.GetDuration()), MapLogic.TICRATE / 2, 0, 0, 16, proj =>
                    {
                        MapNode pnode = MapLogic.Instance.Nodes[proj.X, proj.Y];
                        for (int i = 0; i < pnode.Objects.Count; i++)
                        {
                            MapObject mo = pnode.Objects[i];
                            if (!(mo is MapUnit))
                                continue;

                            MapUnit mov = (MapUnit)mo;
                            // don't affect AI units
                            if (mov.Player == null || mov.Player.DoFullAI)
                                continue;

                            SpellEffects.Effect eff = new SpellEffects.Light(MapLogic.TICRATE / 2, Spell.GetScanRange()); // 1 second
                            mov.AddSpellEffects(eff);
                        }

                    }, lightlevel);
                }
            }

            return false;
        }
    }

    [SpellProcId(Spell.Spells.Darkness)]
    public class SpellProcDarkness : SpellProc
    {
        public SpellProcDarkness(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            // 11x11, but remove corners
            for (int y = TargetY - 5; y <= TargetY + 5; y++)
            {
                for (int x = TargetX - 5; x <= TargetX + 5; x++)
                {
                    // check for corner tiles :)
                    if (new Vector2(x - TargetX, y - TargetY).magnitude > 5.5f)
                        continue;
                    if (x < 0 || y < 0 ||
                        x >= MapLogic.Instance.Width ||
                        y >= MapLogic.Instance.Height) continue;

                    // remove light effects
                    MapNode node = MapLogic.Instance.Nodes[x, y];
                    for (int i = 0; i < node.Objects.Count; i++)
                    {
                        MapObject mo = node.Objects[i];
                        if (mo is MapProjectile &&
                            ((((MapProjectile)mo).ClassID == AllodsProjectile.SpecDarkness) || (((MapProjectile)mo).ClassID == AllodsProjectile.SpecLight)))
                        {
                            mo.Dispose();
                            i--;
                            continue;
                        }
                    }

                    float power = Spell.GetScanRange();
                    int lightlevel = (int)(-(power * 32));

                    Server.SpawnProjectileEOT(AllodsProjectile.SpecDarkness, Spell.User, x + 0.5f, y + 0.5f, 0, (int)(MapLogic.TICRATE * Spell.GetDuration()), MapLogic.TICRATE/2, 0, 0, 16, proj =>
                    {
                        MapNode pnode = MapLogic.Instance.Nodes[proj.X, proj.Y];
                        for (int i = 0; i < pnode.Objects.Count; i++)
                        {
                            MapObject mo = pnode.Objects[i];
                            if (!(mo is MapUnit))
                                continue;

                            MapUnit mov = (MapUnit)mo;
                            // don't affect AI units
                            if (mov.Player == null || mov.Player.DoFullAI)
                                continue;

                            SpellEffects.Effect eff = new SpellEffects.Darkness(MapLogic.TICRATE/2, Spell.GetScanRange()); // 1 second
                            mov.AddSpellEffects(eff);
                        }

                    }, lightlevel);
                }
            }

            return false;
        }
    }

    [SpellProcId(Spell.Spells.Protection_from_Air)]
    public class SpellProcProtectionAir : SpellProc
    {
        public SpellProcProtectionAir(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            SpellEffects.Effect eff = new SpellEffects.ProtectionAir((int)(MapLogic.TICRATE * Spell.GetDuration()), Spell.GetProtection());
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }
}