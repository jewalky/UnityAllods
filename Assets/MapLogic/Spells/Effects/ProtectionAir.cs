using System.Collections.Generic;

namespace SpellEffects
{
    class ProtectionAir : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public ProtectionAir(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            List<ProtectionAir> ps = unit.GetSpellEffects<ProtectionAir>();

            foreach (ProtectionAir p in ps)
            {
                if (p.Power > Power)
                    return false;
            }

            foreach (ProtectionAir p in ps)
                unit.RemoveSpellEffect(p);

            return true;
        }

        public override void OnDetach()
        {
            Unit.UpdateItems();
            Unit.Flags &= ~UnitFlags.ProtectionAir;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.ProtectionAir;

            if (!Attached)
            {
                Attached = true;
                Unit.UpdateItems();
            }

            return true;
        }

        public override void ProcessStats(UnitStats stats)
        {
            stats.ProtectionAir += (byte)Power;
        }
    }
}
