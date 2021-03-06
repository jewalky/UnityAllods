﻿using System;
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
}