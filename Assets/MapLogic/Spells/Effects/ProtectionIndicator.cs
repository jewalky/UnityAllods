using System.Collections.Generic;
using UnityEngine;

namespace SpellEffects
{
    [SpellIndicatorFlags(UnitFlags.ProtectionFire|UnitFlags.ProtectionAir|UnitFlags.ProtectionWater|UnitFlags.ProtectionEarth)]
    public class ProtectionsIndicator : EffectIndicator
    {
        public ProtectionsIndicator(MapUnit unit) : base(unit) { }

        UnitFlags LastFlags;
        List<MapProjectile> Indicators;

        public override void OnEnable()
        {
            LastFlags = 0;
            Indicators = new List<MapProjectile>();
        }

        public override void OnDisable()
        {
            for (int i = 0; i < Indicators.Count; i++)
                Indicators[i].Dispose();
            Indicators.Clear();
            LastFlags = 0;
        }

        private void CreateProjectile(AllodsProjectile proj)
        {
            MapProjectile p = new MapProjectile(proj, Unit);
            p.ZOffset = 64;
            MapLogic.Instance.AddObject(p, true);
            Indicators.Add(p);
        }

        public override void Process()
        {
            if (Unit.Flags != LastFlags)
            {
                UnitFlags leftFlags = Unit.Flags;

                // remove unused projectiles
                for (int i = 0; i < Indicators.Count; i++)
                {
                    UnitFlags flag = 0;
                    switch (Indicators[i].ClassID)
                    {
                        case AllodsProjectile.ProtectionFire:
                            flag = UnitFlags.ProtectionFire;
                            break;
                        case AllodsProjectile.ProtectionWater:
                            flag = UnitFlags.ProtectionWater;
                            break;
                        case AllodsProjectile.ProtectionAir:
                            flag = UnitFlags.ProtectionAir;
                            break;
                        case AllodsProjectile.ProtectionEarth:
                            flag = UnitFlags.ProtectionEarth;
                            break;
                    }

                    if ((Unit.Flags & flag) == 0)
                    {
                        Indicators[i].Dispose();
                        i--;
                        continue;
                    }
                    else
                    {
                        leftFlags &= ~flag;
                    }
                }

                if (leftFlags.HasFlag(UnitFlags.ProtectionAir))
                    CreateProjectile(AllodsProjectile.ProtectionAir);
                if (leftFlags.HasFlag(UnitFlags.ProtectionWater))
                    CreateProjectile(AllodsProjectile.ProtectionWater);
                if (leftFlags.HasFlag(UnitFlags.ProtectionEarth))
                    CreateProjectile(AllodsProjectile.ProtectionEarth);
                if (leftFlags.HasFlag(UnitFlags.ProtectionFire))
                    CreateProjectile(AllodsProjectile.ProtectionFire);
                LastFlags = Unit.Flags;
            }

            for (int i = 0; i < Indicators.Count; i++)
            {
                float indicatorAngle = (Mathf.PI*2) / Indicators.Count * i + Mathf.PI * 0.5f;
                MapProjectile p = Indicators[i];

                float pX = -Mathf.Cos(indicatorAngle) * 0.25f;
                float pY = Mathf.Sin(indicatorAngle) * 0.25f;

                if (Indicators.Count == 1)
                    pY = -pY;

                float pZ = MapLogic.Instance.GetHeightAt(Unit.X + Unit.FracX, Unit.Y + Unit.FracY, Unit.Width, Unit.Height) / 32
                    + (1f + (((Unit.Width + Unit.Height) / 2f) - 1f) / 2f);

                p.Alpha = (Unit.GetVisibility() == 2) ? 1f : 0;
                p.SetPosition(Unit.X + Unit.FracX + Unit.Width / 2f + pX, Unit.Y + Unit.FracY + Unit.Height / 2f + pY, pZ);
                p.ZAbsolute = true;
                p.ZOffset = -32;
                if (Unit.IsFlying)
                    p.ZOffset += 128;
                p.CurrentFrame = (p.CurrentFrame + 1) % p.Class.Phases;
                p.RenderViewVersion++;
            }
        }
    }
}
