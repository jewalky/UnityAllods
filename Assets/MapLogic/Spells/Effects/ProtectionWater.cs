using System.Collections.Generic;

namespace SpellEffects
{
    class ProtectionWater : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public ProtectionWater(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            List<ProtectionWater> ps = unit.GetSpellEffects<ProtectionWater>();

            foreach (ProtectionWater p in ps)
            {
                if (p.Power > Power)
                    return false;
            }

            foreach (ProtectionWater p in ps)
                unit.RemoveSpellEffect(p);

            return true;
        }

        public override void OnDetach()
        {
            Unit.UpdateItems();
            Unit.Flags &= ~UnitFlags.ProtectionWater;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.ProtectionWater;

            if (!Attached)
            {
                Attached = true;
                Unit.UpdateItems();
            }

            return true;
        }

        public override void ProcessStats(UnitStats stats)
        {
            stats.ProtectionWater += (byte)Power;
        }
    }
}
