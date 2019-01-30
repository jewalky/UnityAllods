using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace Spells
{
    [SpellProcId(Spell.Spells.Ice_Missile)]
    public class SpellProcIceMissile : SpellProcProjectileGeneric
    {
        public SpellProcIceMissile(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            SpawnProjectile(AllodsProjectile.IceMissile, 10, Spell.GetDamage());
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Poison_Cloud)]
    public class SpellProcPoisonCloud : SpellProc
    {
        public SpellProcPoisonCloud(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            // 5x5, but remove corners
            for (int y = TargetY - 2; y <= TargetY + 2; y++)
            {
                for (int x = TargetX - 2; x <= TargetX + 2; x++)
                {
                    // check for corner tiles :)
                    if ((x == TargetX - 2 || x == TargetX + 2) && (y == TargetY - 2 || y == TargetY + 2))
                        continue;
                    if (x < 0 || y < 0 ||
                        x >= MapLogic.Instance.Width ||
                        y >= MapLogic.Instance.Height) continue;

                    // check if there are FIRE effects on this cell, don't spawn fog
                    bool spawnblocked = false;
                    foreach (MapObject mo in MapLogic.Instance.Nodes[x, y].Objects)
                    {
                        if (!(mo is MapProjectile))
                            continue;

                        MapProjectile mp = (MapProjectile)mo;
                        if (mp.Class == null || mp.Class.ID != (int)AllodsProjectile.PoisonCloud)
                            continue;

                        // don't remove if on edge of fire wall
                        if (new Vector2(mp.ProjectileX - x + 0.5f, mp.ProjectileY - y + 0.5f).magnitude > 0.8f)
                            continue;

                        spawnblocked = true;
                        break;
                    }

                    if (spawnblocked)
                        continue;

                    Server.SpawnProjectileEOT(AllodsProjectile.PoisonCloud, Spell.User, x + 0.5f, y + 0.5f, 0, (int)(20 * Spell.GetDuration()), 40, 0, 0, 16, proj =>
                    {
                        DamageFlags spdf = SphereToDamageFlags(Spell);
                        // get projectile cells
                        int axFrom = Mathf.Max(0, Mathf.FloorToInt(proj.ProjectileX));
                        int axTo = Mathf.Min(MapLogic.Instance.Width - 1, Mathf.CeilToInt(proj.ProjectileX));
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
                                    if (!(mo is MapUnit))
                                        continue;
                                    MapUnit mov = (MapUnit)mo;
                                    int dmg = (int)(Spell.GetIndirectPower() * pdst);
                                    if (dmg <= 0)
                                        continue; // don't add null effects

                                    SpellEffects.Effect eff = new SpellEffects.Poison(this, dmg, 40 * 8); // originally 8 seconds
                                    mov.AddSpellEffects(eff);
                                }
                            }
                        }
                    });
                }
            }

            return false;
        }
    }

    [SpellProcId(Spell.Spells.Blizzard)]
    public class SpellProcBlizzard : SpellProc
    {
        public SpellProcBlizzard(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        private int TicsSinceLastDrop = -1;
        private int TicsTotal = 0;

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            if (TicsSinceLastDrop < 0 || TicsSinceLastDrop > 6)
            {
                TicsSinceLastDrop = 0;

                // spawn random blizzard projectile in 4x4 grid (center at 1,1)
                float rndX = UnityEngine.Random.Range(-1, 3) + TargetX;
                float rndY = UnityEngine.Random.Range(-1, 3) + TargetY;
                if (rndX >= 0 && rndY >= 0 &&
                    rndX < MapLogic.Instance.Width &&
                    rndY < MapLogic.Instance.Height)
                {
                    Server.SpawnProjectileDirectional(AllodsProjectile.Blizzard, Spell.User, rndX + 0.5f, rndY + 0.5f, 4f, rndX + 0.5f, rndY + 0.5f, 0f, 3f, proj =>
                    {
                        Server.SpawnProjectileSimple(AllodsProjectile.Blizzard, Spell.User, rndX + 0.5f, rndY + 0.5f, 0f, 1f, 1f, 0, 7);
                        // find something to damage in this cell
                        MapNode node = MapLogic.Instance.Nodes[proj.X, proj.Y];
                        for (int i = 0; i < node.Objects.Count; i++)
                        {
                            MapObject mo = node.Objects[i];
                            if (!(mo is IVulnerable))
                                continue;
                            IVulnerable vul = (IVulnerable)mo;
                            int dmg = Spell.GetDamage();
                            vul.TakeDamage(DamageFlags.Water|(Spell.Item==null?DamageFlags.AllowExp:0), Spell.User, dmg);
                        }
                    });
                }
            }

            TicsSinceLastDrop++;
            TicsTotal++;

            return (TicsTotal < 20*Spell.GetDuration());
        }
    }
}