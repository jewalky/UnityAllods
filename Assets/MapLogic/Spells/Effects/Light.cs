using System.Collections.Generic;

namespace SpellEffects
{
    class Light : TimedEffect
    {
        bool Attached = false;
        float Power = 0;

        public Light(int duration, float power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            // remove darkness effects
            List<Darkness> darks = unit.GetSpellEffects<Darkness>();
            for (int i = 0; i < darks.Count; i++)
                unit.RemoveSpellEffect(darks[i]);

            // replace less powerful effects
            List<Light> lights = unit.GetSpellEffects<Light>();

            foreach (Light h in lights)
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
            stats.ScanRange += Power;
        }
    }
}
