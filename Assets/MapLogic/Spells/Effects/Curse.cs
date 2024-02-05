using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpellEffects
{
    class Curse : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public Curse(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            List<Curse> curses = unit.GetSpellEffects<Curse>();

            foreach (Curse c in curses)
            {
                if (c.Power > Power)
                    return false;
            }

            // If opposite spell effect is found, remove it, but do not apply this one.
            List<Bless> blessings = unit.GetSpellEffects<Bless>();

            if (blessings.Count > 0)
            {
                foreach (Bless b in blessings)
                    unit.RemoveSpellEffect(b);
                return false;
            }

            foreach (Curse c in curses)
                unit.RemoveSpellEffect(c);

            return true;
        }

        public override void OnDetach()
        {
            Unit.UpdateItems();
            Unit.Flags &= ~UnitFlags.Curse;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.Curse;

            if (!Attached)
            {
                Attached = true;
                Unit.UpdateItems();
            }

            return true;
        }

        public override void ProcessStats(UnitStats stats)
        {
            stats.Curse += (byte)Power;
        }
    }

    [SpellIndicatorFlags(UnitFlags.Curse)]
    public class CurseIndicator : EffectIndicator
    {
        public CurseIndicator(MapUnit unit) : base(unit) { }

        private List<MapProjectile> Circle = new List<MapProjectile>();
        private int Timer = 0;
        private int Scalar = 1;

        public override void OnEnable()
        {
            Scalar = (Unit.Width + Unit.Height) / 2;
            if (Scalar < 1) Scalar = 1;
            for (int i = 0; i < Scalar*20; i++)
            {
                MapProjectile item = new MapProjectile(AllodsProjectile.Curse, Unit);
                MapLogic.Instance.AddObject(item, true);
                Circle.Add(item);
            }
        }

        public override void OnDisable()
        {
            foreach (MapProjectile proj in Circle)
                proj.Dispose();
            Circle.Clear();
        }

        public override void Process()
        {
            Timer++;
            float zatUnit = MapLogic.Instance.GetHeightAt(Unit.X + Unit.FracX + (float)Unit.Width / 2,
                                                          Unit.Y + Unit.FracY + (float)Unit.Height / 2,
                                                          Unit.Width, Unit.Height) / 32;
            float heightOffset = Mathf.Max(1, ((Unit.Width + Unit.Height) / 2f));
            float effectRadius = 0.6f * heightOffset;
            float effectHeight = 0.6f * heightOffset;
            Vector3 initialPos = new Vector3(Unit.X + Unit.FracX + Unit.Width / 2f, Unit.Y + Unit.FracY + Unit.Height / 2f, effectHeight+zatUnit);
            float maxItems = (Circle.Count / 5) * 5;
            for (int i = 0; i < Circle.Count; i++)
            {
                MapProjectile item = Circle[i];
                Vector3 particlePos = new Vector3(0, 1, 0);
                Vector3 pos = Quaternion.Euler(0, 0, i * 360 / Circle.Count) * particlePos;
                item.SetPosition(pos.x * effectRadius + initialPos.x, pos.y * effectRadius + initialPos.y, pos.z + initialPos.z);
                item.ZOffset = -64;
                if (Unit.IsFlying)
                    item.ZOffset += 128;
                item.CurrentFrame = 2 + Mathf.RoundToInt((1f - Mathf.Abs(pos.z)) * 2);
                item.ZAbsolute = true;
                item.RenderViewVersion++;

                // this ensures that blessing effect always has N full trails (intensity 5 to intensity 0) instead of cutting mid-trail if the effect size is not enough.
                int actualPositionRounded = (int)((float)i / Circle.Count * maxItems);
                item.CurrentFrame = 4 - Mathf.RoundToInt((Timer * 0.75f) + actualPositionRounded) % 5;
            }
        }
    }
}
