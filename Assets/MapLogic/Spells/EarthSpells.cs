using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace Spells
{
    [SpellProcId(Spell.Spells.Diamond_Dust)]
    public class SpellProcDiamondDust : SpellProcProjectileGeneric
    {
        public SpellProcDiamondDust(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            SpawnProjectile(AllodsProjectile.DiamondDust, 10, Spell.GetDamage());
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Protection_from_Earth)]
    public class SpellProcProtectionEarth : SpellProc
    {
        public SpellProcProtectionEarth(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (TargetUnit == null)
                return false;

            SpellEffects.Effect eff = new SpellEffects.ProtectionEarth((int)(MapLogic.TICRATE * Spell.GetDuration()), Spell.GetProtection());
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Wall_of_Earth)]
    public class SpellProcWallOfEarth : SpellProc
    {
        public SpellProcWallOfEarth(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            if (NetworkManager.IsClient)
                return false;

            // we need to spawn several EoT projectiles at the destination
            // 5x2

            float sourceX = Spell.User.X + Spell.User.FracX + Spell.User.Width / 2f;
            float sourceY = Spell.User.Y + Spell.User.FracY + Spell.User.Height / 2f;
            Vector2 direction = new Vector2(TargetX + 0.5f - sourceX, TargetY + 0.5f - sourceY);
            float fortyFiveDegreesAsRadians = 0.785398163f;
            float angle = Mathf.Round((Mathf.Atan2(direction.y, direction.x) - (Mathf.PI / 180) * 90) / fortyFiveDegreesAsRadians) * fortyFiveDegreesAsRadians;
            bool is45deg = !((Mathf.Abs(angle) % (fortyFiveDegreesAsRadians * 2)) < Mathf.Epsilon);

            if (is45deg)
            {
                for (int y = 0; y < 2; y++)
                {
                    int xStart = 0;
                    int xEnd = 0;
                    bool isYNeg = Mathf.Cos(angle) < 0;
                    bool isXNeg = Mathf.Sin(angle) < 0;
                    if (y == 1)
                    {
                        if (isYNeg != isXNeg)
                            xStart = 1;
                        else xEnd = 1;
                    }
                    for (int x = -3 + xStart; x <= 3 - xEnd; x++)
                    {
                        // rotate x and y
                        float fx = x;
                        float fy = y;
                        float rx = Mathf.Cos(angle) * x;
                        float ry = Mathf.Sin(angle) * x;
                        float yOffset = isYNeg ? -y : y;
                        Server.SpawnProjectileEOT(AllodsProjectile.EarthWall, Spell.User, Mathf.Round(TargetX + rx) + 0.5f, Mathf.Round(TargetY + ry + yOffset) + 0.5f, 0, (int)(MapLogic.TICRATE * Spell.GetDuration()), -1, 0, 0, 16);
                    }
                }
            }
            else
            {
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        // rotate x and y
                        float fx = x;
                        float fy = y;
                        float rx = Mathf.Cos(angle) * x - Mathf.Sin(angle) * y;
                        float ry = Mathf.Cos(angle) * y + Mathf.Sin(angle) * x;
                        Server.SpawnProjectileEOT(AllodsProjectile.EarthWall, Spell.User, Mathf.Round(TargetX + rx) + 0.5f, Mathf.Round(TargetY + ry) + 0.5f, 0, (int)(MapLogic.TICRATE * Spell.GetDuration()), -1, 0, 0, 16);
                    }
                }
            }

            return false;
        }
    }
}