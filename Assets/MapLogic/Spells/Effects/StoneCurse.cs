using System.Collections.Generic;

namespace SpellEffects
{
    class StoneCurse : TimedEffect
    {
        public StoneCurse(int duration) : base(duration) { }

        public override bool OnAttach(MapUnit unit)
        {
            List<StoneCurse> curses = unit.GetSpellEffects<StoneCurse>();

            foreach (StoneCurse c in curses)
            {
                if (c.Duration > Duration)
                    return false;
            }

            foreach (StoneCurse c in curses)
                unit.RemoveSpellEffect(c);

            unit.Flags |= UnitFlags.StoneCurse;

            return true;
        }
    
        public override void OnDetach()
        {
            Unit.Flags &= ~UnitFlags.StoneCurse;
        }
    }
}
