using System;
using Mathf = UnityEngine.Mathf;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;


public class ShopStructure : StructureLogic
{
    public class Shelf
    {
        public ItemPack Items { get; private set; }

        // this contains settings for allowed items and amounts
        private List<ItemClass> ItemClasses;
        private List<ItemClass> SpecialItemClasses;

        private bool AllowCommon;
        private bool AllowMagic;

        private bool AllowSpecial;

        private long PriceMin;
        private long PriceMax;

        private uint MaxItems;
        private uint MaxSameType;

        private bool CheckArmorShapeAllowed(Templates.TplArmor armor, AllodsMap.AlmShop.AlmShopShelf rules)
        {
            for (int i = 0; i < 7; i++)
            {
                uint classFlag = 1u << (i + 15);
                for (int j = 0; j < 15; j++)
                {
                    uint materialFlag = 1u << j;
                    // armor itself is not allowed
                    if ((armor.ClassesAllowed[i] & materialFlag) == 0)
                        continue;
                    // not allowed in shop
                    if (((uint)rules.ItemMaterials & materialFlag) == materialFlag &&
                        ((uint)rules.ItemClasses & classFlag) == classFlag) return true;
                }
            }
            return false;
        }

        public Shelf(AllodsMap.AlmShop.AlmShopShelf rules)
        {

            // basic props
            PriceMin = rules.PriceMin;
            PriceMax = rules.PriceMax;
            MaxItems = rules.MaxItems;
            MaxSameType = rules.MaxSameItems;

            // get list of classes supported for the item.
            var Materials = new List<Templates.TplMaterial>();
            var Classes = new List<Templates.TplClass>();
            var Types = new List<Templates.TplArmor>();

            //
            ItemClasses = new List<ItemClass>();
            SpecialItemClasses = new List<ItemClass>();

            //
            AllowMagic = rules.ItemExtras.HasFlag(AllodsMap.AlmShop.AlmShopItemExtra.Magic);
            AllowCommon = AllowMagic ? rules.ItemExtras.HasFlag(AllodsMap.AlmShop.AlmShopItemExtra.Common) : true;

            AllowSpecial = rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.Other);

            // this currently abuses the fact that there is a hardcoded list of materials.
            // will need to be changed if we extend it.
            for (int i = 0; i < 15; i++)
            {
                uint flag = 1u << i;
                uint value = (uint) rules.ItemMaterials;
                if ((flag & value) != 0)
                    Materials.Add(TemplateLoader.GetMaterialById(i));
            }

            // same goes for classes.
            for (int i = 0; i < 7; i++)
            {
                uint flag = 1u << i;
                uint value = ((uint) rules.ItemClasses) >> 15;
                if ((flag & value) != 0)
                    Classes.Add(TemplateLoader.GetClassById(i));
            }

            // types are a bit more complicated, because there is no direct mapping for this.
            // we use "slot"
            // note that there is also separation between mage and warrior armor here.

            // add weapons
            if (rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.Weapon))
            {
                foreach (Templates.TplArmor weapon in TemplateLoader.Templates.Weapons)
                {
                    if (!CheckArmorShapeAllowed(weapon, rules))
                        continue;
                    if (weapon.SuitableFor == 1)
                        Types.Add(weapon);
                }
            }
            
            if (rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.Wands))
            {
                foreach (Templates.TplArmor weapon in TemplateLoader.Templates.Weapons)
                {
                    if (!CheckArmorShapeAllowed(weapon, rules))
                        continue;
                    if (weapon.SuitableFor == 2)
                        Types.Add(weapon);
                }
            }

            // add armor
            if (rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.Armor))
            {
                foreach (Templates.TplArmor armor in TemplateLoader.Templates.Armor)
                {
                    if (!CheckArmorShapeAllowed(armor, rules))
                        continue;
                    if (armor.SuitableFor == 1)
                        Types.Add(armor);
                }
            }

            if (rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.ArmorMage))
            {
                foreach (Templates.TplArmor armor in TemplateLoader.Templates.Armor)
                {
                    if (!CheckArmorShapeAllowed(armor, rules))
                        continue;
                    if (armor.SuitableFor == 2)
                        Types.Add(armor);
                }
            }

            // add armor suitable for everyone (e.g. rings)
            if (rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.Armor) || rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.ArmorMage))
            {
                foreach (Templates.TplArmor armor in TemplateLoader.Templates.Armor)
                {
                    if (!CheckArmorShapeAllowed(armor, rules))
                        continue;
                    if (armor.SuitableFor == 3)
                        Types.Add(armor);
                }
            }

            // add shields
            if (rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.Shield))
            {
                foreach (Templates.TplArmor shield in TemplateLoader.Templates.Shields)
                {
                    if (!CheckArmorShapeAllowed(shield, rules))
                        continue;
                    Types.Add(shield);
                }
            }

            // add special (other)
            // copy the list (we may want to skip some items later)
            if (rules.ItemTypes.HasFlag(AllodsMap.AlmShop.AlmShopItemType.Other))
            {
                // generate id for scroll.
                for (ushort i = 6; i <= 0x3F; i++)
                {
                    ushort itemId = (ushort) (0x0E00 | i);
                    ItemClass cls = ItemClassLoader.GetItemClassById(itemId);
                    if (cls == null)
                        continue;
                    if (cls.Price < PriceMin || cls.Price > PriceMax)
                        continue;
                    SpecialItemClasses.Add(cls);
                }
            }

            // now that we have all the allowed combinations, let's populate items!
            // note that not all possible IDs are valid -- we need to check this
            foreach (Templates.TplArmor armor in Types)
            {
                // class id
                for (int i = 0; i < 7; i++)
                {
                    // material id
                    for (int j = 0; j < 15; j++)
                    {
                        // check if allowed
                        if ((armor.ClassesAllowed[i] & (1 << j)) == 0)
                            continue;
                        ushort itemId = (ushort)((i << 5) | (j << 12) | (armor.Slot << 8) | (armor.Index));
                        ItemClass cls = ItemClassLoader.GetItemClassById(itemId);
                        if (cls == null)
                            continue;
                        if (cls.Price > PriceMax || (!AllowMagic && cls.Price < PriceMin))
                            continue;
                        ItemClasses.Add(cls);
                    }
                }
            }

            Items = new ItemPack();
            GenerateItems();

        }

        private ItemEffect GenerateEffect(Item item)
        {
            Random rnd = new Random();
            float itemPriceMax = 2 * PriceMax - item.Class.Price;
            float manaMax = item.Class.Material.MagicVolume * item.Class.Class.MagicVolume - item.ManaUsage;
            // note: this will not work correctly if SlotsWarrior or SlotsMage values for a slot are not sorted by ascending order.
            //       parameters that appear first will "override" parameters with the same weight appearing later.
            int lastValue;
            Templates.TplModifier lastModifier = TemplateLoader.Templates.Modifiers[TemplateLoader.Templates.Modifiers.Count - 1];
            if ((item.Class.Option.SuitableFor & 1) != 0)
                lastValue = lastModifier.SlotsFighter[item.Class.Option.Slot - 1];
            else lastValue = lastModifier.SlotsMage[item.Class.Option.Slot - 1];
            int randomValue = rnd.Next(0, lastValue) + 1;
            Templates.TplModifier matchingModifier = null;
            if (item.Class.Option.SuitableFor == 2 && item.Class.Option.Slot == 1)
            {
                matchingModifier = TemplateLoader.Templates.Modifiers[(int) ItemEffect.Effects.CastSpell];
            }
            else
            {
                for (int i = 1; i < TemplateLoader.Templates.Modifiers.Count; i++)
                {
                    Templates.TplModifier prevModifier = TemplateLoader.Templates.Modifiers[i - 1];
                    Templates.TplModifier modifier = TemplateLoader.Templates.Modifiers[i];
                    int prevValue;
                    int value;
                    if ((item.Class.Option.SuitableFor & 1) != 0)
                    {
                        prevValue = prevModifier.SlotsFighter[item.Class.Option.Slot - 1];
                        value = modifier.SlotsFighter[item.Class.Option.Slot - 1];
                    }
                    else
                    {
                        prevValue = prevModifier.SlotsMage[item.Class.Option.Slot - 1];
                        value = modifier.SlotsMage[item.Class.Option.Slot - 1];
                    }
                    if (prevValue < randomValue && randomValue <= value)
                    {
                        matchingModifier = modifier;
                        break;
                    }
                }
            }
            if (matchingModifier == null)
            {
                // parameter not found. weird, but happens.
                return null;
            }
            if ((matchingModifier.UsableBy & item.Class.Option.SuitableFor) == 0)
            {
                // parameter for class not found in the item.
                return null;
            }
            // parameter found. calculate max possible power
            ItemEffect effect = new ItemEffect();
            effect.Type1 = (ItemEffect.Effects) matchingModifier.Index;
            float maxPower;
            float absoluteMax = manaMax / matchingModifier.ManaCost;
            if (matchingModifier.Index == (int) ItemEffect.Effects.CastSpell)
            {
                // select spell to cast.
                // if for fighter, choose between fighter-specific spells.
                // if for mage, choose between mage-specific spells.
                Spell.Spells[] spells;
                if ((item.Class.Option.SuitableFor & 1) != 0)
                    spells = new Spell.Spells[] { Spell.Spells.Stone_Curse, Spell.Spells.Drain_Life };
                else spells = new Spell.Spells[] { Spell.Spells.Fire_Arrow, Spell.Spells.Lightning, Spell.Spells.Prismatic_Spray, Spell.Spells.Stone_Curse, Spell.Spells.Drain_Life, Spell.Spells.Ice_Missile, Spell.Spells.Diamond_Dust };
                // choose random spell
                Spell.Spells spell = spells[rnd.Next(0, spells.Length)];
                effect.Value1 = (int) spell;
                // calculate max power
                Templates.TplSpell spellTemplate = TemplateLoader.Templates.Spells[effect.Value1];
                maxPower = Mathf.Log(itemPriceMax / (spellTemplate.ScrollCost * 10f)) / Mathf.Log(2);
                if (!float.IsNaN(maxPower) && maxPower > 0)
                    maxPower = (Mathf.Pow(1.2f, maxPower) - 1) * 30;
                else return null;
                maxPower = Mathf.Min(maxPower, absoluteMax);
                maxPower = Mathf.Min(maxPower, 100);
            }
            else
            {
                maxPower = Mathf.Log(itemPriceMax / (manaMax * 50) - 1) / Mathf.Log(1.5f) * 70f / matchingModifier.ManaCost;
                if (float.IsNaN(maxPower)) return null;
                maxPower = Mathf.Min(maxPower, absoluteMax);
                maxPower = Mathf.Min(maxPower, matchingModifier.AffectMax);
                if (maxPower < matchingModifier.AffectMin) return null;
            }

            if (maxPower <= 1)
            {
                // either some limit hit, or something else
                return null;
            }

            // max parameter power found. randomize values
            switch (effect.Type1)
            {
                case ItemEffect.Effects.CastSpell:
                    effect.Value2 = rnd.Next(1, (int)maxPower + 1);
                    break;

                case ItemEffect.Effects.DamageFire:
                case ItemEffect.Effects.DamageWater:
                case ItemEffect.Effects.DamageAir:
                case ItemEffect.Effects.DamageEarth:
                case ItemEffect.Effects.DamageAstral:
                    effect.Value1 = rnd.Next(1, (int)maxPower + 1);
                    effect.Value2 = rnd.Next(1, (int)(maxPower / 2) + 1);
                    break;

                default:
                    effect.Value1 = (int) Mathf.Max(matchingModifier.AffectMin, rnd.Next(1, (int)maxPower + 1));
                    break;
            }
           
            return effect;
        }

        private void GenerateEffects(Item item)
        {
            Random rnd = new Random();
            ItemEffect effect;
            effect = GenerateEffect(item);
            if (effect != null)
            {
                item.MagicEffects.Add(effect);
                item.UpdateItem();
                if (effect.Type1 == ItemEffect.Effects.CastSpell)
                    return;
            }
            if (rnd.Next(0, 101) >= 50)
            {
                // chance of second effect = 50%
                return;
            }
            effect = GenerateEffect(item);
            if (effect != null)
            {
                if (effect.Type1 == ItemEffect.Effects.CastSpell)
                    return;
                item.MagicEffects.Add(effect);
                item.UpdateItem();
            }
            if (rnd.Next(0, 101) >= 25)
            {
                // chance of third effect = 12.5%
                return;
            }
            effect = GenerateEffect(item);
            if (effect != null)
            {
                if (effect.Type1 == ItemEffect.Effects.CastSpell)
                    return;
                item.MagicEffects.Add(effect);
                item.UpdateItem();
            }
        }

        public void GenerateItems()
        {
            Random rnd = new Random();
            Items.Clear();

            // generate special items if any
            if (AllowSpecial)
            {
                foreach (ItemClass specialItem in SpecialItemClasses)
                {
                    // if it's a book, it can be either added or not added (cannot have count).
                    // this is so in classic game, let's have it like this here for now as well.
                    if (specialItem.IsScroll)
                    {
                        // scroll
                        // 75% chance
                        if (rnd.Next(0, 101) < 50)
                        {
                            int count = rnd.Next(0, (int)MaxSameType + 1);
                            if (count > 0)
                            {
                                Item item = new Item(specialItem.ItemID);
                                item.Count = count;
                                Items.PutItem(Items.Count, item);
                            }
                        }
                    }
                }

                // randomly generate spell books in the price range
                // note: ROM2 uses different logic (supposedly hardcoded list of "allowed" spells). to research / fix
                foreach (var spellTemplate in TemplateLoader.Templates.Spells)
                {
                    if (spellTemplate.Name == "")
                        continue;
                    if (spellTemplate.BookCost >= PriceMin && spellTemplate.BookCost <= PriceMax && rnd.Next(0, 101) < 50)
                    {
                        List<ItemEffect> effects = new List<ItemEffect>();
                        ItemEffect teachSpell = new ItemEffect();
                        teachSpell.Type1 = ItemEffect.Effects.TeachSpell;
                        teachSpell.Value1 = (int)spellTemplate.Index;
                        effects.Add(teachSpell);
                        Item item = new Item((ushort)(0x0E00 | spellTemplate.Sphere), effects);
                        item.Count = 1;
                        Items.PutItem(Items.Count, item);
                    }
                }
                // generate random amounts of potions
                Item potion;
                potion = new Item("Potion Health Regeneration");
                potion.Count = rnd.Next(0, 51) + 50;
                Items.PutItem(Items.Count, potion);
                potion = new Item("Potion Medium Healing");
                potion.Count = rnd.Next(0, 51) + 50;
                Items.PutItem(Items.Count, potion);
                potion = new Item("Potion Big Healing");
                potion.Count = rnd.Next(0, 51) + 50;
                Items.PutItem(Items.Count, potion);
                potion = new Item("Potion Mana Regeneration");
                potion.Count = rnd.Next(0, 51) + 50;
                Items.PutItem(Items.Count, potion);
                potion = new Item("Potion Medium Mana");
                potion.Count = rnd.Next(0, 51) + 50;
                Items.PutItem(Items.Count, potion);
                potion = new Item("Potion Big Mana");
                potion.Count = rnd.Next(0, 51) + 50;
                Items.PutItem(Items.Count, potion);
            }

            if (ItemClasses.Count > 0)
            {
                // generate armor items if any
                // now we already know how many special items we generated in the previous step.
                // we can generate N items, based on max items.
                // "same type" is currently ignored.
                int countArmorItems = (int)MaxItems - Items.Count;
                int discardedItems = 0;
                for (int i = 0; i < countArmorItems; i++)
                {
                    if (discardedItems >= 10 * MaxItems)
                        break;
                    // get any random item from the list of allowed item classes
                    ItemClass cls = ItemClasses[rnd.Next(0, ItemClasses.Count)];
                    // look at chance of it being magical.
                    int magicalChance = 0;
                    if (AllowMagic && !AllowCommon)
                        magicalChance = 100;
                    else if (AllowMagic)
                        magicalChance = 50;
                    //
                    Item item = new Item(cls.ItemID);
                    if (rnd.Next(0, 101) < magicalChance)
                    {
                        GenerateEffects(item);
                    }
                    else
                    {
                        item.Count += rnd.Next(0, (int)MaxSameType + 1);
                    }
                    if (item.Price < PriceMin || item.Price > PriceMax)
                    {
                        discardedItems++;
                        continue;
                    }
                    Items.PutItem(Items.Count, item);
                }
            }
        }
    }

    public Shelf[] Shelves = new Shelf[4];

    public ShopStructure(MapStructure s, AllodsMap.AlmShop rules) : base(s)
    {
        for (int i = 0; i < 4; i++)
        {
            Shelves[i] = new Shelf(rules.Shelves[i]);
            Debug.LogFormat("Generated Shelf [{1}] in shop ID={2}:\n{0}", Shelves[i].Items.ToString(), i+1, s.Tag);
        }
    }

    public override bool OnEnter(MapUnit unit)
    {
        if (!base.OnEnter(unit))
            return false;
        Server.NotifyEnterShop(unit, Structure);
        return true;
    }

    public override void OnLeave(MapUnit unit)
    {
        base.OnLeave(unit);
        Server.NotifyLeaveStructure(unit.Player);
    }

    private void GenerateItems()
    {

    }
}