using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpellEffects
{
    class Poison : TimedEffect
    {
        // this field is effectively unique spell id.
        // we use it to make sure that we don't infinitely stack poison cloud effect for same spell, but multiple poison clouds stack
        Spells.SpellProc ParentProc;
        int Damage;

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

        public override bool Process()
        {
            if (!base.Process())
                return false;

            if (MapLogic.Instance.LevelTime % 40 == 0)
            {
                Unit.TakeDamage(Spells.SpellProc.SphereToDamageFlags(ParentProc.Spell), ParentProc.Spell.User, Damage);
            }

            return true;
        }
    }
}
