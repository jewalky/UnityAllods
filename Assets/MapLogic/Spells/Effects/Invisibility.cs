using System.Collections.Generic;

namespace SpellEffects
{
    class Invisibility : TimedEffect
    {
        public Invisibility(int duration) : base(duration)
        {
            
        }

        public override bool OnAttach(MapUnit unit)
        {
            // always replace existing invisibility effects
            List<Invisibility> invs = unit.GetSpellEffects<Invisibility>();
            foreach (Invisibility i in invs)
                unit.RemoveSpellEffect(i);

            return true;
        }

        public override void OnDetach()
        {
            Unit.Flags &= ~UnitFlags.Invisible;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.Invisible;

            return true;
        }
    }
}
