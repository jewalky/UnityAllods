using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpellEffects
{
    class Darkness : TimedEffect
    {
        bool Attached = false;
        float Power = 0;

        public Darkness(int duration, float power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            // remove light effects
            List<Light> lights = unit.GetSpellEffects<Light>();
            for (int i = 0; i < lights.Count; i++)
                unit.RemoveSpellEffect(lights[i]);

            // replace less powerful effects
            List<Darkness> darks = unit.GetSpellEffects<Darkness>();

            foreach (Darkness h in darks)
            {
                if (Power > h.Power)
                {
                    h.Power = Power;
                    unit.UpdateItems();
                }

                h.Timer = 0;
                return false;
            }

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
            stats.ScanRange -= Power;
        }
    }
}
