using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpellEffects
{
    class Drain : TimedEffect
    {
        bool Attached = false;
        int Power = 0;
        MapUnit Source = null;

        public Drain(int duration, int power, MapUnit source) : base(duration)
        {
            Attached = false;
            Power = power;
            Source = source;
        }

        public override bool OnAttach(MapUnit unit)
        {
            // always replace existing drain effects
            List<Drain> drains = unit.GetSpellEffects<Drain>();
            foreach (Drain h in drains)
                unit.RemoveSpellEffect(h);
            int dmg = unit.TakeDamage(DamageFlags.Astral, Source, Power);
            if (dmg > 0 && Source != null)
                Source.Stats.TrySetHealth(Source.Stats.Health + dmg);
            return true;
        }

        public override void OnDetach()
        {
            Unit.Flags &= ~UnitFlags.Draining;
        }

        public override bool Process()
        {
            if (!base.Process())
                return false;

            Unit.Flags |= UnitFlags.Draining;

            if (!Attached)
            {
                Attached = true;
            }

            return true;
        }
    }

    [SpellIndicatorFlags(UnitFlags.Draining)]
    public class DrainingIndicator : EffectIndicator
    {
        public DrainingIndicator(MapUnit unit) : base(unit) { }

        private class DrainingProjectileLogic : IMapProjectileLogic
        {
            MapProjectile Projectile;
            MapUnit Unit;
            float Height;
            float X;
            float Y;
            float Z;
            float Frac = 0;

            public DrainingProjectileLogic(float height, float x, float y, float z, MapUnit unit)
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
                float cX = Unit.X + Unit.FracX + Unit.Width / 2f;
                float cY = Unit.Y + Unit.FracY + Unit.Height / 2f;
                float zatUnit = MapLogic.Instance.GetHeightAt(Unit.X + Unit.FracX + (float)Unit.Width / 2,
                                                              Unit.Y + Unit.FracY + (float)Unit.Height / 2,
                                                              Unit.Width, Unit.Height) / 32;
                Frac += 3f / MapLogic.TICRATE;
                float animFrac = (1f - Frac);
                Projectile.SetPosition(cX+X, cY+Y, zatUnit + Z + animFrac * Height);
                Projectile.CurrentFrame = Math.Min(8, (int)(Frac * 9));
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
                float rY = Unit.Height / 3f;

                float randRad = UnityEngine.Random.Range(0.0f, 1.0f) * Mathf.PI * 2;

                float pX = -(Mathf.Cos(randRad) * rX);
                float pY = -(Mathf.Sin(randRad) * rY);

                float cZ = -(meanWh / 3f);

                MapProjectile item = new MapProjectile(AllodsProjectile.Drain, Unit, new DrainingProjectileLogic((Unit.Width+Unit.Height)/2f, pX, pY, cZ, Unit));
                item.SetPosition(pX, pY, 0);
                item.ZOffset = -64;
                item.ZAbsolute = true;
                MapLogic.Instance.AddObject(item, true);
            }
        }
    }
}
