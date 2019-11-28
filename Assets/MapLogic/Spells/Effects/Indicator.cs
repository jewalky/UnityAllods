using System;
using System.Collections.Generic;
using System.Reflection;

public class SpellIndicatorFlags : Attribute
{
    public UnitFlags Flags { get; private set; }
    public SpellIndicatorFlags(UnitFlags flags)
    {
        Flags = flags;
    }
}

namespace SpellEffects
{
    public class EffectIndicator
    {
        private static List<Type> Indicators = null;
        private static List<Type> FindIndicatorFromFlags(string ns, UnitFlags flags)
        {
            if (Indicators == null)
            {
                Indicators = new List<Type>();
                Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (Type type in types)
                {
                    SpellIndicatorFlags[] npi = (SpellIndicatorFlags[])type.GetCustomAttributes(typeof(SpellIndicatorFlags), false);
                    if (npi.Length <= 0)
                        continue;
                    Indicators.Add(type);
                }
            }

            List<Type> lv = new List<Type>();
            foreach (Type type in Indicators)
            {
                if (type.Namespace != ns)
                    continue;
                SpellIndicatorFlags[] npi = (SpellIndicatorFlags[])type.GetCustomAttributes(typeof(SpellIndicatorFlags), false);
                if ((npi[0].Flags & flags) != 0)
                    lv.Add(type);
            }

            return lv;
        }

        public static List<Type> FindIndicatorFromFlags(UnitFlags flags)
        {
            return FindIndicatorFromFlags("SpellEffects", flags);
        }

        public MapUnit Unit { get; private set; }
        // OnEnable
        // OnDisable
        // Process
        public EffectIndicator(MapUnit unit)
        {
            Unit = unit;
        }

        public virtual void OnEnable()
        {

        }

        public virtual void OnDisable()
        {

        }

        public virtual void Process()
        {

        }
    }
}
