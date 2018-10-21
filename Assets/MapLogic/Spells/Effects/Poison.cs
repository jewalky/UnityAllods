using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpellEffects
{
    class Poison : TimedEffect
    {
        // this field is effectively unique spell id.
        // we use it to make sure that we don't infinitely stack poison cloud effect for same spell, but multiple poison clouds stack
        Spells.SpellProc ParentProc;
        int Damage;

        // indicator
        MapProjectile Indicator;

        public Poison(Spells.SpellProc parentProc, int damage, int duration) : base(duration)
        {
            ParentProc = parentProc;
            Damage = damage;
        }

        public override bool OnAttach(MapUnit unit)
        {
            // check for existing poison from this cloud. if so, reset timer/duration
            List<Poison> poisons = unit.GetSpellEffects<Poison>();
            foreach (Poison p in poisons)
            {
                if (p.ParentProc == ParentProc)
                {
                    p.Timer = 0;
                    if (Duration > p.Duration)
                        p.Duration = Duration;
                    if (Damage > p.Damage)
                        p.Damage = Damage;
                    return false;
                }
            }

            return true;
        }

        public override void OnDetach()
        {
            if (Indicator != null)
                Indicator.Dispose();
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            if (MapLogic.Instance.LevelTime % 40 == 0)
            {
                Unit.TakeDamage(Spells.SpellProc.SphereToDamageFlags(ParentProc.Spell), ParentProc.Spell.User, Damage);
            }

            // only one instance of Poison should render the indicator. check for this
            List<Poison> poisons = Unit.GetSpellEffects<Poison>();
            if (this == poisons[0] && Indicator == null)
            {
                Indicator = new MapProjectile(AllodsProjectile.PoisonSign, Unit);
                Indicator.ZOffset = 64;
                MapLogic.Instance.Objects.Add(Indicator);
            }
            else if (this != poisons[0] && Indicator != null)
            {
                Indicator.Dispose();
                Indicator = null;
            }

            if (Indicator != null)
            {
                Indicator.SetPosition(Unit.X + Unit.FracX + Unit.Width / 2f, Unit.Y + Unit.FracY + Unit.Height / 2f, 1f+(((Unit.Width+Unit.Height)/2f)-1f) / 2f);
                Indicator.CurrentFrame = (Indicator.CurrentFrame + 1) % Indicator.Class.Phases;
                Indicator.DoUpdateView = true;
            }

            return true;
        }
    }
}
