using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

public struct ItemFile
{
    public string FileName;
    public Images.AllodsSprite File;

    public ItemFile(string fname)
    {
        FileName = fname;
        File = null;
    }
}

public class ItemClass
{
    public string ServerName; // itemserv.txt
    public string VisualName; // itemname.txt

    public Templates.TplMaterial Material;
    public Templates.TplClass Class;
    public Templates.TplArmor Option;
    public bool IsMagic;
    public int MagicID;

    public ushort ItemID;

    public bool UsableWarrior;
    public bool UsableMage;

    public ItemFile File_BodyFF1; // primary/secondary for female/male fighter/mage
    public ItemFile File_BodyMF1;
    public ItemFile File_BodyFM1;
    public ItemFile File_BodyMM1;
    public ItemFile File_BodyFF2;
    public ItemFile File_BodyMF2;
    public ItemFile File_BodyFM2;
    public ItemFile File_BodyMM2;
    public ItemFile File_Pack; // backpack image

    public long Price;

    public List<ItemEffect> Effects = new List<ItemEffect>();
}

public class ItemClassLoader
{
    internal static bool ClassesLoaded = false;
    public static List<ItemClass> Classes = new List<ItemClass>();

    public static ItemClass GetItemClassById(ushort id)
    {
        foreach (ItemClass cls in Classes)
            if (cls.ItemID == id)
                return cls;
        return null;
    }

    public static ItemClass GetItemClassBySpecifier(string specifier)
    {
        specifier = specifier.Trim().ToLower();
        foreach (ItemClass cls in Classes)
            if (cls.ServerName.ToLower() == specifier)
                return cls;

        // try to guess based on specifier. this is used in world.res
        string[] specSplit = specifier.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string searchFor; // this is reused
        // first off, if last word is "shield", then we're looking for something in Templates.Shields
        int gSlot = -1;
        int gOption = -1;
        if (specSplit[specSplit.Length - 1] == "shield")
        {
            searchFor = "";
            for (int j = 0; j < 2; j++)
            {
                if (specSplit.Length - 2 - j < 0)
                    break;

                searchFor = (specSplit[specSplit.Length - 2 - j] + " " + searchFor).Trim(); // e.g. "small" or "wooden small"
                for (int i = 0; i < TemplateLoader.Templates.Shields.Count; i++)
                {
                    Templates.TplArmor shieldKind = TemplateLoader.Templates.Shields[i];
                    if (shieldKind.Name.ToLower() == searchFor)
                    {
                        gOption = i + 1;
                        gSlot = shieldKind.Slot;
                        break;
                    }
                }

                if (gOption >= 0)
                {
                    // remove the option specifier (leaving only class and material)
                    specSplit = specSplit.Take(specSplit.Length - (j + 2)).ToArray();
                    break;
                }
            }
        }
        // otherwise search in weapons and armor.
        else
        {
            searchFor = "";
            for (int j = 0; j < 2; j++)
            {
                if (specSplit.Length - 1 - j < 0)
                    break;

                searchFor = (specSplit[specSplit.Length - 1 - j] + " " + searchFor).Trim(); // e.g. "pike" or "spiked club"
                for (int i = 0; i < TemplateLoader.Templates.Weapons.Count; i++)
                {
                    Templates.TplArmor weaponKind = TemplateLoader.Templates.Weapons[i];
                    if (weaponKind.Name.ToLower() == searchFor)
                    {
                        gOption = i;
                        gSlot = weaponKind.Slot;
                        break;
                    }
                }

                if (gOption < 0)
                {
                    for (int i = 0; i < TemplateLoader.Templates.Armor.Count; i++)
                    {
                        Templates.TplArmor armorKind = TemplateLoader.Templates.Armor[i];
                        if (armorKind.Name.ToLower() == searchFor)
                        {
                            gOption = i;
                            gSlot = armorKind.Slot;
                            break;
                        }
                    }
                }

                if (gOption >= 0)
                {
                    specSplit = specSplit.Take(specSplit.Length - (j + 1)).ToArray();
                    break;
                }
            }
        }

        // here we either have gOption and gSlot or the item is not found.
        if (gOption < 0 || gSlot < 0)
            return null;

        // now find material
        int gMaterial = -1;
        searchFor = "";
        for (int j = 0; j < 2; j++)
        {
            if (specSplit.Length - 1 - j < 0)
                break;

            searchFor = (specSplit[specSplit.Length - 1 - j] + " " + searchFor).Trim(); // e.g. "crystal"
            for (int i = 0; i < TemplateLoader.Templates.Materials.Count; i++)
            {
                Templates.TplMaterial fmat = TemplateLoader.Templates.Materials[i];
                if (fmat.Name.ToLower() == searchFor)
                {
                    gMaterial = i;
                    break;
                }
            }

            if (gMaterial >= 0)
            {
                specSplit = specSplit.Take(specSplit.Length - (j + 1)).ToArray();
                break;
            }
        }

        if (gMaterial < 0)
            return null;

        int gClass = -1; // common by default
        searchFor = "";
        for (int j = 0; j < 2; j++)
        {
            if (specSplit.Length - 1 - j < 0)
                break;

            searchFor = (specSplit[specSplit.Length - 1 - j] + " " + searchFor).Trim(); // e.g. "very rare"
            for (int i = 0; i < TemplateLoader.Templates.Classes.Count; i++)
            {
                Templates.TplClass fcls = TemplateLoader.Templates.Classes[i];
                if (fcls.Name.ToLower() == searchFor)
                {
                    gClass = i;
                    break;
                }
            }

            if (gClass >= 0)
            {
                specSplit = specSplit.Take(specSplit.Length - (j + 1)).ToArray();
                break;
            }
        }

        if (gClass < 0) gClass = 0; // default is common. the only default here.

        // how try to find required item.
        Templates.TplClass reqClass = TemplateLoader.GetClassById(gClass);
        Templates.TplMaterial reqMaterial = TemplateLoader.GetMaterialById(gMaterial);
        Templates.TplArmor reqOption;
        if (reqClass == null || reqMaterial == null) // this shouldnt happen tbh
            return null;

        // do option remap for shields. nival did NOT write "soft helm" or "soft large shield". instead they just wrote "helm" and "large shield".
        reqOption = TemplateLoader.GetOptionByIdAndSlot(gOption, gSlot); // small/large -> soft small/soft large
        if (reqOption == null)
            return null;

        if (reqMaterial.Name.ToLower().Contains("wood") && !reqOption.IsAllowed(gClass, gMaterial))
            gOption += 2; // small/large -> wooden small/wooden large
        else if (reqMaterial.Name.ToLower().Contains("leather") && !reqOption.IsAllowed(gClass, gMaterial))
            gOption += 1; // small/large -> soft small/soft large

        reqOption = TemplateLoader.GetOptionByIdAndSlot(gOption, gSlot); // small/large -> soft small/soft large
        if (reqOption == null)
            return null;

        for (int i = 0; i < Classes.Count; i++)
        {
            if (Classes[i].Class == reqClass &&
                Classes[i].Material == reqMaterial &&
                Classes[i].Option == reqOption) return Classes[i];
        }

        return null;
    }

    public static void InitClasses()
    {
        if (ClassesLoaded)
            return;
        ClassesLoaded = true;

        MemoryStream ms = ResourceManager.OpenRead("world/data/itemname.pkt");
        if (ms == null)
        {
            Core.Abort("Couldn't load \"world/data/itemname.pkt\"!");
            return;
        }

        ms.Position = 3;
        BinaryReader br = new BinaryReader(ms);
        try
        {
            uint item_count = br.ReadUInt32();
            ms.Position += 2;

            for (uint i = 0; i < item_count; i++)
            {
                ItemClass cls = new ItemClass();
                cls.ItemID = br.ReadUInt16();
                cls.ServerName = Locale.ItemServ[(int)i];
                cls.VisualName = Locale.ItemName[(int)i];
                cls.UsableWarrior = br.ReadByte() != 0;
                cls.UsableMage = br.ReadByte() != 0;
                ms.Position++; // unknown byte
                byte count_mods = br.ReadByte();
                ms.Position += 2; // unknown short
                cls.Price = br.ReadUInt32();
                count_mods--;
                for (byte j = 0; j < count_mods; j++)
                {
                    ItemEffect effect = new ItemEffect();
                    byte r_effect = br.ReadByte();
                    sbyte r_value = br.ReadSByte();
                    effect.Type1 = (ItemEffect.Effects)r_effect;
                    effect.Value1 = r_value;
                    cls.Effects.Add(effect);
                }

                // done reading. init auxiliary fields.
                int materialId = (cls.ItemID & 0xF000) >> 12;
                int slotId = (cls.ItemID & 0x0F00) >> 8;
                int classId = (cls.ItemID & 0x0070) >> 5;
                int optionId = (cls.ItemID & 0x001F);

                if (materialId == 0 && slotId == 14) // 0x0E##
                {
                    cls.IsMagic = true;
                    cls.MagicID = (cls.ItemID & 0xFF) - 1;
                }
                else
                {
                    cls.IsMagic = false;
                    cls.MagicID = -1;
                }

                cls.Material = TemplateLoader.GetMaterialById(materialId);
                cls.Class = TemplateLoader.GetClassById(classId);
                cls.Option = TemplateLoader.GetOptionByIdAndSlot(optionId, slotId);

                string imageNameBase = string.Format("{0:D2}{1:D2}{2}{3:D2}", materialId, slotId, classId, optionId);
                cls.File_Pack = new ItemFile("graphics/inventory/" + imageNameBase + ".16a");

                bool hasSecondary = (optionId == 1 ||
                                     optionId == 20 || optionId == 21 || optionId == 22 ||
                                     optionId == 23 ||
                                     optionId == 24 || optionId == 25 || optionId == 26);

                if (cls.UsableWarrior)
                {
                    cls.File_BodyMF1 = new ItemFile("graphics/equipment/mfighter/primary/" + imageNameBase + ".256");
                    cls.File_BodyFF1 = new ItemFile("graphics/equipment/ffighter/primary/" + imageNameBase + ".256");
                    if (hasSecondary)
                    {
                        cls.File_BodyMF2 = new ItemFile("graphics/equipment/mfighter/secondary/" + imageNameBase + ".256");
                        cls.File_BodyFF2 = new ItemFile("graphics/equipment/ffighter/secondary/" + imageNameBase + ".256");
                    }
                }

                if (cls.UsableMage)
                {
                    cls.File_BodyMM1 = new ItemFile("graphics/equipment/mmage/primary/" + imageNameBase + ".256");
                    cls.File_BodyFM1 = new ItemFile("graphics/equipment/fmage/primary/" + imageNameBase + ".256");
                    if (hasSecondary)
                    {
                        cls.File_BodyMM1 = new ItemFile("graphics/equipment/mmage/secondary/" + imageNameBase + ".256");
                        cls.File_BodyFM1 = new ItemFile("graphics/equipment/fmage/secondary/" + imageNameBase + ".256");
                    }
                }

                Classes.Add(cls);
            }
        }
        finally
        {
            ms.Close();
        }
    }
}