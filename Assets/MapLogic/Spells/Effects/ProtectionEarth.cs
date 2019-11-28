using System.Collections.Generic;

namespace SpellEffects
{
    class ProtectionEarth : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public ProtectionEarth(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            List<ProtectionEarth> ps = unit.GetSpellEffects<ProtectionEarth>();

            foreach (ProtectionEarth p in ps)
            {
                if (p.Power > Power)
                    return false;
            }

            foreach (ProtectionEarth p in ps)
                unit.RemoveSpellEffect(p);

            return true;
        }

        public override void OnDetach()
        {
            Unit.UpdateItems();
            Unit.Flags &= ~UnitFlags.ProtectionEarth;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.ProtectionEarth;

            if (!Attached)
            {
                Attached = true;
                Unit.UpdateItems();
            }

            return true;
        }

        public override void ProcessStats(UnitStats stats)
        {
            stats.ProtectionEarth += (byte)Power;
        }
    }
}
