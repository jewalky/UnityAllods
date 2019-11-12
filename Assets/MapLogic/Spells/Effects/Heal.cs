using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpellEffects
{
    class Heal : TimedEffect
    {
        bool Attached = false;
        int Power = 0;

        public Heal(int duration, int power) : base(duration)
        {
            Attached = false;
            Power = power;
        }

        public override bool OnAttach(MapUnit unit)
        {
            // always replace existing heal effects
            List<Heal> heals = unit.GetSpellEffects<Heal>();
            foreach (Heal h in heals)
                unit.RemoveSpellEffect(h);
            unit.Stats.TrySetHealth(unit.Stats.Health + Power);
            return true;
        }

        public override void OnDetach()
        {
            Unit.Flags &= ~UnitFlags.Healing;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.Healing;

            if (!Attached)
            {
                Attached = true;
            }

            return true;
        }
    }

    [SpellIndicatorFlags(UnitFlags.Healing)]
    public class HealingIndicator : EffectIndicator
    {
        public HealingIndicator(MapUnit unit) : base(unit) { }

        private class HealingProjectileLogic : IMapProjectileLogic
        {
            MapProjectile Projectile;
            MapUnit Unit;
            float Height;
            float X;
            float Y;
            float Z;
            float Frac = 0;

            public HealingProjectileLogic(float height, float x, float y, float z, MapUnit unit)
            {
                Height = height;
                X = x;
                Y = y;
                Z = z;
                Unit = unit;
            }

            public void SetProjectile(MapProjectile proj)
            {
                Projectile = proj;
            }

            public bool Update()
            {
                if (!Unit.IsLinked || !Unit.IsAlive)
                    return false;
                float cX = Unit.X + Unit.FracX + Unit.Width / 2f;
                float cY = Unit.Y + Unit.FracY + Unit.Height / 2f;
                float zatUnit = MapLogic.Instance.GetHeightAt(Unit.X + Unit.FracX + (float)Unit.Width / 2,
                                                              Unit.Y + Unit.FracY + (float)Unit.Height / 2,
                                                              Unit.Width, Unit.Height) / 32;
                Frac += 5f / MapLogic.TICRATE;
                Projectile.SetPosition(cX+X, cY+Y, zatUnit + Z + Frac * Height);
                Projectile.CurrentFrame = Math.Min(7, (int)(Frac * 8));
                if (Frac > 1f)
                    return false;
                return true;
            }
        }

        public override void Process()
        {

            int meanWh = (Unit.Width + Unit.Height) / 2;
            for (int i = 0; i < meanWh * 2; i++)
            {
                float rX = Unit.Width / 2f;
                float rY = Unit.Height / 4f;

                float randRad = UnityEngine.Random.Range(0.0f, 1.0f) * Mathf.PI * 2;

                float pX = -(Mathf.Cos(randRad) * rX);
                float pY = -(Mathf.Sin(randRad) * rY);

                float cZ = -(meanWh / 2f);

                MapProjectile item = new MapProjectile(AllodsProjectile.Healing, Unit, new HealingProjectileLogic((Unit.Width+Unit.Height)/2f, pX, pY, cZ, Unit));
                item.ZOffset = -64;
                if (Unit.IsFlying)
                    item.ZOffset += 128;
                item.SetPosition(pX, pY, 0);
                item.ZAbsolute = true;
                MapLogic.Instance.Objects.Add(item);
            }
        }
    }
}
