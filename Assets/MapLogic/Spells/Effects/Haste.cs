using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpellEffects
{
    class Haste : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public Haste(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            // always replace existing haste effects
            List<Haste> hastes = unit.GetSpellEffects<Haste>();

            foreach (Haste h in hastes)
            {
                if (h.Power > Power)
                    return false;
            }

            foreach (Haste h in hastes)
                unit.RemoveSpellEffect(h);

            return true;
        }

        public override void OnDetach()
        {
            Unit.UpdateItems();
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            if (!Attached)
            {
                Attached = true;
                Unit.UpdateItems();
            }

            return true;
        }

        public override void ProcessStats(UnitStats stats)
        {
            stats.Speed += (byte)Power;
        }
    }
}
