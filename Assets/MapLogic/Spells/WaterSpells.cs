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
            SpawnProjectile(AllodsProjectile.IceMissile, 10, 20);
            return false;
        }
    }
}