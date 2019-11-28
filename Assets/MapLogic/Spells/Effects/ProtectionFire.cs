using System.Collections.Generic;

namespace SpellEffects
{
    class ProtectionFire : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public ProtectionFire(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            List<ProtectionFire> ps = unit.GetSpellEffects<ProtectionFire>();

            foreach (ProtectionFire p in ps)
            {
                if (p.Power > Power)
                    return false;
            }

            foreach (ProtectionFire p in ps)
                unit.RemoveSpellEffect(p);

            return true;
        }

        public override void OnDetach()
        {
            Unit.UpdateItems();
            Unit.Flags &= ~UnitFlags.ProtectionFire;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.ProtectionFire;

            if (!Attached)
            {
                Attached = true;
                Unit.UpdateItems();
            }

            return true;
        }

        public override void ProcessStats(UnitStats stats)
        {
            stats.ProtectionFire += (byte)Power;
        }
    }
}
