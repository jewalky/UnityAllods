using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProtoBuf;

// this enum duplicates MapLogicStats.
// it does NOT need to be synced when you edit MapLogicStats,
// but it should be synced with locale/stats.txt or item stats won't be displayed correctly.
[ProtoContract]
public class ItemEffect
{
    public enum Effects
    {
        NoneStat = 0, // dummy value
        Price,
        Body,
        Mind,
        Reaction,
        Spirit,
        Health,
        HealthMax,
        HealthRegeneration,
        Mana,
        ManaMax,
        ManaRegeneration,
        ToHit,
        DamageMin,
        DamageMax,
        Defence, // should use this name because it's used in data.bin
        Absorbtion,
        Speed,
        RotationSpeed,
        ScanRange,
        Protection0, // dummy value
        ProtectionFire,
        ProtectionWater,
        ProtectionAir,
        ProtectionEarth,
        ProtectionAstral,
        FighterSkill0, // dummy value
        SkillBlade,
        SkillAxe,
        SkillBludgeon,
        SkillPike,
        SkillShooting,
        MageSkill0, // dummy value
        SkillFire,
        SkillWater,
        SkillAir,
        SkillEarth,
        SkillAstral,
        ItemLore, // apparently distance?
        MagicLore, // dummy value
        CreatureLore, // dummy value
        CastSpell,
        TeachSpell,
        Damage, // dummy value
        DamageFire,
        DamageWater,
        DamageAir,
        DamageEarth,
        DamageAstral,
        DamageBonus,
        // monster stats only
        ProtectionBlade,
        ProtectionAxe,
        ProtectionBludgeon,
        ProtectionPike,
        ProtectionShooting
    }

    [ProtoMember(1)]
    public Effects Type1 = Effects.NoneStat;
    [ProtoMember(2)]
    public int Value1 = 0;

    // only in castSpell
    [ProtoMember(3)]
    public Effects Type2 = Effects.NoneStat;
    [ProtoMember(4)]
    public int Value2 = 0;

    public ItemEffect()
    {
        // wat
    }

    public ItemEffect(Effects effect, int value)
    {
        Type1 = effect;
        Value1 = value;
    }

    public ItemEffect(string effect)
    {
        string[] efs = effect.Split('='); // yes you can write castSpell=Stone_Curse=30 lol.
        if (efs.Length != 2)
            return;

        try
        {
            Type1 = (Effects)Enum.Parse(typeof(Effects), efs[0], true);
        }
        catch (Exception)
        {
            return;
        }

        if (Type1 == Effects.CastSpell || Type1 == Effects.TeachSpell)
        {
            string[] efs2 = efs[1].Split(':');
            if (efs2.Length != 2)
            {
                Type1 = Effects.NoneStat;
                return;
            }

            try
            {
                int spell = (int)Enum.Parse(typeof(Spell.Spells), efs2[0], true);
                Value1 = spell;
            }
            catch (Exception)
            {
                Type1 = Effects.NoneStat;
                return;
            }

            if (Type1 == Effects.CastSpell)
            {
                Type2 = Effects.MageSkill0;
                try
                {
                    Value2 = int.Parse(efs2[1]);
                }
                catch (Exception)
                {
                    Type1 = Effects.NoneStat;
                    Value1 = 0;
                    Type2 = Effects.NoneStat;
                    Value2 = 0;
                }
            }
        }
        else
        {
            try
            {
                Value1 = int.Parse(efs[1]);
            }
            catch (Exception)
            {
                Type1 = Effects.NoneStat;
                Value1 = 0;
            }
        }
    }

    /*MapLogicItemEffect eff = new MapLogicItemEffect("castSpell=Stone_Curse:255");
    Debug.Log(eff.ToString());*/ // says "CastSpell=Stone_Curse:255"
    public override string ToString()
    {
        string effect = Type1.ToString();
        string value;
        if (Type1 == Effects.TeachSpell || Type1 == Effects.CastSpell)
        {
            value = ((Spell.Spells)Value1).ToString();
            if (Type1 == Effects.CastSpell)
                value += string.Format(":{0}", Value2);
        }
        else value = Value1.ToString();
        return string.Format("{0}={1}", effect, value);
    }

    public static List<ItemEffect> ParseEffectList(string spec_args)
    {
        List<ItemEffect> ov = new List<ItemEffect>();
        string[] spec_args_split = spec_args.Split(',');
        for (int i = 0; i < spec_args_split.Length; i++)
        {
            string spec_arg = spec_args_split[i].Trim();
            if (spec_arg.Length <= 0)
                continue;
            ov.Add(new ItemEffect(spec_args_split[i]));
        }

        return ov;
    }

    // for sequenceequal in itempack
    public override bool Equals(object obj)
    {
        if (obj.GetType() != typeof(ItemEffect))
            return false;

        ItemEffect o = (ItemEffect)obj;
        return (o.Type1 == Type1 &&
                o.Type2 == Type2 &&
                o.Value1 == Value1 &&
                o.Value2 == Value2);
    }

    public override int GetHashCode()
    {
        uint u = 0;
        u |= (byte)Type1;
        u <<= 8;
        u |= (byte)Type2;
        u <<= 8;
        u |= (byte)Value1;
        u <<= 8;
        u |= (byte)Value2;
        return (int)u;
    }
}

[ProtoContract]
public class NetItem
{
    [ProtoMember(1)]
    public ushort ClassID;
    [ProtoMember(2)]
    public List<ItemEffect> MagicEffects = new List<ItemEffect>();
    [ProtoMember(3)]
    public uint UID;
    [ProtoMember(4)]
    public List<uint> ParentUIDs = new List<uint>();
    [ProtoMember(5)]
    public int Count;

    public NetItem()
    {

    }

    public NetItem(Item item)
    {
        ClassID = item.Class.ItemID;
        for (int i = 0; i < item.MagicEffects.Count; i++)
            MagicEffects.Add(item.MagicEffects[i]);
        UID = item.UID;
        for (int i = 0; i < item.ParentUIDs.Count; i++)
            ParentUIDs.Add(item.ParentUIDs[i]);
        Count = item.Count;
    }
}

public class Item
{
    // stats are already in Templates.
    // power for castSpell is stored in paired mageSkill0.
    // so basically castSpell=Stone_Curse:10 translates into castSpell=21;mageSkill0=10
    // just in case, I have an local enum that translates to stat number.
    private static uint LocalTopUID = 0xC0000000;
    public readonly uint UID;
    public readonly List<uint> ParentUIDs = new List<uint>();

    public ItemPack Parent = null;
    public int Index = 0;

    public bool IsValid { get { return (Class != null); } }

    public ItemClass Class = null;
    public long Price = 2;
    public int Count = 1;
    public List<ItemEffect> Effects = new List<ItemEffect>();
    public List<ItemEffect> NativeEffects = new List<ItemEffect>();
    public List<ItemEffect> MagicEffects = new List<ItemEffect>();

    public Item(NetItem netitem)
    {
        UID = netitem.UID;
        ParentUIDs = netitem.ParentUIDs;

        Class = ItemClassLoader.GetItemClassById(netitem.ClassID);
        if (Class == null)
        {
            Debug.LogFormat("Invalid item created (id={0})", netitem.ClassID);
            return;
        }

        Count = netitem.Count;

        InitItem();

        MagicEffects = netitem.MagicEffects;

        UpdateItem();
    }

    public Item(Item original, int count)
    {
        for (int i = 0; i < original.ParentUIDs.Count; i++)
            ParentUIDs.Add(original.ParentUIDs[i]);

        // generate UID (or duplicate old)
        if (count >= original.Count)
        {
            UID = original.UID; // moved old item
        }
        else
        {
            UID = LocalTopUID++; // new item
            ParentUIDs.Add(original.UID);
        }

        Class = original.Class;
        Price = original.Price;
        Count = Math.Max(original.Count, count);

        Parent = original.Parent;
        Index = original.Index;

        InitItem();
        MagicEffects.AddRange(original.MagicEffects);
        UpdateItem();
    }

    public Item(ushort id, List<ItemEffect> effects = null)
    {
        UID = LocalTopUID++;

        Class = ItemClassLoader.GetItemClassById(id);
        if (Class == null)
        {
            Debug.LogFormat("Invalid item created (id={0})", id);
            return;
        }

        InitItem();

        if (effects != null)
            MagicEffects = effects;

        UpdateItem();
    }

    public Item(string specifier)
    {
        UID = LocalTopUID++;

        int spec_argStart = specifier.IndexOf('{');
        int spec_argEnd = (spec_argStart >= 0) ? specifier.IndexOf('}', spec_argStart + 1) : 1;
        string spec_args = "";
        if (spec_argStart >= 0 && spec_argEnd >= 0)
            spec_args = specifier.Substring(spec_argStart + 1, spec_argEnd - spec_argStart - 1);
        if (spec_argStart >= 0)
            specifier = specifier.Substring(0, spec_argStart).Trim();
        Class = ItemClassLoader.GetItemClassBySpecifier(specifier);
        if (Class == null)
        {
            Debug.LogFormat("Invalid item created (specifier={0})", specifier);
            return;
        }

        InitItem();

        // now go through effects
        MagicEffects.AddRange(ItemEffect.ParseEffectList(spec_args));

        UpdateItem();
    }

    private void InitItem()
    {
        if (Class.IsSpecial)
        {
            // do nothing.
            // class has only option, no material or class.
        }
        else if (Class.IsMagic)
        {
            // generate effect list for native effects.
            Templates.TplMagicItem magicItem = TemplateLoader.GetMagicItemById(Class.MagicID);
            if (magicItem != null)
            {
                NativeEffects.Add(new ItemEffect(ItemEffect.Effects.Price, magicItem.Price));
                NativeEffects.AddRange(ItemEffect.ParseEffectList(magicItem.Effects));
            }
        }
        else
        {
            bool hasDamageMinMax = false;
            bool hasToHit = false;
            bool hasDefense = false;
            bool hasAbsorbtion = false;
            for (int i = 0; i < Class.Effects.Count; i++)
            {
                ItemEffect eff = new ItemEffect();
                ItemEffect sourceEff = Class.Effects[i];
                int newValue = sourceEff.Value1;
                if (sourceEff.Type1 == ItemEffect.Effects.Damage ||
                    sourceEff.Type1 == ItemEffect.Effects.DamageMin ||
                    sourceEff.Type1 == ItemEffect.Effects.DamageMax ||
                    sourceEff.Type1 == ItemEffect.Effects.DamageBonus)
                {
                    hasDamageMinMax = true;
                }
                else if (sourceEff.Type1 == ItemEffect.Effects.ToHit)
                {
                    hasToHit = true;
                }
                else if (sourceEff.Type1 == ItemEffect.Effects.Defence)
                {
                    hasDefense = true;
                }
                else if (sourceEff.Type1 == ItemEffect.Effects.Absorbtion)
                {
                    hasAbsorbtion = true;
                }

                eff.Type1 = sourceEff.Type1;
                eff.Value1 = newValue;
                eff.Type2 = sourceEff.Type2;
                eff.Value2 = sourceEff.Value2;
                NativeEffects.Add(eff);
            }

            if (!hasDamageMinMax)
            {
                int damageMin = (int)(Class.Option.PhysicalMin * Class.Material.Damage * Class.Class.Damage);
                int damageMax = (int)(Class.Option.PhysicalMax * Class.Material.Damage * Class.Class.Damage);
                if (damageMin != 0 || damageMax != 0)
                {
                    NativeEffects.Add(new ItemEffect(ItemEffect.Effects.DamageMin, damageMin));
                    NativeEffects.Add(new ItemEffect(ItemEffect.Effects.DamageMax, damageMax));
                }
            }

            if (!hasToHit)
            {
                int toHit = (int)(Class.Option.ToHit * Class.Material.ToHit * Class.Class.ToHit);
                if (toHit != 0)
                    NativeEffects.Add(new ItemEffect(ItemEffect.Effects.ToHit, toHit));
            }

            if (!hasDefense)
            {
                int defense = (int)(Class.Option.Defense * Class.Material.Defense * Class.Class.Defense);
                if (defense != 0)
                    NativeEffects.Add(new ItemEffect(ItemEffect.Effects.Defence, defense));
            }

            if (!hasAbsorbtion)
            {
                int absorbtion = (int)(Class.Option.Absorbtion * Class.Material.Absorbtion * Class.Class.Absorbtion);
                if (absorbtion != 0)
                    NativeEffects.Add(new ItemEffect(ItemEffect.Effects.Absorbtion, absorbtion));
            }
        }

        UpdateItem();
    }

    // calculate price. calculate effects based on native effects + magic effects.
    public void UpdateItem()
    {
        // concat native and magic effects
        Effects.Clear();
        Effects.AddRange(NativeEffects);
        Effects.AddRange(MagicEffects);

        // base price.
        Price = Class.Price;
        if (!Class.IsSpecial)
        {
            Price = (int)(Price * Class.Class.Price);
            Price = (int)(Price * Class.Material.Price);
        }

        int manaUsage = 0;
        for (int i = 0; i < Effects.Count; i++)
        {
            Templates.TplModifier modifier = TemplateLoader.GetModifierById((int)Effects[i].Type1);
            if (modifier == null)
                continue; // wtf was that lol.
            manaUsage += modifier.ManaCost;
        }

        Price = Math.Max(Price, (int)(Price * ((float)manaUsage / 10)));

        // search for override price effect
        for (int i = 0; i < Effects.Count; i++)
        {
            if (Effects[i].Type1 == ItemEffect.Effects.Price)
                Price = Effects[i].Value1;
        }

        if (Price < 2)
            Price = 2; // min price
    }

    public string ToStringWithEffects(bool ownEffects)
    {
        if (Class == null)
            return "<INVALID>";

        StringBuilder sb = new StringBuilder();
        sb.Append(Class.ServerName);
        List<ItemEffect> effts = (ownEffects ? Effects : MagicEffects);
        if (effts.Count > 0)
        {
            sb.Append(" {");
            for (int i = 0; i < effts.Count; i++)
            {
                sb.Append(effts[i].ToString());
                if (i < effts.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("}");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        return ToStringWithEffects(false);
    }

    public string ToVisualString()
    {
        return Class.VisualName;
    }
}