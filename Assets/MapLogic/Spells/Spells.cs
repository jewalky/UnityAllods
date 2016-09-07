using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

public class SpellProcId : Attribute
{
    public int SpellID { get; private set; }
    public SpellProcId(byte id)
    {
        SpellID = id;
    }
    public SpellProcId(Spell.Spells id)
    {
        SpellID = (int)id;
    }
}

namespace Spells
{
    public class SpellProc
    {
        private static List<Type> ProcTypes = null;
        private static Type FindProcTypeFromSpellId(string ns, int sid)
        {
            if (ProcTypes == null)
            {
                ProcTypes = new List<Type>();
                Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (Type type in types)
                {
                    SpellProcId[] npi = (SpellProcId[])type.GetCustomAttributes(typeof(SpellProcId), false);
                    if (npi.Length <= 0)
                        continue;
                    ProcTypes.Add(type);
                }
            }

            foreach (Type type in ProcTypes)
            {
                if (type.Namespace != ns)
                    continue;
                SpellProcId[] npi = (SpellProcId[])type.GetCustomAttributes(typeof(SpellProcId), false);
                if (npi[0].SpellID == sid)
                    return type;
            }

            return null;
        }

        public static Type FindProcTypeFromSpell(Spell spell)
        {
            return FindProcTypeFromSpellId("Spells", spell.SpellID);
        }

        protected readonly int TargetX;
        protected readonly int TargetY;
        protected readonly Spell Spell;
        protected readonly MapUnit TargetUnit;

        public SpellProc(Spell spell, int tgX, int tgY, MapUnit tgUnit)
        {
            Spell = spell;
            TargetX = tgX;
            TargetY = tgY;
            TargetUnit = tgUnit;
        }

        public virtual bool Process()
        {
            return false;
        }
    }

    [SpellProcId(Spell.Spells.Fire_Arrow)]
    public class SpellProcFireBall : SpellProc
    {
        public SpellProcFireBall(Spell spell, int tgX, int tgY, MapUnit tgUnit) : base(spell, tgX, tgY, tgUnit) { }

        public override bool Process()
        {
            Debug.LogFormat("FIREARROW");
            return false;
        }
    }
}