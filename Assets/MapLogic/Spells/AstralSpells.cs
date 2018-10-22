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
                unit.SetPosition(utpX, utpY);
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

            SpellEffects.Effect eff = new SpellEffects.Haste((int)(20 * Spell.GetDuration()), Spell.GetSpeed());
            TargetUnit.AddSpellEffects(eff);
            return false;
        }
    }
}