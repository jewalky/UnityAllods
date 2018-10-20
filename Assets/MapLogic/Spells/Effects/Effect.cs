using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpellEffects
{
    public class Effect
    {
        public MapUnit Unit { get; private set; }

        public bool SetUnit(MapUnit unit)
        {
            if (Unit != null)
                return false;
            Unit = unit;
            return true;
        }

        // this method is called when effect is about to be attached to the unit
        // return true if should continue attaching.
        public virtual bool OnAttach(MapUnit unit)
        {
            return true;
        }

        // this method is called when effect is about to be detached to the unit
        public virtual void OnDetach()
        {

        }

        // this method is called every tic. returning false detaches this effect
        public virtual bool Process()
        {
            return false;
        }

        // this method is called on stat recalculation. for protection effects
        public virtual void ProcessStats(UnitStats stats)
        {
            
        }
    }

    public class TimedEffect : Effect
    {
        public int Duration { get; protected set; }
        public int Timer { get; protected set; }

        public TimedEffect(int duration)
        {
            Duration = duration;
            Timer = 0;
        }

        public override bool Process()
        {
            Timer++;
            if (Timer > Duration)
                return false;
            return true;
        }
    }
}
