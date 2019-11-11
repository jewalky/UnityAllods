using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpellEffects
{
    class Shield : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public Shield(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            // always replace existing shield effects
            List<Shield> shields = unit.GetSpellEffects<Shield>();

            foreach (Shield s in shields)
            {
                if (s.Power > Power)
                    return false;
            }

            foreach (Shield h in shields)
                unit.RemoveSpellEffect(h);

            return true;
        }

        public override void OnDetach()
        {
            Unit.UpdateItems();
            Unit.Flags &= ~UnitFlags.Shield;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.Shield;

            if (!Attached)
            {
                Attached = true;
                Unit.UpdateItems();
            }

            return true;
        }

        public override void ProcessStats(UnitStats stats)
        {
            stats.Absorbtion += (byte)Power;
        }
    }

    [SpellIndicatorFlags(UnitFlags.Shield)]
    public class ShieldIndicator : EffectIndicator
    {
        public ShieldIndicator(MapUnit unit) : base(unit) { }

        private List<MapProjectile> Grid = new List<MapProjectile>();
        private List<MapProjectile> Circles = new List<MapProjectile>();
        private int Timer = 0;
        private int Scalar = 1;

        public override void OnEnable()
        {
            Scalar = (Unit.Width + Unit.Height) / 2;
            if (Scalar < 1) Scalar = 1;
            for (int i = 0; i < Scalar*16*2; i++)
            {
                MapProjectile item = new MapProjectile(AllodsProjectile.Shield, Unit);
                MapLogic.Instance.Objects.Add(item);
                Grid.Add(item);
            }
            for (int i = 0; i < Scalar*16 *2; i++)
            {
                MapProjectile item = new MapProjectile(AllodsProjectile.Shield, Unit);
                MapLogic.Instance.Objects.Add(item);
                Circles.Add(item);
            }
        }

        public override void OnDisable()
        {
            foreach (MapProjectile proj in Grid)
                proj.Dispose();
            foreach (MapProjectile proj in Circles)
                proj.Dispose();
            Grid.Clear();
            Circles.Clear();
        }

        public override void Process()
        {
            Timer++;
            float zatUnit = MapLogic.Instance.GetHeightAt(Unit.X + Unit.FracX + (float)Unit.Width / 2,
                                                          Unit.Y + Unit.FracY + (float)Unit.Height / 2,
                                                          Unit.Width, Unit.Height) / 32;
            float heightOffset = Mathf.Max(1, ((Unit.Width + Unit.Height) / 4f));
            float shieldRadius = 0.5f * Mathf.Max(1, ((Unit.Width + Unit.Height) / 2f));
            float shieldHeight = 0.6f * heightOffset;
            Vector3 initialPos = new Vector3(Unit.X + Unit.FracX + Unit.Width / 2f, Unit.Y + Unit.FracY + Unit.Height / 2f, 0.1f * heightOffset+zatUnit);
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < Scalar*16; i++)
                {
                    MapProjectile item = Grid[i+Scalar*16*j];
                    Vector3 particlePos = new Vector3(0, 0, 1);
                    Vector3 pos = Quaternion.Euler(0, 0, Timer * 2 + 90 * j) * Quaternion.Euler(0, i * 360 / (Scalar*16), 0) * particlePos;
                    item.SetPosition(pos.x * shieldRadius + initialPos.x, pos.y * shieldRadius + initialPos.y, pos.z * shieldHeight + initialPos.z);
                    item.ZOffset = (int)(pos.z * 32);
                    if (Unit.IsFlying)
                        item.ZOffset += 128;
                    item.CurrentFrame = 2 + Mathf.RoundToInt((1f - Mathf.Abs(pos.z)) * 2);
                    item.ZAbsolute = true;
                    item.DoUpdateView = true;
                }
            }
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < Scalar*16; i++)
                {
                    MapProjectile item = Circles[i+Scalar*16*j];
                    Vector3 particlePos = new Vector3(0, 1, 0);
                    Vector3 pos = Quaternion.Euler(0, 0, i * 360 / (Scalar*16)) * Quaternion.Euler(Timer*2 + 180*j, 0, 0) * particlePos;
                    item.SetPosition(pos.x * shieldRadius + initialPos.x, pos.y * shieldRadius + initialPos.y, pos.z * shieldHeight + initialPos.z);
                    item.ZOffset = (int)(pos.z * 32);
                    if (Unit.IsFlying)
                        item.ZOffset += 128;
                    item.CurrentFrame = 2 + Mathf.RoundToInt((1f - Mathf.Abs(pos.z)) * 2);
                    item.ZAbsolute = true;
                    item.DoUpdateView = true;
                }
            }
        }
    }
}
